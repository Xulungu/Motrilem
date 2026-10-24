# Carte de l'Étang Acide (monde ouvert). Source unique : génère Assets/Editor/PondLayout.cs + un aperçu PNG.
X0,X1,Y0,Y1=-12,300,-62,40
SURF_END=290
surface=[(0,31,0),(36,44,0),(45,49,1),(50,57,2),(63,70,2),(71,74,1),(75,79,0),(101,106,0),(107,110,1),(111,124,2),
         (151,160,2),(161,166,4),(167,172,6),(173,190,8),(191,194,6),(195,198,4),(199,222,2),
         (223,228,4),(229,243,6),(253,268,6),(269,290,8)]   # collines de l'est
pools=[(32,35,0),(58,62,2),(80,100,0),(125,150,2),(64,70,-10),(125,129,-10),(122,170,-28),(244,252,6),(100,112,-46)]
carves=[ # x0,x1,y0(sol),y1(dernière case vide)
 (25,27,-10,-1),                                   # puits A
 (16,40,-10,-6),(41,59,-10,-6),(60,79,-10,-4),(80,99,-10,-7),(102,112,-10,-4),(113,150,-10,-6),(151,169,-12,-3),  # galeries
 (104,106,-28,-11),                                # puits B
 (170,173,-12,-7),(174,177,-10,-5),(178,181,-8,-3),(182,185,-6,-1),(186,189,-4,1),(190,193,-2,3),(194,197,0,3),(198,198,2,3), # escalier
 (86,120,-28,-20),(59,85,-28,-23),(42,58,-28,-21),(121,171,-28,-16),(172,185,-28,-22),(186,248,-30,-12),  # profondeurs
 (249,281,-30,-25),(278,280,-29,7),                # tunnel et puits de l'est (2e accès au donjon)
 (158,160,-15,-13),                                # raccourci galerie -> lac souterrain
 (78,80,-45,-29),(182,184,-38,-29),(40,92,-46,-40),(93,140,-46,-42),(141,186,-46,-39)]  # galerie des racines
adds=[ # blocs pleins : îlots, piliers
 (52,54,2,3),(56,58,5,5),(61,63,7,7),(66,69,9,9),(72,77,11,11),
 (140,142,-2,3),
 (125,127,-32,-27),(131,133,-32,-26),(137,139,-32,-25),(155,157,-32,-26),(161,163,-32,-27),(166,168,-32,-28),
 (81,84,13,13),(88,90,15,15),(94,97,16,16),(101,104,17,17),(108,124,17,18),(129,131,15,15),(135,137,13,13),(141,143,10,10),  # route des cimes
 (60,62,-46,-45),(150,152,-46,-45),(164,166,-40,-39),(128,129,-43,-42)]  # racines : bosses et stalactites
planks=[(25,27,t) for t in (0,-2,-4,-6,-8)]+[(104,106,t) for t in range(-10,-27,-2)]\
      +[(278,280,t) for t in range(8,-29,-2)]+[(158,160,t) for t in range(-12,-25,-2)]\
      +[(78,80,t) for t in range(-28,-45,-2)]+[(182,184,t) for t in range(-28,-45,-2)]
pads=[(60.5,2,0),(82.5,0,0),(86.5,0,1),(90.5,0,0),(94.5,0,1),(98.5,0,0),(127.5,2,0),(147.5,2,1),(67.5,-10,0),(246.5,6,0),(250.5,6,1),(103.5,-46,0),(108.5,-46,1)]
platforms=[(131,1.95,137.5,1.95,2.6),(142,-24.0,151,-24.0,3.0)]
statues=[(12.5,0,"etang_1"),(34.5,-10,"etang_2"),(181.5,8,"etang_3"),(112.5,-28,"etang_4"),(179.5,-28,"etang_5"),(110.5,19,"etang_6"),(264.5,6,"etang_7"),(120.5,-46,"etang_8")]
shards=[(75.5,12.9,"canopee"),(48.5,-27.1,"crypte"),(122.5,19.9,"nuages"),(41.5,-45.1,"racines")]
checkpoints=[(20,0),(45,1),(63,2),(75,0),(101,0),(111,2),(124,2),(151,2),(173,8),(199,2),
             (18,-10),(41,-10),(60,-10),(81,-10),(102,-10),(113,-10),(151,-12),(170,-12),
             (88,-28),(116,-28),(60,-28),(172,-28),(187,-30),
             (82,14),(109,19),(130,16),(215,2),(225,4),(238,6),(254,6),(271,8),(250,-30),(275,-30),
             (80,-46),(98,-46),(114,-46),(140,-46),(183,-46),(45,-46)]
hints=[(2,12,0,"Bienvenue dans l'Étang Acide. Ne tombe pas dans l'acide vert !\\nExplore : des passages mènent sous terre et vers les hauteurs."),
       (20,30,0,"Des planches recouvrent un puits. S ou ↓ pour descendre."),
       (46,51,1,"Des rochers en surplomb mènent vers les hauteurs...\\nRelâche Z plus tôt pour faire un petit saut."),
       (74,79,0,"Les nénuphars bruns s'enfoncent quand on reste dessus : ne traîne pas !"),
       (118,124,2,"Les plateformes de pierre se déplacent : saute au bon moment."),
       (100,103,-10,"Le puits continue vers les profondeurs..."),
       (60,70,-28,"Un mur de cristal bloque le passage... Ta magie devrait en venir à bout."),
       (114,121,-28,"Un lac d'acide. Les piliers de pierre permettent de le traverser.")]
slimes=[(66,2,1),(104,0,0),(118,2,1),(156,2,0),(205,2,1),(209,2,0),
        (48,-10,0),(76,-10,1),(90,-10,0),(135,-10,0),(155,-12,1),(161,-12,0),(166,-12,1),
        (95,-28,0),(110,-28,1),(75,-28,0),(195,-30,1),(206,-30,0),(214,-30,0),
        (114,19,0),(218,2,0),(232,6,1),(258,6,0),(284,8,1),(255,-30,1),(268,-30,0),
        (55,-46,1),(88,-46,0),(125,-46,1),(160,-46,0),(175,-46,1)]
mosq=[("Moustique",70,12,[(64,10.5),(68,12),(72,13.5),(76,14),(70,13)],1.3,0),
      ("Moustique",90,1.5,[(84,1.2),(87,2.2),(90,1.4),(93,2.3),(96,1.3)],1.4,0),
      ("Moustique piqueur",138,6,[(130,5),(135,6.5),(140,6),(145,7),(149,5.5)],1.2,1),
      ("Moustique",182,11,[(176,10.5),(180,11.5),(184,10.8),(188,11.6)],1.4,0),
      ("Moustique",70,-6,[(62,-7),(66,-5.5),(70,-6.5),(74,-5),(78,-6.5)],1.3,0),
      ("Moustique piqueur",160,-6,[(153,-7),(158,-5),(163,-6),(167,-4.5)],1.2,1),
      ("Moustique piqueur",140,-19.5,[(130,-20),(136,-18.5),(142,-19.5),(148,-18),(154,-19.5)],1.2,1),
      ("Moustique",160,-22,[(156,-21),(161,-22.5),(166,-21.5)],1.3,0),
      ("Moustique",96,-23.5,[(90,-24),(95,-22.5),(100,-24),(105,-22.5)],1.3,0),
      ("Moustique",205,-24,[(198,-25),(203,-22),(208,-24),(213,-21)],1.4,0),
      ("Moustique piqueur",222,-20,[(216,-22),(221,-19),(226,-21)],1.2,1),
      ("Moustique piqueur",116,22,[(110,21.5),(114,23),(118,22),(122,23.5)],1.2,1),
      ("Moustique",92,19,[(86,18),(90,19.5),(95,20),(99,21)],1.4,0),
      ("Moustique piqueur",248,11,[(242,10),(247,12),(252,11),(256,12.5)],1.2,1),
      ("Moustique",262,-27,[(256,-28),(262,-26.5),(268,-27.5)],1.3,0),
      ("Moustique",70,-42,[(64,-43),(70,-41.5),(76,-42.5)],1.3,0),
      ("Moustique piqueur",106,-42,[(101,-43),(106,-41),(111,-43)],1.2,1),
      ("Moustique",160,-41,[(152,-42),(158,-40.5),(165,-42),(172,-41)],1.4,0)]
crystal_walls=[(59.5,-28,6,6)]
facade=(230,-30)

def surf(x):
    if x<0: return 16
    if x>SURF_END: return Y1+1
    for a,b,h in pools:
        if a<=x<=b and h>=-1: return h   # bassins de surface : le bord compte comme surface
    for a,b,h in surface:
        if a<=x<=b: return h
    raise Exception("trou de surface en x=%d"%x)
def build():
    W=X1-X0+1; Hh=Y1-Y0+1
    g=[[True]*Hh for _ in range(W)]
    for x in range(X0,X1+1):
        s=surf(x)
        for y in range(max(s,Y0),Y1+1): g[x-X0][y-Y0]=False
    for a,b,h in pools:
        for x in range(a,b+1):
            for y in range(h-4,h): g[x-X0][y-Y0]=False
    for a,b,y0,y1 in carves:
        for x in range(a,b+1):
            for y in range(y0,y1+1): g[x-X0][y-Y0]=False
    for a,b,y0,y1 in adds:
        for x in range(a,b+1):
            for y in range(y0,y1+1): g[x-X0][y-Y0]=True
    # scellement : fond et bords de chaque bassin toujours pleins (jamais d'acide suspendu)
    for a,b,h in pools:
        for x in range(a-1,b+2): g[x-X0][h-5-Y0]=True
        for y in range(h-5,h): g[a-1-X0][y-Y0]=True; g[b+1-X0][y-Y0]=True
    return g

def emit_cs(path):
    def arr(name, rows, fmt):
        return "    static readonly %s %s =\n    {\n        %s\n    };\n" % (fmt[0], name, ",\n        ".join(fmt[1](r) for r in rows))
    f=lambda v: ("%gf" % v)
    out=["// FICHIER GÉNÉRÉ (outil pond_layout.py) : données de la carte de l'Étang Acide.",
         "public static partial class TutorialSceneBuilder","{",
         "    const int PX0 = %d, PX1 = %d, PY0 = %d, PY1 = %d, PSurfEnd = %d;" % (X0,X1,Y0,Y1,SURF_END)]
    out.append(arr("PSurface", surface, ("int[,]", lambda r: "{ %d, %d, %d }" % r)))
    out.append(arr("PPools", pools, ("int[,]", lambda r: "{ %d, %d, %d }" % r)))
    out.append(arr("PCarves", carves, ("int[,]", lambda r: "{ %d, %d, %d, %d }" % r)))
    out.append(arr("PAdds", adds, ("int[,]", lambda r: "{ %d, %d, %d, %d }" % r)))
    out.append(arr("PPlanks", planks, ("int[,]", lambda r: "{ %d, %d, %d }" % r)))
    out.append(arr("PPads", pads, ("float[,]", lambda r: "{ %s, %s, %s }" % (f(r[0]),f(r[1]),f(r[2])))))
    out.append(arr("PPlatforms", platforms, ("float[,]", lambda r: "{ %s }" % ", ".join(f(v) for v in r))))
    out.append(arr("PStatues", statues, ("object[][]", lambda r: "new object[] { %sf, %d, \"%s\" }" % (r[0],r[1],r[2]))).replace("object[][] PStatues =\n    {","object[][] PStatues =\n    {"))
    out.append(arr("PShards", shards, ("object[][]", lambda r: "new object[] { %sf, %sf, \"%s\" }" % (r[0],r[1],r[2]))))
    out.append(arr("PCheckpoints", checkpoints, ("int[,]", lambda r: "{ %d, %d }" % r)))
    out.append(arr("PHints", hints, ("object[][]", lambda r: "new object[] { %d, %d, %d, \"%s\" }" % r)))
    out.append(arr("PSlimes", slimes, ("int[,]", lambda r: "{ %d, %d, %d }" % r)))
    out.append(arr("PMosquitoes", mosq, ("object[][]", lambda r: "new object[] { \"%s\", %sf, %sf, new[] { %s }, %sf, %s }" % (r[0], r[1], r[2], ", ".join("new UnityEngine.Vector2(%sf, %sf)" % p for p in r[3]), r[4], "true" if r[5] else "false"))))
    out.append(arr("PCrystalWalls", crystal_walls, ("float[,]", lambda r: "{ %s }" % ", ".join(f(v) for v in r))))
    out.append("    static readonly UnityEngine.Vector2 PFacade = new UnityEngine.Vector2(%sf, %sf);" % facade)
    out.append("}")
    open(path,"w",encoding="utf-8").write("\n".join(out)+"\n")

if __name__=="__main__":
    import sys
    build()
    emit_cs(sys.argv[1])
    print("ok")
