import math, os, sys
from PIL import Image, ImageDraw
OUT=sys.argv[1]
def C(h,a=255): h=h.lstrip('#'); return (int(h[0:2],16),int(h[2:4],16),int(h[4:6],16),a)
OUTL=C('1b1a22'); S0=C('34363f'); S1=C('555864'); S2=C('7b7f8c'); S3=C('a3a7b4'); S4=C('c9ccd6')
MO=C('4f8f3a'); ML=C('7fc25a')
W,H=64,56; CX=32
def outline(im):
    src=im.copy(); px=im.load(); sp=src.load()
    for y in range(im.height):
        for x in range(im.width):
            if sp[x,y][3]==0:
                for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
                    xx,yy=x+dx,y+dy
                    if 0<=xx<im.width and 0<=yy<im.height and sp[xx,yy][3]>0: px[x,y]=OUTL; break
    return im
def wing(d, side, ang, length):
    # aile : polygone en éventail ancré à l'épaule, ang=0 repliée (le long du corps), ang=1 déployée
    sx, sy = CX+side*5, 20
    a0 = math.radians(-100 + 75*ang)       # direction principale (0 = vers la droite)
    pts=[(sx,sy)]
    n=7
    for i in range(n+1):
        t=i/n
        # du bord supérieur (haut) au bas de l'aile
        a = a0 - math.radians(55*(1-ang*0.3))*t*(-1) 
        r = length*(1.0 - 0.45*t) * (1 if t<0.85 else 0.9)
        ax = math.cos(math.radians(-90 + ang*75 + t*(60+ang*40)))
        ay = math.sin(math.radians(-90 + ang*75 + t*(60+ang*40)))
        pts.append((sx+side*ax*r, sy+ay*r))
    pts.append((sx+side*2, sy+10))
    d.polygon(pts, fill=S2)
    # plumes
    for i in range(1,6):
        t=i/6
        ax=math.cos(math.radians(-90 + ang*75 + t*(60+ang*40))); ay=math.sin(math.radians(-90 + ang*75 + t*(60+ang*40)))
        r=length*(1-0.45*t)
        d.line([(sx+side*ax*r*0.45, sy+ay*r*0.45),(sx+side*ax*r*0.95, sy+ay*r*0.95)], fill=S1)
    # bord d'attaque clair
    ax=math.cos(math.radians(-90 + ang*75)); ay=math.sin(math.radians(-90 + ang*75))
    d.line([(sx,sy),(sx+side*ax*length, sy+ay*length)], fill=S3)
def statue(ang):
    im=Image.new("RGBA",(W,H),(0,0,0,0)); d=ImageDraw.Draw(im)
    L=18+ang*8
    wing(d,-1,ang,L); wing(d,1,ang,L)
    # socle
    d.rectangle([CX-13,H-6,CX+12,H-1],fill=S1); d.rectangle([CX-13,H-6,CX+12,H-6],fill=S3)
    d.rectangle([CX-10,H-9,CX+9,H-7],fill=S2); d.line([(CX-10,H-9),(CX+9,H-9)],fill=S3)
    for x in (CX-12,CX-11,CX+10,CX+11): im.putpixel((x,H-7+ (x%2)),MO) if 0<=x<W else None
    im.putpixel((CX-11,H-6),ML); im.putpixel((CX+8,H-9),MO)
    # robe
    d.polygon([(CX-4,22),(CX+3,22),(CX+7,H-10),(CX-8,H-10)],fill=S2)
    d.polygon([(CX,22),(CX+3,22),(CX+7,H-10),(CX+1,H-10)],fill=S1)
    for y in range(30,H-10,4): im.putpixel((CX-3,y),S1); im.putpixel((CX-5,y+2),S1)
    # tête (capuche) et visage
    d.ellipse([CX-4,11,CX+3,19],fill=S2); d.ellipse([CX-3,13,CX+2,19],fill=S3)
    d.rectangle([CX-2,15,CX+1,18],fill=S4)
    im.putpixel((CX-2,16),S1); im.putpixel((CX+1,16),S1)
    # épée (pointe vers le bas) tenue devant
    d.rectangle([CX-1,27,CX,H-11],fill=S4); d.line([(CX,27),(CX,H-11)],fill=S3)
    d.rectangle([CX-4,25,CX+3,26],fill=S3)   # garde
    d.rectangle([CX-1,21,CX,24],fill=S1)     # poignée
    d.rectangle([CX-3,22,CX+2,24],fill=S3)   # mains
    im.putpixel((CX-1,H-11),S3)
    # rune sur la garde
    im.putpixel((CX-1,25),C('8ff0ff') if ang>0 else S4)
    return outline(im)
for i,a in enumerate([0.0,0.35,0.7,1.0]):
    statue(a).save(os.path.join(OUT,f"statue_{i}.png"))
# trompe dupliquée (aiguille)
n=Image.new("RGBA",(14,3),(0,0,0,0)); d=ImageDraw.Draw(n)
d.line([(0,1),(13,1)],fill=C('5a1f22')); d.line([(0,0),(7,0)],fill=C('7a2a2c')); d.line([(0,2),(7,2)],fill=C('3a1214'))
n.putpixel((12,1),C('ff6b6b')); n.putpixel((13,1),C('ffd0d0'))
n.save(os.path.join(OUT,"trompe.png"))
print("ok")
