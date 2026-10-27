using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Apparence et animation du personnage.
//   - 7 skins (rouge par défaut, gris, rose, vert, bleu, blanc, noir) : une feuille d'images par skin dans Resources/Sprites/skin_<nom>.png ;
//   - les images sont choisies ici selon ce que fait le joueur : repos, marche, course (cape qui traîne), saut, chute, réception.
// Le composant s'ajoute tout seul aux joueurs (aucune modification du prefab). Si une feuille manque, l'Animator d'origine reste utilisé.
[DefaultExecutionOrder(1000)]
public class PlayerSkin : MonoBehaviour
{
    public static readonly string[] Ids = { "rouge", "gris", "rose", "vert", "bleu", "blanc", "noir" };
    public static readonly string[] Names = { "Rouge", "Gris", "Rose", "Vert", "Bleu", "Blanc", "Noir" };
    public static readonly Color[] Swatches =
    {
        new Color(0.55f, 0.20f, 0.22f), new Color(0.45f, 0.48f, 0.50f), new Color(0.62f, 0.34f, 0.48f), new Color(0.28f, 0.48f, 0.30f),
        new Color(0.26f, 0.38f, 0.60f), new Color(0.80f, 0.80f, 0.76f), new Color(0.13f, 0.14f, 0.19f)
    };

    const string PrefKey = "motrilem_skin";
    const int Cols = 12, CellW = 48, CellH = 90;
    // lignes de la feuille (de haut en bas) : repos (6), marche (12), course (12), air (6)
    const int RowIdle = 0, RowWalk = 1, RowRun = 2, RowAir = 3;

    static int current = -1;
    static readonly Dictionary<int, Sprite[]> cache = new Dictionary<int, Sprite[]>();
    static float ppu = 16f;
    static bool ppuKnown;

    public static int Current
    {
        get
        {
            if (current < 0) current = Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 0), 0, Ids.Length - 1);
            return current;
        }
    }

    public static void Set(int index)
    {
        current = ((index % Ids.Length) + Ids.Length) % Ids.Length;
        PlayerPrefs.SetInt(PrefKey, current);
        PlayerPrefs.Save();
    }

    /// <summary>Une image de repos du skin (pour l'aperçu dans les paramètres).</summary>
    public static Sprite Preview(int index)
    {
        Sprite[] s = Load(index);
        return s == null ? null : s[RowIdle * Cols];
    }

    static Sprite[] Load(int index)
    {
        Sprite[] arr;
        if (cache.TryGetValue(index, out arr)) return arr;
        Texture2D tex = Resources.Load<Texture2D>("Sprites/skin_" + Ids[index]);
        if (tex == null) { cache[index] = null; return null; }
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        arr = new Sprite[Cols * 4];
        int rows = tex.height / CellH;
        for (int r = 0; r < 4 && r < rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                Rect rect = new Rect(c * CellW, tex.height - (r + 1) * CellH, CellW, CellH);
                Sprite sp = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
                sp.name = "skin_" + Ids[index] + "_" + (r * Cols + c);
                arr[r * Cols + c] = sp;
            }
        cache[index] = arr;
        return arr;
    }

    // ------------------------------------------------------------------ mise en place automatique
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded += (s, m) => Attach();
        Attach();
    }

    static void Attach()
    {
        foreach (PlayerMovement pm in FindObjectsOfType<PlayerMovement>())
            if (pm.GetComponent<PlayerSkin>() == null) pm.gameObject.AddComponent<PlayerSkin>();
    }

    // ------------------------------------------------------------------ animation
    private PlayerMovement pm;
    private Rigidbody2D rb;
    private float phase;          // position dans la foulée (en images)
    private float idleTimer;
    private float landTimer;
    private int loadedFor = -1;
    private Sprite[] sprites;
    private bool wasAir;
    private float lowestVy;

    void Awake()
    {
        pm = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        if (pm == null || rb == null) return;
        SpriteRenderer sr = pm.spriteRenderer;
        if (sr == null) return;
        if (sr.sprite != null && !ppuKnown && !sr.sprite.name.StartsWith("skin_"))
        {
            ppu = sr.sprite.pixelsPerUnit;
            ppuKnown = true;
            cache.Clear();
        }
        if (loadedFor != Current) { sprites = Load(Current); loadedFor = Current; }
        if (sprites == null) return;

        float dt = Time.deltaTime;
        float vx = rb.velocity.x, vy = rb.velocity.y;
        bool frozen = pm.externalControl;
        AcidSwim sw = GetComponent<AcidSwim>();
        bool swim = sw != null && sw.Swimming && !frozen;
        bool air = !pm.isGrounded && !frozen && !swim;

        // réception : une courte image « genoux fléchis » après une chute un peu haute
        if (air) lowestVy = Mathf.Min(lowestVy, vy);
        if (wasAir && !air && lowestVy < -6f) landTimer = 0.12f;
        if (!air) lowestVy = 0f;
        wasAir = air;
        if (landTimer > 0f) landTimer -= dt;

        int index;
        if (air)
        {
            int f = vy > 4f ? 0 : vy > 1f ? 1 : vy > -1f ? 2 : vy > -9f ? 3 : 4;
            index = RowAir * Cols + f;
        }
        else if (landTimer > 0f && Mathf.Abs(vx) < 1f)
        {
            index = RowAir * Cols + 5;
        }
        else
        {
            float speed = Mathf.Abs(vx);
            if (swim && speed < 0.3f && Mathf.Abs(vy) > 0.5f) speed = 1f; // nage verticale : brassée
            if (speed < 0.3f)
            {
                idleTimer += dt;
                index = RowIdle * Cols + ((int)(idleTimer / 0.32f) % 6);
                phase = 0f;
            }
            else
            {
                idleTimer = 0f;
                bool running = !swim && (pm.Momentum > 0.3f || speed > pm.moveSpeed * 1.15f);
                float rate = running ? Mathf.Lerp(16f, 22f, Mathf.Clamp01(speed / Mathf.Max(1f, pm.runSpeed))) : Mathf.Lerp(10f, 14f, Mathf.Clamp01(speed / Mathf.Max(1f, pm.moveSpeed)));
                phase = (phase + dt * rate) % 12f;
                index = (running ? RowRun : RowWalk) * Cols + (int)phase;
            }
        }
        if (index >= 0 && index < sprites.Length && sprites[index] != null) sr.sprite = sprites[index];
        UpdateSwimPose(sr, swim, vx, vy);
        UpdateCapeGlow(sr, dt);
    }

    // ------------------------------------------------------------------ nage : corps incliné, ondulation lente
    private bool wasSwim;
    void UpdateSwimPose(SpriteRenderer sr, bool swim, float vx, float vy)
    {
        Transform vt = sr.transform;
        if (swim)
        {
            wasSwim = true;
            float lean = -pm.Facing * Mathf.Clamp(Mathf.Abs(vx) / 3f, 0f, 1f) * 18f;
            float climb = pm.Facing * Mathf.Clamp(vy / 3f, -1f, 1f) * 10f;
            float wave = Mathf.Sin(Time.time * 4f) * 4f;
            float z = Mathf.LerpAngle(vt.localEulerAngles.z, lean + climb + wave, Time.deltaTime * 8f);
            vt.localEulerAngles = new Vector3(0f, 0f, z);
        }
        else if (wasSwim)
        {
            float z = Mathf.LerpAngle(vt.localEulerAngles.z, 0f, Time.deltaTime * 12f);
            if (Mathf.Abs(Mathf.DeltaAngle(z, 0f)) < 0.5f) { z = 0f; wasSwim = false; }
            vt.localEulerAngles = new Vector3(0f, 0f, z);
        }
    }

    // ------------------------------------------------------------------ cape luisante (immunité à l'acide)
    private SpriteRenderer capeGlow;
    private float capeSparkle;

    void UpdateCapeGlow(SpriteRenderer sr, float dt)
    {
        if (!AcidImmunity.Has)
        {
            if (capeGlow != null) capeGlow.enabled = false;
            return;
        }
        if (capeGlow == null)
        {
            Sprite g = FxUtil.LoadSprite("lueur_douce");
            if (g == null) return;
            capeGlow = FxUtil.MakeSprite("Lueur de la cape", g, transform.position, sr.sortingOrder - 1, new Color(0.5f, 1f, 0.35f, 0.2f), FxUtil.Additive, transform);
            capeGlow.transform.localScale = new Vector3(1.1f, 1.5f, 1f);
        }
        capeGlow.enabled = true;
        capeGlow.sortingLayerID = sr.sortingLayerID;
        capeGlow.sortingOrder = sr.sortingOrder - 1;
        // la lueur reste dans le dos du héros, du côté de la cape
        float back = sr.flipX ? 0.3f : -0.3f;
        capeGlow.transform.position = sr.transform.position + new Vector3(back, -0.05f, 0f);
        capeGlow.color = new Color(0.5f, 1f, 0.35f, 0.17f + 0.07f * Mathf.Sin(Time.time * 2.2f));
        // de rares étincelles vertes se détachent de la cape
        capeSparkle -= dt;
        if (capeSparkle <= 0f)
        {
            capeSparkle = Random.Range(0.5f, 1.1f);
            Sprite e = FxUtil.LoadSprite("eclat");
            if (e != null)
                SimpleParticle.Spawn(e, sr.transform.position + new Vector3(back * 1.4f + Random.Range(-0.2f, 0.2f), Random.Range(-0.4f, 0.4f), 0f),
                    new Vector2(0f, Random.Range(0.3f, 0.8f)), 0.8f, new Color(0.6f, 1f, 0.4f, 0.7f), Random.Range(0.3f, 0.5f), sr.sortingOrder + 1, FxUtil.Additive, -0.2f, 0.5f);
        }
    }
}
