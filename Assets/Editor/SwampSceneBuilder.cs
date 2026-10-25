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
    };

    static Color SwTint(int x, int y, float brightness)
    {
        Color c = SwTints[SwRegion(x, y)];
        float depth = y < 0 ? Mathf.Lerp(1f, 0.6f, Mathf.Clamp01(-y / 66f)) : 1f;
        return new Color(c.r * depth * brightness, c.g * depth * brightness, c.b * depth * brightness, 1f);
    }

    // ================================================================== construction
    static void BuildPond()
    {
        buildingTutorial = false;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        tileCache.Clear();
        LoadSwampGrid();
        System.Random rng = new System.Random(77);

        // ---- tuiles : fond des cavernes, sol, décor, planches
        GameObject gridGO = new GameObject("Grid");
        gridGO.AddComponent<Grid>();
        BuildGridTiles(gridGO, rng, SwTint, new Color(0.62f, 0.62f, 0.72f));

        // ---- joueur, caméra, ciel
        GameObject player = PlacePlayer(new Vector3(3.5f, 0.8f, 0f), true);
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

        // Fond de forêt : il suit la caméra (on ne voit jamais son bord, même en sautant)
        GameObject sky = new GameObject("Ciel et forêt");
        sky.transform.SetParent(outside.transform, false);
        float cx = (SX0 + SX1) * 0.5f;
        AddParallaxFollow(sky, "BACKGROUND", 1f, -100, cx, 0.5f, 2.2f, new Color(0.85f, 1f, 0.9f));
        AddParallaxFollow(sky, "WOODS - Fourth", 0.85f, -90, cx, 0.3f, 1.6f, new Color(0.8f, 0.95f, 0.8f));
        AddParallaxFollow(sky, "WOODS - Third", 0.7f, -80, cx, 0f, 1.6f, new Color(0.75f, 0.92f, 0.75f));
        AddParallaxFollow(sky, "VINES - Second", 0.6f, -75, cx, 1.5f, 1.6f, new Color(0.7f, 0.9f, 0.7f));
        AddParallaxFollow(sky, "WOODS - Second", 0.5f, -70, cx, -0.3f, 1.6f, new Color(0.65f, 0.85f, 0.65f));
        AddParallaxFollow(sky, "BUSH - BACKGROUND", 0.3f, -60, cx, -1.2f, 1.6f, new Color(0.6f, 0.85f, 0.55f));

        // ---- acide, nénuphars, plateformes mobiles
        GameObject level = new GameObject("Marécage");
        for (int i = 0; i < SwPools.GetLength(0); i++)
            PlaceSwampPool(level, SwPools[i, 0], SwPools[i, 1], SwPools[i, 2], SwPools[i, 3], SwPools[i, 4] == 1, rng);
        for (int i = 0; i < SwPads.GetLength(0); i++) PlacePad(level, SwPads[i, 0], (int)SwPads[i, 1], SwPads[i, 2] > 0.5f);
        for (int i = 0; i < SwMovers.GetLength(0); i++)
            PlaceMovingPlatform(level, new Vector3(SwMovers[i, 0], SwMovers[i, 1], 0f), new Vector3(SwMovers[i, 2], SwMovers[i, 3], 0f), SwMovers[i, 4]);
        // le monte-charge : il relie la falaise (en haut) à la plaine putride (en bas)
        MovingPlatform lift = PlaceMovingPlatform(level, new Vector3(SwElevX, SwElevBottom, 0f), new Vector3(SwElevX, SwElevTop, 0f), 6f);
        lift.name = "Monte-charge";
        lift.pause = 2.5f;
        // les ascenseurs des grands puits (remplacent les longues échelles de planches)
        foreach (float[] l in SwLifts) PlaceLift(level, l);

        // ---- trappes, grilles, leviers, murs fissurés, voiles de racines
        GameObject mech = new GameObject("Mécanismes");
        foreach (object[] g in SwGates) PlaceGate(mech.transform, (int)g[0], (int)g[1], (int)g[2], (int)g[3], (int)g[4], (string)g[5]);
        foreach (object[] l in SwLevers) PlaceLever(mech.transform, (float)l[0], (int)l[1], (string)l[2]);
        foreach (object[] c in SwCracks) PlaceCrack(mech.transform, (int)c[0], (int)c[1], (int)c[2], (string)c[3]);
        foreach (object[] v in SwVeils) PlaceVeil(mech.transform, (int)v[0], (int)v[1], (int)v[2], (int)v[3], (string)v[4], rng);

        // ---- ennemis
        GameObject slimes = new GameObject("Slimes");
        for (int i = 0; i < SwSlimes.GetLength(0); i++)
            Slime.Create(new Vector3(SwSlimes[i, 0] + 0.5f, SwSlimes[i, 1] + 0.6f, 0f), SwSlimes[i, 2] == 1, slimes.transform);
        foreach (object[] m in SwMosquitoes)
            PlaceMosquito((string)m[0], new Vector2((float)m[1], (float)m[2]), (Vector2[])m[3], (float)m[4], (bool)m[5]);

        // ---- statues de prière, urnes, éclats de vie
        GameObject story2 = new GameObject("Urnes");
        foreach (object[] s in SwStatues) PlaceStatue(new Vector3((float)s[0], (int)s[1], 0f), (string)s[2]);
        for (int i = 0; i < SwUrns.GetLength(0); i++) PlaceUrn(story2, SwUrns[i, 0], (int)SwUrns[i, 1]);
        foreach (object[] s in SwShards) PlaceShard(new Vector3((float)s[0], (float)s[1], 0f), (string)s[2]);

        // ---- indices d'histoire, panneaux, carte, gouttes d'acide, titres de région
        GameObject story = new GameObject("Indices");
        foreach (object[] l in SwLore) PlaceLore(story.transform, (float)l[0], (int)l[1], (string)l[2], (string)l[3], (string[])l[4]);
        foreach (object[] s in SwSigns) PlaceSwampSign(story.transform, (int)s[0], (int)s[1], (string)s[2]);
        GameObject ambience = new GameObject("Ambiance");
        for (int i = 0; i < SwDrips.GetLength(0); i++)
        {
            GameObject drip = new GameObject("Goutte d'acide");
            drip.transform.SetParent(ambience.transform, false);
            drip.transform.position = new Vector3(SwDrips[i, 0], SwDrips[i, 1], 0f);
            drip.AddComponent<AcidDrip>().interval = 2f + (i % 4) * 0.6f;
        }
        AddHalo(ambience, "Lueur du nœud", new Vector3(103f, -26f, 0f), new Vector2(8f, 5f), new Color(0.45f, 0.8f, 1f, 0.22f));
        AddHalo(ambience, "Lueur de la cuve", new Vector3(155f, -26f, 0f), new Vector2(10f, 5f), new Color(0.55f, 1f, 0.4f, 0.2f));
        AddHalo(ambience, "Lueur des colonnes", new Vector3(146f, -44f, 0f), new Vector2(14f, 6f), new Color(0.5f, 1f, 0.45f, 0.18f));
        AddHalo(ambience, "Lueur du scriptorium", new Vector3(182f, -44f, 0f), new Vector2(9f, 5f), new Color(1f, 0.85f, 0.55f, 0.22f));
        AddHalo(ambience, "Lueur de la citerne", new Vector3(208f, -50f, 0f), new Vector2(16f, 8f), new Color(0.5f, 1f, 0.4f, 0.16f));
        AddHalo(ambience, "Lueur de la plaine", new Vector3(273f, -40f, 0f), new Vector2(22f, 8f), new Color(0.55f, 1f, 0.3f, 0.2f));
        AddHalo(ambience, "Lueur du parvis", new Vector3(318f, -38f, 0f), new Vector2(12f, 8f), new Color(1f, 0.45f, 0.3f, 0.16f));
        GameObject zones = new GameObject("Zones");
        AddFallZone(zones, (SX0 + SX1) * 0.5f, SY0 - 8f, 500f);

        // ---- musiques par région (fondu enchaîné à l'entrée de chaque zone)
        GameObject musicZones = new GameObject("Musiques");
        AddMusicZone(musicZones, 0f, 144f, -1f, 40f, "musique_etang", 0.5f);
        AddMusicZone(musicZones, 86f, 252f, -38f, -1f, "musique_racines", 0.5f);
        AddMusicZone(musicZones, 90f, 252f, -66f, -38f, "musique_ruines", 0.5f);
        AddMusicZone(musicZones, 144f, 290f, 19f, 50f, "musique_hauteurs", 0.5f);
        AddMusicZone(musicZones, 252f, 334f, -52f, -20f, "musique_coeur", 0.5f);

        // ---- sortie vers la région suivante (plateau de la falaise) et entrée du donjon
        PlaceZoneExit(zones.transform);
        PlaceDungeonEntrance(new Vector3(SwDoorX, SwDoorY, 0f));

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
        AddMusic("musique_etang", 0.5f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, PondScene);
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
                if (gPlank[x - gX0, y - gY0])
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

    static void PlaceShard(Vector3 pos, string id)
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
        GameObject go = new GameObject("Levier");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x + 0.5f, stand + 0.65f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Art("levier_0");
        sr.sortingOrder = 9980;
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2.2f, 2.2f);
        Lever lv = go.AddComponent<Lever>();
        lv.flag = flag;
        lv.render = sr;
        lv.offSprite = Art("levier_0");
        lv.onSprite = Art("levier_1");
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
    static void PlaceLift(GameObject parent, float[] data)
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

    static void PlaceZoneExit(Transform parent)
    {
        GameObject go = new GameObject("Sortie de la zone");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(SwExitX, SwExitY + 3f, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2f, 8f);
        go.AddComponent<ZoneExit>();
        AddHalo(go, "Lumière de la sortie", new Vector3(0f, -1f, 0f), new Vector2(6f, 8f), new Color(1f, 0.95f, 0.7f, 0.22f));
        PlaceSwampSign(parent, (int)SwExitX - 6, (int)SwExitY, "Au-delà de cette crête, le marécage s'efface. Une autre région t'attend... un jour.");
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
        foreach (float dx in new[] { -7.2f, 7.2f })
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
}
