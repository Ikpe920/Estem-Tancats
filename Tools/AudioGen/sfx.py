import sys,os; sys.path.insert(0,'.')
from synth import *
OUT=sys.argv[1]; os.makedirs(OUT,exist_ok=True)
S={}
def blip(n,d=0.07,kind='square',duty=0.5):
    x=tone(note(n),d,kind,duty); return x*env(len(x),0.002,r=d*0.6)

# trets i impactes
x=osc(sweep(1100,380,0.09),'square',0.25); S['shoot']=x*env(len(x),0.002,r=0.07)*0.6
x=mix(lowpass(noise(0.07),2500)*expdec(int(SR*0.07),40), np.sin(2*np.pi*sweep(260,90,0.07))*expdec(int(SR*0.07),30)*0.8); S['hit']=x
h=mix(S['hit'], osc(note(91)*t_(0.16),'square',0.5)*expdec(int(SR*0.16),18)*0.45, osc(note(98)*t_(0.16),'square',0.5)*expdec(int(SR*0.16),22)*0.3); S['crit']=h
x=osc(sweep(700,120,0.2),'square',0.5)*env(int(SR*0.2),0.002,r=0.15)*0.55; S['enemy_die']=mix(x, lowpass(noise(0.12),1800)*expdec(int(SR*0.12),25)*0.6)
# el xef
x=tone(note(57),0.12,'square',0.5)*0.6; y=tone(note(52),0.16,'square',0.5)*0.6
S['hurt']=mix(seq(x,y), lowpass(noise(0.1),1200)*0.5)*env(len(seq(x,y)),0.002,r=0.1)
S['death']=seq(*[blip(n,0.16,'square',0.5)*0.7 for n in (67,64,60,55)], tone(note(48),0.5,'tri')*expdec(int(SR*0.5),4))
n=lowpass(noise(0.22),3000); S['dash']=n*np.sin(np.linspace(0,np.pi,len(n)))**1.5*0.8
# premis i menús
S['pickup']=seq(blip(84,0.06,'square',0.25),blip(91,0.1,'square',0.25))*0.7
S['coin']=seq(blip(83,0.06,'square',0.5),blip(88,0.22,'square',0.5)*expdec(int(SR*0.22),10))*0.6
S['coin_big']=seq(*[blip(n,0.07,'square',0.5)*0.6 for n in (79,83,86,91)], blip(95,0.3,'square',0.5)*expdec(int(SR*0.3),6)*0.6)
S['boon']=seq(*[blip(n,0.07,'square',0.25)*0.65 for n in (72,76,79)], blip(84,0.25,'square',0.25)*expdec(int(SR*0.25),8)*0.65)
sp=mix(*[np.pad(blip(96+k*3,0.05,'square',0.125)*0.3,(int(SR*0.04*k),0)) for k in range(6)])
S['set']=mix(seq(*[blip(n,0.08,'square',0.5)*0.55 for n in (72,76,79,84,88)]), np.pad(sp,(int(SR*0.3),0)))
S['wave_clear']=seq(blip(72,0.1)*0.5,blip(76,0.1)*0.5,blip(79,0.35,'square',0.5)*expdec(int(SR*0.35),5)*0.5)
S['wave_perfect']=seq(*[blip(n,0.08)*0.5 for n in (72,76,79,84,79,84)], blip(88,0.45)*expdec(int(SR*0.45),4)*0.5)
S['combo']=osc(sweep(500,1400,0.18),'square',0.25)*env(int(SR*0.18),0.002,r=0.1)*0.45
S['challenge']=seq(*[blip(n,0.09,'square',0.5)*0.55 for n in (79,84,88,91)], mix(blip(96,0.5,'square',0.5)*expdec(int(SR*0.5),4)*0.55, blip(91,0.5,'square',0.25)*expdec(int(SR*0.5),4)*0.3))
S['event']=seq(*[blip(n,0.08,'saw')*0.5 for n in (60,64,67)], blip(72,0.35,'saw')*expdec(int(SR*0.35),5)*0.55)
S['ui_click']=blip(84,0.03,'square',0.5)*0.5
S['ui_back']=blip(72,0.04,'square',0.5)*0.5
# caps, explosions i escenari
b1=lowpass(tone(note(36),0.5,'saw'),900)*expdec(int(SR*0.5),2.5); b2=lowpass(tone(note(35),0.7,'saw'),900)*expdec(int(SR*0.7),2)
kick=np.sin(2*np.pi*sweep(150,40,0.3))*expdec(int(SR*0.3),9)
S['boss_intro']=mix(seq(b1,b2)*0.8, kick, np.pad(kick,(int(SR*0.5),0)))
boom=np.sin(2*np.pi*sweep(120,35,0.45))*expdec(int(SR*0.45),6)
S['explosion']=mix(lowpass(noise(0.45),1500)*expdec(int(SR*0.45),7)*0.9, boom*0.9)
S['table']=mix(np.sin(2*np.pi*sweep(160,70,0.18))*expdec(int(SR*0.18),18), lowpass(noise(0.05),3000)*0.5)
cr=np.concatenate([lowpass(noise(0.05),2200)*expdec(int(SR*0.05),40) for _ in range(4)])
S['box_break']=mix(cr*0.8, np.sin(2*np.pi*sweep(200,90,0.12))*expdec(int(SR*0.12),20)*0.6)
t=t_(0.16); S['bounce']=np.sin(2*np.pi*np.cumsum(500+250*np.sin(2*np.pi*14*t))/SR)*expdec(len(t),14)*0.6
t=t_(0.8); S['fuse']=osc(np.cumsum(np.linspace(900,2200,len(t))+80*np.sin(2*np.pi*30*t))/SR,'square',0.5)*np.linspace(0.15,0.45,len(t))
S['heal']=seq(blip(76,0.07,'tri')*0.7, blip(81,0.07,'tri')*0.7, blip(88,0.18,'tri')*expdec(int(SR*0.18),8)*0.7)
for k,v in S.items(): save_wav(os.path.join(OUT,k+'.wav'), norm(v, 0.8))
print(len(S),'sons')
