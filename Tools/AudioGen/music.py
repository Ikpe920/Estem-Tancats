import sys,os; sys.path.insert(0,'.')
from synth import *
import numpy as np
OUT=sys.argv[1]; os.makedirs(OUT,exist_ok=True)

QUAL={'maj':[0,4,7],'min':[0,3,7],'7':[0,4,7,10],'m7':[0,3,7,10],'maj7':[0,4,7,11],'dim':[0,3,6],'sus':[0,5,7]}
MAJOR=[0,2,4,5,7,9,11]; MINOR=[0,2,3,5,7,8,10]; DORIAN=[0,2,3,5,7,9,10]

class Track:
    def __init__(s,bpm,bars,swing=0.0):
        s.bpm=bpm; s.bars=bars; s.swing=swing
        s.step=60.0/bpm/4                       # una semicorxera
        s.n=int(round(s.step*16*bars*SR))
        s.buf=np.zeros(s.n)
    def pos(s,step):
        t=step*s.step
        if s.swing and step%2==1: t+=s.swing*s.step   # les semicorxeres senars, una mica tard
        return int(t*SR)
    def add(s,x,step,gain):
        p=s.pos(step)%s.n; x=x*gain; L=len(x)
        end=min(s.n,p+L); s.buf[p:end]+=x[:end-p]
        if p+L>s.n: s.buf[:p+L-s.n]+=x[end-p:]      # el que sobra va al principi: el bucle no talla
def inst(kind,freq,dur,duty=0.5,a=0.004,dec=None,vib=0,lp=None):
    x=tone(freq,dur,kind,duty,vib=vib)
    e=env(len(x),a,r=min(dur*0.5,0.08))
    if dec: e*=expdec(len(x),dec)
    x=x*e
    if lp: x=lowpass(x,lp)
    return x
def kick(): return np.sin(2*np.pi*sweep(140,42,0.16))*expdec(int(SR*0.16),18)
def snare(): return mix(lowpass(noise(0.13),5000)*expdec(int(SR*0.13),22)*0.8, np.sin(2*np.pi*190*t_(0.08))*expdec(int(SR*0.08),30)*0.4)
def hat(open_=False): d=0.12 if open_ else 0.035; x=noise(d); x=x-lowpass(x,6000); return x*expdec(len(x),18 if open_ else 70)

def chord_notes(key,ch):
    root,q=ch; return [key+root+i for i in QUAL[q]]

def compose(name,bpm,key,scale,prog,lead='square',duty=0.5,bars=16,swing=0.0,drums='std',bass='beat',arp=True,
            lead_lp=None,vib=0,seed=1,lead_oct=72,dens=0.55,gains=(0.30,0.10,0.20,1.0)):
    rnd=np.random.default_rng(seed)
    T=Track(bpm,bars,swing); g_bass,g_arp,g_lead,g_dr=gains
    stepdur=T.step
    # ---- frase de melodia (2 compassos) que es repeteix i varia: A A' B A
    def phrase(chs, rseed):
        r=np.random.default_rng(rseed); notes=[]
        rhythm=[1 if (i%4==0) or r.random()<dens*(0.6 if i%2 else 1.0) else 0 for i in range(32)]
        prev=None
        for i in range(32):
            if not rhythm[i]: continue
            ch=chs[i//16]; tones=[n%12 for n in chord_notes(key,ch)]
            if i%4==0 or prev is None:   # temps fort: nota de l'acord
                cand=[n for n in range(lead_oct-5,lead_oct+12) if n%12 in tones]
            else:                         # a contratemps: passos per l'escala
                cand=[n for n in range(lead_oct-5,lead_oct+12) if (n-key)%12 in scale]
            if prev is not None: cand=sorted(cand,key=lambda n:abs(n-prev)+r.random()*3)[:3]
            n=int(cand[r.integers(len(cand))]); prev=n
            length=1
            while i+length<32 and not rhythm[i+length] and length<4: length+=1
            notes.append((i,n,length))
        return notes
    nch=len(prog)
    sections=[0,0,1,0] if bars>=8 else [0]
    for b in range(bars):
        ch=prog[b%nch]; base=b*16
        tones=chord_notes(key,ch)
        # baix
        if bass=='beat':
            for k in range(4):
                n=tones[0]-24 if k%2==0 else tones[0]-12-12+(7 if k==3 else 0)
                T.add(inst('tri',note(n),stepdur*3.2,dec=3),base+k*4,g_bass)
        elif bass=='drive':
            for k in range(8):
                n=tones[0]-24+(12 if k%2 else 0)
                T.add(inst('tri',note(n),stepdur*1.8,dec=4),base+k*2,g_bass)
        elif bass=='walk':
            walk=[tones[0],tones[1%len(tones)],tones[2%len(tones)],tones[0]+(10 if len(tones)>3 else 7)]
            for k in range(4): T.add(inst('tri',note(walk[k]-24),stepdur*3.5,dec=2),base+k*4,g_bass)
        # arpegi de l'acord
        if arp:
            pat=[0,1,2,1] if len(tones)<4 else [0,1,2,3]
            for k in range(16):
                n=tones[pat[k%len(pat)]]+(12 if k%8>=4 else 0)
                T.add(inst('square',note(n),stepdur*0.9,duty=0.125,dec=12),base+k,g_arp)
        # bateria
        for k in range(16):
            if drums=='light':
                if k==0: T.add(kick(),base+k,g_dr*0.5)
                if k==8: T.add(snare(),base+k,g_dr*0.18)
                if k%4==2: T.add(hat(),base+k,g_dr*0.12)
            elif drums=='four':
                if k%4==0: T.add(kick(),base+k,g_dr*0.6)
                if k in (4,12): T.add(snare(),base+k,g_dr*0.3)
                T.add(hat(k%4==2),base+k,g_dr*(0.10 if k%2==0 else 0.06))
            elif drums=='fast':
                if k in (0,6,8,10): T.add(kick(),base+k,g_dr*0.55)
                if k in (4,12): T.add(snare(),base+k,g_dr*0.3)
                T.add(hat(),base+k,g_dr*0.08)
            else:
                if k in (0,8,10): T.add(kick(),base+k,g_dr*0.55)
                if k in (4,12): T.add(snare(),base+k,g_dr*0.28)
                if k%2==0: T.add(hat(k==14),base+k,g_dr*0.09)
        if b%4==3:  # petit redoblament cada 4 compassos
            for k in (13,14,15): T.add(snare(),base+k,g_dr*0.15)
    # melodia
    for b in range(0,bars,2):
        sec=sections[(b//2)%len(sections)]
        chs=[prog[b%nch],prog[(b+1)%nch]]
        notes=phrase(chs, seed*100+sec*7+(1 if (b//2)%4==1 else 0))
        for (i,n,L) in notes:
            x=inst(lead,note(n),stepdur*L*0.95,duty=duty,vib=vib,lp=lead_lp,dec=2.5)
            T.add(x,b*16+i,g_lead)
            T.add(x,b*16+i+3,g_lead*0.28)   # ressò
    y=T.buf
    y=y-np.mean(y)
    save_ogg(name,norm(y,0.85))

def save_ogg(name,y):
    wav=os.path.join(OUT,name+'.wav'); save_wav(wav,y)
    os.system(f'ffmpeg -loglevel error -y -i {wav} -c:a libvorbis -q:a 4 {os.path.join(OUT,name+".ogg")}')
    os.remove(wav)

def jingle(name,notes,bpm,kind='square',duty=0.5,bassn=None,tail=1.2):
    step=60/bpm/4; total=sum(L for _,L in notes)*step+tail
    buf=np.zeros(int(total*SR)); p=0
    for n,L in notes:
        if n is not None:
            x=inst(kind,note(n),step*L*0.95,duty=duty,dec=1.5); s=int(p*SR); buf[s:s+len(x)]+=x*0.5
            if bassn: 
                b=inst('tri',note(n-24),step*L,dec=1.5); buf[s:s+len(b)]+=b*0.35
        p+=step*L
    save_ogg(name,norm(buf,0.8))

# ---- els temes ----
C=60
compose('menu',  96, 53, MAJOR, [(0,'maj7'),(9,'m7'),(2,'m7'),(7,'7')], lead='tri', swing=0.18, drums='light', bass='walk', seed=11, lead_oct=72, dens=0.45)
compose('act0', 118, 60, MAJOR, [(0,'maj'),(7,'maj'),(9,'min'),(5,'maj')], lead='square', duty=0.5, seed=21, dens=0.55)
compose('act1', 104, 62, DORIAN,[(0,'m7'),(5,'7'),(0,'m7'),(7,'m7')], lead='tri', swing=0.22, drums='std', bass='walk', seed=31, dens=0.5)
compose('act2', 146, 57, MINOR, [(0,'min'),(8,'maj'),(3,'maj'),(10,'maj')], lead='square', duty=0.25, drums='fast', bass='drive', seed=41, dens=0.6)
compose('act3', 124, 55, MAJOR, [(0,'maj'),(5,'maj'),(9,'min'),(7,'7')], lead='square', duty=0.5, vib=3, seed=51, dens=0.55)
compose('act4', 128, 52, MINOR, [(0,'min'),(5,'min'),(8,'maj'),(7,'maj')], lead='saw', lead_lp=2200, drums='std', bass='drive', seed=61, dens=0.5)
compose('boss', 152, 48, MINOR, [(0,'min'),(8,'maj'),(10,'maj'),(0,'min')], lead='square', duty=0.25, drums='four', bass='drive', seed=71, dens=0.65, lead_oct=74)
jingle('gameover',[(69,4),(67,4),(64,4),(62,4),(60,6),(None,2),(57,12)],70,'tri',bassn=True,tail=1.5)
jingle('win',[(60,2),(64,2),(67,2),(72,4),(67,2),(72,8),(None,2),(74,2),(76,2),(79,2),(84,12)],130,'square',0.25,bassn=True,tail=1.5)
print('fet')
