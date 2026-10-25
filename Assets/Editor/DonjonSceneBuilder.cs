using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Le donjon du premier gardien (le moustique géant), sous le temple du Parvis.
//  Le Seuil (statue, fresques) -> La Galerie des Remerciements (calme, silencieuse, de plus en plus menaçante)
//  -> l'antichambre (statue juste avant le combat) -> La Fosse du Gardien (arène) -> La Brèche (sortie).
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
        Color c = x >= 64 ? new Color(0.70f, 0.92f, 0.72f) : x >= 28 ? new Color(0.68f, 0.72f, 0.86f) : new Color(0.78f, 0.80f, 0.92f);
        return new Color(c.r * 0.82f * brightness, c.g * 0.82f * brightness, c.b * 0.82f * brightness, 1f);
    }

    static void BuildDungeon()
    {
        buildingTutorial = false;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        tileCache.Clear();
        GridInit(-4, -8, 145, 30);
        DSolid(gX0, gX1, gY0, gY1);
        System.Random rng = new System.Random(5);

        // ---- plan
        DCarve(0, 27, 0, 9);          // le Seuil
        DCarve(28, 62, 0, 5);         // la galerie des Remerciements (plafond bas, étroite)
        DCarve(52, 62, 0, 9);         // l'antichambre : on respire avant le combat
        DCarve(63, 65, 0, 6);         // le sas (la grille se ferme ici)
        DCarve(66, 122, 0, 23);       // la Fosse du Gardien
        DCarve(123, 125, 0, 6);       // passage vers la Brèche (fermé par une grille)
        DCarve(126, 140, 0, 10);      // la Brèche
        // fosses d'acide dans le sol de l'arène
        DCarve(80, 84, -5, -1);
        DCarve(103, 107, -5, -1);
        // plateformes (planches) : de petites marches de 2 cases, jamais plus
        int[,] plat =
        {
            { 68, 72, 2 }, { 74, 78, 4 }, { 80, 86, 6 }, { 88, 91, 2 }, { 93, 96, 4 }, { 98, 104, 6 }, { 98, 101, 2 },
            { 109, 113, 2 }, { 115, 119, 4 }, { 89, 93, 8 }
        };
        for (int i = 0; i < plat.GetLength(0); i++) DPlanks(plat[i, 0], plat[i, 1], plat[i, 2]);
        // un peu de relief dans la galerie
        DSolid(38, 41, 0, 0); DCarve(38, 41, 1, 1);
        DSolid(46, 48, 0, 1);
        DSolid(33, 34, 4, 5);

        // ---- tuiles
        GameObject gridGO = new GameObject("Grid");
        gridGO.AddComponent<Grid>();
        BuildGridTiles(gridGO, rng, DungeonTint, new Color(0.45f, 0.5f, 0.55f));

        // ---- joueur, caméra, musique
        GameObject player = PlacePlayer(new Vector3(4.5f, 0.8f, 0f), true);
        EnableSafeRespawn(player);
        PlaceCamera(player, new Color(0.03f, 0.05f, 0.04f));
        AddMusic("musique_donjon", 0.5f);

        // ---- acide des fosses
        GameObject level = new GameObject("Donjon");
        PlaceSwampPool(level, 80, 84, -5, 0, false, rng);
        PlaceSwampPool(level, 103, 107, -5, 0, false, rng);

        // ---- statue de prière : une seule, juste avant le combat
        PlaceStatue(new Vector3(58.5f, 0f, 0f), "donjon_avant_salle");

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
            xe.sceneToLoad = "EtangAcide";
            xe.returnPosition = new Vector2(SwDoorX, SwDoorY + 0.8f);
        }

        // ---- indices d'histoire
        GameObject story = new GameObject("Indices");
        PlaceLore(story.transform, 8.5f, 0, "d_statue", "lore_statue", null);
        PlaceLore(story.transform, 12.5f, 0, "d_sceau", "lore_sceau", null);
        PlaceLore(story.transform, 41.5f, 0, "d_relief", "lore_relief", null);
        PlaceLore(story.transform, 28f, 0, "d_procession", "lore_procession", null);
        PlaceLore(story.transform, 137.5f, 0, "d_offrande2", "lore_offrande", null);
        foreach (float ux in new[] { 20.5f, 47.5f, 60.5f, 128.5f })
            PlaceUrn(story, ux, 0);

        // ---- ambiance : coulées d'acide sur les murs, gouttes, lueurs
        GameObject ambience = new GameObject("Ambiance");
        float[] streakX = { 66.5f, 66.5f, 122.5f, 122.5f, 70f, 76f, 88f, 97f, 109f, 116f };
        float[] streakLen = { 12f, 7f, 14f, 6f, 5f, 8f, 4f, 9f, 6f, 10f };
        for (int i = 0; i < streakX.Length; i++)
        {
            bool onWall = i < 4;
            float top = onWall ? 23f - (i % 2) * 4f : 24f;
            SpriteRenderer s = Child(ambience.transform, "Coulée d'acide", Art("acide_coulee"), new Vector3(streakX[i], top - streakLen[i] * 0.5f, 0f), onWall ? 6 : 5, new Color(1f, 1f, 1f, 0.9f), null);
            s.drawMode = SpriteDrawMode.Tiled;
            s.size = new Vector2(1f, streakLen[i]);
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
        AddHalo(ambience, "Lueur de l'arène", new Vector3(94f, 8f, 0f), new Vector2(40f, 18f), new Color(0.45f, 1f, 0.3f, 0.12f));
        AddHalo(ambience, "Lueur de la brèche", new Vector3(133f, 5f, 0f), new Vector2(14f, 8f), new Color(1f, 0.95f, 0.7f, 0.25f));
        foreach (float bx in new[] { 4f, 24f })
        {
            Child(ambience.transform, "Brasero", Art("brasero"), new Vector3(bx, 0.5f, 0f), 9989, Color.white, null);
            SpriteRenderer fire = Child(ambience.transform, "Lueur du brasero", Art("lueur"), new Vector3(bx, 0.9f, 0f), 7, new Color(1f, 0.55f, 0.2f, 0.5f), additive);
            fire.transform.localScale = new Vector3(2.6f, 2.6f, 1f);
            fire.gameObject.AddComponent<LightFlicker>().baseAlpha = 0.5f;
        }

        // ---- grille d'entrée de l'arène (se ferme pendant le combat) et grille de sortie (s'ouvre à la victoire)
        GameObject mech = new GameObject("Mécanismes");
        GameObject lockGO = new GameObject("Grille de l'arène");
        lockGO.transform.SetParent(mech.transform, false);
        lockGO.transform.position = new Vector3(64.5f, 3.5f, 0f);
        BoxCollider2D lockBox = lockGO.AddComponent<BoxCollider2D>();
        lockBox.size = new Vector2(1f, 7f);
        SpriteRenderer lockSr = lockGO.AddComponent<SpriteRenderer>();
        lockSr.sprite = Art("tuile_grille");
        lockSr.drawMode = SpriteDrawMode.Tiled;
        lockSr.size = new Vector2(1f, 7f);
        lockSr.sortingOrder = 9990;
        ArenaLock arenaLock = lockGO.AddComponent<ArenaLock>();
        arenaLock.render = lockSr;
        PlaceGate(mech.transform, 1, 124, 0, 1, 7, "boss1");

        // ---- le gardien
        PlaceBoss(arenaLock);

        // ---- récompense du gardien : un éclat de vie dans la Brèche (derrière la grille, atteignable seulement après le combat)
        PlaceShard(new Vector3(131f, 1.1f, 0f), "donjon_gardien");

        // ---- la page d'amélioration (apparaît quand le gardien est vaincu)
        {
            GameObject pg = new GameObject("Page d'amélioration");
            pg.transform.position = new Vector3(108.5f, 1.8f, 0f);
            GameObject vis = new GameObject("Visuel");
            vis.transform.SetParent(pg.transform, false);
            SpriteRenderer pglow = Child(vis.transform, "Lueur", Art("lueur"), Vector3.zero, 9998, new Color(1f, 0.95f, 0.75f, 0.5f), additive);
            pglow.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            SpriteRenderer psp = Child(vis.transform, "Sprite", Art("page"), Vector3.zero, 10000, Color.white, null);
            CircleCollider2D pz = pg.AddComponent<CircleCollider2D>();
            pz.isTrigger = true;
            pz.radius = 1.5f;
            BossUpgradePage bp = pg.AddComponent<BossUpgradePage>();
            bp.visual = vis;
            bp.pageRenderer = psp;
            bp.glowRenderer = pglow;
            bp.iconFire = Art("icone_feu");
            bp.iconLightning = Art("icone_foudre");
            bp.iconIce = Art("icone_glace");
            bp.glowSprite = Art("lueur");
            bp.fragmentSprite = Art("fragment");
            bp.additiveMaterial = additive;
        }

        // ---- la sortie (La Brèche)
        GameObject exit = new GameObject("Sortie de la Brèche");
        exit.transform.position = new Vector3(139f, 4f, 0f);
        BoxCollider2D eb = exit.AddComponent<BoxCollider2D>();
        eb.isTrigger = true;
        eb.size = new Vector2(2f, 8f);
        ZoneExit ze = exit.AddComponent<ZoneExit>();
        ze.message = "Premier gardien vaincu";
        ze.sub = "À suivre...";

        // ---- titre de la zone, intro
        GameObject zones = new GameObject("Zones");
        AddFallZone(zones, 68f, -20f, 300f);

        GameObject intro = new GameObject("Intro");
        TutorialIntro ti = intro.AddComponent<TutorialIntro>();
        ti.player = player.GetComponent<PlayerMovement>();
        ti.fadeFrom = Color.black;
        ti.fadeDuration = 1.6f;
        ti.lines = new string[0];
        ti.zoneTitle = "Le Donjon du Gardien";
        ti.zoneSubtitle = "Là où l'on veille depuis cinq mille ans";

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, DungeonScene);
    }

    static void PlaceBoss(ArenaLock arenaLock)
    {
        Sprite bossSprite = Art("boss_moustique");
        GameObject root = new GameObject("Gardien : moustique géant");
        root.transform.position = new Vector3(94f, 18f, 0f);

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
        boss.arenaMin = new Vector2(66f, 0f);
        boss.arenaMax = new Vector2(123f, 24f);
        boss.triggerX = 70f;
        boss.entranceLock = arenaLock;
    }
}
