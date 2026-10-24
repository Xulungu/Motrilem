import math, random, os, sys
from PIL import Image, ImageDraw
OUT=sys.argv[1]; random.seed(5)
def C(h,a=255): h=h.lstrip('#'); return (int(h[0:2],16),int(h[2:4],16),int(h[4:6],16),a)
OUTL=C('15121c'); S0=C('262a33'); S1=C('3c4350'); S2=C('566070'); S3=C('76828f'); S4=C('9aa6b0')
MO=C('3f7a32'); ML=C('6fb24a'); RED=C('ff3b3b')
def outline(im,col=OUTL):
    src=im.copy(); p=im.load(); s=src.load()
    for y in range(im.height):
        for x in range(im.width):
            if s[x,y][3]==0:
                for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
                    xx,yy=x+dx,y+dy
                    if 0<=xx<im.width and 0<=yy<im.height and s[xx,yy][3]>200: p[x,y]=col; break
    return im
def bricks(d,x0,y0,x1,y1,base,dark,light,bh=6,bw=12):
    d.rectangle([x0,y0,x1,y1],fill=base)
    row=0
    for y in range(y0,y1+1,bh):
        d.line([(x0,y),(x1,y)],fill=dark)
        off=(bw//2)*(row%2)
        for x in range(x0+off,x1+1,bw): d.line([(x,y),(x,min(y1,y+bh-1))],fill=dark)
        d.line([(x0,y+1),(x1,y+1)],fill=light) if y+1<=y1 else None
        row+=1
W,H=176,144
f=Image.new("RGBA",(W,H),(0,0,0,0)); d=ImageDraw.Draw(f)
# mur du fond (assise du temple)
bricks(d,14,34,W-15,H-14,S2,S1,S3)
# fronton triangulaire
d.polygon([(6,36),(W//2,4),(W-7,36)],fill=S2); d.polygon([(14,34),(W//2,10),(W-15,34)],fill=S1)
d.line([(6,36),(W//2,4),(W-7,36)],fill=S3)
d.rectangle([4,34,W-5,40],fill=S3); d.line([(4,40),(W-5,40)],fill=S1)
# emblème : moustique sculpté dans le fronton
cx=W//2
d.ellipse([cx-7,18,cx+7,28],fill=S3); d.ellipse([cx-4,14,cx+4,21],fill=S3)
d.line([(cx-4,18),(cx-16,30)],fill=S4); d.line([(cx-16,30),(cx-20,32)],fill=S4)  # trompe
for s in (-1,1):
    d.polygon([(cx+s*3,20),(cx+s*20,12),(cx+s*16,22)],fill=S4)    # ailes
    for k in range(3): d.line([(cx+s*(2+k*2),27),(cx+s*(6+k*4),33)],fill=S3)  # pattes
f.putpixel((cx-2,16),RED); f.putpixel((cx+1,16),RED)
# renfoncement de la porte (profondeur : murs intérieurs en perspective)
dx0,dx1,dy0=cx-26,cx+26,58
d.rectangle([dx0,dy0,dx1,H-14],fill=S0)
d.ellipse([dx0,dy0-26,dx1,dy0+26],fill=S0)
for i in range(6):   # murs intérieurs qui rétrécissent
    k=i*3
    col=(int(S1[0]*(1-i*0.12)),int(S1[1]*(1-i*0.12)),int(S1[2]*(1-i*0.12)),255)
    d.rectangle([dx0+k,dy0+k//2,dx1-k,H-14],outline=col)
    d.arc([dx0+k,dy0-26+k,dx1-k,dy0+26+k],180,360,fill=col)
# porte intérieure sombre
ix0,ix1=cx-14,cx+14
d.rectangle([ix0,dy0+8,ix1,H-14],fill=C('0b0a10')); d.ellipse([ix0,dy0-6,ix1,dy0+22],fill=C('0b0a10'))
# arche sculptée autour du renfoncement
d.arc([dx0-4,dy0-30,dx1+4,dy0+30],180,360,fill=S4,width=3)
d.line([(dx0-3,dy0),(dx0-3,H-14)],fill=S4,width=3); d.line([(dx1+3,dy0),(dx1+3,H-14)],fill=S4,width=3)
for a in range(185,360,25):   # claveaux
    r=29; x=cx+int(math.cos(math.radians(a))*r); y=dy0+int(math.sin(math.radians(a))*r)
    d.line([(x,y),(cx+int(math.cos(math.radians(a))*(r+5)),dy0+int(math.sin(math.radians(a))*(r+5)))],fill=S1)
# runes rouges de part et d'autre
for y in range(70,120,12):
    for x in (dx0-12,dx1+10):
        d.line([(x,y),(x+2,y+3),(x,y+6)],fill=C('a02a30'))
# marches
for i in range(3):
    y=H-14+i*5; ext=10+i*8
    d.rectangle([cx-40-ext,y,cx+40+ext,y+4],fill=S3 if i==0 else S2); d.line([(cx-40-ext,y),(cx+40+ext,y)],fill=S4)
# fissures, mousse, lianes
for _ in range(14):
    x=random.randint(16,W-17); y=random.randint(42,H-20); 
    if dx0-6<x<dx1+6: continue
    for k in range(random.randint(3,7)):
        f.putpixel((x,y),S0); x+=random.choice((-1,0,1)); y+=1
for x in range(6,W-6,2):
    if random.random()<0.35: f.putpixel((x,41),MO)
    if random.random()<0.15:
        L=random.randint(4,18)
        for y in range(41,41+L): f.putpixel((x,y),MO if y%3 else ML)
outline(f).save(os.path.join(OUT,"donjon_facade.png"))
# colonne (premier plan)
c=Image.new("RGBA",(22,120),(0,0,0,0)); d=ImageDraw.Draw(c)
d.rectangle([3,10,18,110],fill=S2)
for x in (6,10,14): d.line([(x,12),(x,108)],fill=S1)
d.line([(4,10),(4,110)],fill=S4)
d.rectangle([0,0,21,10],fill=S3); d.line([(0,10),(21,10)],fill=S1); d.rectangle([2,3,19,7],fill=S2)
d.rectangle([0,110,21,119],fill=S3); d.line([(0,110),(21,110)],fill=S4)
for y in range(20,110,9):
    if random.random()<0.5: c.putpixel((17,y),MO)
for y in range(0,40): 
    if random.random()<0.4: c.putpixel((random.choice((2,3,18,19)),y+10),ML)
outline(c).save(os.path.join(OUT,"donjon_colonne.png"))
# brasero
b=Image.new("RGBA",(12,16),(0,0,0,0)); d=ImageDraw.Draw(b)
d.polygon([(1,7),(10,7),(8,10),(3,10)],fill=S3); d.rectangle([5,10,6,14],fill=S2); d.rectangle([3,15,8,15],fill=S2)
d.polygon([(3,7),(6,0),(8,7)],fill=C('ff9a2a')); d.polygon([(4,7),(6,3),(7,7)],fill=C('fff0a0'))
outline(b).save(os.path.join(OUT,"brasero.png"))
# éclat de vie (cristal en forme de cœur)
e=Image.new("RGBA",(13,13),(0,0,0,0)); d=ImageDraw.Draw(e)
d.polygon([(6,12),(0,5),(1,2),(3,1),(6,3),(9,1),(11,2),(12,5)],fill=C('ff5a7a'))
d.polygon([(6,12),(6,3),(9,1),(11,2),(12,5)],fill=C('d83a5c'))
d.line([(2,3),(4,3)],fill=C('ffd0dc')); e.putpixel((3,4),C('ffffff'))
outline(e).save(os.path.join(OUT,"eclat_vie.png"))
print("ok")
