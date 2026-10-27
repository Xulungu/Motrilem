using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Le Marécage Acide : zone en monde ouvert (metroidvania), 27 salles réparties en 5 régions.
//  Les berges contaminées, Les racines noyées, Les ruines englouties, Les hauteurs infestées, Le cœur du marécage.
// La carte (grille de cases, objets, ennemis...) est dans SwampLayout.cs, généré par l'outil de conception (world.py).
public static partial class TutorialSceneBuilder
{
    // ================================================================== grille générique (marécage et donjon)
    static int gX0, gY0, gX1, gY1;
    static bool[,] gSolid, gBack, gPlank;

    static void GridInit(int x0, int y0, int x1, int y1)
    {
        gX0 = x0; gY0 = y0; gX1 = x1; gY1 = y1;
        gSolid = new bool[x1 - x0 + 1, y1 - y0 + 1];
        gBack = new bool[x1 - x0 + 1, y1 - y0 + 1];
        gPlank = new bool[x1 - x0 + 1, y1 - y0 + 1];
    }

    static bool GSolid(int x, int y)
    {
        if (x < gX0 || x > gX1 || y < gY0 || y > gY1) return true;
        return gSolid[x - gX0, y - gY0];
    }

    static void GSet(int x, int y, bool solid)
    {
        if (x < gX0 || x > gX1 || y < gY0 || y > gY1) return;
        gSolid[x - gX0, y - gY0] = solid;
    }

    static bool GBack(int x, int y)
    {
        if (x < gX0 || x > gX1 || y < gY0 || y > gY1) return false;
        return gBack[x - gX0, y - gY0];
    }

    /// <summary>Choix de la tuile de terrain selon les voisins (haut, bas, côtés, remplissage).</summary>
    static int GTileIndex(int x, int y)
    {
        bool up = !GSolid(x, y + 1), down = !GSolid(x, y - 1), left = !GSolid(x - 1, y), right = !GSolid(x + 1, y);
        if (up) return left ? 66 : right ? 68 : 67;
        if (down) return left ? 90 : right ? 92 : 91;
        if (left) return 78;
        if (right) return 80;
        return 79;
    }

    /// <summary>Faut-il un fond de caverne derrière cette case ? Oui dans les cavernes, et derrière les murs qui les bordent.</summary>
    static bool GNeedsBack(int x, int y)
    {
        if (GBack(x, y)) return true;
        if (!GSolid(x, y)) return false;
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if ((dx != 0 || dy != 0) && GBack(x + dx, y + dy)) return true;
        return false;
    }

    static void LoadSwampGrid()
    {
        GridInit(SX0, SY0, SX1, SY1);
        for (int r = 0; r < SwRows.Length; r++)
        {
            int y = SY1 - r;
            string row = SwRows[r];
            for (int i = 0; i < row.Length; i++)
            {
                int x = SX0 + i;
                switch (row[i])
                {
                    case '#': gSolid[x - gX0, y - gY0] = true; break;
                    case ',': gBack[x - gX0, y - gY0] = true; break;
                    case '=': gBack[x - gX0, y - gY0] = true; gPlank[x - gX0, y - gY0] = true; break;
                    case '-': gPlank[x - gX0, y - gY0] = true; break;
                }
            }
        }
    }

    // ================================================================== régions : teintes et profondeur
    static int SwRegion(int x, int y)
    {
        if (x >= 335 && y >= -5 && y <= 40) return 5;          // l'intérieur du grand arbre (Terrier du Gluant)
        if (x >= 252 && y <= -20) return 4;
        if (y <= -38 && x >= 90 && x <= 255) return 2;
        if (y <= -8 && y > -38 && x >= 10 && x <= 252) return 1;
        if (x >= 136 && y >= -20) return 3;
        return 0;
    }

    static readonly Color[] SwTints =
    {
        new Color(1f, 1f, 1f),            // berges
        new Color(0.78f, 0.86f, 0.80f),   // racines
        new Color(0.76f, 0.80f, 0.96f),   // ruines
        new Color(1f, 0.96f, 0.83f),      // hauteurs
        new Color(0.78f, 0.98f, 0.64f),   // cœur du marécage
        new Color(0.95f, 0.88f, 0.78f),   // intérieur de l'arbre (bois)
    };

    static Color SwTint(int x, int y, float brightness)
    {
        Color c = SwTints[SwRegion(x, y)];
        float depth = y < 0 ? Mathf.Lerp(1f, 0.6f, Mathf.Clamp01(-y / 66f)) : 1f;
        return new Color(c.r * depth * brightness, c.g * depth * brightness, c.b * depth * brightness, 1f);
    }

    // ================================================================== construction
    // ================================================================== le marécage est découpé en cartes (une scène par groupe de zones)
    // Comme dans Hollow Knight : chaque carte regroupe des zones voisines et on passe de l'une à l'autre par un couloir, un puits
    // ou un pont (cf. portails dans portals.py). Chaque carte recouvre sa voisine sur quelques cases pour que le passage soit invisible.
    // Les objets n'existent que dans la carte où ils se trouvent ; les monstres vaincus réapparaissent au rechargement d'une carte.
    static string[] SwGroupNames { get { return SwampZones.Names; } }
    const int SwMargin = 20;
    const int SwDoorGroup = 3;                         // la porte du donjon est sur la carte du cœur du marécage
    static readonly Vector3[] SwSpawns =               // départ de chaque carte (utile seulement pour tester une carte seule)
    {
        new Vector3(3.5f, 0.8f, 0f), new Vector3(98.5f, -49.2f, 0f), new Vector3(146.5f, 0.8f, 0f), new Vector3(286.5f, 30.8f, 0f), new Vector3(SwG4EntryX, SwG4EntryY + 0.8f, 0f)
    };
    static readonly string[] SwBaseMusic = { "musique_etang", "musique_ruines", "musique_hauteurs", "musique_coeur", "musique_racines" };
    static int swGroup;
    static bool[,] swMask;                             // cases construites dans la carte en cours (zone + marge de recouvrement)
    static int swMinX, swMaxX, swMinY, swMaxY;         // emprise de la carte en cours

    static string SwScenePath(int g) { return "Assets/Scenes/" + SwGroupNames[g] + ".unity"; }
    static bool Own(float x, float y) { return SwampZones.GroupOf(x, y) == swGroup; }
    static bool InMask(float x, float y)
    {
        int cx = Mathf.FloorToInt(x) - SX0, cy = Mathf.FloorToInt(y) - SY0;
        if (swMask == null || cx < 0 || cy < 0 || cx >= swMask.GetLength(0) || cy >= swMask.GetLength(1)) return false;
        return swMask[cx, cy];
    }

    /// <summary>Calcule les cases de la carte 'group' : ses propres cases, élargies de SwMargin cases (recouvrement avec les cartes voisines).</summary>
    static void ComputeSwMask(int group)
    {
        int w = SX1 - SX0 + 1, h = SY1 - SY0 + 1;
        bool[,] own = new bool[w, h];
        for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) own[x, y] = SwampZones.GroupOf(x + SX0, y + SY0) == group;
        // dilatation séparable (carré de côté 2*SwMargin+1)
        bool[,] tmp = new bool[w, h];
        for (int y = 0; y < h; y++)
        {
            int last = -999999;
            for (int x = 0; x < w; x++) { if (own[x, y]) last = x; tmp[x, y] = x - last <= SwMargin; }
            last = 999999;
            for (int x = w - 1; x >= 0; x--) { if (own[x, y]) last = x; if (last - x <= SwMargin) tmp[x, y] = true; }
        }
        swMask = new bool[w, h];
        for (int x = 0; x < w; x++)
        {
            int last = -999999;
            for (int y = 0; y < h; y++) { if (tmp[x, y]) last = y; swMask[x, y] = y - last <= SwMargin; }
            last = 999999;
            for (int y = h - 1; y >= 0; y--) { if (tmp[x, y]) last = y; if (last - y <= SwMargin) swMask[x, y] = true; }
        }
        swMinX = int.MaxValue; swMinY = int.MaxValue; swMaxX = int.MinValue; swMaxY = int.MinValue;
        for (int x = 0; x < w; x++) for (int y = 0; y < h; y++)
            if (swMask[x, y]) { swMinX = Mathf.Min(swMinX, x + SX0); swMaxX = Mathf.Max(swMaxX, x + SX0); swMinY = Mathf.Min(swMinY, y + SY0); swMaxY = Mathf.Max(swMaxY, y + SY0); }
    }

    static void SwMusicZone(GameObject parent, float x0, float x1, float y0, float y1, string music, float volume)
    {
        if (x1 >= swMinX && x0 <= swMaxX && y1 >= swMinY && y0 <= swMaxY) AddMusicZone(parent, x0, x1, y0, y1, music, volume);
    }

    static void BuildPond()
    {
        // anciennes cartes (découpage en bandes verticales) : supprimées
        foreach (string old in new[] { "Assets/Scenes/EtangAcide2.unity", "Assets/Scenes/EtangAcide3.unity" })
            if (File.Exists(old)) AssetDatabase.DeleteAsset(old);
        for (int g = 0; g < SwGroupNames.Length; g++)
        {
            EditorUtility.DisplayProgressBar("Motrilem", "Le Marécage Acide (carte " + (g + 1) + "/" + SwGroupNames.Length + ")...", 0.7f + 0.1f * g / SwGroupNames.Length);
            BuildSwampZone(g);
        }
        swGroup = 0; swMask = null;
    }

    static void BuildSwampZone(int group)
    {
        swGroup = group; ComputeSwMask(group);
        buildingTutorial = false;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        tileCache.Clear();
        LoadSwampGrid();
        System.Random rng = new System.Random(77);

        // ---- tuiles : fond des cavernes, sol, décor, planches
        GameObject gridGO = new GameObject("Grid");
        gridGO.AddComponent<Grid>();
        BuildGridTiles(gridGO, rng, SwTint, group == 4 ? new Color(0.5f, 0.42f, 0.38f) : new Color(0.62f, 0.62f, 0.72f));
        if (group == 4) PlaceTreeInterior(gridGO, rng);

        // ---- joueur, caméra, ciel
        GameObject player = PlacePlayer(SwSpawns[group], true);
        EnableSafeRespawn(player);
        PlaceCamera(player, new Color(0.36f, 0.68f, 0.66f));
        GameObject outside = new GameObject("Décor extérieur");
        GameObject exit = new GameObject("Sortie de la grotte");
        exit.transform.SetParent(outside.transform, false);
        exit.transform.position = new Vector3(0.6f, 1.5f, 0f);
        Child(exit.transform, "Entrée", Art("entree_grotte"), Vector3.zero, 3, Color.white, null);
        SpriteRenderer sun = Child(exit.transform, "Rayons de soleil", Art("rayons"), new Vector3(3f, 4f, 0f), 4, new Color(1f, 0.95f, 0.75f, 0.12f), additive);
        sun.transform.localScale = new Vector3(5f, 5f, 1f);
        sun.transform.rotation = Quaternion.Euler(0, 0, 20f);
        if (group != 0) Object.DestroyImmediate(exit);   // la sortie de la grotte n'existe que sur la première carte

        // Fond de forêt : il suit la caméra (on ne voit jamais son bord, même en sautant)
        GameObject sky = new GameObject("Ciel et forêt");
        sky.transform.SetParent(outside.transform, false);
        float cx = (swMinX + swMaxX) * 0.5f;
        AddParallaxFollow(sky, "BACKGROUND", 1f, -100, cx, 0.5f, 2.2f, new Color(0.85f, 1f, 0.9f));
        AddParallaxFollow(sky, "WOODS - Fourth", 0.85f, -90, cx, 0.3f, 1.6f, new Color(0.8f, 0.95f, 0.8f));
        AddParallaxFollow(sky, "WOODS - Third", 0.7f, -80, cx, 0f, 1.6f, new Color(0.75f, 0.92f, 0.75f));
        AddParallaxFollow(sky, "VINES - Second", 0.6f, -75, cx, 1.5f, 1.6f, new Color(0.7f, 0.9f, 0.7f));
        AddParallaxFollow(sky, "WOODS - Second", 0.5f, -70, cx, -0.3f, 1.6f, new Color(0.65f, 0.85f, 0.65f));
        AddParallaxFollow(sky, "BUSH - BACKGROUND", 0.3f, -60, cx, -1.2f, 1.6f, new Color(0.6f, 0.85f, 0.55f));

        // ---- acide, nénuphars, plateformes mobiles
        GameObject level = new GameObject("Marécage");
        for (int i = 0; i < SwPools.GetLength(0); i++)
        {
            // seule la partie du bassin qui se trouve dans cette carte (zone + marge) est construite
            int pa = int.MaxValue, pb = int.MinValue;
            for (int px = SwPools[i, 0]; px <= SwPools[i, 1]; px++)
                for (int py = SwPools[i, 2]; py <= SwPools[i, 3]; py++)
                    if (InMask(px, py)) { pa = Mathf.Min(pa, px); pb = Mathf.Max(pb, px); break; }
            if (pb < pa) continue;
            PlaceSwampPool(level, pa, pb, SwPools[i, 2], SwPools[i, 3], SwPools[i, 4] == 1, rng);
        }
        for (int i = 0; i < SwPads.GetLength(0); i++)
            if (Own(SwPads[i, 0], SwPads[i, 1])) PlacePad(level, SwPads[i, 0], (int)SwPads[i, 1], SwPads[i, 2] > 0.5f);
        for (int i = 0; i < SwMovers.GetLength(0); i++)
            if (Own(SwMovers[i, 0], SwMovers[i, 1]))
                PlaceMovingPlatform(level, new Vector3(SwMovers[i, 0], SwMovers[i, 1], 0f), new Vector3(SwMovers[i, 2], SwMovers[i, 3], 0f), SwMovers[i, 4]);
        // le monte-charge : il relie la falaise (en haut) à la plaine putride (en bas)
        if (Own(SwElevX, SwElevBottom))
        {
            MovingPlatform lift = PlaceMovingPlatform(level, new Vector3(SwElevX, SwElevBottom, 0f), new Vector3(SwElevX, SwElevTop, 0f), 6f);
            lift.name = "Monte-charge";
            lift.pause = 2.5f;
        }
        // les ascenseurs des grands puits (remplacent les longues échelles de planches)
        // (un ascenseur qui traverse une frontière de carte existe dans les deux cartes ; son état est repris au passage)
        for (int li = 0; li < SwLifts.Length; li++)
        {
            float[] l = SwLifts[li];
            bool crossing = false;
            int g0 = SwampZones.GroupOf(l[0], l[1]);
            for (int k = 2; k < l.Length; k++) if (SwampZones.GroupOf(l[0], l[k]) != g0) crossing = true;
            bool here = crossing ? (InMask(l[0], l[1]) || InMask(l[0], l[l.Length - 1])) : Own(l[0], l[1]);
            if (here) PlaceLift(level, l, SwLiftNames[li]);
        }

        // ---- trappes, grilles, leviers, murs fissurés, voiles de racines
        GameObject mech = new GameObject("Mécanismes");
        foreach (object[] g in SwGates) if (Own((int)g[1] + (int)g[3] * 0.5f, (int)g[2])) PlaceGate(mech.transform, (int)g[0], (int)g[1], (int)g[2], (int)g[3], (int)g[4], (string)g[5]);
        foreach (object[] l in SwLevers) if (Own((float)l[0], (int)l[1])) PlaceLever(mech.transform, (float)l[0], (int)l[1], (string)l[2]);
        // ce qui se trouve sous une trappe reste caché tant que le raccourci n'est pas ouvert
        foreach (object[] c in SwCovers) if (Own(((int)c[1] + (int)c[2]) * 0.5f, ((int)c[3] + (int)c[4]) * 0.5f)) PlaceGateCover(mech.transform, (string)c[0], (int)c[1], (int)c[2], (int)c[3], (int)c[4], (int)c[5], (float)c[6]);
        foreach (object[] c in SwCracks) if (Own((int)c[0], (int)c[1])) PlaceCrack(mech.transform, (int)c[0], (int)c[1], (int)c[2], (string)c[3]);
        foreach (object[] v in SwVeils) if (Own(((int)v[0] + (int)v[1]) * 0.5f, (int)v[2])) PlaceVeil(mech.transform, (int)v[0], (int)v[1], (int)v[2], (int)v[3], (string)v[4], rng);

        // ---- ennemis
        GameObject slimes = new GameObject("Slimes");
        for (int i = 0; i < SwSlimes.GetLength(0); i++)
            if (Own(SwSlimes[i, 0], SwSlimes[i, 1]))
                Slime.Create(new Vector3(SwSlimes[i, 0] + 0.5f, SwSlimes[i, 1] + 0.6f, 0f), SwSlimes[i, 2] == 1, slimes.transform);
        foreach (object[] m in SwMosquitoes)
            if (Own((float)m[1], (float)m[2]))
            PlaceMosquito((string)m[0], new Vector2((float)m[1], (float)m[2]), (Vector2[])m[3], (float)m[4], (bool)m[5]);

        // ---- statues de prière, urnes, éclats de vie
        GameObject story2 = new GameObject("Urnes");
        foreach (object[] s in SwStatues) if (Own((float)s[0], (int)s[1])) PlaceStatue(new Vector3((float)s[0], (int)s[1], 0f), (string)s[2]);
        for (int i = 0; i < SwUrns.GetLength(0); i++) if (Own(SwUrns[i, 0], SwUrns[i, 1])) PlaceUrn(story2, SwUrns[i, 0], (int)SwUrns[i, 1]);
        foreach (object[] s in SwShards) if ((string)s[2] != "marais_gluant" && Own((float)s[0], (float)s[1])) PlaceShard(new Vector3((float)s[0], (float)s[1], 0f), (string)s[2]);

        // ---- zones réservées aux compétences futures : visibles mais hors de portée pour l'instant.
        //      Chaque récompense est déjà en place (cristal flottant) ; on branchera l'inventaire plus tard.
        GameObject future = new GameObject("Zones réservées");
        foreach (object[] f in SwFuture)
            if (Own((float)f[0], (float)f[1]))
            {
                RewardPickup r = RewardPickup.Create(new Vector3((float)f[0], (float)f[1], 0f), (string)f[2], (string)f[5], future.transform);
                if (r != null) r.name = "Récompense (" + (string)f[4] + ") : " + (string)f[3];
            }

        // ---- indices d'histoire, panneaux, carte, gouttes d'acide, titres de région
        GameObject story = new GameObject("Indices");
        foreach (object[] l in SwLore) if (Own((float)l[0], (int)l[1])) PlaceLore(story.transform, (float)l[0], (int)l[1], (string)l[2], (string)l[3], (string[])l[4]);
        foreach (object[] s in SwSigns) if (Own((int)s[0], (int)s[1])) PlaceSwampSign(story.transform, (int)s[0], (int)s[1], (string)s[2]);
        GameObject ambience = new GameObject("Ambiance");
        for (int i = 0; i < SwDrips.GetLength(0); i++)
        {
            if (!Own(SwDrips[i, 0], SwDrips[i, 1])) continue;
            GameObject drip = new GameObject("Goutte d'acide");
            drip.transform.SetParent(ambience.transform, false);
            drip.transform.position = new Vector3(SwDrips[i, 0], SwDrips[i, 1], 0f);
            drip.AddComponent<AcidDrip>().interval = 2f + (i % 4) * 0.6f;
        }
        if (Own(103f, -26f)) AddHalo(ambience, "Lueur du nœud", new Vector3(103f, -26f, 0f), new Vector2(8f, 5f), new Color(0.45f, 0.8f, 1f, 0.22f));
        if (Own(155f, -26f)) AddHalo(ambience, "Lueur de la cuve", new Vector3(155f, -26f, 0f), new Vector2(10f, 5f), new Color(0.55f, 1f, 0.4f, 0.2f));
        if (InMask(146f, -44f)) AddHalo(ambience, "Lueur des colonnes", new Vector3(146f, -44f, 0f), new Vector2(14f, 6f), new Color(0.5f, 1f, 0.45f, 0.18f));
        if (Own(182f, -44f)) AddHalo(ambience, "Lueur du scriptorium", new Vector3(182f, -44f, 0f), new Vector2(9f, 5f), new Color(1f, 0.85f, 0.55f, 0.22f));
        if (Own(208f, -50f)) AddHalo(ambience, "Lueur de la citerne", new Vector3(208f, -50f, 0f), new Vector2(16f, 8f), new Color(0.5f, 1f, 0.4f, 0.16f));
        if (Own(273f, -40f)) AddHalo(ambience, "Lueur de la plaine", new Vector3(273f, -40f, 0f), new Vector2(22f, 8f), new Color(0.55f, 1f, 0.3f, 0.2f));
        if (Own(318f, -38f)) AddHalo(ambience, "Lueur du parvis", new Vector3(318f, -38f, 0f), new Vector2(12f, 8f), new Color(1f, 0.45f, 0.3f, 0.16f));
        GameObject zones = new GameObject("Zones");
        AddFallZone(zones, (swMinX + swMaxX) * 0.5f, swMinY - 8f, swMaxX - swMinX + 80f);

        // ---- musiques par région (fondu enchaîné à l'entrée de chaque zone)
        GameObject musicZones = new GameObject("Musiques");
        SwMusicZone(musicZones, 0f, 144f, -1f, 40f, "musique_etang", 0.5f);
        SwMusicZone(musicZones, 86f, 252f, -38f, -1f, "musique_racines", 0.5f);
        SwMusicZone(musicZones, 90f, 252f, -66f, -38f, "musique_ruines", 0.5f);
        SwMusicZone(musicZones, 144f, 290f, 19f, 50f, "musique_hauteurs", 0.5f);
        SwMusicZone(musicZones, 252f, 334f, -52f, -20f, "musique_coeur", 0.5f);

        // ---- entrée du donjon (la suite de la surface, sur la falaise, est barrée par un lac d'acide : cf. panneau)
        if (Own(SwDoorX, SwDoorY)) PlaceDungeonEntrance(new Vector3(SwDoorX, SwDoorY, 0f));

        // ---- passages vers les cartes voisines (même endroit du monde, fondu)
        GameObject links = new GameObject("Passages entre les cartes");
        foreach (object[] pt in SwPortals)
        {
            int ga = (int)pt[2], gb = (int)pt[3];
            float[] ra = (float[])pt[4], rb = (float[])pt[5], sa = (float[])pt[6], sb = (float[])pt[7];
            int axisP = (int)pt[1];
            int dirP = axisP == 0 ? (ra[0] > rb[0] ? 1 : -1) : (ra[2] > rb[2] ? 1 : -1);   // sens A -> B
            if (ga == group) PlaceZoneTransition(links.transform, (string)pt[0] + " vers " + SwGroupNames[gb], axisP, ra, dirP, SwGroupNames[gb], new Vector2(sb[0], sb[1]), (string)pt[8]);
            if (gb == group) PlaceZoneTransition(links.transform, (string)pt[0] + " vers " + SwGroupNames[ga], axisP, rb, -dirP, SwGroupNames[ga], new Vector2(sa[0], sa[1]), (string)pt[8]);
        }
        // le trou du belvédère des Cimes mène au Terrier du Gluant (et inversement)
        if (group == 2) PlaceTreeEntrance(links.transform, new Vector3(SwTerrierX, SwTerrierY, 0f), SwGroupNames[4], new Vector2(SwG4EntryX, SwG4EntryY));
        if (group == 4) BuildGluantArea(level, mech.transform, links.transform, slimes.transform, rng);

        // ---- frontières entre les cartes : voile opaque au-delà (on ne voit pas de l'autre côté), brume et lueur sur les passages
        PlaceBorderVeil(group);
        foreach (object[] pt in SwPortals)
        {
            int ga = (int)pt[2], gb = (int)pt[3], axis = (int)pt[1];
            float[] ra = (float[])pt[4], rb = (float[])pt[5];
            int dirA = axis == 0 ? (ra[0] > rb[0] ? 1 : -1) : (ra[2] > rb[2] ? 1 : -1);   // sens A -> B
            if (ga == group) PlacePortalMist(links.transform, (string)pt[0], axis, ra, dirA);
            if (gb == group) PlacePortalMist(links.transform, (string)pt[0], axis, rb, -dirA);
        }
        new GameObject("Noms des zones").AddComponent<ZoneBanner>();

        // ---- intro, musique
        GameObject intro = new GameObject("Intro");
        TutorialIntro ti = intro.AddComponent<TutorialIntro>();
        ti.player = player.GetComponent<PlayerMovement>();
        ti.fadeFrom = new Color(1f, 0.98f, 0.92f, 1f);
        ti.fadeDuration = 2.5f;
        ti.lines = new[]
        {
            "L'air frais... le soleil... Ça faisait si longtemps.",
            "Mais cette odeur... De l'acide ? Quelque chose ronge ces marais.",
            "Quelque part dans ce monde, les pages du livre m'attendent. Et avec elles, mon nom."
        };
        ti.zoneTitle = "Le Marécage Acide";
        ti.zoneSubtitle = "Les terres rongées";
        if (group != 0) Object.DestroyImmediate(intro);   // l'intro n'est jouée que sur la première carte
        AddMusic(SwBaseMusic[group], 0.5f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SwScenePath(group));
    }

    // ================================================================== tuiles à partir de la grille (marécage et donjon)
    static void BuildGridTiles(GameObject gridGO, System.Random rng, System.Func<int, int, float, Color> tint, Color backColor)
    {
        Tilemap back = CreateTilemap("Fond des cavernes", gridGO.transform, -10, false, backColor);
        Tilemap ground = CreateTilemap("Sol", gridGO.transform, 0, true, Color.white);
        Tilemap decor = CreateTilemap("Décor", gridGO.transform, 1, false, Color.white);
        Tilemap planks = CreateTilemap("Planches", gridGO.transform, 2, false, Color.white);
        TilemapCollider2D pc = planks.gameObject.AddComponent<TilemapCollider2D>();
        pc.usedByEffector = true;
        PlatformEffector2D eff = planks.gameObject.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.surfaceArc = 160f;

        int[] backVariants = { 27, 27, 27, 27, 23, 49, 87 };
        int[] floorDeco = { 81, 82, 93, 94, 53, 53, 83, 95 };
        int[] ceilDeco = { 76, 77, 88, 89 };
        List<Vector3Int> gPos = new List<Vector3Int>(), bPos = new List<Vector3Int>();
        List<TileBase> gTiles = new List<TileBase>(), bTiles = new List<TileBase>();
        List<Color> gCols = new List<Color>(), bCols = new List<Color>();
        for (int x = gX0; x <= gX1; x++)
        {
            for (int y = gY0; y <= gY1; y++)
            {
                if (swMask != null && !InMask(x, y)) continue;
                bool solid = GSolid(x, y);
                if (GNeedsBack(x, y))
                {
                    bPos.Add(new Vector3Int(x, y, 0));
                    bTiles.Add(SheetTile(backVariants[rng.Next(backVariants.Length)]));
                    bCols.Add(tint(x, y, 0.9f));
                }
                if (solid)
                {
                    gPos.Add(new Vector3Int(x, y, 0));
                    gTiles.Add(SheetTile(GTileIndex(x, y)));
                    gCols.Add(tint(x, y, 1f));
                }
                else if (GBack(x, y))
                {
                    // vide dans une caverne : un peu de végétation au sol et de stalactites au plafond
                    if (GSolid(x, y - 1) && !GSolid(x, y + 1) && rng.NextDouble() < 0.2)
                        SetDeco(decor, x, y, SheetTile(floorDeco[rng.Next(floorDeco.Length)]), tint(x, y, 1f));
                    else if (GSolid(x, y + 1) && !GSolid(x, y - 1) && rng.NextDouble() < 0.25)
                        SetDeco(decor, x, y, SheetTile(ceilDeco[rng.Next(ceilDeco.Length)]), tint(x, y, 1f));
                }
                else if (GSolid(x, y - 1) && !GSolid(x, y + 1) && x > 2 && rng.NextDouble() < 0.3 && !InSwPool(x, y))
                {
                    SetDeco(decor, x, y, SheetTile(floorDeco[rng.Next(floorDeco.Length)]), tint(x, y, 1f));
                }
            }
        }
        ground.SetTiles(gPos.ToArray(), gTiles.ToArray());
        back.SetTiles(bPos.ToArray(), bTiles.ToArray());
        for (int i = 0; i < gPos.Count; i++) { ground.SetTileFlags(gPos[i], TileFlags.None); ground.SetColor(gPos[i], gCols[i]); }
        for (int i = 0; i < bPos.Count; i++) { back.SetTileFlags(bPos[i], TileFlags.None); back.SetColor(bPos[i], bCols[i]); }
        TileBase plank = PlankTile();
        for (int x = gX0; x <= gX1; x++)
            for (int y = gY0; y <= gY1; y++)
                if (gPlank[x - gX0, y - gY0] && (swMask == null || InMask(x, y)))
                {
                    Vector3Int p = new Vector3Int(x, y, 0);
                    planks.SetTile(p, plank);
                    planks.SetTileFlags(p, TileFlags.None);
                    planks.SetColor(p, tint(x, y, 1f));
                }
    }

    static void SetDeco(Tilemap decor, int x, int y, TileBase tile, Color color)
    {
        Vector3Int p = new Vector3Int(x, y, 0);
        decor.SetTile(p, tile);
        decor.SetTileFlags(p, TileFlags.None);
        decor.SetColor(p, color);
    }

    static bool InSwPool(int x, int y)
    {
        for (int i = 0; i < SwPools.GetLength(0); i++)
            if (x >= SwPools[i, 0] - 1 && x <= SwPools[i, 1] + 1 && y >= SwPools[i, 2] && y <= SwPools[i, 3] + 1) return true;
        return false;
    }

    // ------------------------------------------------------------------ éléments
    static void AddParallaxFollow(GameObject parent, string spriteName, float fx, int order, float x, float offsetY, float scale, Color color)
    {
        Sprite s = Art(spriteName);
        if (s == null) return;
        GameObject go = new GameObject(spriteName);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(x, offsetY, 10f);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.color = color;
        sr.sortingOrder = order;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(s.bounds.size.x * 24f, s.bounds.size.y);
        ParallaxLayer p = go.AddComponent<ParallaxLayer>();
        p.factorX = fx;
        p.followCameraY = true;
        p.offsetY = offsetY;
    }

    static void PlaceSwampPool(GameObject parent, int x0, int x1, int y0, int top, bool harmless, System.Random rng)
    {
        float surface = top - 0.35f;
        float bottom = y0;
        float width = x1 - x0 + 1;
        GameObject go = new GameObject(harmless ? "Bassin d'acide (décor)" : "Bassin d'acide");
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(x0 + width * 0.5f, (surface + bottom) * 0.5f, 0f);
        go.AddComponent<BoxCollider2D>().isTrigger = true;
        AcidPool pool = go.AddComponent<AcidPool>();
        pool.size = new Vector2(width, surface - bottom);
        pool.damage = 25;
        pool.harmless = harmless;
        pool.bodyOrder = -5; pool.surfaceOrder = -4; pool.glowOrder = -3; // derrière les rochers (îlots, piliers)
        if (harmless) return;

        // Roseaux sur les bords des bassins à l'air libre (certains au premier plan)
        foreach (float ex in new[] { x0 - 0.6f, x1 + 1.6f })
        {
            int fx = Mathf.FloorToInt(ex);
            if (GSolid(fx, top) || GBack(fx, top) || width > 40f) continue;
            bool front = rng.NextDouble() < 0.4;
            SpriteRenderer r = Child(go.transform, "Roseaux", Art("roseau_" + rng.Next(3)), Vector3.zero, front ? 10012 : 4, Color.white, null);
            r.transform.position = new Vector3(ex, top + 0.6f, 0f);
        }
    }

    static void PlacePad(GameObject parent, float x, int baseH, bool sinks)
    {
        GameObject go = new GameObject(sinks ? "Nénuphar (coule)" : "Nénuphar");
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(x, baseH - 0.3f, 0f);
        Child(go.transform, "Sprite", Art(sinks ? "nenuphar_brun" : "nenuphar"), Vector3.zero, 9, Color.white, null);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.85f, 0.3f);
        box.offset = new Vector2(0f, 0.08f);
        go.AddComponent<LilyPad>().sinks = sinks;
    }

    static MovingPlatform PlaceMovingPlatform(GameObject parent, Vector3 a, Vector3 b, float travelTime)
    {
        GameObject go = new GameObject("Plateforme mobile");
        go.transform.SetParent(parent.transform, false);
        go.transform.position = a;
        Child(go.transform, "Sprite", Art("plateforme_pierre"), Vector3.zero, 9, Color.white, null);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(2.95f, 0.6f);
        box.offset = new Vector2(0f, -0.02f);
        MovingPlatform mp = go.AddComponent<MovingPlatform>();
        mp.pointA = a;
        mp.pointB = b;
        mp.travelTime = travelTime;
        return mp;
    }

    static void PlaceCrystalWallAt(float x, float floor, float height, int hp)
    {
        GameObject go = new GameObject("Mur de cristal");
        go.transform.position = new Vector3(x, floor + height * 0.5f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Art("cristal");
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(0.875f, height);
        sr.sortingOrder = 9993;
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.85f, height);
        EnemyHealth eh = go.AddComponent<EnemyHealth>();
        eh.maxHealth = hp;
        eh.rootToDestroy = go;
        eh.deathStyle = EnemyHealth.DeathStyle.Eclatement;
        eh.deathSound = "impact";
        eh.countsAsEnemy = false;
    }

    static GameObject PlaceShard(Vector3 pos, string id)
    {
        GameObject go = new GameObject("Éclat de vie");
        go.transform.position = pos;
        SpriteRenderer glow = Child(go.transform, "Lueur", Art("lueur"), Vector3.zero, 10001, new Color(1f, 0.4f, 0.55f, 0.5f), additive);
        glow.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        Child(go.transform, "Sprite", Art("eclat_vie"), Vector3.zero, 10002, Color.white, null);
        CircleCollider2D c = go.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = 0.6f;
        go.AddComponent<HealthShard>().shardId = id;
        return go;
    }

    // ------------------------------------------------------------------ mécanismes
    static void PlaceGate(Transform parent, int kind, int x0, int y0, int w, int h, string flag)
    {
        GameObject go = new GameObject(kind == 0 ? "Trappe" : "Grille");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x0 + w * 0.5f, y0 + h * 0.5f, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(w, h);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Art(kind == 0 ? "tuile_trappe" : "tuile_grille");
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(w, h);
        sr.sortingOrder = 3;
        SwampGate gate = go.AddComponent<SwampGate>();
        gate.flag = flag;
        gate.hatch = kind == 0;
        gate.render = sr;
    }

    static void PlaceLever(Transform parent, float x, int stand, string flag)
    {
        // Le levier est petit et fixé sur le mur le plus proche (jamais en l'air)
        int lx = Mathf.FloorToInt(x);
        int wall = int.MinValue;
        for (int d = 1; d <= 5 && wall == int.MinValue; d++)
        {
            foreach (int c in new[] { lx - d, lx + d })
            {
                if (!(GSolid(c, stand) && GSolid(c, stand + 1))) continue;
                bool clear = true;
                int step = lx < c ? 1 : -1;
                for (int k = lx; k != c; k += step) if (GSolid(k, stand + 1)) { clear = false; break; }
                if (clear) { wall = c; break; }
            }
        }
        GameObject go = new GameObject("Levier");
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Art("levier_0");
        sr.sortingOrder = 9980;
        go.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
        int leverSide = 0;   // côté où le joueur doit se trouver pour actionner (jamais à travers le mur)
        if (wall != int.MinValue)
        {
            bool wallOnRight = lx < wall;
            leverSide = wallOnRight ? -1 : 1;
            float face = wallOnRight ? wall : wall + 1f;          // face visible du mur
            go.transform.position = new Vector3(face + (wallOnRight ? -0.22f : 0.22f), stand + 1.15f, 0f);
            sr.flipX = wallOnRight;
        }
        else go.transform.position = new Vector3(x + 0.5f, stand + 0.5f, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(3.4f, 3.2f);                       // compense l'échelle réduite : même zone d'interaction
        box.offset = new Vector2(leverSide * 0.9f, 0f);            // la zone d'interaction déborde du côté dégagé, pas dans le mur
        Lever lv = go.AddComponent<Lever>();
        lv.flag = flag;
        lv.side = leverSide;
        lv.render = sr;
        lv.offSprite = Art("levier_0");
        lv.onSprite = Art("levier_1");
    }

    static void PlaceZoneTransition(Transform parent, string name, int axis, float[] rect, int headingSign, string targetScene, Vector2 safeGround, string liftId)
    {
        Vector2 h, b;
        PortalGeometry(axis, rect, headingSign, out h, out b);
        GameObject go = new GameObject("Passage : " + name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(b.x, b.y, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = axis == 0 ? new Vector2(1f, rect[3] - rect[2]) : new Vector2(rect[1] - rect[0], 1f);
        ZoneTransition zt = go.AddComponent<ZoneTransition>();
        zt.targetScene = targetScene;
        zt.safeGround = safeGround;
        zt.liftId = liftId;
        zt.heading = h;
        zt.boundary = b;
        zt.halfSpan = (axis == 0 ? rect[3] - rect[2] : rect[1] - rect[0]) * 0.5f;
    }

    /// <summary>
    /// Le voile des frontières : tout ce qui n'appartient pas à la carte (de l'autre côté d'une frontière) est caché par un noir opaque,
    /// et la carte s'assombrit progressivement en s'approchant de la frontière (brume). Le joueur voit donc qu'il atteint la limite
    /// d'une zone, sans jamais voir la zone voisine.
    /// </summary>
    static void PlaceBorderVeil(int group)
    {
        const int Ext = 70;
        const float Soft = 7f;
        int x0 = SX0 - Ext, y0 = SY0 - Ext;
        int w = SX1 - SX0 + 1 + 2 * Ext, h = SY1 - SY0 + 1 + 2 * Ext;
        float[,] d = new float[w, h];
        bool any = false;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                // hors du monde, une case appartient à la même carte que la case la plus proche du monde
                int gx = Mathf.Clamp(x0 + x, SX0, SX1), gy = Mathf.Clamp(y0 + y, SY0, SY1);
                bool veil = SwampZones.GroupOf(gx + 0.5f, gy + 0.5f) != group;
                d[x, y] = veil ? 0f : 9999f;
                if (veil) any = true;
            }
        if (!any) return;                       // une seule carte : rien à cacher

        // distance (en cases) jusqu'à la zone voilée : deux passes
        const float D = 1.4142f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = d[x, y];
                if (x > 0) v = Mathf.Min(v, d[x - 1, y] + 1f);
                if (y > 0)
                {
                    v = Mathf.Min(v, d[x, y - 1] + 1f);
                    if (x > 0) v = Mathf.Min(v, d[x - 1, y - 1] + D);
                    if (x < w - 1) v = Mathf.Min(v, d[x + 1, y - 1] + D);
                }
                d[x, y] = v;
            }
        for (int y = h - 1; y >= 0; y--)
            for (int x = w - 1; x >= 0; x--)
            {
                float v = d[x, y];
                if (x < w - 1) v = Mathf.Min(v, d[x + 1, y] + 1f);
                if (y < h - 1)
                {
                    v = Mathf.Min(v, d[x, y + 1] + 1f);
                    if (x < w - 1) v = Mathf.Min(v, d[x + 1, y + 1] + D);
                    if (x > 0) v = Mathf.Min(v, d[x - 1, y + 1] + D);
                }
                d[x, y] = v;
            }

        // La brume qui s'épaissit n'existe qu'aux passages : partout ailleurs la frontière est un bord net (roche pleine),
        // sinon des cavernes entières, juste au-dessus d'une frontière, seraient plongées dans le noir.
        List<float[]> boxes = new List<float[]>();   // hx, hy, bx, by, demi-largeur
        foreach (object[] pt in SwPortals)
        {
            if (string.IsNullOrEmpty((string)pt[8])) continue;     // seuls les ascenseurs (passages entre grands groupes) plongent dans le noir
            int ga = (int)pt[2], gb = (int)pt[3], axisP = (int)pt[1];
            float[] ra = (float[])pt[4], rb = (float[])pt[5];
            int dirP = axisP == 0 ? (ra[0] > rb[0] ? 1 : -1) : (ra[2] > rb[2] ? 1 : -1);
            Vector2 hh, bb;
            if (ga == group) { PortalGeometry(axisP, ra, dirP, out hh, out bb); boxes.Add(new[] { hh.x, hh.y, bb.x, bb.y, (axisP == 0 ? ra[3] - ra[2] : ra[1] - ra[0]) * 0.5f }); }
            if (gb == group) { PortalGeometry(axisP, rb, -dirP, out hh, out bb); boxes.Add(new[] { hh.x, hh.y, bb.x, bb.y, (axisP == 0 ? rb[3] - rb[2] : rb[1] - rb[0]) * 0.5f }); }
        }

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dist = d[x, y];
                float a = 0f;
                float near = 0f;                           // 1 : dans la zone d'un ascenseur
                float wx = x0 + x + 0.5f, wy = y0 + y + 0.5f;
                float maxDepth = dist > 0f ? 0.5f : 60f;
                foreach (float[] bx in boxes)
                {
                    float dx = wx - bx[2], dy = wy - bx[3];
                    float depth = dx * bx[0] + dy * bx[1];                 // < 0 : côté de la carte
                    float lateral = Mathf.Abs(-dx * bx[1] + dy * bx[0]);
                    if (depth > maxDepth || depth < -10f) continue;
                    near = Mathf.Max(near, 1f - Mathf.Clamp01((lateral - bx[4] - 1.5f) / 3f));
                }
                if (dist > 0f) a = near * Mathf.Pow(Mathf.Clamp01(1f - (dist - 0.5f) / Soft), 2f);
                else a = near;                             // au-delà de la frontière : noir opaque seulement à l'ascenseur
                px[y * w + x] = new Color32(4, 8, 8, (byte)Mathf.RoundToInt(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        string dir = "Assets/Scenes/Voiles";
        Directory.CreateDirectory(dir);
        string path = dir + "/Voile_" + SwGroupNames[group] + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 1f;                  // 1 pixel = 1 case
            ti.filterMode = FilterMode.Bilinear;          // bords adoucis entre les cases
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 2048;
            TextureImporterSettings st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
        }
        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp == null) { Debug.LogWarning("Motrilem : voile des frontières introuvable : " + path); return; }
        GameObject go = new GameObject("Voile des frontières");
        go.transform.position = new Vector3(x0 + w * 0.5f, y0 + h * 0.5f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = 9998;                           // au-dessus des décors et des monstres, sous le joueur (9999)
    }

    /// <summary>Frontière d'un passage dans la carte en cours : direction du noir et point de la frontière (au centre du passage).</summary>
    static void PortalGeometry(int axis, float[] rect, int headingSign, out Vector2 heading, out Vector2 boundary)
    {
        float cx = (rect[0] + rect[1]) * 0.5f, cy = (rect[2] + rect[3]) * 0.5f;
        Vector2 h = axis == 0 ? new Vector2(headingSign, 0f) : new Vector2(0f, headingSign);
        // on avance (ou on recule) depuis le centre du passage jusqu'au changement de carte
        float t = 0f;
        if (Own(cx, cy)) { while (t < 80f && Own(cx + h.x * t, cy + h.y * t)) t += 0.25f; }
        else { while (t > -80f && !Own(cx + h.x * t, cy + h.y * t)) t -= 0.25f; t += 0.25f; }
        heading = h;
        boundary = new Vector2(cx + h.x * t, cy + h.y * t);
        // les frontières suivent toujours le bord d'une case : on recale sur l'entier le plus proche (sur l'axe de passage)
        if (axis == 0) boundary.x = Mathf.Round(boundary.x); else boundary.y = Mathf.Round(boundary.y);
    }

    /// <summary>Ligne lumineuse et volutes de brume sur un passage : on comprend que c'est une entrée de zone, pas un mur.</summary>
    static void PlacePortalMist(Transform parent, string id, int axis, float[] rect, int headingSign)
    {
        Vector2 h, b;
        PortalGeometry(axis, rect, headingSign, out h, out b);
        GameObject go = new GameObject("Brume du passage : " + id);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(b.x, b.y, 0f);
        BorderMist bm = go.AddComponent<BorderMist>();
        bm.heading = h;
        bm.depth = 6f;
        bm.length = axis == 0 ? rect[3] - rect[2] : rect[1] - rect[0];
    }

    /// <summary>
    /// Le Terrier du Gluant est l'intérieur du grand arbre creux : les parois sont de l'écorce (la roche n'est plus dessinée,
    /// mais garde ses collisions), le fond est du bois sombre, des racines pendent des plafonds.
    /// </summary>
    static void PlaceTreeInterior(GameObject gridGO, System.Random rng)
    {
        Transform sol = gridGO.transform.Find("Sol");
        if (sol != null)
        {
            TilemapRenderer tr = sol.GetComponent<TilemapRenderer>();
            if (tr != null) tr.enabled = false;
        }
        const int xa = 335, ya = -5, yb = 40;
        int xb = SX1;
        GameObject root = new GameObject("Intérieur de l'arbre");
        Sprite bark = Art("tuile_ecorce"), backBark = Art("tuile_ecorce_fond"), hang = Art("racine_pendante");

        // fond des cavernes : bois sombre sur toute la carte
        SpriteRenderer bg = Child(root.transform, "Fond de bois", backBark, new Vector3((xa + xb + 1) * 0.5f, (ya + yb + 1) * 0.5f, 0f), -9, new Color(0.85f, 0.8f, 0.78f), null);
        bg.drawMode = SpriteDrawMode.Tiled;
        bg.size = new Vector2(xb - xa + 1, yb - ya + 1);

        // parois : les cases pleines, regroupées en rectangles d'écorce
        Dictionary<long, int> open = new Dictionary<long, int>();
        for (int y = ya; y <= yb + 1; y++)
        {
            HashSet<long> cur = new HashSet<long>();
            if (y <= yb)
            {
                int x = xa;
                while (x <= xb)
                {
                    if (GSolid(x, y) && InMask(x, y))
                    {
                        int st = x;
                        while (x + 1 <= xb && GSolid(x + 1, y) && InMask(x + 1, y)) x++;
                        long key = ((long)st << 32) | (long)(x & 0x7fffffff);
                        cur.Add(key);
                        if (!open.ContainsKey(key)) open[key] = y;
                    }
                    x++;
                }
            }
            foreach (long key in new List<long>(open.Keys))
            {
                if (cur.Contains(key)) continue;
                int x0 = (int)(key >> 32), x1 = (int)(key & 0x7fffffff), y0 = open[key];
                open.Remove(key);
                SpriteRenderer w = Child(root.transform, "Écorce", bark, new Vector3((x0 + x1 + 1) * 0.5f, (y0 + y) * 0.5f, 0f), 0, new Color(0.95f, 0.9f, 0.88f), null);
                w.drawMode = SpriteDrawMode.Tiled;
                w.size = new Vector2(x1 - x0 + 1, y - y0);
            }
        }

        // racines qui pendent des plafonds
        for (int x = xa; x <= xb; x++)
            for (int y = ya; y <= yb; y++)
            {
                if (!InMask(x, y) || GSolid(x, y) || !GSolid(x, y + 1) || rng.NextDouble() > 0.12) continue;
                float len = 0.6f + (float)rng.NextDouble() * 0.8f;
                SpriteRenderer r = Child(root.transform, "Racine pendante", hang, new Vector3(x + 0.5f, y + 1f - 1.5f * len, 0f), 3, new Color(0.9f, 0.85f, 0.8f), null);
                r.transform.localScale = new Vector3(rng.NextDouble() < 0.5 ? 1f : -1f, len, 1f);
            }
    }

    /// <summary>L'entrée du Terrier du Gluant : un grand arbre creux, couvert de glu, avec un trou où le joueur peut se faufiler (E).</summary>
    static void PlaceTreeEntrance(Transform parent, Vector3 floorPos, string targetScene, Vector2 arrival)
    {
        GameObject go = new GameObject("Arbre du Terrier (entrée)");
        go.transform.SetParent(parent, false);
        go.transform.position = floorPos;
        // le sprite fait 128 x 152 pixels (8 x 9,5 cases) : le sol est à 4,5 cases sous son centre
        Child(go.transform, "Arbre creux", Art("arbre_terrier"), new Vector3(0f, 4.5f, 0f), 4, Color.white, null);
        SpriteRenderer rim = Child(go.transform, "Lueur du trou", Art("lueur"), new Vector3(0f, 1.2f, 0f), 6, new Color(0.55f, 1f, 0.3f, 0.4f), additive);
        rim.transform.localScale = new Vector3(3.4f, 3.4f, 1f);
        rim.gameObject.AddComponent<LightFlicker>().baseAlpha = 0.4f;
        // des slimes qui grouillent sur les racines et les branches (décor inoffensif)
        foreach (float[] sl in new[] { new[] { -3.3f, 0.45f, 0.8f }, new[] { 3.1f, 0.4f, 1f }, new[] { -2.4f, 6.9f, 0.7f }, new[] { 2.7f, 6.6f, 0.75f }, new[] { -1.4f, 0.4f, 0.6f }, new[] { 1.7f, 0.4f, 0.55f } })
        {
            SpriteRenderer s = Child(go.transform, "Slime posé", Art("slime_0"), new Vector3(sl[0], sl[1] + 0.5f * sl[2], 0f), 5, Color.white, null);
            s.transform.localScale = new Vector3(sl[2], sl[2], 1f);
            s.gameObject.AddComponent<SlimeIdle>().scale = sl[2];
        }
        // de la glu qui goutte des branches
        foreach (float dx in new[] { -2.2f, 0.3f, 2.4f })
        {
            GameObject drip = new GameObject("Goutte de glu");
            drip.transform.SetParent(go.transform, false);
            drip.transform.localPosition = new Vector3(dx, 5.2f, 0f);
            drip.AddComponent<AcidDrip>().interval = 1.3f + (dx + 3f) * 0.25f;
        }
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(3.2f, 2.8f);
        box.offset = new Vector2(0f, 1.3f);
        WarpDoor wd = go.AddComponent<WarpDoor>();
        wd.targetScene = targetScene;
        wd.arrival = arrival;
        wd.label = "Se faufiler dans le tronc";
        wd.squeeze = true;
    }

    /// <summary>Trou / terrier : on appuie sur E pour passer dans une autre carte.</summary>
    static void PlaceWarp(Transform parent, string name, Vector3 floorPos, string targetScene, Vector2 arrival, string label, bool hole)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = floorPos;
        if (hole)
        {
            SpriteRenderer dark = Child(go.transform, "Trou", FxUtil.WhiteSprite, new Vector3(0f, 0.05f, 0f), 5, new Color(0.04f, 0.07f, 0.03f, 1f), null);
            dark.transform.localScale = new Vector3(2.6f, 0.35f, 1f);
            SpriteRenderer rim = Child(go.transform, "Bord gluant", Art("lueur"), new Vector3(0f, 0.15f, 0f), 6, new Color(0.55f, 1f, 0.3f, 0.35f), additive);
            rim.transform.localScale = new Vector3(3.6f, 1.2f, 1f);
            rim.gameObject.AddComponent<LightFlicker>().baseAlpha = 0.35f;
        }
        else
        {
            // vraie sortie : une ouverture dans le tronc (arche d'écorce) donnant sur le dehors, sol d'herbe compris
            SpriteRenderer halo = Child(go.transform, "Lumière du dehors", Art("lueur"), new Vector3(0f, 2.2f, 0f), 2, new Color(1f, 0.97f, 0.8f, 0.22f), additive);
            halo.transform.localScale = new Vector3(3.6f, 3.6f, 1f);
            Child(go.transform, "Sortie du tronc", Art("sortie_tronc"), new Vector3(0f, 2f, 0f), 3, Color.white, null);
        }
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(3.2f, 2.4f);
        box.offset = new Vector2(0f, 1.1f);
        WarpDoor wd = go.AddComponent<WarpDoor>();
        wd.targetScene = targetScene;
        wd.arrival = arrival;
        wd.label = label;
    }

    /// <summary>Cache qui masque ce qu'il y a sous une trappe fermée (il s'efface quand le levier est actionné).</summary>
    static void PlaceGateCover(Transform parent, string flag, int x0, int x1, int y0, int y1, int axis, float threshold)
    {
        GameObject go = new GameObject("Cache sous la trappe (" + flag + ")");
        go.transform.SetParent(parent, false);
        UnityEngine.Tilemaps.Tile tile = SheetTile(79) as UnityEngine.Tilemaps.Tile;
        Sprite rockSprite = tile != null ? tile.sprite : FxUtil.WhiteSprite;
        List<SpriteRenderer> all = new List<SpriteRenderer>();
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                GameObject c = new GameObject("Roche");
                c.transform.SetParent(go.transform, false);
                c.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
                SpriteRenderer sr = c.AddComponent<SpriteRenderer>();
                sr.sprite = rockSprite;
                sr.sortingOrder = 9985;   // au-dessus des leviers et des trappes : on ne voit rien de ce qui est dessous
                sr.color = SwTint(x, y, 1f);
                all.Add(sr);
            }
        GateCover gc = go.AddComponent<GateCover>();
        gc.flag = flag;
        gc.byX = axis == 0;
        gc.threshold = threshold;
        gc.renders = all.ToArray();
    }

    static void PlaceCrack(Transform parent, int x, int y, int h, string id)
    {
        GameObject go = new GameObject("Mur fissuré");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x + 0.5f, y + h * 0.5f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Art("tuile_fissure");
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(1f, h);
        sr.sortingOrder = 3;
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, h);
        EnemyHealth eh = go.AddComponent<EnemyHealth>();
        eh.maxHealth = 3;
        eh.rootToDestroy = go;
        eh.deathStyle = EnemyHealth.DeathStyle.Eclatement;
        eh.deathSound = "cristal";
        eh.shardColor = new Color(0.75f, 0.72f, 0.65f);
        eh.countsAsEnemy = false;
        go.AddComponent<CrackedWall>().wallId = id;
    }

    /// <summary>Voile de racines : des tuiles de terrain posées devant une ouverture (elles s'estompent quand on s'approche).</summary>
    static void PlaceVeil(Transform parent, int x0, int x1, int y0, int y1, string id, System.Random rng)
    {
        GameObject go = new GameObject("Voile de racines (" + id + ")");
        go.transform.SetParent(parent, false);
        Tilemap tm = go.AddComponent<Tilemap>();
        TilemapRenderer r = go.AddComponent<TilemapRenderer>();
        r.sortingOrder = 10050;
        // on calcule les tuiles comme si les cases étaient pleines
        for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) GSet(x, y, true);
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                Vector3Int p = new Vector3Int(x, y, 0);
                tm.SetTile(p, SheetTile(GTileIndex(x, y)));
                tm.SetTileFlags(p, TileFlags.None);
                tm.SetColor(p, SwTint(x, y, 0.93f));
            }
        for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) GSet(x, y, false);

        GameObject trig = new GameObject("Zone");
        trig.transform.SetParent(go.transform, false);
        trig.transform.position = new Vector3((x0 + x1 + 1) * 0.5f, (y0 + y1 + 1) * 0.5f, 0f);
        BoxCollider2D box = trig.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(x1 - x0 + 3f, y1 - y0 + 3f);
        RootVeil v = trig.AddComponent<RootVeil>();
        v.veilId = id;
        v.tilemap = tm;
        Rigidbody2D rb = trig.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    // ------------------------------------------------------------------ indices et panneaux
    // Indice d'histoire : un simple décor de fond (aucune infobulle). Les offrandes de bougies s'allument avec un sort de feu.
    static GameObject PlaceLore(Transform parent, float x, int stand, string id, string spriteName, string[] lines)
    {
        Sprite sprite = Art(spriteName);
        if (sprite == null) return null;
        GameObject go = new GameObject("Indice : " + id);
        go.transform.SetParent(parent, false);
        float h = sprite.bounds.size.y;
        bool onWall = spriteName == "lore_fresque" || spriteName == "lore_adoration" || spriteName == "lore_tablette" || spriteName == "lore_gravure" || spriteName == "lore_relief" || spriteName == "lore_mosaique" || spriteName == "lore_porte" || spriteName == "lore_sceau" || spriteName == "lore_procession";
        go.transform.position = new Vector3(x, stand + h * 0.5f + (onWall ? 1.1f : 0f), 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        sr.color = new Color(0.5f, 0.52f, 0.56f, 1f); // sombre : ça doit se fondre dans le décor
        string note = LoreNote(spriteName);
        if (note != null)
        {
            // on peut observer la statue / la fresque (touche E) : un court texte, jamais explicite
            BoxCollider2D area = go.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            float cy = go.transform.position.y;
            area.size = new Vector2(Mathf.Max(2.6f, sprite.bounds.size.x), 4.6f);
            area.offset = new Vector2(0f, stand + 1.8f - cy);
            TalkInteractable talk = go.AddComponent<TalkInteractable>();
            talk.promptText = "Observer";
            talk.speaker = "";
            talk.lines = new string[] { note };
            talk.promptOffset = new Vector3(0f, stand + 2.1f - cy, 0f);
        }
        if (spriteName == "lore_offrande")
        {
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.4f, 1.1f);
            EnemyHealth eh = go.AddComponent<EnemyHealth>();
            eh.maxHealth = 99999;
            eh.rootToDestroy = go;
            eh.countsAsEnemy = false;
            CandleOffering c = go.AddComponent<CandleOffering>();
            c.offeringId = id;
            c.glowSprite = Art("lueur");
            c.glowMaterial = additive;
        }
        return go;
    }

    // Texte d'observation (volontairement vague : il ne révèle rien du secret)
    static string LoreNote(string spriteName)
    {
        switch (spriteName)
        {
            case "lore_statue": return "Une statue de pierre, usée par les siècles. La mousse est grattée autour du visage : quelqu'un vient encore l'entretenir.";
            case "lore_fresque": return "Un homme en robe claire, une toute petite bête posée sur la main. Il a l'air de lui sourire.";
            case "lore_adoration": return "Une foule, les bras levés vers un homme en robe claire. Son visage a été gratté de la pierre.";
            case "lore_mosaique": return "Une mosaïque : un grand soleil, et une mer qui monte. Les carreaux du bas sont noyés depuis longtemps.";
            case "lore_stele": return "Des colonnes de signes que tu ne sais pas lire. Quelqu'un a laissé une fleur séchée au pied.";
            case "lore_porte": return "Une porte immense, enchaînée de maillons dorés. Ils brillent encore, comme entretenus.";
            case "lore_sceau": return "Un cercle gravé, cerné de maillons. Au centre, une croix : on dirait un verrou.";
            case "lore_procession": return "Des silhouettes encapuchonnées, une lampe à la main, marchent vers une porte. Aucune ne se retourne.";
            case "lore_relief": return "Une étoile à huit branches, gravée avec soin. On la retrouve au bout d'un bâton, sur les fresques.";
        }
        return null;
    }

    // Urne en terre cuite : un sort la brise, et elle contient parfois un peu de soin
    static void PlaceUrn(GameObject parent, float x, int stand)
    {
        GameObject go = new GameObject("Urne");
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(x, stand + 0.5f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Art("urne");
        sr.sortingOrder = 4;
        sr.color = new Color(0.62f, 0.6f, 0.6f, 1f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.7f, 0.95f);
        EnemyHealth eh = go.AddComponent<EnemyHealth>();
        eh.maxHealth = 1;
        eh.rootToDestroy = go;
        eh.countsAsEnemy = false;
        eh.deathStyle = EnemyHealth.DeathStyle.Eclatement;
        eh.deathSound = "cristal";
        eh.shardColor = new Color(0.72f, 0.45f, 0.3f);
        go.AddComponent<BreakableUrn>();
    }

    // Ascenseur : x du centre, puis le sol de chaque arrêt (du plus bas au plus haut)
    static void PlaceLift(GameObject parent, float[] data, string liftName)
    {
        float x = data[0];
        int n = data.Length - 1;
        GameObject go = new GameObject("Ascenseur");
        go.transform.SetParent(parent.transform, false);
        Vector3[] stops = new Vector3[n];
        for (int i = 0; i < n; i++) stops[i] = new Vector3(x, data[i + 1] - 0.26f, 0f);
        go.transform.position = stops[0];
        SpriteRenderer sp = Child(go.transform, "Plateforme", Art("plateforme_pierre"), Vector3.zero, 9, Color.white, null);
        sp.transform.localScale = new Vector3(0.66f, 1f, 1f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.95f, 0.6f);
        box.offset = new Vector2(0f, -0.02f);
        ElevatorPlatform el = go.AddComponent<ElevatorPlatform>();
        el.stops = stops;
        el.liftId = liftName;
        // rails de guidage le long du puits
        float lo = data[1] - 0.6f, hi = data[n] + 0.6f;
        foreach (float dx in new[] { -0.98f, 0.98f })
        {
            GameObject rail = new GameObject("Rail");
            rail.transform.SetParent(parent.transform, false);
            rail.transform.position = new Vector3(x + dx, (lo + hi) * 0.5f, 0f);
            rail.transform.localScale = new Vector3(0.1f, hi - lo, 1f);
            SpriteRenderer rs = rail.AddComponent<SpriteRenderer>();
            rs.sprite = FxUtil.WhiteSprite;
            rs.sortingOrder = 3;
            rs.color = new Color(0.24f, 0.2f, 0.17f, 0.95f);
        }
    }

    static void PlaceSwampSign(Transform parent, int tileX, int stand, string message)
    {
        GameObject go = new GameObject("Panneau");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(tileX + 0.5f, stand + 1f, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(4f, 4f);
        go.AddComponent<SignPost>().message = message;
        // le panneau visible : le même que dans la grotte (la bulle n'apparaît qu'à côté d'un vrai panneau)
        UnityEngine.Tilemaps.Tile tile = SheetTile(14) as UnityEngine.Tilemaps.Tile;
        if (tile != null && tile.sprite != null)
        {
            GameObject vis = new GameObject("Sprite du panneau");
            vis.transform.SetParent(go.transform, false);
            vis.transform.position = new Vector3(tileX + 0.5f, stand + tile.sprite.bounds.extents.y, 0f);
            SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = tile.sprite;
            sr.sortingOrder = 1;
        }
    }

    static void AddMusicZone(GameObject parent, float x0, float x1, float y0, float y1, string music, float volume)
    {
        GameObject go = new GameObject("Musique : " + music);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(x1 - x0, y1 - y0);
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        MusicZone mz = go.AddComponent<MusicZone>();
        mz.music = music;
        mz.volume = volume;
    }

    static void PlaceMapBoard(Transform parent)
    {
        Sprite s = Art("panneau_carte");
        GameObject go = new GameObject("Panneau-carte");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(SwMapX, SwMapY + 1.25f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = 9980;
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(3.2f, 3f);
        go.AddComponent<MapBoard>();
    }

    // ------------------------------------------------------------------ donjon
    static void PlaceDungeonEntrance(Vector3 floorPos)
    {
        GameObject root = new GameObject("Entrée du donjon");
        root.transform.position = floorPos;
        Color stone = new Color(0.82f, 0.85f, 0.82f);

        // Arrière-plan : colonnes plus sombres et plus petites (profondeur)
        foreach (float dx in new[] { -13f, -10.5f, 10.5f, 13f })
        {
            SpriteRenderer col = Child(root.transform, "Colonne (fond)", Art("donjon_colonne"), new Vector3(dx, 3.4f, 0f), -4, new Color(0.45f, 0.48f, 0.5f), null);
            col.transform.localScale = new Vector3(0.85f, 0.9f, 1f);
        }
        SpriteRenderer fog = Child(root.transform, "Brume", Art("lueur"), new Vector3(0f, 0.6f, 0f), 6, new Color(0.45f, 0.8f, 0.4f, 0.18f), additive);
        fog.transform.localScale = new Vector3(14f, 2.2f, 1f);
        fog.gameObject.AddComponent<LightFlicker>().baseAlpha = 0.18f;
        Child(root.transform, "Façade", Art("donjon_facade"), new Vector3(0f, 4.5f, 0f), 2, stone, null);
        foreach (float ex in new[] { -0.3f, 0.3f })
        {
            SpriteRenderer eye = Child(root.transform, "Œil", Art("lueur"), new Vector3(ex, 3.3f, 0f), 3, new Color(1f, 0.15f, 0.15f, 0.8f), additive);
            eye.transform.localScale = new Vector3(0.22f, 0.15f, 1f);
            LightFlicker lf = eye.gameObject.AddComponent<LightFlicker>();
            lf.baseAlpha = 0.75f; lf.amount = 0.25f; lf.speed = 1.5f;
        }
        foreach (float dx in new[] { -4.6f, 4.6f })
        {
            Child(root.transform, "Brasero", Art("brasero"), new Vector3(dx, 0.5f, 0f), 9989, Color.white, null);
            SpriteRenderer fire = Child(root.transform, "Lueur du brasero", Art("lueur"), new Vector3(dx, 0.9f, 0f), 7, new Color(1f, 0.55f, 0.2f, 0.5f), additive);
            fire.transform.localScale = new Vector3(2.6f, 2.6f, 1f);
            fire.gameObject.AddComponent<LightFlicker>();
        }
        foreach (float dx in new[] { -7.2f })   // une seule colonne de premier plan : à droite se trouve la chapelle de l'ascenseur
        {
            SpriteRenderer col = Child(root.transform, "Colonne (premier plan)", Art("donjon_colonne"), new Vector3(dx, 3.75f, 0f), 10011, new Color(0.7f, 0.72f, 0.75f), null);
            col.transform.localScale = new Vector3(1.1f, 1f, 1f);
        }

        // La porte : on l'écoute, puis on entre (fondu, nouvelle scène)
        GameObject door = new GameObject("Porte du donjon");
        door.transform.SetParent(root.transform, false);
        door.transform.localPosition = new Vector3(0f, 2f, 0f);
        BoxCollider2D zone = door.AddComponent<BoxCollider2D>();
        zone.isTrigger = true;
        zone.size = new Vector2(4f, 4f);
        DungeonDoor dd = door.AddComponent<DungeonDoor>();
        dd.sceneToLoad = DungeonSceneName;
        dd.lines = new[]
        {
            "Un bourdonnement assourdissant résonne au fond de ce temple...",
            "Quelque chose d'énorme vit là-dessous. Et je sens qu'une page du livre est tout près."
        };
    }

    // ================================================================== le Terrier du Gluant (zones du mini-boss)
    // Boyau (descente en pente) -> Mare gluante (salle à vagues, 4 nappes d'acide, 3 îlots) -> rive + statue -> couloir -> Antre.
    const int GlBoyauX0 = 380, GlMareX0 = 421, GlMareX1 = 453, GlAntreX0 = 467, GlAntreX1 = 494, GlFloor = 12;

    static ArenaLock MakeLockAt(Transform parent, float x, float baseY, float height)
    {
        GameObject go = new GameObject("Grille");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, baseY + height * 0.5f, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, height);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Art("tuile_grille");
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(1f, height);
        sr.sortingOrder = 9990;
        ArenaLock al = go.AddComponent<ArenaLock>();
        al.render = sr;
        return al;
    }

    static void BuildGluantArea(GameObject level, Transform mech, Transform links, Transform slimes, System.Random rng)
    {
        // ---- sorties : un trou dans le boyau et un autre au fond de l'antre ramènent sur le belvédère
        PlaceWarp(links, "Remontée (boyau)", new Vector3(381.5f, 20f, 0f), SwGroupNames[2], new Vector2(SwTerrierX, SwTerrierY), "Sortir de l'arbre", false);
        PlaceWarp(links, "Remontée (antre)", new Vector3(GlAntreX1 - 0.5f, GlFloor, 0f), SwGroupNames[2], new Vector2(SwTerrierX, SwTerrierY), "Sortir de l'arbre", false);

        // ---- la mare : salle à vagues entre deux grilles
        ArenaLock lkIn = MakeLockAt(mech, GlMareX0 + 1.5f, GlFloor, 6f);
        ArenaLock lkOut = MakeLockAt(mech, GlMareX1 - 0.5f, GlFloor, 13f);
        GameObject waveRoot = new GameObject("Vagues");
        GameObject wgo = new GameObject("Salle de vagues : La Mare gluante");
        wgo.transform.SetParent(waveRoot.transform, false);
        WaveRoom wr = wgo.AddComponent<WaveRoom>();
        wr.roomId = "gluant_mare";
        wr.roomName = "La Mare gluante";
        wr.min = new Vector2(GlMareX0, 7f);
        wr.max = new Vector2(GlMareX1, 25f);
        wr.entryLock = lkIn;
        wr.exitLock = lkOut;
        wr.respawnPoint = new Vector2(GlMareX0 + 2.8f, GlFloor + 0.8f);   // une chute dans l'acide ramène ici, derrière la grille
        wr.hasRespawnPoint = true;
        wr.waves = new[]
        {
            Wv(Sp(0, 423f, 13f), Sp(0, 429.5f, 13f), Sp(0, 448f, 13f)),
            Wv(Sp(1, 435.5f, 13.2f), Sp(0, 441.5f, 13f), Sp(2, 432f, 17f), Sp(2, 444f, 17f)),
            Wv(Sp(1, 423f, 13.2f), Sp(3, 436f, 18f), Sp(3, 448f, 18f), Sp(0, 441.5f, 13f))
        };

        // ---- l'antre : le Gros Gluant, grille d'entrée, éclat de vie caché jusqu'à sa victoire
        ArenaLock bossLock = MakeLockAt(mech, GlAntreX0 - 0.5f, GlFloor, 6f);
        GameObject shard = null;
        foreach (object[] sh in SwShards)
            if ((string)sh[2] == "marais_gluant") shard = PlaceShard(new Vector3((float)sh[0], (float)sh[1], 0f), (string)sh[2]);
        if (shard != null) shard.SetActive(false);
        MiniBossSlime boss = MiniBossSlime.Create(new Vector3(SwMiniBossX, SwMiniBossY + 1.4f, 0f), slimes);
        boss.arenaMin = new Vector2(GlAntreX0, GlFloor);
        boss.arenaMax = new Vector2(GlAntreX1 + 1, 30f);
        boss.entryLock = bossLock;
        boss.rewardShard = shard;

        // ---- ambiance
        GameObject amb = new GameObject("Ambiance du terrier");
        AddHalo(amb, "Lueur du boyau", new Vector3(398f, 17f, 0f), new Vector2(30f, 8f), new Color(0.5f, 1f, 0.4f, 0.16f));
        AddHalo(amb, "Lueur de la mare", new Vector3(437f, 14f, 0f), new Vector2(34f, 12f), new Color(0.55f, 1f, 0.3f, 0.2f));
        AddHalo(amb, "Lueur de l'antre", new Vector3(481f, 18f, 0f), new Vector2(34f, 16f), new Color(0.6f, 1f, 0.35f, 0.18f));
        foreach (float dx in new[] { 392f, 408f, 432f, 446f, 474f, 488f })
        {
            GameObject drip = new GameObject("Goutte d'acide");
            drip.transform.SetParent(amb.transform, false);
            drip.transform.position = new Vector3(dx, dx < 425f ? 25f : 24f, 0f);
            drip.AddComponent<AcidDrip>().interval = 2.2f + (dx % 3f) * 0.4f;
        }
    }
}
