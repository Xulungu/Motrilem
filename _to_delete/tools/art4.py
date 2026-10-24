import math, random, os, sys
from PIL import Image, ImageDraw
OUT=sys.argv[1]; random.seed(21)
def C(h,a=255): h=h.lstrip('#'); return (int(h[0:2],16),int(h[2:4],16),int(h[4:6],16),a)
def new(w,h): return Image.new("RGBA",(w,h),(0,0,0,0))
def outline(im,col=C('14201a')):
    src=im.copy(); p=im.load(); s=src.load()
    for y in range(im.height):
        for x in range(im.width):
            if s[x,y][3]==0:
                for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
                    xx,yy=x+dx,y+dy
                    if 0<=xx<im.width and 0<=yy<im.height and s[xx,yy][3]>200: p[x,y]=col; break
    return im
A0=C('2f8f2a'); A1=C('5fd23a'); A2=C('a8f060'); A3=C('e4ffb0')
# acide : corps (16x16, uniforme, légèrement texturé) et surface animée (4 images 16x6)
b=new(16,16)
for y in range(16):
    for x in range(16):
        v=0.5+0.5*math.sin(x*0.9+y*0.4)*math.sin(y*0.7)
        c=A0 if v<0.55 else C('379a30')
        b.putpixel((x,y),(c[0],c[1],c[2],215))
for (x,y) in [(3,4),(11,9),(7,13)]: b.putpixel((x,y),(A1[0],A1[1],A1[2],230))
b.save(os.path.join(OUT,"acide.png"))
for f in range(4):
    s=new(16,6)
    for x in range(16):
        h=2+round(1.2*math.sin((x/16)*2*math.pi*2+f*math.pi/2))
        for y in range(h,6): s.putpixel((x,y),(A0[0],A0[1],A0[2],215))
        s.putpixel((x,h),A2); 
        if h+1<6: s.putpixel((x,h+1),A1)
    for x in range(16):
        if (x+f*4)%8==0:
            hh=2+round(1.2*math.sin((x/16)*2*math.pi*2+f*math.pi/2)); s.putpixel((x,max(0,hh-1)),A3)
    s.save(os.path.join(OUT,f"acide_surface_{f}.png"))
# bulle
bb=new(5,5); d=ImageDraw.Draw(bb); d.ellipse([0,0,4,4],outline=A2); bb.putpixel((1,1),A3); bb.save(os.path.join(OUT,"bulle.png"))
# nénuphars (vert = stable, brun = s'enfonce)
def pad(c0,c1,c2,flower):
    p=new(32,9); d=ImageDraw.Draw(p)
    d.ellipse([0,1,31,8],fill=c0); d.ellipse([2,1,29,6],fill=c1)
    d.polygon([(15,4),(19,1),(23,1)],fill=(0,0,0,0))  # encoche
    for i in range(5): d.line([(16,4),(16+int(12*math.cos(i*0.6+0.4)),4-int(3*math.sin(i*0.6+0.4)))],fill=c2)
    if flower:
        d.ellipse([6,0,10,3],fill=C('f2a6d8')); p.putpixel((8,1),C('fff3a0'))
    return outline(p)
pad(C('2e6b2a'),C('4f9a3a'),C('3a7a30'),True).save(os.path.join(OUT,"nenuphar.png"))
pad(C('5a4320'),C('7d6230'),C('4a3418'),False).save(os.path.join(OUT,"nenuphar_brun.png"))
# plateforme de pierre mobile 48x12
pl=new(48,12); d=ImageDraw.Draw(pl)
d.rectangle([0,2,47,11],fill=C('4e6b78')); d.rectangle([0,2,47,3],fill=C('7a9aa5')); d.rectangle([0,10,47,11],fill=C('2c3f4a'))
for x in (11,23,35): d.line([(x,4),(x,9)],fill=C('2c3f4a'))
for x in range(0,48,3): 
    if random.random()<0.5: pl.putpixel((x,2),C('4f8f3a')); 
for (x,y) in [(5,6),(30,7),(41,5)]: pl.putpixel((x,y),C('3fb3d6'))
outline(pl,C('1b1020')).save(os.path.join(OUT,"plateforme_pierre.png"))
# slime 20x16 : repos 0/1 et saut
def slime(squash, eyes_up=0):
    s=new(22,18); d=ImageDraw.Draw(s)
    w=int(18*squash); h=int(13/squash)
    x0=11-w//2; y1=17; y0=y1-h
    d.ellipse([x0,y0,x0+w,y1+6],fill=A1)
    d.rectangle([x0,y1-2,x0+w,y1],fill=A1)
    d.ellipse([x0+2,y0+1,x0+w-3,y0+h//2+2],fill=C('7fe050'))
    s.putpixel((x0+4,y0+3),A3); s.putpixel((x0+5,y0+3),A3); s.putpixel((x0+4,y0+4),A3)
    ey=y0+h//2-eyes_up
    for ex in (11-3,11+2):
        d.rectangle([ex,ey,ex+1,ey+2],fill=C('14201a'))
        s.putpixel((ex,ey),C('ffffff'))
    d.line([(x0,y1),(x0+w,y1)],fill=A0)
    # gouttes
    s.putpixel((x0+w-2,y1+0),A0)
    for y in range(18):
        for x in range(22):
            if y>17: s.putpixel((x,y),(0,0,0,0))
    return outline(s)
slime(1.0).save(os.path.join(OUT,"slime_0.png"))
slime(1.12).save(os.path.join(OUT,"slime_1.png"))
slime(0.82,1).save(os.path.join(OUT,"slime_saut.png"))
# roseaux (décor)
for k in range(3):
    r=new(10,20); d=ImageDraw.Draw(r)
    for i in range(3+k):
        x=2+i*2+random.randint(-1,1); top=random.randint(2,8)
        d.line([(x,19),(x+random.randint(-1,1),top)],fill=C('4f8f3a'))
        if random.random()<0.6: d.rectangle([x-1,top,x,top+3],fill=C('7a5530'))
    r.save(os.path.join(OUT,f"roseau_{k}.png"))
print("ok")
