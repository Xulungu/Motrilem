import math, random, os, sys
from PIL import Image, ImageDraw, ImageFilter
OUT=sys.argv[1]
random.seed(7)
def C(h,a=255):
    h=h.lstrip('#'); return (int(h[0:2],16),int(h[2:4],16),int(h[4:6],16),a)
def new(w,h): return Image.new("RGBA",(w,h),(0,0,0,0))
def px(im,x,y,c):
    if 0<=x<im.width and 0<=y<im.height: im.putpixel((x,y),c)
def rect(im,x0,y0,x1,y1,c):
    for y in range(y0,y1+1):
        for x in range(x0,x1+1): px(im,x,y,c)
def save(im,name): im.save(os.path.join(OUT,name)); print(name,im.size)

# palette
OUTL=C('1b1020'); LD=C('4a2416'); LM=C('6e3a22'); LL=C('8f5432'); LH=C('b0703f')
GD=C('9c6a1c'); GM=C('d9a441'); GL=C('f6dc7a')
PD=C('b8996a'); PM=C('dcc596'); PL=C('efe0b8'); INK=C('7a5a3a')
CY=C('8ff0ff'); CYD=C('3fb3d6'); WH=C('ffffff')

# 1. livre fermé 24x18
b=new(24,18)
rect(b,1,1,22,16,OUTL)
rect(b,2,2,21,15,LM)
rect(b,2,2,4,15,LD)            # dos
rect(b,5,2,21,2,LH); rect(b,5,3,21,3,LL)
rect(b,2,15,21,15,LD)
for x in range(5,22,1): px(b,x,14,LD)
# pages visibles en bas
rect(b,5,16,21,16,PM); px(b,22,16,OUTL)
# coins dorés
for (x,y) in [(5,2),(21,2),(5,13),(21,13)]:
    px(b,x,y,GM)
px(b,6,2,GD);px(b,5,3,GD);px(b,20,2,GD);px(b,21,3,GD);px(b,5,12,GD);px(b,6,13,GD);px(b,21,12,GD);px(b,20,13,GD)
# bandes dorées sur le dos
rect(b,2,5,4,5,GD); rect(b,2,12,4,12,GD)
# emblème : deux losanges entrelacés (les deux frères)
cx,cy=13,8
for d in range(0,4):
    px(b,cx-2+d-1,cy-d+1,GM); px(b,cx-2+d-1,cy+d-1,GM)
for (x,y) in [(10,8),(11,7),(12,6),(13,7),(14,8),(13,9),(12,10),(11,9)]: px(b,x,y,GM)
for (x,y) in [(13,8),(14,7),(15,6),(16,7),(17,8),(16,9),(15,10),(14,9)]: px(b,x,y,CY)
px(b,12,8,GL); px(b,15,8,WH)
save(b,"livre_ferme.png")

# 2. livre ouvert 34x20
o=new(34,20)
rect(o,0,6,33,18,OUTL); rect(o,1,7,32,17,LM); rect(o,1,17,32,17,LD)
# pages (légèrement bombées)
for x in range(2,32):
    t=abs(x-16.5)/14.5
    top=int(round(2+ (1-t)*1.5)) if x not in (16,17) else 5
    top=max(1,min(5,4-int((1-abs(x-9)/7.0)*2.5) if x<17 else 4-int((1-abs(x-24)/7.0)*2.5)))
    rect(o,x,top,x,15,PM); px(o,x,top-1,OUTL)
    px(o,x,15,PD); px(o,x,16,PL)
for y in range(3,16): px(o,16,y,PD); px(o,17,y,PD)
for y in range(4,15): px(o,15,y,PD); px(o,18,y,PD)
# lignes de texte
for i,y in enumerate(range(6,14,2)):
    for x in range(4,14):
        if random.random()<0.8: px(o,x,y,INK)
for i,y in enumerate(range(6,14,2)):
    if y in (8,10): continue
    for x in range(20,30):
        if random.random()<0.8: px(o,x,y,INK)
# rune lumineuse page droite
for (x,y) in [(24,8),(25,9),(24,10),(23,9),(24,9)]: px(o,x,y,CY)
px(o,24,9,WH)
save(o,"livre_ouvert.png")

# 3. page 11x14
p=new(11,14)
rect(p,1,1,9,12,PM)
for y in range(1,13): px(p,0,y,OUTL); px(p,10,y,OUTL)
for x in range(1,10): px(p,x,0,OUTL); px(p,x,13,OUTL)
px(p,0,0,(0,0,0,0)); px(p,10,13,(0,0,0,0)); px(p,9,12,PD); px(p,9,11,PD); px(p,8,12,PD)
px(p,10,12,OUTL); px(p,9,13,OUTL)
# bord déchiré à droite
for y in (3,4,8): px(p,9,y,PD)
rect(p,1,1,9,1,PL)
for y in (3,10):
    for x in range(2,9):
        if (x+y)%3: px(p,x,y,INK)
# trois symboles (feu, foudre, glace) en triangle
px(p,5,5,C('ff6a2a')); px(p,5,6,C('ffb347'))
px(p,3,8,C('ffe14d')); px(p,4,7,C('ffe14d'))
px(p,7,8,C('9fe8ff')); px(p,7,7,C('d8f8ff'))
save(p,"page.png")

# 4. fragment 7x9 (losange de souvenir)
f=new(7,9)
pts=[(3,0),(2,1),(4,1),(1,2),(5,2),(1,3),(5,3),(0,4),(6,4),(1,5),(5,5),(1,6),(5,6),(2,7),(4,7),(3,8)]
fill=[(3,1),(2,2),(3,2),(4,2),(2,3),(3,3),(4,3),(1,4),(2,4),(3,4),(4,4),(5,4),(2,5),(3,5),(4,5),(2,6),(3,6),(4,6),(3,7)]
for q in fill: px(f,*q,C('d8f6ff'))
for q in pts: px(f,*q,CYD)
for q in [(3,2),(2,3),(3,3),(2,4)]: px(f,*q,WH)
save(f,"fragment.png")

# 5. lueur 64x64 (douce, filtrage bilinéaire)
g=new(64,64)
for y in range(64):
    for x in range(64):
        d=math.hypot(x-31.5,y-31.5)/31.5
        a=max(0.0,1-d); a=a**2.2
        g.putpixel((x,y),(255,255,255,int(255*a)))
save(g,"lueur.png")

# 6. rayons 128x128
r=new(128,128)
n=14; offs=[random.random()*0.15 for _ in range(n)]; widths=[0.05+random.random()*0.06 for _ in range(n)]; lens=[0.6+random.random()*0.4 for _ in range(n)]
for y in range(128):
    for x in range(128):
        dx,dy=x-63.5,y-63.5; d=math.hypot(dx,dy)/63.5
        if d>1: continue
        ang=(math.atan2(dy,dx)/(2*math.pi))%1.0
        a=0
        for i in range(n):
            c=(i/n+offs[i])%1.0; da=min(abs(ang-c),1-abs(ang-c))
            if da<widths[i]/2 and d<lens[i]:
                a=max(a,(1-da/(widths[i]/2))*(1-d/lens[i]))
        a=a*min(1,d*4)
        r.putpixel((x,y),(255,255,255,int(255*min(1,a*1.3))))
save(r,"rayons.png")

# 7. socle 20x22
s=new(20,22)
SD=C('2c3f4a'); SM=C('4e6b78'); SL=C('7a9aa5'); MO=C('4f8f3a'); ML=C('7fc25a')
rect(s,1,0,18,4,OUTL); rect(s,2,1,17,3,SM); rect(s,2,1,17,1,SL); rect(s,2,3,17,3,SD)
rect(s,4,4,15,18,OUTL); rect(s,5,5,14,17,SM); rect(s,5,5,6,17,SL); rect(s,13,5,14,17,SD)
for y in (8,12,16): rect(s,5,y,14,y,SD)
rect(s,1,18,18,21,OUTL); rect(s,2,19,17,20,SM); rect(s,2,19,17,19,SL)
for (x,y) in [(2,0),(3,0),(4,1),(15,0),(16,0),(5,6),(5,7),(14,10),(6,18),(7,18),(12,18),(2,18),(17,18)]: px(s,x,y,MO)
for (x,y) in [(3,0),(16,0),(5,6),(7,18)]: px(s,x,y,ML)
# rune gravée
for (x,y) in [(9,9),(10,9),(9,10),(10,11),(9,12),(10,12)]: px(s,x,y,C('3fb3d6'))
save(s,"socle.png")

# 8. barrière magique 12x48
bar=new(12,48)
for y in range(48):
    for x in range(12):
        d=abs(x-5.5)/5.5
        a=int(200*(1-d**2))
        col=(150,110,255,a)
        bar.putpixel((x,y),col)
for y in range(2,48,6):
    for (dx,dy) in [(0,0),(1,1),(0,2),(-1,1)]:
        px(bar,6+dx,y+dy,(235,220,255,255))
for y in range(48): px(bar,5,y,(220,200,255,230)); px(bar,6,y,(220,200,255,230))
save(bar,"barriere.png")

# 9. icônes 24x24
def outline(im,col=OUTL):
    src=im.copy()
    for y in range(im.height):
        for x in range(im.width):
            if src.getpixel((x,y))[3]==0:
                for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
                    xx,yy=x+dx,y+dy
                    if 0<=xx<im.width and 0<=yy<im.height and src.getpixel((xx,yy))[3]>0:
                        im.putpixel((x,y),col); break
    return im
# feu
fi=new(24,24)
for y in range(24):
    for x in range(24):
        # flamme : goutte inversée
        t=(y-3)/18.0
        if t<0 or t>1: continue
        w=(math.sin(t*math.pi*0.95)**0.8)*8*(0.55+0.45*t)
        cx=12+math.sin(t*5)*1.2*(1-t)
        if abs(x-cx)<=w:
            k=abs(x-cx)/max(w,0.01)
            if t>0.55 and k<0.45: c=C('fff3a0')
            elif t>0.3 and k<0.7: c=C('ffb030')
            else: c=C('e8461e')
            fi.putpixel((x,y),c)
for (x,y) in [(7,6),(6,5),(17,7),(18,6),(12,1),(12,2)]: px(fi,x,y,C('e8461e'))
save(outline(fi),"icone_feu.png")
# foudre
fo=new(24,24)
bolt=[(14,1),(5,13),(11,13),(8,22),(19,9),(13,9),(17,1)]
d=ImageDraw.Draw(fo); d.polygon(bolt,fill=C('ffe14d'))
d.line([(14,2),(7,12)],fill=C('fffbe0')); d.line([(12,13),(9,20)],fill=C('fffbe0'))
for q in [(4,4),(20,15),(3,18),(21,3)]: px(fo,*q,C('c9a2ff'))
save(outline(fo),"icone_foudre.png")
# glace
gl=new(24,24); d=ImageDraw.Draw(gl)
cx,cy=12,12
for k in range(6):
    a=k*math.pi/3
    x2,y2=cx+math.cos(a)*9,cy+math.sin(a)*9
    d.line([(cx,cy),(x2,y2)],fill=C('9fe8ff'),width=2)
    for s2 in (0.55,):
        bx,by=cx+math.cos(a)*9*s2,cy+math.sin(a)*9*s2
        for sgn in (-1,1):
            a2=a+sgn*math.pi/4
            d.line([(bx,by),(bx+math.cos(a2)*3,by+math.sin(a2)*3)],fill=C('d8f8ff'))
rect(gl,11,11,12,12,WH)
save(outline(gl),"icone_glace.png")

# 10. mages 20x28
def mage(robe,robe_d,trim,face,eye,staff_col,orb):
    m=new(20,28)
    d=ImageDraw.Draw(m)
    # robe (trapèze)
    d.polygon([(7,11),(12,11),(16,27),(3,27)],fill=robe)
    d.polygon([(10,11),(12,11),(16,27),(11,27)],fill=robe_d)
    rect(m,3,26,16,27,trim)
    # capuche / chapeau pointu
    d.polygon([(5,10),(14,10),(9,0)],fill=robe)
    d.polygon([(9,0),(14,10),(10,10)],fill=robe_d)
    rect(m,4,10,15,10,trim)
    # visage dans l'ombre
    rect(m,8,11,11,13,face)
    px(m,8,12,eye); px(m,10,12,eye)
    # bâton
    for y in range(6,28): px(m,17,y,staff_col)
    rect(m,16,3,18,5,orb); px(m,17,4,WH)
    # bras
    rect(m,13,15,16,16,robe_d)
    return outline(m)
save(mage(C('e9e4d4'),C('b9b1a0'),C('d9a441'),C('3a2a2a'),C('8ff0ff'),C('8f5432'),C('8ff0ff')),"mage_lumiere.png")
save(mage(C('3b2350'),C('24142f'),C('8a2be2'),C('120a14'),C('ff3b3b'),C('2a1a2a'),C('c03bff')),"mage_ombre.png")

# 11. livre UI 200x128 (double page)
u=new(200,128)
rect(u,0,4,199,127,OUTL); rect(u,1,5,198,126,LM)
rect(u,1,124,198,126,LD); rect(u,1,5,198,6,LL)
for (x,y) in [(3,7),(196,7),(3,122),(196,122)]:
    rect(u,x-1,y-1,x+1,y+1,GM)
# pages
for side in (0,1):
    x0=8 if side==0 else 101; x1=98 if side==0 else 191
    for x in range(x0,x1+1):
        t=(x-x0)/(x1-x0)
        bow=int(round(3*math.sin(t*math.pi)))
        rect(u,x,4-bow+4,x,118,PM)
        px(u,x,3-bow+4,OUTL)
    for x in range(x0,x1+1):
        px(u,x,118,PD); px(u,x,119,PL); px(u,x,120,PD)
# reliure
for y in range(6,121):
    for x in range(92,108):
        k=1-abs(x-99.5)/8
        if u.getpixel((x,y))[3]>0 and u.getpixel((x,y))!=OUTL:
            c=PM; 
            if k>0.6: c=PD
            px(u,x,y,c)
# ombre douce bords de page
for y in range(8,118):
    px(u,9,y,PL); px(u,190,y,PL)
save(u,"livre_ui.png")

# 12. étincelle 5x5
e=new(5,5)
for q in [(2,0),(2,1),(0,2),(1,2),(2,2),(3,2),(4,2),(2,3),(2,4)]: px(e,*q,(255,255,255,200))
px(e,2,2,WH)
save(e,"etincelle.png")
