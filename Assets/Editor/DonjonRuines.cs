using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Passe « ruines » du donjon de Séraphel : plafonds éboulés, gravats, colonnes brisées, toiles, bannières déchirées, puits de lumière.
// Aucun relief ajouté au sol : tout ce qui est posé dessus est du décor plat que l'on traverse, le sol reste partout lisse.
public static partial class TutorialSceneBuilder
{
    // plafond libre (dernière case vide) de chaque tronçon ; les couloirs fermés par des grilles ne sont pas touchés
    static readonly int[,] RuinCeil =
    {
        { 0, 27, 9 }, { 28, 51, 5 }, { 52, 62, 9 }, { 66, 89, 10 }, { 95, 119, 10 }, { 125, 149, 10 }, { Off + 66, Off + 122, 23 }
    };

    static float RuinNoise(int x, int k)
    {
        return 0.5f + 0.5f * Mathf.Sin(x * 0.37f + k * 1.7f) * Mathf.Cos(x * 0.13f + k * 0.9f + 0.5f * Mathf.Sin(x * 0.71f));
    }

    /// <summary>Avant les tuiles : plafonds effondrés (brèches irrégulières).</summary>
    static void DungeonRuinShape()
    {
        for (int i = 0; i < RuinCeil.GetLength(0); i++)
        {
            int c = RuinCeil[i, 2];
            for (int x = RuinCeil[i, 0]; x <= RuinCeil[i, 1]; x++)
            {
                float n = RuinNoise(x, i);
                int depth = Mathf.RoundToInt(Mathf.Clamp01(n * 1.5f - 0.5f) * (c > 15 ? 4f : 3f));
                if (depth > 0) DCarve(x, x, c + 1, c + depth);
            }
        }
        // puits d'effondrement (la lumière du dessus tombe dedans)
        DCarve(44, 45, 6, 15);
        DCarve(16, 17, 10, 17);
        DCarve(57, 58, 10, 16);
    }

    static SpriteRenderer RuinProp(Transform parent, string spr, float x, float bottom, int order, float shade, bool flip, float rot)
    {
        Sprite s = Art(spr);
        if (s == null) return null;
        float h = s.bounds.size.y;
        SpriteRenderer r = Child(parent, spr, s, new Vector3(x, bottom + h * 0.5f, 0f), order, new Color(shade, shade, shade * 1.05f, 1f), null);
        r.flipX = flip;
        if (rot != 0f) r.transform.rotation = Quaternion.Euler(0f, 0f, rot);
        return r;
    }

    static void RuinHang(Transform parent, string spr, float x, float top, int order, float shade, bool flip)
    {
        Sprite s = Art(spr);
        if (s == null) return;
        SpriteRenderer r = Child(parent, spr, s, new Vector3(x, top - s.bounds.size.y * 0.5f, 0f), order, new Color(shade, shade, shade * 1.05f, 1f), null);
        r.flipX = flip;
    }

    /// <summary>Après la grille : le décor des ruines.</summary>
    static void DungeonRuinDecor(Transform root)
    {
        System.Random rr = new System.Random(24);
        GameObject g = new GameObject("Ruines");
        g.transform.SetParent(root, false);
        Transform t = g.transform;

        // colonnes brisées dans le fond (derrière le sol, devant le mur du fond)
        string[] cols = { "ruine_colonne_a", "ruine_colonne_b", "ruine_colonne_c" };
        float[] colX = { 6.5f, 16f, 26f, 36f, 49.5f, 54f, 61f, 71f, 79f, 84f, 100f, 108f, 113f, 130f, 137f, 143f,
                         Off + 67.5f, Off + 77.5f, Off + 86.5f, Off + 101.5f, Off + 122.3f, Off + 60f }; // jamais au-dessus d'une fosse ni d'une plaque qui s'effondre
        for (int i = 0; i < colX.Length; i++)
        {
            string cn = cols[i % 3];
            if (colX[i] >= 28 && colX[i] < 52) cn = "ruine_colonne_b";          // galerie basse : colonnes courtes
            RuinProp(t, cn, colX[i], colX[i] > Off + 60 ? 0f : 0f, -5, 0.6f + 0.1f * (i % 3), (i & 1) == 1, 0f);
        }
        // colonnes couchées, gravats visuels
        RuinProp(t, "ruine_colonne_couchee", 13.5f, 0f, -4, 0.5f, false, 0f);
        RuinProp(t, "ruine_colonne_couchee", 40f, 0f, -4, 0.5f, true, 0f);
        RuinProp(t, "ruine_colonne_couchee", Off + 63f, 0f, -4, 0.5f, false, 0f);
        float[] pileX = { 8f, 22f, 33f, 47f, 59f, 72f, 88f, 102f, 116f, 131f, 146f, 156f, Off + 68.5f, Off + 78.5f, Off + 101f };
        string[] piles = { "ruine_gravats_a", "ruine_gravats_b", "ruine_gravats_c" };
        for (int i = 0; i < pileX.Length; i++)
            if (pileX[i] < 8.5f || pileX[i] > 11f) RuinProp(t, piles[i % 3], pileX[i], 0f, 3, 0.75f, (i & 1) == 0, 0f);
        // l'éboulis sous les puits de lumière
        RuinProp(t, "ruine_gravats_c", 44.8f, 0f, 3, 0.8f, false, 0f);
        RuinProp(t, "ruine_gravats_c", 16.8f, 0f, 3, 0.8f, true, 0f);
        RuinProp(t, "ruine_gravats_b", 57.8f, 0f, 3, 0.8f, false, 0f);
        // poutres tombées
        RuinProp(t, "ruine_poutre", 31f, 2.4f, -4, 0.8f, false, 14f);
        RuinProp(t, "ruine_poutre", 50f, 2.0f, -4, 0.75f, true, -10f);
        RuinProp(t, "ruine_poutre", 77f, 5.5f, -4, 0.75f, false, 16f);
        RuinProp(t, "ruine_poutre", 140f, 5.0f, -4, 0.75f, true, -14f);
        // arches brisées
        RuinProp(t, "ruine_arche", 25f, 0f, -6, 0.65f, false, 0f);
        RuinProp(t, "ruine_arche", 62f, 0f, -6, 0.65f, true, 0f);
        RuinProp(t, "ruine_arche", 94f, 0f, -6, 0.65f, false, 0f);
        RuinProp(t, "ruine_arche", 148f, 0f, -6, 0.65f, true, 0f);

        // bannières déchirées, chaînes, toiles (suspendues aux plafonds)
        float[] banX = { 11f, 22f, 31f, 39f, 53f, 60f, 69f, 82f, 98f, 114f, 128f, 144f };
        int[] banTop = { 10, 10, 6, 6, 10, 10, 11, 11, 11, 11, 11, 11 };
        for (int i = 0; i < banX.Length; i++)
            RuinHang(t, i % 3 == 0 ? "ruine_banniere_b" : "ruine_banniere_a", banX[i], banTop[i] - 0.1f, -3, 0.8f, (i & 1) == 1);
        foreach (float cx in new[] { 35f, 48f, 75f, 103f, 133f })
            RuinHang(t, "ruine_chaine", cx, cx < 70f ? 6f : 11f, -3, 0.85f, false);
        float[,] webs = { { 29f, 6f }, { 50.5f, 6f }, { 53f, 10f }, { 61.5f, 10f }, { 1f, 10f }, { 26f, 10f }, { 66.5f, 11f }, { 88.5f, 11f }, { 95.5f, 11f }, { 118.5f, 11f }, { 125.5f, 11f }, { 148.5f, 11f } };
        for (int i = 0; i < webs.GetLength(0); i++)
            RuinHang(t, "ruine_toile_coin", webs[i, 0] + 0.9f * ((i & 1) == 0 ? 1 : -1), webs[i, 1] - 0.05f, -2, 1f, (i & 1) == 1);

        // semis de détails : racines et blocs qui pendent, gravats et mousse au sol
        string[] hangs = { "ruine_d_racine0", "ruine_d_racine1", "ruine_d_pierre0", "ruine_d_pierre1", "ruine_d_toile" };
        string[] floors = { "ruine_d_sol0", "ruine_d_sol1", "ruine_d_sol2", "ruine_d_sol3", "ruine_d_mousse0", "ruine_d_mousse1" };
        for (int x = gX0; x <= gX1; x++)
            for (int y = 0; y <= 24; y++)
            {
                if (GSolid(x, y)) continue;
                if (x >= 7 && x <= 12 && y == 0) continue;
                if (GSolid(x, y + 1) && !GSolid(x, y - 1) && rr.NextDouble() < 0.12)
                    RuinHang(t, hangs[rr.Next(hangs.Length)], x + 0.5f, y + 1f, 1, 0.8f, rr.Next(2) == 0);
                else if (GSolid(x, y - 1) && !GSolid(x, y + 1) && y == 0 && x > 2 && rr.NextDouble() < 0.25)
                    RuinProp(t, floors[rr.Next(floors.Length)], x + 0.5f, y, 2, 0.8f, rr.Next(2) == 0, 0f);
            }

        // puits de lumière dans les brèches du plafond
        float[,] shafts = { { 45f, 4f, 2.6f, 14f }, { 17f, 5f, 2.6f, 16f }, { 58f, 6f, 2.6f, 14f } };
        for (int i = 0; i < shafts.GetLength(0); i++)
        {
            AddHalo(g, "Rayon de lumière", new Vector3(shafts[i, 0], shafts[i, 1] + shafts[i, 3] * 0.35f, 0f), new Vector2(shafts[i, 2], shafts[i, 3]), new Color(0.85f, 0.92f, 1f, 0.13f));
            AddHalo(g, "Poussière lumineuse", new Vector3(shafts[i, 0], 1.2f, 0f), new Vector2(5f, 2.6f), new Color(0.9f, 0.95f, 1f, 0.16f));
        }
    }

    // ------------------------------------------------------------------ tuiles en briques abîmées
    static readonly Dictionary<string, Tile> ruinTiles = new Dictionary<string, Tile>();

    static Tile RuinTile(string name, bool solid)
    {
        Tile t;
        if (ruinTiles.TryGetValue(name, out t) && t != null) return t;
        string path = ArtDir + "/ruine_" + name + ".asset";
        t = UnityEditor.AssetDatabase.LoadAssetAtPath<Tile>(path);
        Sprite sp = Art("tuile_ruine_" + name);
        if (t == null)
        {
            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sp;
            t.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            UnityEditor.AssetDatabase.CreateAsset(t, path);
        }
        else if (t.sprite != sp)
        {
            t.sprite = sp;
            t.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            UnityEditor.EditorUtility.SetDirty(t);
        }
        ruinTiles[name] = t;
        return t;
    }

    static string RuinPick(System.Random r, string prefix, int[] weights)
    {
        int tot = 0;
        for (int i = 0; i < weights.Length; i++) tot += weights[i];
        int k = r.Next(tot);
        for (int i = 0; i < weights.Length; i++)
        {
            if (k < weights[i]) return prefix + i;
            k -= weights[i];
        }
        return prefix + "0";
    }

    /// <summary>Remplace les tuiles du donjon (sol, murs, plafond, fond) par des briques fissurées, mousseuses, aux bords éclatés.
    /// Même emplacement et même collider carré qu'avant : seul l'aspect change.</summary>
    static void DungeonRuinTiles(GameObject gridGO)
    {
        System.Random r = new System.Random(77);
        Tilemap ground = gridGO.transform.Find("Sol").GetComponent<Tilemap>();
        Tilemap back = gridGO.transform.Find("Fond des cavernes").GetComponent<Tilemap>();
        BoundsInt b = ground.cellBounds;
        foreach (Vector3Int p in b.allPositionsWithin)
        {
            if (!ground.HasTile(p)) continue;
            Color col = ground.GetColor(p);
            string n;
            switch (GTileIndex(p.x, p.y))
            {
                case 66: n = RuinPick(r, "hg", new[] { 1, 1 }); break;
                case 67: n = RuinPick(r, "h", new[] { 3, 2, 2, 2 }); break;
                case 68: n = RuinPick(r, "hd", new[] { 1, 1 }); break;
                case 78: n = RuinPick(r, "g", new[] { 3, 2, 2, 1 }); break;
                case 80: n = RuinPick(r, "d", new[] { 3, 2, 2, 1 }); break;
                case 90: n = RuinPick(r, "bg", new[] { 1, 1 }); break;
                case 91: n = RuinPick(r, "b", new[] { 2, 2, 2, 2 }); break;
                case 92: n = RuinPick(r, "bd", new[] { 1, 1 }); break;
                default: n = RuinPick(r, "c", new[] { 4, 2, 2, 1, 2, 2 }); break;
            }
            ground.SetTile(p, RuinTile(n, true));
            ground.SetTileFlags(p, TileFlags.None);
            ground.SetColor(p, col);
        }
        b = back.cellBounds;
        foreach (Vector3Int p in b.allPositionsWithin)
        {
            if (!back.HasTile(p)) continue;
            Color col = back.GetColor(p);
            back.SetTile(p, RuinTile(RuinPick(r, "f", new[] { 4, 4, 2, 1, 1, 1, 2 }), false));
            back.SetTileFlags(p, TileFlags.None);
            back.SetColor(p, col);
        }
        UnityEditor.AssetDatabase.SaveAssets();
    }
}
