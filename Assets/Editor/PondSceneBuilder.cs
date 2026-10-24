using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Zone 1 : l'Étang Acide, en monde ouvert (façon metroidvania).
//  - Surface : marais, bassins d'acide, nénuphars, plateformes mobiles, falaise
//  - Hauteurs : îlots rocheux menant à un éclat de vie
//  - Galeries (sous la surface) : puits couvert de planches, bassins, nid de slimes, escalier qui remonte
//  - Profondeurs : salle secrète derrière un mur de cristal, lac d'acide à piliers, esplanade du donjon
// Les données de la carte sont dans PondLayout.cs (généré par l'outil pond_layout.py).
public static partial class TutorialSceneBuilder
{
    const int PoolDepth = 4;
    static bool[,] pondGrid;

    static int PondSurface(int x)
    {
        if (x < 0) return 16;
        if (x > PSurfEnd) return PY1 + 1;
        for (int i = 0; i < PPools.GetLength(0); i++)
            if (x >= PPools[i, 0] && x <= PPools[i, 1] && PPools[i, 2] >= -1) return PPools[i, 2];
        for (int i = 0; i < PSurface.GetLength(0); i++)
            if (x >= PSurface[i, 0] && x <= PSurface[i, 1]) return PSurface[i, 2];
        return 0;
    }

    static void BuildPondGrid()
    {
        int w = PX1 - PX0 + 1, h = PY1 - PY0 + 1;
        pondGrid = new bool[w, h];
        for (int x = PX0; x <= PX1; x++)
        {
            int s = PondSurface(x);
            for (int y = PY0; y <= PY1; y++) pondGrid[x - PX0, y - PY0] = y < s;
        }
        for (int i = 0; i < PPools.GetLength(0); i++)
            for (int x = PPools[i, 0]; x <= PPools[i, 1]; x++)
                for (int y = PPools[i, 2] - PoolDepth; y < PPools[i, 2]; y++) SetPond(x, y, false);
        for (int i = 0; i < PCarves.GetLength(0); i++)
            for (int x = PCarves[i, 0]; x <= PCarves[i, 1]; x++)
                for (int y = PCarves[i, 2]; y <= PCarves[i, 3]; y++) SetPond(x, y, false);
        for (int i = 0; i < PAdds.GetLength(0); i++)
            for (int x = PAdds[i, 0]; x <= PAdds[i, 1]; x++)
                for (int y = PAdds[i, 2]; y <= PAdds[i, 3]; y++) SetPond(x, y, true);
        // Scellement : le fond et les bords de chaque bassin sont toujours pleins (jamais d'acide suspendu)
        for (int i = 0; i < PPools.GetLength(0); i++)
        {
            int a = PPools[i, 0], b = PPools[i, 1], top = PPools[i, 2];
            for (int x = a - 1; x <= b + 1; x++) SetPond(x, top - PoolDepth - 1, true);
            for (int y = top - PoolDepth - 1; y < top; y++) { SetPond(a - 1, y, true); SetPond(b + 1, y, true); }
        }
    }

    /// <summary>Case vide sous la surface (intérieur d'une caverne).</summary>
    static bool PondUnderground(int x, int y)
    {
        return !PondSolid(x, y) && y < PondSurface(x);
    }

    /// <summary>Faut-il un fond de caverne derrière cette case ? Oui pour les cavernes, et aussi derrière
    /// les murs qui les bordent (leurs tuiles de bord ont des parties transparentes).</summary>
    static bool PondNeedsBack(int x, int y)
    {
        if (PondUnderground(x, y)) return true;
        if (!PondSolid(x, y)) return false;
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if ((dx != 0 || dy != 0) && PondUnderground(x + dx, y + dy)) return true;
        return false;
    }

    static void SetPond(int x, int y, bool solid)
    {
        if (x < PX0 || x > PX1 || y < PY0 || y > PY1) return;
        pondGrid[x - PX0, y - PY0] = solid;
    }

    static bool PondSolid(int x, int y)
    {
        if (x < PX0 || x > PX1 || y < PY0 || y > PY1) return true;
        return pondGrid[x - PX0, y - PY0];
    }

    static int PondTileIndex(int x, int y)
    {
        bool up = !PondSolid(x, y + 1), down = !PondSolid(x, y - 1), left = !PondSolid(x - 1, y), right = !PondSolid(x + 1, y);
        if (up) return left ? 66 : right ? 68 : 67;
        if (down) return left ? 90 : right ? 92 : 91;
        if (left) return 78;
        if (right) return 80;
        return 79;
    }

    // ================================================================== construction
    static void BuildPond()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        tileCache.Clear();
        BuildPondGrid();
        System.Random rng = new System.Random(77);

        // ---- tuiles : fond des cavernes, sol, décor, planches
        GameObject gridGO = new GameObject("Grid");
        gridGO.AddComponent<Grid>();
        Tilemap back = CreateTilemap("Fond des cavernes", gridGO.transform, -10, false, new Color(0.55f, 0.55f, 0.65f));
        Tilemap ground = CreateTilemap("Sol", gridGO.transform, 0, true, Color.white);
        Tilemap decor = CreateTilemap("Décor", gridGO.transform, 1, false, Color.white);
        Tilemap planks = CreateTilemap("Planches", gridGO.transform, 2, false, Color.white);
        TilemapCollider2D pc = planks.gameObject.AddComponent<TilemapCollider2D>();
        pc.usedByEffector = true;
        PlatformEffector2D eff = planks.gameObject.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.surfaceArc = 160f;

        List<Vector3Int> gPos = new List<Vector3Int>(), bPos = new List<Vector3Int>();
        List<TileBase> gTiles = new List<TileBase>(), bTiles = new List<TileBase>();
        int[] backVariants = { 27, 27, 27, 27, 23, 49, 87 };
        int[] floorDeco = { 81, 82, 93, 94, 53, 53, 83, 95 };
        int[] ceilDeco = { 76, 77, 88, 89 };
        for (int x = PX0; x <= PX1; x++)
        {
            int surf = PondSurface(x);
            for (int y = PY0; y <= PY1; y++)
            {
                if (PondNeedsBack(x, y))
                {
                    bPos.Add(new Vector3Int(x, y, 0));
                    bTiles.Add(SheetTile(backVariants[rng.Next(backVariants.Length)]));
                }
                if (PondSolid(x, y))
                {
                    gPos.Add(new Vector3Int(x, y, 0));
                    gTiles.Add(SheetTile(PondTileIndex(x, y)));
                }
                else if (y < surf)
                {
                    // vide sous la surface : on est dans une caverne
                    if (PondSolid(x, y - 1) && !PondSolid(x, y + 1) && rng.NextDouble() < 0.2)
                        decor.SetTile(new Vector3Int(x, y, 0), SheetTile(floorDeco[rng.Next(floorDeco.Length)]));
                    else if (PondSolid(x, y + 1) && !PondSolid(x, y - 1) && rng.NextDouble() < 0.25)
                        decor.SetTile(new Vector3Int(x, y, 0), SheetTile(ceilDeco[rng.Next(ceilDeco.Length)]));
                }
                else if (PondSolid(x, y - 1) && y == surf && x > 2 && !InPondPool(x) && rng.NextDouble() < 0.35)
                {
                    decor.SetTile(new Vector3Int(x, y, 0), SheetTile(floorDeco[rng.Next(floorDeco.Length)]));
                }
            }
        }
        ground.SetTiles(gPos.ToArray(), gTiles.ToArray());
        back.SetTiles(bPos.ToArray(), bTiles.ToArray());
        TileBase plank = PlankTile();
        for (int i = 0; i < PPlanks.GetLength(0); i++)
            for (int x = PPlanks[i, 0]; x <= PPlanks[i, 1]; x++) planks.SetTile(new Vector3Int(x, PPlanks[i, 2] - 1, 0), plank);

        // ---- joueur, caméra, ciel
        GameObject player = PlacePlayer(new Vector3(3.5f, 0.8f, 0f), true);
        PlaceCamera(player, new Color(0.36f, 0.68f, 0.66f));
        GameObject outside = new GameObject("Décor extérieur");
        GameObject exit = new GameObject("Sortie de la grotte");
        exit.transform.SetParent(outside.transform, false);
        exit.transform.position = new Vector3(0.6f, 1.5f, 0f);
        Child(exit.transform, "Entrée", Art("entree_grotte"), Vector3.zero, 3, Color.white, null);
        SpriteRenderer sun = Child(exit.transform, "Rayons de soleil", Art("rayons"), new Vector3(3f, 4f, 0f), 4, new Color(1f, 0.95f, 0.75f, 0.12f), additive);
        sun.transform.localScale = new Vector3(5f, 5f, 1f);
        sun.transform.rotation = Quaternion.Euler(0, 0, 20f);

        // Fond de forêt : il suit la caméra verticalement (on ne voit jamais son bord, même en sautant)
        GameObject sky = new GameObject("Ciel et forêt");
        sky.transform.SetParent(outside.transform, false);
        float cx = (PX0 + PX1) * 0.5f;
        AddParallaxFollow(sky, "BACKGROUND", 1f, -100, cx, 0.5f, 2.2f, new Color(0.85f, 1f, 0.9f));
        AddParallaxFollow(sky, "WOODS - Fourth", 0.85f, -90, cx, 0.3f, 1.6f, new Color(0.8f, 0.95f, 0.8f));
        AddParallaxFollow(sky, "WOODS - Third", 0.7f, -80, cx, 0f, 1.6f, new Color(0.75f, 0.92f, 0.75f));
        AddParallaxFollow(sky, "VINES - Second", 0.6f, -75, cx, 1.5f, 1.6f, new Color(0.7f, 0.9f, 0.7f));
        AddParallaxFollow(sky, "WOODS - Second", 0.5f, -70, cx, -0.3f, 1.6f, new Color(0.65f, 0.85f, 0.65f));
        AddParallaxFollow(sky, "BUSH - BACKGROUND", 0.3f, -60, cx, -1.2f, 1.6f, new Color(0.6f, 0.85f, 0.55f));

        // ---- acide, nénuphars, plateformes
        GameObject level = new GameObject("Étang");
        for (int i = 0; i < PPools.GetLength(0); i++) PlacePool(level, PPools[i, 0], PPools[i, 1], PPools[i, 2], rng);
        for (int i = 0; i < PPads.GetLength(0); i++) PlacePad(level, PPads[i, 0], (int)PPads[i, 1], PPads[i, 2] > 0.5f);
        for (int i = 0; i < PPlatforms.GetLength(0); i++)
            PlaceMovingPlatform(level, new Vector3(PPlatforms[i, 0], PPlatforms[i, 1], 0f), new Vector3(PPlatforms[i, 2], PPlatforms[i, 3], 0f), PPlatforms[i, 4]);
        for (int i = 0; i < PCrystalWalls.GetLength(0); i++)
            PlaceCrystalWallAt(PCrystalWalls[i, 0], PCrystalWalls[i, 1], PCrystalWalls[i, 2], (int)PCrystalWalls[i, 3]);

        // ---- ennemis
        GameObject slimes = new GameObject("Slimes");
        for (int i = 0; i < PSlimes.GetLength(0); i++)
            Slime.Create(new Vector3(PSlimes[i, 0] + 0.5f, PSlimes[i, 1] + 0.6f, 0f), PSlimes[i, 2] == 1, slimes.transform);
        foreach (object[] m in PMosquitoes)
            PlaceMosquito((string)m[0], new Vector2((float)m[1], (float)m[2]), (Vector2[])m[3], (float)m[4], (bool)m[5]);

        // ---- statues, éclats de vie, checkpoints, aides
        foreach (object[] s in PStatues) PlaceStatue(new Vector3((float)s[0], (int)s[1], 0f), (string)s[2]);
        foreach (object[] s in PShards) PlaceShard(new Vector3((float)s[0], (float)s[1], 0f), (string)s[2]);
        GameObject zones = new GameObject("Zones");
        for (int i = 0; i < PCheckpoints.GetLength(0); i++) AddCheckpoint(zones, PCheckpoints[i, 0], PCheckpoints[i, 1]);

        // ---- l'entrée du donjon (antre du premier gardien : le moustique géant)
        PlaceDungeonEntrance(new Vector3(PFacade.x, PFacade.y, 0f));

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
        ti.zoneTitle = "L'Étang Acide";
        ti.zoneSubtitle = "Les marais rongés";
        AddMusic("musique_etang", 0.5f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, PondScene);
    }

    static bool InPondPool(int x)
    {
        for (int i = 0; i < PPools.GetLength(0); i++)
            if (x >= PPools[i, 0] - 1 && x <= PPools[i, 1] + 1 && PPools[i, 2] >= -1) return true;
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

    static void PlacePool(GameObject parent, int x0, int x1, int baseH, System.Random rng)
    {
        float surface = baseH - 0.35f;
        float bottom = baseH - PoolDepth;
        float width = x1 - x0 + 1;
        GameObject go = new GameObject("Bassin d'acide");
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(x0 + width * 0.5f, (surface + bottom) * 0.5f, 0f);
        go.AddComponent<BoxCollider2D>().isTrigger = true;
        AcidPool pool = go.AddComponent<AcidPool>();
        pool.size = new Vector2(width, surface - bottom);
        pool.damage = 25;

        // Roseaux sur les bords (certains au premier plan)
        foreach (float ex in new[] { x0 - 0.6f, x1 + 1.6f })
        {
            int fx = Mathf.FloorToInt(ex);
            if (PondSolid(fx, baseH)) continue; // pas de place (mur)
            bool front = rng.NextDouble() < 0.4;
            SpriteRenderer r = Child(go.transform, "Roseaux", Art("roseau_" + rng.Next(3)), Vector3.zero, front ? 10012 : 4, Color.white, null);
            r.transform.position = new Vector3(ex, baseH + 0.6f, 0f);
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

    static void PlaceMovingPlatform(GameObject parent, Vector3 a, Vector3 b, float travelTime)
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
        // Halo sombre et brume au sol
        SpriteRenderer fog = Child(root.transform, "Brume", Art("lueur"), new Vector3(0f, 0.6f, 0f), 6, new Color(0.45f, 0.8f, 0.4f, 0.18f), additive);
        fog.transform.localScale = new Vector3(14f, 2.2f, 1f);
        fog.gameObject.AddComponent<LightFlicker>().baseAlpha = 0.18f;

        // La façade du temple (avec son renfoncement en perspective)
        Child(root.transform, "Façade", Art("donjon_facade"), new Vector3(0f, 4.5f, 0f), 2, stone, null);

        // Yeux rouges dans l'obscurité de la porte
        foreach (float ex in new[] { -0.3f, 0.3f })
        {
            SpriteRenderer eye = Child(root.transform, "Œil", Art("lueur"), new Vector3(ex, 3.3f, 0f), 3, new Color(1f, 0.15f, 0.15f, 0.8f), additive);
            eye.transform.localScale = new Vector3(0.22f, 0.15f, 1f);
            LightFlicker lf = eye.gameObject.AddComponent<LightFlicker>();
            lf.baseAlpha = 0.75f; lf.amount = 0.25f; lf.speed = 1.5f;
        }

        // Braseros allumés de part et d'autre des marches
        foreach (float dx in new[] { -4.6f, 4.6f })
        {
            Child(root.transform, "Brasero", Art("brasero"), new Vector3(dx, 0.5f, 0f), 9989, Color.white, null);
            SpriteRenderer fire = Child(root.transform, "Lueur du brasero", Art("lueur"), new Vector3(dx, 0.9f, 0f), 7, new Color(1f, 0.55f, 0.2f, 0.5f), additive);
            fire.transform.localScale = new Vector3(2.6f, 2.6f, 1f);
            fire.gameObject.AddComponent<LightFlicker>();
        }

        // Premier plan : deux grandes colonnes devant le joueur (effet de profondeur)
        foreach (float dx in new[] { -7.2f, 7.2f })
        {
            SpriteRenderer col = Child(root.transform, "Colonne (premier plan)", Art("donjon_colonne"), new Vector3(dx, 3.75f, 0f), 10011, new Color(0.7f, 0.72f, 0.75f), null);
            col.transform.localScale = new Vector3(1.1f, 1f, 1f);
        }

        // Interaction : le joueur entend le gardien
        GameObject door = new GameObject("Porte du donjon");
        door.transform.SetParent(root.transform, false);
        door.transform.localPosition = new Vector3(0f, 2f, 0f);
        BoxCollider2D zone = door.AddComponent<BoxCollider2D>();
        zone.isTrigger = true;
        zone.size = new Vector2(4f, 4f);
        TalkInteractable talk = door.AddComponent<TalkInteractable>();
        talk.promptText = "Écouter";
        talk.promptOffset = new Vector3(0f, 3.2f, 0f);
        talk.shakeCamera = true;
        talk.lines = new[]
        {
            "Un bourdonnement assourdissant résonne au fond de ce temple...",
            "Quelque chose d'énorme vit là-dessous. Et je sens qu'une page du livre est tout près.",
            "Je devrais me préparer avant d'y entrer."
        };
    }
}
