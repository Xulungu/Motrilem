using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Le donjon de Séraphel (le moustique géant), sous le temple du Parvis.
//  Le Seuil (statue, fresques) -> La Galerie des Remerciements (calme, silencieuse, de plus en plus menaçante)
//  -> l'antichambre (statue juste avant le combat) -> La Fosse de Séraphel (arène) -> La Brèche (sortie).
public static partial class TutorialSceneBuilder
{
    const string DungeonSceneName = "Donjon";
    const string DungeonScene = "Assets/Scenes/Donjon.unity";

    static void DCarve(int x0, int x1, int y0, int y1)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                GSet(x, y, false);
                if (x >= gX0 && x <= gX1 && y >= gY0 && y <= gY1) gBack[x - gX0, y - gY0] = true;
            }
    }

    static void DSolid(int x0, int x1, int y0, int y1)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++) GSet(x, y, true);
    }

    static void DPlanks(int x0, int x1, int stand)
    {
        for (int x = x0; x <= x1; x++)
            if (x >= gX0 && x <= gX1 && stand - 1 >= gY0 && stand - 1 <= gY1) gPlank[x - gX0, stand - 1 - gY0] = true;
    }

    static Color DungeonTint(int x, int y, float brightness)
    {
        Color c = x >= 166 ? new Color(0.70f, 0.92f, 0.72f) : x >= 28 ? new Color(0.68f, 0.72f, 0.86f) : new Color(0.78f, 0.80f, 0.92f);
        // pierre sale et inégale : plaques sombres, taches de mousse
        float patch = 0.62f + 0.38f * Mathf.Abs(Mathf.Sin(x * 0.53f + y * 0.71f) * Mathf.Cos(x * 0.19f - y * 0.37f));
        float mossy = Mathf.Max(0f, Mathf.Sin(x * 0.23f + y * 1.3f) * Mathf.Sin(y * 0.47f + x * 0.11f));
        float k = 0.74f * brightness * patch;
        return new Color(c.r * k * (1f - 0.25f * mossy), c.g * k * (1f + 0.1f * mossy), c.b * k * (1f - 0.3f * mossy), 1f);
    }

    const int Off = 100; // décalage de l'arène et de la Brèche (les salles de vagues sont avant)

    static ArenaLock MakeLock(Transform parent, float x, float height)
    {
        GameObject go = new GameObject("Grille");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, height * 0.5f, 0f);
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

    static WaveRoom.Spawn Sp(int kind, float x, float y)
    {
        WaveRoom.Spawn s = new WaveRoom.Spawn();
        s.kind = kind; s.x = x; s.y = y;
        return s;
    }

    static WaveRoom.Wave Wv(params WaveRoom.Spawn[] spawns)
    {
        WaveRoom.Wave w = new WaveRoom.Wave();
        w.spawns = spawns;
        return w;
    }

    static WaveRoom PlaceWaveRoom(Transform parent, string id, string name, int x0, int x1, ArenaLock entry, ArenaLock exit, WaveRoom.Wave[] waves)
    {
        GameObject go = new GameObject("Salle de vagues : " + name);
        go.transform.SetParent(parent, false);
        WaveRoom wr = go.AddComponent<WaveRoom>();
        wr.roomId = id;
        wr.roomName = name;
        wr.min = new Vector2(x0, 0f);
        wr.max = new Vector2(x1 + 1, 11f);
        wr.entryLock = entry;
        wr.exitLock = exit;
        wr.waves = waves;
        return wr;
    }

    static void BuildDungeon()
    {
        buildingTutorial = false;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        tileCache.Clear();
        GridInit(-4, -8, 250, 30);
        DSolid(gX0, gX1, gY0, gY1);
        System.Random rng = new System.Random(5);

        // ---- plan
        DCarve(0, 27, 0, 9);                 // le Seuil (statue de sauvegarde)
        DCarve(28, 62, 0, 5);                // la galerie des Remerciements (plafond bas, étroite)
        DCarve(52, 62, 0, 9);                // l'antichambre
        DCarve(63, 65, 0, 6);                // couloir
        DCarve(66, 89, 0, 10);               // salle des vagues 1
        DCarve(90, 94, 0, 6);
        DCarve(95, 119, 0, 10);              // salle des vagues 2
        DCarve(120, 124, 0, 6);
        DCarve(125, 149, 0, 10);             // salle des vagues 3
        DCarve(150, 165, 0, 6);              // couloir de la statue du héros, puis le sas
        DCarve(Off + 66, Off + 122, 0, 23);  // la Fosse du Gardien
        DCarve(Off + 123, Off + 125, 0, 6);  // passage vers la Brèche (fermé par une grille)
        DCarve(Off + 126, Off + 140, 0, 10); // la Brèche
        DSolid(Off + 136, Off + 140, 4, 10); // le recoin bas où dort le fragment de vie et la page
        // fosses d'acide dans le sol de l'arène
        DCarve(Off + 80, Off + 84, -5, -1);
        DCarve(Off + 103, Off + 107, -5, -1);
        // plaques de sol qui s'effondrent pendant le combat (acide en dessous)
        int[,] plates = { { 70, 74 }, { 88, 92 }, { 95, 99 }, { 110, 114 }, { 116, 120 } };
        for (int i = 0; i < plates.GetLength(0); i++) DCarve(Off + plates[i, 0], Off + plates[i, 1], -5, -1);
        // plateformes (planches) : de petites marches de 2 cases, jamais plus
        int[,] plat =
        {
            { 68, 72, 2 }, { 74, 78, 4 }, { 80, 86, 6 }, { 88, 91, 2 }, { 93, 96, 4 }, { 98, 104, 6 }, { 98, 101, 2 },
            { 109, 113, 2 }, { 115, 119, 4 }, { 89, 93, 8 }
        };
        for (int i = 0; i < plat.GetLength(0); i++) DPlanks(Off + plat[i, 0], Off + plat[i, 1], plat[i, 2]);
        // planches des salles de vagues
        int[,] wplat =
        {
            { 70, 74, 2 }, { 76, 80, 4 }, { 82, 86, 2 },
            { 99, 103, 2 }, { 105, 109, 4 }, { 111, 115, 2 },
            { 129, 133, 2 }, { 135, 139, 4 }, { 141, 145, 2 }
        };
        for (int i = 0; i < wplat.GetLength(0); i++) DPlanks(wplat[i, 0], wplat[i, 1], wplat[i, 2]);
        // un peu de relief dans la galerie
        DSolid(38, 41, 0, 0); DCarve(38, 41, 1, 1);
        DSolid(46, 48, 0, 1);
        DSolid(33, 34, 4, 5);
        DungeonRuinShape();                  // plafonds éboulés, petits tas de gravats

        // ---- tuiles
        GameObject gridGO = new GameObject("Grid");
        gridGO.AddComponent<Grid>();
        BuildGridTiles(gridGO, rng, DungeonTint, new Color(0.45f, 0.5f, 0.55f));
        DungeonRuinTiles(gridGO);            // briques abîmées (fissures, mousse, bords éclatés)

        // ---- joueur, caméra, musique
        GameObject player = PlacePlayer(new Vector3(4.5f, 0.8f, 0f), true);
        EnableSafeRespawn(player);
        PlaceCamera(player, new Color(0.03f, 0.05f, 0.04f));
        AddMusic("musique_donjon", 0.5f);

        // ---- acide des fosses et sous les plaques
        GameObject level = new GameObject("Donjon");
        PlaceSwampPool(level, Off + 80, Off + 84, -5, 0, false, rng);
        PlaceSwampPool(level, Off + 103, Off + 107, -5, 0, false, rng);
        GameObject platesGO = new GameObject("Plaques du sol");
        for (int i = 0; i < plates.GetLength(0); i++)
        {
            int px0 = Off + plates[i, 0], px1 = Off + plates[i, 1];
            PlaceSwampPool(level, px0, px1, -5, 0, false, rng);
            float w = px1 - px0 + 1;
            GameObject pl = new GameObject("Plaque de sol");
            pl.transform.SetParent(platesGO.transform, false);
            pl.transform.position = new Vector3(px0 + w * 0.5f, -2.5f, 0f);
            BoxCollider2D pb = pl.AddComponent<BoxCollider2D>();
            pb.size = new Vector2(w, 5f);
            // même briques et même teinte que le reste du sol (la plaque ne doit pas se distinguer avant de s'effondrer)
            System.Random pr = new System.Random(900 + i);
            for (int cx = px0; cx <= px1; cx++)
                for (int cy = -5; cy <= -1; cy++)
                {
                    string tn = cy == -1 ? RuinPick(pr, "h", new[] { 3, 2, 2, 2 }) : RuinPick(pr, "c", new[] { 4, 2, 2, 1, 2, 2 });
                    SpriteRenderer tsr = Child(pl.transform, "Tuile", Art("tuile_ruine_" + tn), new Vector3(cx + 0.5f - (px0 + w * 0.5f), cy + 0.5f + 2.5f, 0f), 0, DungeonTint(cx, cy, 1f), null);
                    tsr.gameObject.layer = pl.layer;
                }
            pl.AddComponent<CrumblePlate>();
        }

        // ---- statue de sauvegarde à l'entrée du donjon, statue du héros avant l'arène
        PlaceStatue(new Vector3(9.5f, 0f, 0f), "donjon_entree");
        GameObject story = new GameObject("Décor");
        PlaceLore(story.transform, 157.5f, 0, "d_statue", "lore_statue", null);
        foreach (float ux in new[] { 20.5f, 47.5f, 60.5f, 72.5f, 102.5f, 132.5f, 155.5f, Off + 128.5f })
            PlaceUrn(story, ux, 0);

        // ---- porte de sortie (retour au marécage)
        {
            GameObject xd = new GameObject("Porte de sortie du donjon");
            xd.transform.position = new Vector3(1.6f, 0f, 0f);
            Child(xd.transform, "Porte", Art("porte_ouverte"), new Vector3(0f, 1.8f, 0f), 3, new Color(0.55f, 0.58f, 0.6f), null).transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            SpriteRenderer xg = Child(xd.transform, "Lueur", Art("lueur"), new Vector3(0f, 1.8f, 0f), 2, new Color(0.7f, 0.85f, 0.6f, 0.18f), additive);
            xg.transform.localScale = new Vector3(2.4f, 3.4f, 1f);
            BoxCollider2D xb = xd.AddComponent<BoxCollider2D>();
            xb.isTrigger = true;
            xb.size = new Vector2(2.6f, 3.6f);
            xb.offset = new Vector2(0f, 1.8f);
            DungeonExitDoor xe = xd.AddComponent<DungeonExitDoor>();
            xe.sceneToLoad = SwGroupNames[SwDoorGroup];   // la porte du donjon se trouve sur la carte du cœur du marécage
            xe.returnPosition = new Vector2(SwDoorX, SwDoorY + 0.8f);
        }

        DungeonRuinDecor(story.transform);   // colonnes brisées, gravats, bannières déchirées, toiles, poutres, puits de lumière

        // ---- ambiance : coulées d'acide sur les murs, gouttes, lueurs
        GameObject ambience = new GameObject("Ambiance");
        float[] streakX = { Off + 66.5f, Off + 66.5f, Off + 122.5f, Off + 122.5f, Off + 70f, Off + 76f, Off + 88f, Off + 97f, Off + 109f, Off + 116f };
        float[] streakLen = { 12f, 7f, 14f, 6f, 5f, 8f, 4f, 9f, 6f, 10f };
        for (int i = 0; i < streakX.Length; i++)
        {
            bool onWall = i < 4;
            float top = onWall ? 23f - (i % 2) * 4f : 24f;
            SpriteRenderer sr = Child(ambience.transform, "Coulée d'acide", Art("acide_coulee"), new Vector3(streakX[i], top - streakLen[i] * 0.5f, 0f), onWall ? 6 : 5, new Color(1f, 1f, 1f, 0.9f), null);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(1f, streakLen[i]);
            SpriteRenderer glow = Child(ambience.transform, "Lueur de la coulée", Art("lueur"), new Vector3(streakX[i], top - streakLen[i] * 0.5f, 0f), -3, new Color(0.5f, 1f, 0.3f, 0.12f), additive);
            glow.transform.localScale = new Vector3(2.5f, streakLen[i] * 0.7f, 1f);
            GameObject drip = new GameObject("Goutte d'acide");
            drip.transform.SetParent(ambience.transform, false);
            drip.transform.position = new Vector3(streakX[i], top - streakLen[i], 0f);
            drip.AddComponent<AcidDrip>().interval = 1.6f + (i % 3) * 0.5f;
        }
        foreach (float gx in new[] { 36f, 44f, 52f })
        {
            GameObject drip = new GameObject("Goutte d'acide");
            drip.transform.SetParent(ambience.transform, false);
            drip.transform.position = new Vector3(gx, 5.2f, 0f);
            drip.AddComponent<AcidDrip>().interval = 3.2f;
        }
        AddHalo(ambience, "Lueur du seuil", new Vector3(14f, 4f, 0f), new Vector2(10f, 6f), new Color(0.55f, 0.8f, 1f, 0.2f));
        AddHalo(ambience, "Lueur de l'arène", new Vector3(Off + 94f, 8f, 0f), new Vector2(40f, 18f), new Color(0.45f, 1f, 0.3f, 0.12f));
        AddHalo(ambience, "Lueur de la brèche", new Vector3(Off + 133f, 5f, 0f), new Vector2(14f, 8f), new Color(1f, 0.95f, 0.7f, 0.25f));
        foreach (float rx in new[] { 78f, 107f, 137f })
            AddHalo(ambience, "Lueur de salle", new Vector3(rx, 5f, 0f), new Vector2(22f, 8f), new Color(0.5f, 0.75f, 1f, 0.14f));
        foreach (float bx in new[] { 4f, 24f, 68f, 87f, 97f, 117f, 127f, 147f })
        {
            Child(ambience.transform, "Brasero", Art("brasero"), new Vector3(bx, 0.5f, 0f), 9989, Color.white, null);
            SpriteRenderer fire = Child(ambience.transform, "Lueur du brasero", Art("lueur"), new Vector3(bx, 0.9f, 0f), 7, new Color(1f, 0.55f, 0.2f, 0.5f), additive);
            fire.transform.localScale = new Vector3(2.6f, 2.6f, 1f);
            fire.gameObject.AddComponent<LightFlicker>().baseAlpha = 0.5f;
        }

        // ---- grilles et salles de vagues
        GameObject mech = new GameObject("Mécanismes");
        ArenaLock lk0 = MakeLock(mech.transform, 64.5f, 7f);    // entrée salle 1
        ArenaLock lk1 = MakeLock(mech.transform, 90.5f, 7f);    // sortie salle 1
        ArenaLock lk2 = MakeLock(mech.transform, 94.5f, 7f);    // entrée salle 2
        ArenaLock lk3 = MakeLock(mech.transform, 120.5f, 7f);   // sortie salle 2
        ArenaLock lk4 = MakeLock(mech.transform, 124.5f, 7f);   // entrée salle 3
        ArenaLock lk5 = MakeLock(mech.transform, 150.5f, 7f);   // sortie salle 3
        GameObject waveRoot = new GameObject("Vagues");
        PlaceWaveRoom(waveRoot.transform, "salle1", "Salle des vagues I", 66, 89, lk0, lk1, new[]
        {
            Wv(Sp(0, 74f, 0.6f), Sp(0, 80f, 0.6f), Sp(0, 86f, 0.6f)),
            Wv(Sp(1, 76f, 0.8f), Sp(1, 84f, 0.8f), Sp(2, 70f, 7f))
        });
        PlaceWaveRoom(waveRoot.transform, "salle2", "Salle des vagues II", 95, 119, lk2, lk3, new[]
        {
            Wv(Sp(2, 98f, 7f), Sp(2, 104f, 8f), Sp(2, 112f, 7f), Sp(2, 116f, 8f)),
            Wv(Sp(1, 100f, 0.8f), Sp(1, 112f, 0.8f), Sp(2, 106f, 6f), Sp(2, 108f, 8f)),
            Wv(Sp(0, 98f, 0.6f), Sp(0, 104f, 0.6f), Sp(0, 110f, 0.6f), Sp(0, 116f, 0.6f))
        });
        PlaceWaveRoom(waveRoot.transform, "salle3", "Salle des vagues III", 125, 149, lk4, lk5, new[]
        {
            Wv(Sp(3, 129f, 7f), Sp(3, 145f, 7f), Sp(0, 135f, 0.6f), Sp(0, 140f, 0.6f)),
            Wv(Sp(1, 130f, 0.8f), Sp(1, 138f, 0.8f), Sp(1, 146f, 0.8f)),
            Wv(Sp(3, 128f, 8f), Sp(3, 138f, 9f), Sp(3, 146f, 8f), Sp(2, 132f, 6f), Sp(2, 142f, 6f))
        });

        // ---- grille d'entrée de l'arène (se ferme pendant le combat) et grille de sortie (s'ouvre à la victoire)
        ArenaLock arenaLock = MakeLock(mech.transform, 164.5f, 7f);
        PlaceGate(mech.transform, 1, Off + 124, 0, 1, 7, "boss1");

        // ---- éboulement : scène où le couloir d'entrée s'effondre après le combat (mur de pierres ruinées construit à l'exécution)
        GameObject cave = new GameObject("Éboulement");
        cave.transform.position = new Vector3(164f, 0f, 0f);
        CaveCollapse cc = cave.AddComponent<CaveCollapse>();
        cc.width = 4;
        cc.height = 7;
        cc.abdomenRest = new Vector3(Off + 94f, 0.9f, 0f);
        cc.wallMin = new Vector2(Off + 62f, 4f);
        cc.wallMax = new Vector2(Off + 67f, 9f);

        // ---- le gardien
        PlaceBoss(arenaLock, cc);

        // ---- la Brèche : statue de sauvegarde, récompense du gardien (fragment de vie et page dans le recoin), porte vers la suite
        PlaceStatue(new Vector3(Off + 129.5f, 0f, 0f), "donjon_breche");
        PlaceShard(new Vector3(Off + 137.4f, 1.1f, 0f), "donjon_gardien");
        {
            GameObject pg = new GameObject("Page de Séraphel");
            pg.transform.position = new Vector3(Off + 139.2f, 1.9f, 0f);
            GameObject vis = new GameObject("Visuel");
            vis.transform.SetParent(pg.transform, false);
            SpriteRenderer pglow = Child(vis.transform, "Lueur", Art("lueur"), Vector3.zero, 9998, new Color(1f, 0.95f, 0.75f, 0.5f), additive);
            pglow.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            SpriteRenderer psp = Child(vis.transform, "Sprite", Art("page"), Vector3.zero, 10000, Color.white, null);
            CircleCollider2D pz = pg.AddComponent<CircleCollider2D>();
            pz.isTrigger = true;
            pz.radius = 1.3f;
            BossUpgradePage bp = pg.AddComponent<BossUpgradePage>();
            bp.visual = vis;
            bp.pageRenderer = psp;
            bp.glowRenderer = pglow;
            bp.glowSprite = Art("lueur");
            bp.fragmentSprite = Art("fragment");
            bp.additiveMaterial = additive;
        }
        {
            GameObject nd = new GameObject("Porte vers la suite");
            nd.transform.position = new Vector3(Off + 133.5f, 0f, 0f);
            Child(nd.transform, "Porte", Art("porte_ouverte"), new Vector3(0f, 1.95f, 0f), 3, new Color(0.7f, 0.7f, 0.68f), null).transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            SpriteRenderer ng = Child(nd.transform, "Lueur", Art("lueur"), new Vector3(0f, 1.95f, 0f), 2, new Color(1f, 0.95f, 0.7f, 0.3f), additive);
            ng.transform.localScale = new Vector3(3f, 4f, 1f);
            BoxCollider2D nb = nd.AddComponent<BoxCollider2D>();
            nb.isTrigger = true;
            nb.size = new Vector2(2.8f, 3.9f);
            nb.offset = new Vector2(0f, 1.95f);
            nd.AddComponent<ToBeContinuedDoor>();
        }

        // ---- zones de chute, intro
        GameObject zones = new GameObject("Zones");
        AddFallZone(zones, 120f, -20f, 400f);

        GameObject intro = new GameObject("Intro");
        TutorialIntro ti = intro.AddComponent<TutorialIntro>();
        ti.player = player.GetComponent<PlayerMovement>();
        ti.fadeFrom = Color.black;
        ti.fadeDuration = 1.6f;
        ti.lines = new string[0];
        ti.zoneTitle = "Le Donjon de Séraphel";
        ti.zoneSubtitle = "Là où l'on prie depuis cinq mille ans";

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, DungeonScene);
    }

    static void PlaceBoss(ArenaLock arenaLock, CaveCollapse collapse)
    {
        Sprite bossSprite = Art("boss_moustique");
        GameObject root = new GameObject("Séraphel : moustique géant");
        root.transform.position = new Vector3(Off + 94f, 18f, 0f);

        GameObject bodyGO = new GameObject("Corps");
        bodyGO.transform.SetParent(root.transform, false);
        bodyGO.transform.localScale = new Vector3(0.17f, 0.17f, 1f);
        SpriteRenderer sr = bodyGO.AddComponent<SpriteRenderer>();
        sr.sprite = bossSprite;
        sr.sortingOrder = 9997;

        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;

        // zone où les sorts le touchent
        CircleCollider2D hurt = root.AddComponent<CircleCollider2D>();
        hurt.isTrigger = true;
        hurt.radius = 1.25f;

        // corps dangereux au contact
        GameObject hit = new GameObject("Hitbox");
        hit.transform.SetParent(root.transform, false);
        BoxCollider2D hb = hit.AddComponent<BoxCollider2D>();
        hb.isTrigger = true;
        hb.size = new Vector2(2.3f, 1.5f);
        EnemyDamage dmg = hit.AddComponent<EnemyDamage>();
        dmg.damage = 25;
        dmg.knockback = new Vector2(6f, 5f);

        GameObject remains = new GameObject("Dépouille");
        remains.transform.SetParent(root.transform, false);
        EnemyHealth eh = root.AddComponent<EnemyHealth>();
        eh.maxHealth = 40;
        eh.rootToDestroy = remains;
        eh.deathStyle = EnemyHealth.DeathStyle.Chute;
        eh.deathSound = "mort_ennemi";
        eh.shardColor = new Color(0.6f, 1f, 0.3f);

        BossMosquito boss = root.AddComponent<BossMosquito>();
        boss.body = sr;
        boss.arenaMin = new Vector2(Off + 66f, 0f);
        boss.arenaMax = new Vector2(Off + 123f, 24f);
        boss.triggerX = Off + 70f;
        boss.entranceLock = arenaLock;
        boss.pitCenters = new[] { new Vector2(Off + 82.5f, 0f), new Vector2(Off + 105.5f, 0f) };
        boss.collapse = collapse;
    }
}
