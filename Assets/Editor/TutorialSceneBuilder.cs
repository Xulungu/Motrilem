using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Construit les scènes de Motrilem :
//   - Menu      : menu d'accueil (Jouer / Paramètres / Quitter)
//   - Tutorial  : la grotte (tutoriel de base, livre ancien, tutoriel de magie, porte de sortie)
//   - SampleScene (scène principale) : transformée en extérieur, on y arrive en sortant de la grotte
// Menu Motrilem > Tout (re)construire. À l'ouverture du projet, Unity propose de le faire si besoin.
public static partial class TutorialSceneBuilder
{
    const int Version = 28;
    const string VersionKey = "Motrilem.BuilderVersion";

    const string MenuScene = "Assets/Scenes/Menu.unity";
    const string TutorialScene = "Assets/Scenes/Tutorial.unity";
    const string MainScene = "Assets/Scenes/SampleScene.unity";
    const string PondScene = "Assets/Scenes/EtangAcide.unity";   // = première carte du marécage (SwampZones.Names[0])
    const string ArtDir = "Assets/Art/Tutorial";
    const string ResSprites = "Assets/Resources/Sprites";
    const string BackgroundDir = "Assets/karsiori/TileMap/Backgrounds";
    const string PrefabDir = "Assets/Prefabs";
    const string TileDir = "Assets/Tiles";

    // Limites de la zone de tuiles (en cases)
    const int MinX = -18, MaxX = 228, MinY = -16, MaxY = 22;
    // Intérieur de la grotte
    const int CaveStart = 0, CaveEnd = 208;

    // ================================================================== menus
    [MenuItem("Motrilem/Tout (re)construire")]
    public static void BuildAllFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!EditorUtility.DisplayDialog("Motrilem",
                "Reconstruire le menu, la grotte (Tutorial) et mettre à jour la scène principale ?\n\n(Les modifications faites à la main dans Menu et Tutorial seront perdues. Dans SampleScene, seuls le décor extérieur, l'intro et la position de départ du joueur sont modifiés.)",
                "Construire", "Annuler"))
            return;
        BuildAll();
    }

    [MenuItem("Motrilem/Construire seulement la grotte (Tutorial)")]
    public static void BuildTutorialFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Run(() => { PrepareAssets(); BuildTutorial(); AddToBuildSettings(); });
    }

    [MenuItem("Motrilem/Créer le jeu (.exe Windows)")]
    public static void BuildGame()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(MenuScene))
        {
            EditorUtility.DisplayDialog("Motrilem", "Construis d'abord les scènes (Motrilem > Tout (re)construire).", "OK");
            return;
        }
        AddToBuildSettings();

        PlayerSettings.productName = "Motrilem";
        PlayerSettings.companyName = "Motrilem";
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultIsNativeResolution = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.resizableWindow = true;

        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene sc in EditorBuildSettings.scenes) if (sc.enabled) scenes.Add(sc.path);

        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..")); // le .exe est à la racine du projet
        Directory.CreateDirectory(folder);
        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = scenes.ToArray();
        options.locationPathName = Path.Combine(folder, "Motrilem.exe");
        options.target = BuildTarget.StandaloneWindows64;
        options.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            EditorUtility.DisplayDialog("Motrilem",
                "Le jeu est prêt !\n\n" + options.locationPathName + "\n\nPour le partager, envoie le .exe avec UnityPlayer.dll, UnityCrashHandler64.exe et les dossiers Motrilem_Data et MonoBleedingEdge (il a besoin des fichiers à côté de lui).",
                "Ouvrir le dossier");
            EditorUtility.RevealInFinder(options.locationPathName);
        }
        else
        {
            EditorUtility.DisplayDialog("Motrilem", "La création du jeu a échoué (" + report.summary.result + ").\nRegarde la Console pour le détail.", "OK");
        }
    }

    [MenuItem("Motrilem/Effacer la sauvegarde")]
    public static void DeleteSave()
    {
        SaveSystem.NewGame();
        Debug.Log("Motrilem : sauvegarde effacée (" + SaveSystem.FilePath + ").");
    }

    [MenuItem("Motrilem/Effacer la magie sauvegardée")]
    public static void ResetSavedMagic()
    {
        PlayerPrefs.DeleteKey(PlayerMagic.SaveKey);
        PlayerPrefs.Save();
        Debug.Log("Motrilem : affinité magique sauvegardée effacée.");
    }

    [InitializeOnLoadMethod]
    static void OfferAutoBuild()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetInt(VersionKey + "." + Application.dataPath, 0) >= Version && File.Exists(MenuScene)) return;
            if (SessionState.GetBool("Motrilem.Proposed", false)) return;
            SessionState.SetBool("Motrilem.Proposed", true);
            if (EditorUtility.DisplayDialog("Motrilem",
                    "Nouvelle version des scènes disponible :\n• le Marécage Acide entièrement refait (27 salles, 5 régions, raccourcis, secrets)\n• le donjon et le combat contre le moustique géant\n\nLes (re)construire maintenant ?\n(Possible plus tard via le menu Motrilem.)",
                    "Construire", "Plus tard"))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) BuildAll();
            }
        };
    }

    static void BuildAll()
    {
        Run(() =>
        {
            PrepareAssets();
            EditorUtility.DisplayProgressBar("Motrilem", "Scène principale (extérieur)...", 0.3f);
            UpdateMainScene();
            EditorUtility.DisplayProgressBar("Motrilem", "Grotte et tutoriels...", 0.55f);
            BuildTutorial();
            EditorUtility.DisplayProgressBar("Motrilem", "L'Étang Acide...", 0.75f);
            BuildPond();
            EditorUtility.DisplayProgressBar("Motrilem", "Le donjon de Séraphel...", 0.82f);
            BuildDungeon();
            EditorUtility.DisplayProgressBar("Motrilem", "Menu d'accueil...", 0.9f);
            BuildMenu();
            AddToBuildSettings();
            EditorPrefs.SetInt(VersionKey + "." + Application.dataPath, Version);
            EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Motrilem",
                "Tout est prêt !\n\nLa scène Menu est ouverte : appuie sur Play pour lancer le jeu depuis le menu d'accueil.",
                "Super");
        });
    }

    static void Run(System.Action action)
    {
        try
        {
            action();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Motrilem", "La construction a échoué :\n" + e.Message + "\n\n(Détails dans la Console.)", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ================================================================== assets
    static Material additive;
    static GameObject playerPrefab, mosquitoPrefab;

    static void PrepareAssets()
    {
        EditorUtility.DisplayProgressBar("Motrilem", "Réglage des sprites et des sons...", 0.05f);
        AssetDatabase.Refresh();
        ConfigureSprites(ArtDir, false);
        ConfigureSprites(ResSprites, false);
        ConfigureSprites(BackgroundDir, true);
        additive = GetAdditiveMaterial();
        EditorUtility.DisplayProgressBar("Motrilem", "Prefabs du joueur et du moustique...", 0.15f);
        CreatePrefabs();
    }

    static void ConfigureSprites(string folder, bool tiledBackground)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            string file = Path.GetFileNameWithoutExtension(path);
            bool soft = file.StartsWith("lueur") || file == "rayons" || file == "poussiere";
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = tiledBackground || soft ? 32f : 16f;
            ti.filterMode = soft ? FilterMode.Bilinear : FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            bool repeat = tiledBackground || file == "cristal" || file.StartsWith("acide") || file.StartsWith("tuile_");
            ti.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            // Les sprites répétés (mode "Tiled") ont besoin d'un maillage rectangulaire complet
            TextureImporterSettings settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = repeat ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }
    }

    static Sprite Art(string name)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/" + name + ".png");
        if (s == null) s = AssetDatabase.LoadAssetAtPath<Sprite>(ResSprites + "/" + name + ".png");
        if (s == null) s = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundDir + "/" + name + ".png");
        if (s == null) Debug.LogWarning("Motrilem : sprite introuvable : " + name);
        return s;
    }

    static Material GetAdditiveMaterial()
    {
        // Dans Resources pour que les scripts puissent le charger en jeu (FxUtil.Additive)
        string path = "Assets/Resources/Additif.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        Shader sh = Shader.Find("Legacy Shaders/Particles/Additive");
        if (sh == null) sh = Shader.Find("Particles/Additive");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        m = new Material(sh);
        if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void CreatePrefabs()
    {
        if (!AssetDatabase.IsValidFolder(PrefabDir)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Joueur.prefab");
        mosquitoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Moustique.prefab");
        if (playerPrefab != null && mosquitoPrefab != null) return;

        Scene main = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
        GameObject player = FindRoot(main, "Player");
        GameObject enemy = FindRoot(main, "Ennemy");
        if (player == null) throw new System.Exception("Objet 'Player' introuvable dans " + MainScene);
        if (playerPrefab == null) playerPrefab = PrefabUtility.SaveAsPrefabAsset(player, PrefabDir + "/Joueur.prefab");
        if (mosquitoPrefab == null && enemy != null) mosquitoPrefab = PrefabUtility.SaveAsPrefabAsset(enemy, PrefabDir + "/Moustique.prefab");
    }

    static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            Transform r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    static SpriteRenderer Child(Transform parent, string name, Sprite sprite, Vector3 local, int order, Color color, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        sr.color = color;
        if (mat != null) sr.sharedMaterial = mat;
        return sr;
    }

    static void AddMusic(string music, float volume)
    {
        GameObject go = new GameObject("Musique");
        SceneMusic sm = go.AddComponent<SceneMusic>();
        sm.music = music;
        sm.volume = volume;
    }

    // ================================================================== menu
    static void BuildMenu()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.055f, 0.045f, 0.09f);
        camGO.AddComponent<AudioListener>();
        camGO.transform.position = new Vector3(0, 0, -10);

        GameObject menu = new GameObject("Menu");
        menu.AddComponent<MainMenu>().firstScene = "Tutorial";
        AddMusic("musique_grotte", 0.5f);

        EditorSceneManager.SaveScene(scene, MenuScene);
    }

    // ================================================================== grotte : profil du niveau
    // Hauteur du sol (première case vide) pour chaque colonne
    static int FloorAt(int x)
    {
        if (x < CaveStart || x > CaveEnd) return 999;
        if (x >= 22 && x <= 26) return 1;                               // marche (apprendre à sauter)
        if ((x >= 38 && x <= 40) || (x >= 44 && x <= 51)) return -14;   // gouffres (le 2e demande de l'élan)
        if (x >= 136 && x <= 153) return -4;                            // tunnel sous les planches
        if (x == 154) return -3;
        if (x == 155) return -2;
        if (x == 156) return -1;
        return 0;
    }

    // Première case pleine du plafond pour chaque colonne
    static int CeilAt(int x)
    {
        if (x >= 87 && x <= 110) return 10 + Mathf.RoundToInt(2.5f * Mathf.Sin(Mathf.PI * (x - 87) / 23f)); // salle du livre
        if (x >= 128 && x <= 130) return 4;                           // passage bloqué par les cristaux
        if (x >= 111 && x <= 133) return 7;                           // couloir de la magie
        if (x >= 146 && x <= 150) return 0;                           // mur : il faut passer par le tunnel
        if (x >= 134 && x <= 156) return 6;
        if (x >= 157 && x <= 196) return 9 + Mathf.RoundToInt(Mathf.Sin(x * 0.3f) * 0.8f); // arène des piqueurs
        if (x >= 197) return 6;                                       // hall de sortie
        return 8 + Mathf.RoundToInt(Mathf.Sin(x * 0.35f) * 0.7f + Mathf.Sin(x * 0.13f + 1f) * 0.8f);
    }

    static bool Solid(int x, int y)
    {
        if (x < CaveStart || x > CaveEnd) return true;
        return y < FloorAt(x) || y >= CeilAt(x);
    }

    // Choix automatique de la tuile selon les voisins (tuiles de sheet.png)
    static int TileIndexFor(int x, int y)
    {
        bool up = !Solid(x, y + 1), down = !Solid(x, y - 1), left = !Solid(x - 1, y), right = !Solid(x + 1, y);
        if (up) return left ? 66 : right ? 68 : 67;
        if (down) return left ? 90 : right ? 92 : 91;
        if (left) return 78;
        if (right) return 80;
        return 79;
    }

    static readonly Dictionary<int, TileBase> tileCache = new Dictionary<int, TileBase>();
    static TileBase SheetTile(int n)
    {
        TileBase t;
        if (tileCache.TryGetValue(n, out t) && t != null) return t;
        t = AssetDatabase.LoadAssetAtPath<TileBase>(TileDir + "/sheet_" + n + ".asset");
        if (t == null) throw new System.Exception("Tuile introuvable : " + TileDir + "/sheet_" + n + ".asset");
        tileCache[n] = t;
        return t;
    }

    static TileBase PlankTile()
    {
        string path = ArtDir + "/planche_tile.asset";
        Tile t = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (t != null) return t;
        t = ScriptableObject.CreateInstance<Tile>();
        t.sprite = Art("planche");
        t.colliderType = Tile.ColliderType.Grid;
        AssetDatabase.CreateAsset(t, path);
        return t;
    }

    static Tilemap CreateTilemap(string name, Transform grid, int order, bool collider, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(grid, false);
        Tilemap tm = go.AddComponent<Tilemap>();
        tm.color = color;
        TilemapRenderer r = go.AddComponent<TilemapRenderer>();
        r.sortingOrder = order;
        if (collider)
        {
            // TilemapCollider2D simple : ses formes sont recalculées au chargement de la scène.
            // (Un CompositeCollider2D enregistré avant d'avoir été calculé restait vide quand
            //  la scène était chargée depuis le menu : le joueur passait à travers le sol.)
            go.AddComponent<TilemapCollider2D>();
        }
        return tm;
    }

    static readonly int[] SignXs = { 5, 17, 30, 62, 91, 115, 132, 159, 199 };

    static void BuildTiles()
    {
        tileCache.Clear();
        GameObject gridGO = new GameObject("Grid");
        gridGO.AddComponent<Grid>();
        Tilemap back = CreateTilemap("Fond", gridGO.transform, -10, false, new Color(0.62f, 0.6f, 0.72f));
        Tilemap ground = CreateTilemap("Sol", gridGO.transform, 0, true, Color.white);
        Tilemap decor = CreateTilemap("Décor", gridGO.transform, 1, false, Color.white);

        // Plateformes traversables (on saute à travers par-dessous, S pour descendre)
        Tilemap planks = CreateTilemap("Planches", gridGO.transform, 2, false, Color.white);
        TilemapCollider2D pc = planks.gameObject.AddComponent<TilemapCollider2D>();
        pc.usedByEffector = true;
        PlatformEffector2D eff = planks.gameObject.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.surfaceArc = 160f;
        TileBase plank = PlankTile();
        for (int x = 136; x <= 145; x++) planks.SetTile(new Vector3Int(x, -1, 0), plank);

        List<Vector3Int> gPos = new List<Vector3Int>(), bPos = new List<Vector3Int>();
        List<TileBase> gTiles = new List<TileBase>(), bTiles = new List<TileBase>();
        System.Random rng = new System.Random(5000);
        int[] backVariants = { 27, 27, 27, 27, 27, 23, 49, 87 };

        for (int x = MinX; x <= MaxX; x++)
        {
            for (int y = MinY; y <= MaxY; y++)
            {
                if (Solid(x, y))
                {
                    gPos.Add(new Vector3Int(x, y, 0));
                    gTiles.Add(SheetTile(TileIndexFor(x, y)));
                    // fond aussi derrière les murs (leurs tuiles de bord ont des parties transparentes)
                    if (!Solid(x - 1, y) || !Solid(x + 1, y) || !Solid(x, y - 1) || !Solid(x, y + 1) ||
                        !Solid(x - 1, y - 1) || !Solid(x + 1, y - 1) || !Solid(x - 1, y + 1) || !Solid(x + 1, y + 1))
                    {
                        bPos.Add(new Vector3Int(x, y, 0));
                        bTiles.Add(SheetTile(backVariants[rng.Next(backVariants.Length)]));
                    }
                }
                else
                {
                    bPos.Add(new Vector3Int(x, y, 0));
                    bTiles.Add(SheetTile(backVariants[rng.Next(backVariants.Length)]));
                }
            }
        }
        ground.SetTiles(gPos.ToArray(), gTiles.ToArray());
        back.SetTiles(bPos.ToArray(), bTiles.ToArray());

        // Décor : panneaux d'aide, feu de camp, fleurs, cailloux, lianes
        foreach (int sx in SignXs) decor.SetTile(new Vector3Int(sx, FloorAt(sx), 0), SheetTile(sx >= 91 ? 16 : 14));
        decor.SetTile(new Vector3Int(1, 0, 0), SheetTile(71));   // feu de camp du réveil
        decor.SetTile(new Vector3Int(9, 0, 0), SheetTile(63));   // caisse
        int[] floorDeco = { 81, 82, 93, 94, 53, 83, 95, 53 };
        int[] ceilDeco = { 76, 77, 88, 89 };
        for (int x = CaveStart; x <= CaveEnd; x++)
        {
            int f = FloorAt(x);
            bool nearSign = false;
            foreach (int sx in SignXs) if (Mathf.Abs(sx - x) <= 1) nearSign = true;
            bool reserved = x == 9 || (x >= 96 && x <= 104) || (x >= 117 && x <= 131) || (x >= 202 && x <= 208);
            if (f < 5 && f > -5 && !nearSign && x > 2 && !reserved && rng.NextDouble() < 0.3)
                decor.SetTile(new Vector3Int(x, f, 0), SheetTile(floorDeco[rng.Next(floorDeco.Length)]));
            int c = CeilAt(x);
            if (c > f + 3 && rng.NextDouble() < 0.25)
                decor.SetTile(new Vector3Int(x, c - 1, 0), SheetTile(ceilDeco[rng.Next(ceilDeco.Length)]));
        }
    }

    // ================================================================== grotte : construction
    static void BuildTutorial()
    {
        buildingTutorial = true;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildTiles();

        GameObject player = PlacePlayer(new Vector3(3f, 0.8f, 0f), false);
        PlaceCamera(player, new Color(0.07f, 0.06f, 0.11f));
        PlaceZones();

        // Tutoriel 1 : un moustique simple, assez bas pour lui sauter sur la tête
        PlaceMosquito("Moustique", new Vector2(72f, 0.7f), new[]
        {
            new Vector2(66f, 0.6f), new Vector2(69f, 0.8f), new Vector2(72f, 0.6f), new Vector2(75f, 0.8f),
            new Vector2(78f, 0.6f), new Vector2(81f, 0.75f), new Vector2(67.5f, 0.8f), new Vector2(70.5f, 0.55f),
            new Vector2(73.5f, 0.8f), new Vector2(76.5f, 0.55f), new Vector2(79.5f, 0.8f)
        }, 1.5f, false);

        // Tutoriel 2 : cristaux, planches, moustiques piqueurs
        PlaceCrystal("Cristal d'entraînement", 119f, 2, false);
        PlaceCrystal("Cristal d'entraînement", 123f, 2, false);
        PlaceCrystalWall(129.5f, 4f, 6);
        PlaceMosquito("Moustique piqueur", new Vector2(170f, 1.8f), new[]
        {
            new Vector2(165f, 1.4f), new Vector2(168f, 2.4f), new Vector2(171f, 1.5f), new Vector2(174f, 2.5f), new Vector2(176f, 1.6f)
        }, 1.2f, true);
        PlaceMosquito("Moustique", new Vector2(180f, 0.7f), new[]
        {
            new Vector2(178f, 0.6f), new Vector2(180f, 0.8f), new Vector2(182f, 0.6f)
        }, 1.2f, false);
        PlaceMosquito("Moustique piqueur", new Vector2(188f, 2f), new[]
        {
            new Vector2(184f, 1.5f), new Vector2(187f, 2.6f), new Vector2(190f, 1.4f), new Vector2(193f, 2.4f), new Vector2(195f, 1.6f)
        }, 1.2f, true);

        // Statues de sauvegarde : avant la salle du livre, et dans le tunnel avant l'arène des piqueurs
        PlaceStatue(new Vector3(84f, 0f, 0f), "grotte_1");
        PlaceStatue(new Vector3(151.5f, -4f, 0f), "grotte_2");

        MagicBarrier barrier = PlaceBarrier();
        LastPage page = PlaceLastPage(barrier);
        PlaceBook(page);
        PlaceDoor();
        PlaceAmbience();

        GameObject intro = new GameObject("Intro");
        TutorialIntro ti = intro.AddComponent<TutorialIntro>();
        ti.player = player.GetComponent<PlayerMovement>();
        AddMusic("musique_grotte", 0.5f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TutorialScene);
    }

    static GameObject PlacePlayer(Vector3 position, bool loadSavedMagic)
    {
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.name = "Player";
        player.transform.position = position;
        // Le personnage était trop grand pour les passages : on le ramène à une taille cohérente avec les cases (≈ 0,85 case de haut)
        player.transform.localScale = new Vector3(1.02f, 1.01f, 1f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);

        PlayerMagic magic = player.GetComponent<PlayerMagic>();
        if (magic == null) magic = player.AddComponent<PlayerMagic>();
        magic.loadSavedMagic = loadSavedMagic;
        magic.magic = MagicType.Aucune;
        magic.auraSprite = Art("lueur");
        magic.auraMaterial = additive;
        PrefabUtility.RecordPrefabInstancePropertyModifications(magic);
        if (player.GetComponent<FallImpact>() == null) player.AddComponent<FallImpact>();
        return player;
    }

    // Après une chute dans l'acide ou le vide : retour sur le dernier sol sûr (et non au début de la zone)
    static void EnableSafeRespawn(GameObject player)
    {
        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph == null) return;
        ph.safeGroundRespawn = true;
        PrefabUtility.RecordPrefabInstancePropertyModifications(ph);
    }

    static void PlaceCamera(GameObject player, Color background)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        go.AddComponent<AudioListener>();
        CameraFollow follow = go.AddComponent<CameraFollow>();
        follow.player = player;
        follow.timeOffset = 0.25f;
        follow.posOffset = new Vector3(1.5f, 1.2f, -10f);
        go.transform.position = player.transform.position + follow.posOffset;
    }

    static void PlaceZones()
    {
        GameObject parent = new GameObject("Tutoriel");
        // --- Tutoriel 1
        // --- Tutoriel 2 (magie)

        AddCheckpoint(parent, 29f, 0);
        AddCheckpoint(parent, 53f, 0);
        AddCheckpoint(parent, 61f, 0);
        AddCheckpoint(parent, 88f, 0);
        AddCheckpoint(parent, 114f, 0);
        AddCheckpoint(parent, 131f, 0);
        AddCheckpoint(parent, 158f, 0);
        AddCheckpoint(parent, 198f, 0);

        // --- Panneaux en bois : leur bulle n'apparaît que quand on passe devant
        AddSign(parent, 5, "Q / D ou ← / → : se déplacer");
        AddSign(parent, 17, "Espace : sauter\n(maintiens pour sauter plus haut)");
        AddSign(parent, 30, "Plus tu cours longtemps, plus tu vas vite.\nPrends de l'élan !");
        AddSign(parent, 62, "Retombe sur la tête des ennemis pour les blesser et rebondir.\nTouche leur corps et tu seras blessé !");
        AddSign(parent, 91, "Approche-toi du livre et appuie sur E.");
        AddSign(parent, 115, "J, K ou clic gauche : lancer un sort\nZ / S (+ Q / D) pour viser en haut, en bas ou en diagonale.\nCes cristaux bloquent le passage : détruis-les !");
        AddSign(parent, 132, "S ou ↓ : descendre à travers les planches");
        AddSign(parent, 159, "Attention aux moustiques piqueurs (rouges) !\nQuand ils tremblent, ils vont lancer leur trompe :\nesquive, puis riposte.");
        AddSign(parent, 199, "La sortie ! Appuie sur E pour ouvrir la porte.");

        AddFallZone(parent, 44.75f, -7f, 15f);
    }

    static void AddHint(GameObject parent, string name, float x0, float x1, float floor, string message)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3((x0 + x1) * 0.5f, floor + 3f, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(x1 - x0, 8f);
        box.offset = new Vector2(0f, -1f);
        go.AddComponent<TutorialHint>().message = message;
    }

    static void AddSign(GameObject parent, int tileX, string message)
    {
        GameObject go = new GameObject("Panneau");
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(tileX + 0.5f, FloorAt(tileX) + 1f, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(4f, 4f);
        go.AddComponent<SignPost>().message = message;
    }

    static void AddCheckpoint(GameObject parent, float x, int floor)
    {
        GameObject go = new GameObject("Checkpoint");
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(x + 0.5f, floor + 2f, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1f, 4f);
        Checkpoint cp = go.AddComponent<Checkpoint>();
        cp.spawnOffset = new Vector3(0f, -1.2f, 0f);
    }

    static void AddFallZone(GameObject parent, float x, float y, float width)
    {
        GameObject go = new GameObject("Vide");
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(x, y, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(width, 2f);
        go.AddComponent<FallZone>().damage = 20;
    }

    // ------------------------------------------------------------------ ennemis
    // Points de vie des moustiques : l'écrasement ne tue plus d'un coup, il faut aussi utiliser la magie
    static bool buildingTutorial;
    static int MosquitoHp(bool stinger, float x)
    {
        if (buildingTutorial) return x < 100f ? 2 : (stinger ? 5 : 4);   // avant la magie : deux écrasements suffisent
        return stinger ? 8 : 6;
    }

    static void PlaceMosquito(string name, Vector2 start, Vector2[] points, float speed, bool stinger)
    {
        if (mosquitoPrefab == null) return;
        GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(mosquitoPrefab);
        // Chaque scène a ses propres points de passage : on détache l'instance du prefab
        PrefabUtility.UnpackPrefabInstance(enemy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        enemy.name = name;

        // On garde autant de points de passage que nécessaire
        List<Transform> wps = new List<Transform>();
        foreach (Transform t in enemy.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith("Waypoint")) wps.Add(t);
        for (int i = 0; i < wps.Count; i++)
        {
            if (i < points.Length) wps[i].position = new Vector3(points[i].x, points[i].y, 0f);
            else Object.DestroyImmediate(wps[i].gameObject);
        }
        wps.RemoveRange(Mathf.Min(points.Length, wps.Count), Mathf.Max(0, wps.Count - points.Length));

        Transform graphics = FindDeep(enemy.transform, "Graphics");
        if (graphics == null) return;
        graphics.position = new Vector3(start.x, start.y, 0f);
        EnemyPatrol patrol = graphics.GetComponent<EnemyPatrol>();
        if (patrol != null)
        {
            patrol.speed = speed;
            patrol.waypoints = wps.ToArray();
            patrol.health = MosquitoHp(stinger, start.x);
        }
        if (stinger)
        {
            graphics.localScale = new Vector3(1.15f, 1.15f, 1f);
            SpriteRenderer sr = graphics.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 0.62f, 0.6f);
            graphics.gameObject.AddComponent<MosquitoStinger>();
        }
    }

    static void PlaceCrystal(string name, float x, int hp, bool blocking)
    {
        GameObject go = new GameObject(name);
        go.transform.position = new Vector3(x, 0.6875f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Art("cristal");
        sr.sortingOrder = 9993;
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 1.35f);
        box.isTrigger = !blocking;
        EnemyHealth eh = go.AddComponent<EnemyHealth>();
        eh.maxHealth = hp;
        eh.rootToDestroy = go;
        eh.deathStyle = EnemyHealth.DeathStyle.Eclatement;
        eh.deathSound = "impact";
        eh.countsAsEnemy = false;
    }

    static void PlaceCrystalWall(float x, float height, int hp)
    {
        GameObject go = new GameObject("Mur de cristal");
        go.transform.position = new Vector3(x, height * 0.5f, 0f);
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

    // ------------------------------------------------------------------ livre, page, sceau, porte
    static void PlaceBook(LastPage page)
    {
        GameObject socle = new GameObject("Socle");
        socle.transform.position = new Vector3(100f, 0.6875f, 0f);
        SpriteRenderer ss = socle.AddComponent<SpriteRenderer>();
        ss.sprite = Art("socle");
        ss.sortingOrder = 9990;

        GameObject book = new GameObject("Livre ancien");
        book.transform.position = new Vector3(100f, 2.45f, 0f);
        SpriteRenderer glow = Child(book.transform, "Lueur", Art("lueur"), Vector3.zero, 9994, new Color(0.56f, 0.94f, 1f, 0.55f), additive);
        SpriteRenderer sprite = Child(book.transform, "Sprite", Art("livre_ferme"), Vector3.zero, 9995, Color.white, null);

        CircleCollider2D zone = book.AddComponent<CircleCollider2D>();
        zone.isTrigger = true;
        zone.radius = 2.2f;
        zone.offset = new Vector2(0f, -1.4f);

        LegendBook legend = book.AddComponent<LegendBook>();
        legend.bookSprite = Art("livre_ui");
        legend.mageLight = Art("mage_lumiere");
        legend.mageDark = Art("mage_ombre");
        legend.glow = Art("lueur");
        legend.spark = Art("etincelle");
        legend.closedBook = Art("livre_ferme");
        legend.fragment = Art("fragment");

        AncientBook ab = book.AddComponent<AncientBook>();
        ab.bookRenderer = sprite;
        ab.closedSprite = Art("livre_ferme");
        ab.openSprite = Art("livre_ouvert");
        ab.glowRenderer = glow;
        ab.fragmentSprite = Art("fragment");
        ab.pageSprite = Art("page");
        ab.glowSprite = Art("lueur");
        ab.raysSprite = Art("rayons");
        ab.additiveMaterial = additive;
        ab.lastPage = page;
        ab.promptText = "Lire le livre";
        ab.promptOffset = new Vector3(0f, 1.1f, 0f);
    }

    static LastPage PlaceLastPage(MagicBarrier barrier)
    {
        GameObject go = new GameObject("Dernière page");
        go.transform.position = new Vector3(100f, 2.45f, 0f);
        SpriteRenderer glow = Child(go.transform, "Lueur", Art("lueur"), Vector3.zero, 9998, new Color(1f, 0.95f, 0.75f, 0.5f), additive);
        glow.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        SpriteRenderer sprite = Child(go.transform, "Sprite", Art("page"), Vector3.zero, 10000, Color.white, null);

        CircleCollider2D zone = go.AddComponent<CircleCollider2D>();
        zone.isTrigger = true;
        zone.radius = 1.3f;

        LastPage page = go.AddComponent<LastPage>();
        page.pageRenderer = sprite;
        page.glowRenderer = glow;
        page.iconFire = Art("icone_feu");
        page.iconLightning = Art("icone_foudre");
        page.iconIce = Art("icone_glace");
        page.glowSprite = Art("lueur");
        page.fragmentSprite = Art("fragment");
        page.additiveMaterial = additive;
        page.barrier = barrier;
        page.promptText = "Examiner la page";

        go.SetActive(false); // elle n'apparaît qu'à la fin de la cinématique
        return page;
    }

    static MagicBarrier PlaceBarrier()
    {
        GameObject go = new GameObject("Sceau magique");
        go.transform.position = new Vector3(112.5f, 3.5f, 0f);
        Child(go.transform, "Lueur", Art("lueur"), Vector3.zero, 9993, new Color(0.65f, 0.5f, 1f, 0.35f), additive)
            .transform.localScale = new Vector3(1.2f, 4f, 1f);
        SpriteRenderer sr = Child(go.transform, "Sprite", Art("barriere"), Vector3.zero, 9996, Color.white, null);
        sr.transform.localScale = new Vector3(1f, 7f / 3f, 1f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.75f, 7f);
        MagicBarrier barrier = go.AddComponent<MagicBarrier>();
        barrier.barrierRenderer = sr;
        barrier.sparkSprite = Art("etincelle");
        barrier.additiveMaterial = additive;
        return barrier;
    }

    static void PlaceDoor()
    {
        GameObject go = new GameObject("Porte de sortie");
        go.transform.position = new Vector3(205f, 1.5f, 0f);
        SpriteRenderer lightSr = Child(go.transform, "Lumière", Art("lueur"), new Vector3(0f, 0.3f, 0f), 9989, new Color(1f, 0.95f, 0.8f, 0f), additive);
        lightSr.transform.localScale = new Vector3(3f, 3.5f, 1f);
        SpriteRenderer door = Child(go.transform, "Sprite", Art("porte_fermee"), Vector3.zero, 9988, Color.white, null);
        BoxCollider2D zone = go.AddComponent<BoxCollider2D>();
        zone.isTrigger = true;
        zone.size = new Vector2(3.5f, 3f);
        CaveDoor cd = go.AddComponent<CaveDoor>();
        cd.doorRenderer = door;
        cd.closedSprite = Art("porte_fermee");
        cd.openSprite = Art("porte_ouverte");
        cd.lightRenderer = lightSr;
        cd.sceneToLoad = "EtangAcide";
        cd.promptText = "Ouvrir la porte";
    }

    static void PlaceStatue(Vector3 floorPos, string id)
    {
        GameObject go = new GameObject("Statue de sauvegarde");
        go.transform.position = floorPos + new Vector3(0f, 1.4f, 0f);
        SpriteRenderer glow = Child(go.transform, "Lueur", Art("lueur"), new Vector3(0f, 0.2f, 0f), 9986, new Color(0.6f, 0.9f, 1f, 0f), additive);
        glow.transform.localScale = new Vector3(3f, 3f, 1f);
        SpriteRenderer sr = Child(go.transform, "Sprite", Art("statue_0"), Vector3.zero, 9987, Color.white, null);
        sr.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        BoxCollider2D zone = go.AddComponent<BoxCollider2D>();
        zone.isTrigger = true;
        zone.size = new Vector2(3f, 3f);
        SaveStatue st = go.AddComponent<SaveStatue>();
        st.statueId = id;
        st.statueRenderer = sr;
        st.glowRenderer = glow;
        st.frames = new[] { Art("statue_0"), Art("statue_1"), Art("statue_2"), Art("statue_3") };
        st.promptText = "Prier (sauvegarder)";
        st.promptOffset = new Vector3(0f, 1.9f, 0f);
    }

    static void PlaceAmbience()
    {
        GameObject amb = new GameObject("Ambiance");
        SpriteRenderer fire = Child(amb.transform, "Lueur feu de camp", Art("lueur"), new Vector3(1.5f, 0.6f, 0f), 2, new Color(1f, 0.55f, 0.2f, 0.45f), additive);
        fire.transform.localScale = new Vector3(2.5f, 2.5f, 1f);
        fire.gameObject.AddComponent<LightFlicker>();
        AddHalo(amb, "Halo salle du livre", new Vector3(100f, 4f, 0f), new Vector2(7f, 6f), new Color(0.4f, 0.75f, 1f, 0.25f));
        AddHalo(amb, "Halo cristaux", new Vector3(125f, 2f, 0f), new Vector2(6f, 4f), new Color(0.6f, 0.45f, 1f, 0.2f));
        AddHalo(amb, "Lumière sous la porte", new Vector3(205f, 0.5f, 0f), new Vector2(4f, 2f), new Color(1f, 0.9f, 0.7f, 0.15f));
    }

    static void AddHalo(GameObject parent, string name, Vector3 pos, Vector2 scale, Color color)
    {
        SpriteRenderer h = Child(parent.transform, name, Art("lueur"), pos, -5, color, additive);
        h.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        LightFlicker lf = h.gameObject.AddComponent<LightFlicker>();
        lf.baseAlpha = color.a; lf.amount = color.a * 0.2f; lf.speed = 0.8f;
    }

    // ================================================================== scène principale (extérieur)
    static void UpdateMainScene()
    {
        Scene main = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
        foreach (string old in new[] { "Extérieur", "Intro", "Musique", "Statue de sauvegarde" })
        {
            GameObject o = FindRoot(main, old);
            if (o != null) Object.DestroyImmediate(o);
        }

        GameObject player = FindRoot(main, "Player");
        if (player == null) throw new System.Exception("Objet 'Player' introuvable dans " + MainScene);
        PlayerMagic magic = player.GetComponent<PlayerMagic>();
        if (magic == null) magic = player.AddComponent<PlayerMagic>();
        magic.loadSavedMagic = true;
        magic.auraSprite = Art("lueur");
        magic.auraMaterial = additive;

        // Le sol de la scène : le Tilemap qui a un collider
        Tilemap ground = null;
        foreach (TilemapCollider2D tc in Object.FindObjectsOfType<TilemapCollider2D>())
        {
            if (tc.gameObject.scene == main) { ground = tc.GetComponent<Tilemap>(); break; }
        }
        if (ground == null) throw new System.Exception("Aucun Tilemap avec collider dans " + MainScene);
        ground.CompressBounds();
        BoundsInt b = ground.cellBounds;

        // Première colonne (en partant de la gauche) ouverte vers le ciel : c'est là qu'on sort de la grotte
        int col = b.xMin + 1;
        for (int x = b.xMin; x < b.xMax; x++)
        {
            if (!ground.HasTile(new Vector3Int(x, b.yMax - 1, 0))) { col = x; break; }
        }
        int floorTop = b.yMin;
        for (int y = b.yMax - 1; y >= b.yMin; y--)
        {
            if (ground.HasTile(new Vector3Int(col, y, 0))) { floorTop = y + 1; break; }
        }
        Vector3 floorWorld = ground.CellToWorld(new Vector3Int(col, floorTop, 0));
        float levelCenterX = ground.CellToWorld(new Vector3Int((b.xMin + b.xMax) / 2, 0, 0)).x;

        GameObject outside = new GameObject("Extérieur");

        // Sortie de la grotte, contre la paroi de gauche
        GameObject exit = new GameObject("Sortie de la grotte");
        exit.transform.SetParent(outside.transform, false);
        exit.transform.position = new Vector3(floorWorld.x + 1f, floorWorld.y + 1.5f, 0f);
        Child(exit.transform, "Entrée", Art("entree_grotte"), Vector3.zero, 3, Color.white, null);
        SpriteRenderer sun = Child(exit.transform, "Rayons de soleil", Art("rayons"), new Vector3(2f, 3f, 0f), 4, new Color(1f, 0.95f, 0.75f, 0.12f), additive);
        sun.transform.localScale = new Vector3(4f, 4f, 1f);
        sun.transform.rotation = Quaternion.Euler(0, 0, 20f);

        // Décor de forêt en parallaxe
        GameObject sky = new GameObject("Ciel et forêt");
        sky.transform.SetParent(outside.transform, false);
        float baseY = floorWorld.y;
        AddParallax(sky, "BACKGROUND", 1f, 1f, -100, new Vector3(levelCenterX, baseY + 3f, 20f), 2.2f, Color.white);
        AddParallax(sky, "WOODS - Fourth", 0.85f, 0.7f, -90, new Vector3(levelCenterX, baseY + 2.5f, 18f), 1.5f, Color.white);
        AddParallax(sky, "WOODS - Third", 0.7f, 0.55f, -80, new Vector3(levelCenterX, baseY + 2f, 16f), 1.5f, Color.white);
        AddParallax(sky, "VINES - Second", 0.6f, 0.5f, -75, new Vector3(levelCenterX, baseY + 4f, 15f), 1.5f, Color.white);
        AddParallax(sky, "WOODS - Second", 0.5f, 0.4f, -70, new Vector3(levelCenterX, baseY + 1.5f, 14f), 1.5f, Color.white);
        AddParallax(sky, "BUSH - BACKGROUND", 0.3f, 0.25f, -60, new Vector3(levelCenterX, baseY + 0.5f, 12f), 1.5f, Color.white);

        // Une statue de sauvegarde un peu après la sortie de la grotte
        int statueCol = col + 7;
        int statueFloor = b.yMin;
        for (int y = b.yMax - 1; y >= b.yMin; y--)
        {
            if (ground.HasTile(new Vector3Int(statueCol, y, 0))) { statueFloor = y + 1; break; }
        }
        Vector3 statueWorld = ground.CellToWorld(new Vector3Int(statueCol, statueFloor, 0)) + new Vector3(0.5f, 0f, 0f);
        PlaceStatue(statueWorld, "exterieur_1");

        // Le joueur sort de la grotte
        player.transform.position = new Vector3(floorWorld.x + 2.8f, floorWorld.y + 0.8f, 0f);
        Camera cam = Camera.main;
        foreach (Camera c in Object.FindObjectsOfType<Camera>()) if (c.gameObject.scene == main) cam = c;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.38f, 0.75f, 0.77f);
            CameraFollow follow = cam.GetComponent<CameraFollow>();
            Vector3 offset = follow != null ? follow.posOffset : new Vector3(0, 0, -10f);
            cam.transform.position = player.transform.position + offset;
        }

        GameObject intro = new GameObject("Intro");
        TutorialIntro ti = intro.AddComponent<TutorialIntro>();
        ti.player = player.GetComponent<PlayerMovement>();
        ti.fadeFrom = new Color(1f, 0.98f, 0.92f, 1f);
        ti.fadeDuration = 2.5f;
        ti.lines = new[]
        {
            "L'air frais... le soleil... Ça faisait si longtemps.",
            "Quelque part dans ce monde, les pages du livre m'attendent. Et avec elles, mon nom."
        };
        AddMusic("musique_foret", 0.45f);

        EditorSceneManager.MarkSceneDirty(main);
        EditorSceneManager.SaveScene(main);
    }

    static void AddParallax(GameObject parent, string spriteName, float fx, float fy, int order, Vector3 pos, float scale, Color color)
    {
        Sprite s = Art(spriteName);
        if (s == null) return;
        GameObject go = new GameObject(spriteName);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.color = color;
        sr.sortingOrder = order;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(s.bounds.size.x * 12f, s.bounds.size.y);
        ParallaxLayer p = go.AddComponent<ParallaxLayer>();
        p.factorX = fx;
        p.factorY = fy;
    }

    // ================================================================== build settings
    static void AddToBuildSettings()
    {
        List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>();
        string[] order = { MenuScene, TutorialScene, PondScene, SwScenePath(1), SwScenePath(2), SwScenePath(3), SwScenePath(4), DungeonScene, MainScene };
        foreach (string p in order)
            if (File.Exists(p)) list.Add(new EditorBuildSettingsScene(p, true));
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            if (System.Array.IndexOf(order, s.path) < 0 && File.Exists(s.path)) list.Add(s);   // (les anciennes cartes disparues sont retirées)
        EditorBuildSettings.scenes = list.ToArray();
    }
}
