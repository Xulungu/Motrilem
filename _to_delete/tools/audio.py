import numpy as np, wave, os, sys
OUT=sys.argv[1]; SR=32000
rng=np.random.default_rng(42)
def t_(d): return np.arange(int(d*SR))/SR
def env(n,a=0.005,r=None,shape=1.0):
    e=np.ones(n); na=max(1,int(a*SR)); e[:na]=np.linspace(0,1,na)
    if r is None: r=n/SR-a
    nr=max(1,min(n-na,int(r*SR))); e[-nr:]*=np.linspace(1,0,nr)**shape
    return e
def expdec(n,tau): return np.exp(-np.arange(n)/SR/tau)
def fft_filter(x,lo=None,hi=None,soft=0.15):
    n=len(x); N=1<<int(np.ceil(np.log2(n+1))); X=np.fft.rfft(x,N); f=np.fft.rfftfreq(N,1/SR); g=np.ones_like(f)
    if hi: g*=1/(1+(f/hi)**4)
    if lo: g*=1/(1+(lo/np.maximum(f,1e-3))**4)
    return np.fft.irfft(X*g,N)[:n]
def noise(n): return rng.uniform(-1,1,n)
def sweep(f0,f1,d,kind='sin',curve=1.0):
    t=t_(d); k=(t/d)**curve; f=f0+(f1-f0)*k; ph=2*np.pi*np.cumsum(f)/SR
    if kind=='sin': return np.sin(ph)
    if kind=='sq': return np.sign(np.sin(ph))*0.6
    if kind=='saw': return 2*((ph/(2*np.pi))%1)-1
def echo(x,delay,fb,taps=5):
    d=int(delay*SR); y=np.concatenate([x,np.zeros(d*taps)])
    for i in range(1,taps+1): y[d*i:d*i+len(x)]+=x*(fb**i)
    return y
def norm(x,peak=0.7):
    m=np.max(np.abs(x)); return x if m==0 else x/m*peak
def save(name,x,peak=0.7):
    x=norm(np.asarray(x,dtype=float),peak); x=np.clip(x,-1,1)
    with wave.open(os.path.join(OUT,name+".wav"),"wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((x*32767).astype(np.int16).tobytes())
    print(name, round(len(x)/SR,2),"s")
def note(n): return 440*2**((n-69)/12)

# ---------- bruitages ----------
# page qui se tourne : souffle filtré + froissement
d=0.42; n=int(d*SR); e=np.sin(np.pi*np.clip(t_(d)/d,0,1))**1.5
sw=fft_filter(noise(n),lo=1200,hi=6000)*e
cr=np.zeros(n); idx=rng.integers(0,n,60); cr[idx]=rng.uniform(-1,1,60); cr=fft_filter(cr,lo=2500,hi=9000)*e*3
save("page", sw+cr*0.6, 0.55)
# livre qui s'ouvre
d=0.6; n=int(d*SR)
th=np.sin(2*np.pi*70*t_(d))*expdec(n,0.08)+fft_filter(noise(n),hi=400)*expdec(n,0.06)
save("livre_ouvre", th+0.4*fft_filter(noise(n),lo=1500,hi=5000)*env(n,0.05,0.4), 0.6)
# voix (blip de dialogue) et voix du livre
d=0.055; n=int(d*SR); b=(np.sign(np.sin(2*np.pi*520*t_(d)))*0.5+np.sin(2*np.pi*1040*t_(d))*0.3)*env(n,0.003,0.04,2)
save("voix", fft_filter(b,hi=3500), 0.45)
d=0.07; n=int(d*SR); b=(np.sin(2*np.pi*330*t_(d))+0.3*np.sin(2*np.pi*660*t_(d)))*env(n,0.006,0.05,2)
save("voix_livre", b+0.15*fft_filter(noise(n),lo=2000,hi=6000)*env(n,0.003,0.06), 0.35)
# saut / atterrissage / pas
d=0.14; n=int(d*SR); save("saut", sweep(260,620,d)*env(n,0.004,0.1,2)+0.2*fft_filter(noise(n),lo=800,hi=3000)*env(n,0.002,0.08), 0.4)
d=0.16; n=int(d*SR); save("atterrissage", np.sin(2*np.pi*sweep(1,1,d).size*0+2*np.pi*90*t_(d))*expdec(n,0.04)+0.7*fft_filter(noise(n),hi=700)*expdec(n,0.03), 0.45)
d=0.07; n=int(d*SR); save("pas", fft_filter(noise(n),lo=150,hi=1200)*expdec(n,0.015), 0.25)
# dégâts joueur
d=0.25; n=int(d*SR); save("degat", sweep(700,180,d,'sq')*env(n,0.002,0.2,1.5)+0.4*fft_filter(noise(n),hi=3000)*expdec(n,0.05), 0.55)
# écrasement d'un ennemi
d=0.22; n=int(d*SR); save("ecrase", sweep(500,90,d,'sin',0.5)*expdec(n,0.07)+0.6*fft_filter(noise(n),lo=300,hi=2500)*expdec(n,0.04), 0.6)
# mort d'un moustique
d=0.5; n=int(d*SR); t=t_(d); f=260*(1-t/d*0.75)*(1+0.05*np.sin(2*np.pi*35*t)); ph=2*np.pi*np.cumsum(f)/SR
save("mort_ennemi", (2*((ph/(2*np.pi))%1)-1)*env(n,0.005,0.45,1.5)*0.6, 0.5)
# bourdonnement d'alerte (moustique qui va piquer)
d=0.6; n=int(d*SR); t=t_(d); f=240+40*t/d+12*np.sin(2*np.pi*28*t); ph=2*np.pi*np.cumsum(f)/SR
bz=(2*((ph/(2*np.pi))%1)-1)*(0.6+0.4*np.sin(2*np.pi*14*t))*env(n,0.05,0.1)
save("bzz", fft_filter(bz,hi=2500), 0.45)
# trompe lancée
d=0.16; n=int(d*SR); save("trompe", fft_filter(noise(n),lo=1500,hi=7000)*env(n,0.005,0.13,2)+0.5*sweep(400,1400,d)*env(n,0.003,0.12,2), 0.5)
# sorts
d=0.45; n=int(d*SR); t=t_(d)
fire=fft_filter(noise(n),lo=100,hi=1800)*env(n,0.02,0.4,1.2)
crack=np.zeros(n); idx=rng.integers(0,n,90); crack[idx]=rng.uniform(-1,1,90); crack=fft_filter(crack,lo=1500)*env(n,0.01,0.4)
save("sort_feu", fire+crack*2+0.3*sweep(120,60,d)*env(n,0.01,0.4), 0.6)
d=0.3; n=int(d*SR); t=t_(d)
zap=np.sign(np.sin(2*np.pi*(90+60*np.sin(2*np.pi*55*t))*t))*0.5*(rng.uniform(0.6,1,n))
save("sort_foudre", (zap+fft_filter(noise(n),lo=2000)*0.8)*expdec(n,0.08), 0.6)
d=0.5; n=int(d*SR); t=t_(d)
ice=sum(np.sin(2*np.pi*f*t+rng.uniform(0,6))*expdec(n,0.25)/(i+1)**0.5 for i,f in enumerate([1760,2217,2637,3520,4186]))
save("sort_glace", ice*env(n,0.01,0.45)+0.4*fft_filter(noise(n),lo=4000)*expdec(n,0.12), 0.5)
d=0.14; n=int(d*SR); save("impact", sweep(900,200,d)*expdec(n,0.03)+0.5*fft_filter(noise(n),hi=4000)*expdec(n,0.02), 0.5)
# cristal qui se brise
d=0.8; n=int(d*SR); t=t_(d)
gl=sum(np.sin(2*np.pi*rng.uniform(2000,6500)*t)*expdec(n,rng.uniform(0.05,0.35))*(t>rng.uniform(0,0.05)) for _ in range(26))
save("cristal", gl+0.6*fft_filter(noise(n),lo=3000)*expdec(n,0.05), 0.55)
# fragment de souvenir (carillon)
d=0.9; n=int(d*SR); t=t_(d)
ch=(np.sin(2*np.pi*1568*t)+0.5*np.sin(2*np.pi*3136*t)+0.25*np.sin(2*np.pi*4704*t+1))*expdec(n,0.22)*env(n,0.004)
save("fragment", echo(ch,0.11,0.35,3), 0.4)
# explosion du livre
d=2.2; n=int(d*SR); t=t_(d)
boom=fft_filter(noise(n),hi=500)*expdec(n,0.5)+np.sin(2*np.pi*np.cumsum(55*(1-0.5*t/d))/SR)*expdec(n,0.6)*0.8
whoosh=fft_filter(noise(n),lo=800,hi=6000)*expdec(n,0.35)*0.6
shim=sum(np.sin(2*np.pi*f*t)*expdec(n,0.9) for f in [880,1320,1760,2640])*0.12*(t>0.05)
save("explosion_livre", boom+whoosh+shim, 0.8)
# lévitation / perte de mémoire (montée mystérieuse)
d=3.0; n=int(d*SR); t=t_(d)
sw=sum(np.sin(2*np.pi*np.cumsum(f*(1+0.6*(t/d)**2))/SR) for f in [220,277.2,329.6,440])/4
lev=sw*(t/d)**1.5*env(n,0.1,0.3)+0.3*fft_filter(noise(n),lo=2000,hi=8000)*(t/d)**2
save("levitation", lev, 0.55)
# magie choisie : arpège montant
seq=[62,66,69,74,78,81]; out=np.zeros(int(1.8*SR))
for i,m in enumerate(seq):
    s=int(i*0.09*SR); d=1.2; n=int(d*SR); tt=t_(d)
    tone=(np.sin(2*np.pi*note(m)*tt)+0.4*np.sin(2*np.pi*note(m)*2*tt))*expdec(n,0.4)*env(n,0.003)
    out[s:s+n]+=tone[:len(out)-s]
save("choix", echo(out,0.15,0.3,3), 0.55)
# menu
d=0.05; n=int(d*SR); save("menu_deplacer", np.sin(2*np.pi*880*t_(d))*expdec(n,0.012), 0.3)
out=np.zeros(int(0.5*SR))
for i,m in enumerate([76,83]):
    s=int(i*0.08*SR); d=0.4; n=int(d*SR); tt=t_(d); tone=np.sin(2*np.pi*note(m)*tt)*expdec(n,0.12)*env(n,0.003); out[s:s+n]+=tone[:len(out)-s]
save("menu_valider", out, 0.45)
# porte de pierre
d=1.8; n=int(d*SR); t=t_(d)
grind=fft_filter(noise(n),lo=40,hi=350)*(0.6+0.4*np.abs(np.sin(2*np.pi*7*t)))*env(n,0.15,0.5)
thud=np.zeros(n); k=int(1.55*SR); thud[k:]=(np.sin(2*np.pi*60*t_(d-1.55))*expdec(n-k,0.12))[:n-k]
save("porte", grind+thud*1.2, 0.7)
# sceau qui se brise
d=1.0; n=int(d*SR); t=t_(d)
gl=sum(np.sin(2*np.pi*rng.uniform(1200,4000)*t)*expdec(n,rng.uniform(0.1,0.5)) for _ in range(16))
save("sceau", gl*0.6+fft_filter(noise(n),lo=500,hi=5000)*expdec(n,0.2)+0.5*sweep(300,80,d)*expdec(n,0.3), 0.6)

# ---------- musiques ----------
def pad(freqs,d,att=2.0,rel=2.5,bright=900):
    n=int(d*SR); t=t_(d); x=np.zeros(n)
    for f in freqs:
        for det in (-0.25,0.0,0.27):
            ph=rng.uniform(0,6.28); ff=f*(1+det/100)
            x+=np.sin(2*np.pi*ff*t+ph)+0.35*np.sin(2*np.pi*2*ff*t+ph)+0.15*np.sin(2*np.pi*3*ff*t)
    x=fft_filter(x,hi=bright)
    e=np.minimum(1,np.minimum(t/att,(d-t)/rel)); return x*np.clip(e,0,1)
def bell(f,d=3.0,tau=0.8):
    n=int(d*SR); t=t_(d)
    return (np.sin(2*np.pi*f*t)+0.45*np.sin(2*np.pi*f*2.01*t)*expdec(n,tau*0.5)+0.2*np.sin(2*np.pi*f*3.98*t)*expdec(n,tau*0.3))*expdec(n,tau)*env(n,0.004)
def place(buf,x,s):
    s=int(s*SR); e=min(len(buf),s+len(x)); buf[s:e]+=x[:e-s]
def loopify(x,L):
    n=int(L*SR); y=x[:n].copy(); tail=x[n:]; y[:len(tail)]+=tail; return y

# Grotte : ré mineur, mystérieux
L=64.0; buf=np.zeros(int((L+8)*SR))
prog=[[38,45,52,57],[34,41,50,53],[31,38,46,50],[33,40,45,52]]   # Dm9, Bbmaj7, Gm, Asus
for k in range(8):
    ch=prog[k%4]; place(buf, pad([note(m) for m in ch],10.0,2.5,3.0,700)*0.18, k*8.0)
drone=np.sin(2*np.pi*note(26)*t_(L+8))*(0.7+0.3*np.sin(2*np.pi*t_(L+8)/16))
buf+=fft_filter(drone,hi=200)*0.25
pent=[62,65,67,69,72,74,77,79]
bells=np.zeros_like(buf); tt=1.5
while tt<L:
    m=pent[rng.integers(len(pent))]; place(bells, bell(note(m),4.0,1.1)*rng.uniform(0.25,0.45), tt)
    if rng.random()<0.3: place(bells, bell(note(m+7 if m+7 in pent else m-5),4.0,1.0)*0.2, tt+0.4)
    tt+=rng.choice([2.0,3.0,4.0,5.0])
buf+=echo(bells,0.375,0.45,6)[:len(buf)]*0.6
for _ in range(14):  # gouttes d'eau
    s=rng.uniform(0,L); d=0.05; dr=sweep(2600,1300,d)*expdec(int(d*SR),0.015)
    place(buf, echo(dr,0.21,0.4,4)*0.12, s)
wind=fft_filter(noise(len(buf)),lo=150,hi=700)*(0.5+0.5*np.sin(2*np.pi*np.arange(len(buf))/SR/23))
buf+=wind*0.05
save("musique_grotte", loopify(buf,L), 0.6)

# Forêt : sol majeur, lumineux mais calme
L=48.0; buf=np.zeros(int((L+8)*SR))
prog=[[43,50,59,62],[40,47,55,59],[36,43,52,55],[38,45,54,57]]   # G, Em, C, D
for k in range(8):
    place(buf, pad([note(m) for m in prog[k%4]],7.0,1.5,2.0,1400)*0.13, k*6.0)
arp=[0,2,1,3,2,1]; step=0.375; tt=0.0; k=0
while tt<L:
    ch=prog[int(tt//6)%4]; m=ch[arp[k%len(arp)]]+24 if arp[k%len(arp)]<2 else ch[arp[k%len(arp)]]+12
    if rng.random()<0.85: place(buf, bell(note(m),1.5,0.35)*0.16, tt)
    tt+=step; k+=1
for _ in range(10):  # oiseaux
    s=rng.uniform(0,L); 
    for j in range(rng.integers(2,5)):
        d=0.08; ch=sweep(rng.uniform(2500,3500),rng.uniform(3800,5000),d)*env(int(d*SR),0.005,0.07)
        place(buf, ch*0.05, s+j*0.12)
wind=fft_filter(noise(len(buf)),lo=300,hi=1500)*(0.5+0.5*np.sin(2*np.pi*np.arange(len(buf))/SR/17))
buf+=wind*0.035
save("musique_foret", loopify(buf,L), 0.55)
