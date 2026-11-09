import numpy as np, wave, struct
SR=22050
rng=np.random.default_rng(3)

def t_(d): return np.arange(int(SR*d))/SR
def env(n, a=0.005, d=None, sustain=1.0, r=0.05, curve=1.0):
    # envolupant: atac, sosteniment i caiguda exponencial
    e=np.ones(n)
    na=max(1,int(a*SR)); e[:na]=np.linspace(0,1,na)
    nr=max(1,int(r*SR))
    if nr<n: e[-nr:]*=np.linspace(1,0,nr)**curve
    return e
def expdec(n, k): return np.exp(-np.arange(n)/SR*k)
def sweep(f0,f1,d,shape='exp'):
    n=int(SR*d)
    f=np.geomspace(max(f0,1),max(f1,1),n) if shape=='exp' else np.linspace(f0,f1,n)
    return np.cumsum(f)/SR
def osc(phase, kind='square', duty=0.5):
    p=phase%1.0
    if kind=='square': return np.where(p<duty,1.0,-1.0)
    if kind=='tri': return 4*np.abs(p-0.5)-1
    if kind=='saw': return 2*p-1
    return np.sin(2*np.pi*p)
def tone(freq,d,kind='square',duty=0.5,vib=0.0,vibf=6):
    t=t_(d); ph=freq*t
    if vib: ph=ph+vib*np.sin(2*np.pi*vibf*t)/ (2*np.pi)
    return osc(ph,kind,duty)
def noise(d): return rng.uniform(-1,1,int(SR*d))
def lowpass(x, cutoff):
    # filtre passabaix d'un pol (prou per a sons de 8 bits)
    a=np.exp(-2*np.pi*cutoff/SR); y=np.zeros_like(x); acc=0.0
    for i in range(len(x)): acc=(1-a)*x[i]+a*acc; y[i]=acc
    return y
def mix(*parts):
    n=max(len(p) for p in parts); out=np.zeros(n)
    for p in parts: out[:len(p)]+=p
    return out
def seq(*parts, gap=0.0):
    out=[]; 
    for p in parts:
        out.append(p)
        if gap: out.append(np.zeros(int(gap*SR)))
    return np.concatenate(out)
def note(n): return 440.0*2**((n-69)/12)   # MIDI -> Hz
def norm(x, peak=0.85):
    m=np.max(np.abs(x)); return x if m==0 else x/m*peak
def save_wav(path,x):
    x=np.clip(x,-1,1); data=(x*32767).astype('<i2').tobytes()
    with wave.open(path,'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data)
