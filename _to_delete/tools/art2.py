import math, random, os, sys
from PIL import Image, ImageDraw
OUT=sys.argv[1]; random.seed(11)
def C(h,a=255): h=h.lstrip('#'); return (int(h[0:2],16),int(h[2:4],16),int(h[4:6],16),a)
def new(w,h): return Image.new("RGBA",(w,h),(0,0,0,0))
def px(im,x,y,c):
    if 0<=x<im.width and 0<=y<im.height: im.putpixel((x,y),c)
def rect(im,x0,y0,x1,y1,c):
    for y in range(y0,y1+1):
        for x in range(x0,x1+1): px(im,x,y,c)
def save(im,n): im.save(os.path.join(OUT,n)); print(n,im.size)
OUTL=C('1b1020'); SD=C('2c3f4a'); SM=C('4e6b78'); SL=C('7a9aa5'); MO=C('4f8f3a'); ML=C('7fc25a')
WD=C('4a2a16'); WM=C('6e4024'); WL=C('94603a'); GM=C('d9a441')

def arch(inner):
    im=new(32,48)
    # montants et arche de pierre
    for y in range(48):
        for x in range(32):
            cx=15.5; top=14
            r=math.hypot(x-cx,(y-top)*1.0)
            outer = (y>=top and 1<=x<=30) or (y<top and r<=15)
            hole  = (y>=top and 6<=x<=25) or (y<top and r<=10)
            if outer and not hole:
                c=SM
                if (x+y*3)%11==0: c=SD
                if x<=2 or (y<top and r>14): c=SL
                if x>=29: c=SD
                px(im,x,y,c)
            if hole: px(im,x,y,inner(x,y))
    # joints de pierre
    for y in range(16,48,6):
        rect(im,1,y,5,y,SD); rect(im,26,y,30,y,SD)
    for a in range(0,180,30):
        ang=math.radians(180+a); 
        for rr in range(10,16): px(im,int(15.5+math.cos(ang)*rr),int(14+math.sin(ang)*rr),SD)
    # mousse
    for (x,y) in [(3,2),(4,2),(5,1),(6,1),(25,1),(26,2),(27,3),(1,20),(30,30),(2,40),(29,12)]: px(im,x,y,MO)
    for (x,y) in [(4,2),(26,2),(1,21)]: px(im,x,y,ML)
    # rune clé de voûte
    for (x,y) in [(15,1),(16,1),(15,2),(16,3),(15,4)]: px(im,x,y,C('8ff0ff'))
    return im
def door_closed(x,y):
    c=WM
    if x in (6,25) or y in (5,): c=WD
    if x==15 or x==16: c=WD
    if (x-6)%4==0: c=WL if c==WM else c
    if y in (20,36): c=C('3a3a44')
    if (x,y) in [(13,28),(18,28)]: c=GM
    return c
def door_open(x,y):
    t=min(1,abs(x-15.5)/10); v=int(255-60*t)
    return (v,int(v*0.97),int(v*0.85),255)
def cave_dark(x,y):
    t=min(1,(y-4)/40) if y>4 else 0
    return (int(18+10*t),int(15+8*t),int(28+8*t),255)
save(arch(door_closed),"porte_fermee.png")
save(arch(door_open),"porte_ouverte.png")
save(arch(cave_dark),"entree_grotte.png")

# planche (tuile 16x16 : plateforme traversable)
p=new(16,16)
rect(p,0,0,15,4,WM); rect(p,0,0,15,0,WL); rect(p,0,4,15,4,WD)
for x in (5,11): px(p,x,1,WD); px(p,x,2,WD); px(p,x,3,WD)
for x in (2,13): rect(p,x,5,x,7,WD)
px(p,3,2,C('2a1a10')); px(p,8,2,C('2a1a10'))
save(p,"planche.png")

# boule de feu 10x10
f=new(10,10)
for y in range(10):
    for x in range(10):
        d=math.hypot(x-5.5,y-4.5)
        if d<4.6:
            c=C('e8461e') if d>3.4 else C('ffb030') if d>1.8 else C('fff3a0')
            px(f,x,y,c)
for (x,y) in [(1,3),(0,5),(1,6),(2,8)]: px(f,x,y,C('e8461e'))
save(f,"boule_feu.png")
# éclair 18x7
e=new(18,7); pts=[(0,3),(4,1),(7,4),(11,1),(14,4),(17,3)]
d=ImageDraw.Draw(e); d.line(pts,fill=C('ffe14d'),width=2); d.line(pts,fill=C('fffbe0'),width=1)
save(e,"eclair.png")
# éclat de glace 12x7
g=new(12,7); d=ImageDraw.Draw(g); d.polygon([(0,3),(4,0),(11,3),(4,6)],fill=C('9fe8ff')); d.line([(1,3),(10,3)],fill=C('ffffff')); d.polygon([(4,0),(7,2),(4,3)],fill=C('d8f8ff'))
save(g,"eclat_glace.png")
# cristal d'entraînement 14x22
cr=new(14,22); d=ImageDraw.Draw(cr)
d.polygon([(7,0),(13,6),(12,21),(2,21),(1,6)],fill=C('7b5cd6'))
d.polygon([(7,0),(7,21),(2,21),(1,6)],fill=C('a58bff'))
d.line([(7,0),(7,21)],fill=C('d9ccff')); d.line([(3,8),(5,16)],fill=C('ffffff'))
d.polygon([(1,6),(7,0),(13,6)],outline=C('3b2a7a'))
rect(cr,1,21,12,21,C('3b2a7a'))
save(cr,"cristal.png")
# poussière 6x6 (douce)
p=new(6,6)
for y in range(6):
    for x in range(6):
        dd=math.hypot(x-2.5,y-2.5)
        if dd<3: px(p,x,y,(220,210,230,int(255*(1-dd/3)**0.8)))
save(p,"poussiere.png")
# éclat (débris) 4x4
s=new(4,4); rect(s,1,0,2,3,(255,255,255,255)); rect(s,0,1,3,2,(255,255,255,255))
save(s,"eclat.png")
