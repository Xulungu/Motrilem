using UnityEngine;
using UnityEngine.UI;

// Mini-boss du Terrier du Gluant : le Gros Gluant, un slime géant dans son antre.
// Combat en arène : les grilles se ferment, une barre de vie apparaît, il enchaîne bonds, écrasements (ondes au sol),
// crachats de glu et appels de petits (cf. GluantAttacks). À sa mort : les grilles s'ouvrent, il laisse son noyau et
// l'éclat de vie qu'il gardait. Si le joueur meurt, le combat est réinitialisé.
[RequireComponent(typeof(Slime))]
public class MiniBossSlime : MonoBehaviour
{
    public string bossName = "Le Gros Gluant";
    public string flag = "miniboss_gluant";
    public string rewardId = "noyau_gluant";
    public string rewardLabel = "Noyau gluant";

    public Vector2 arenaMin, arenaMax;           // limites de l'antre
    public ArenaLock entryLock;                  // grille qui se ferme derrière le joueur
    public GameObject rewardShard;               // l'éclat de vie, caché tant que le gardien n'est pas vaincu

    private EnemyHealth health;
    private Slime slime;
    private GluantAttacks attacks;
    private Vector3 home;
    private bool fighting;
    private string prevMusic; private float prevVol = 0.5f;
    private Transform player;
    private RectTransform barRoot;
    private HealthBar bar;
    private bool dead;

    /// <summary>Crée le mini-boss (rien si le joueur l'a déjà vaincu).</summary>
    public static MiniBossSlime Create(Vector3 position, Transform parent)
    {
        Slime s = Slime.Create(position, true, parent, 2.3f);
        s.splitCount = 4;
        s.hop = new Vector2(2.4f, 7.5f);
        s.chaseHop = new Vector2(3.6f, 9f);
        s.hopInterval = 1.7f;
        s.detectRange = 10f;
        s.patrolRange = 4f;
        EnemyHealth eh = s.GetComponent<EnemyHealth>();
        eh.maxHealth = 30;
        eh.shardColor = new Color(0.55f, 1f, 0.3f);
        EnemyDamage dmg = s.GetComponentInChildren<EnemyDamage>();
        dmg.damage = 25;
        dmg.knockback = new Vector2(6f, 5f);
        dmg.slowFactor = 0.45f;
        dmg.slowDuration = 2.8f;
        s.gameObject.AddComponent<GluantAttacks>();
        return s.gameObject.AddComponent<MiniBossSlime>();
    }

    void Start()
    {
        if (SaveSystem.HasFlag(flag))
        {
            // déjà vaincu : l'éclat de vie, s'il n'a pas été ramassé, est à sa place
            if (rewardShard != null) rewardShard.SetActive(true);
            Destroy(gameObject);
            return;
        }
        home = transform.position;
        health = GetComponent<EnemyHealth>();
        slime = GetComponent<Slime>();
        attacks = GetComponent<GluantAttacks>();
        if (attacks != null) { attacks.arenaMin = arenaMin; attacks.arenaMax = arenaMax; }
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) player = pm.transform;
        if (health != null) health.OnDeath += Died;
    }

    void OnDestroy()
    {
        if (health != null) health.OnDeath -= Died;
        if (barRoot != null) Destroy(barRoot.gameObject);
    }

    void Update()
    {
        if (dead || health == null || player == null) return;
        bool inside = player.position.x > arenaMin.x + 3f && player.position.x < arenaMax.x && player.position.y > arenaMin.y - 1f && player.position.y < arenaMax.y + 2f;
        if (!fighting)
        {
            // le combat commence quand le joueur s'avance dans l'antre
            if (inside) StartFight();
            return;
        }
        // le joueur est mort (il a réapparu ailleurs) ou a quitté l'antre : tout est remis à zéro
        if (player.position.x < arenaMin.x - 4f || player.position.x > arenaMax.x + 6f || player.position.y < arenaMin.y - 6f || player.position.y > arenaMax.y + 10f)
        {
            ResetFight();
            return;
        }
        if (bar != null) bar.SetHealth(health.Health);
    }

    void StartFight()
    {
        fighting = true;
        if (entryLock != null) entryLock.SetLocked(true);
        if (attacks != null) attacks.active = true;
        if (barRoot == null) BuildBar();
        string cur = AudioManager.CurrentMusic;
        if (cur != "musique_combat") { prevMusic = cur; prevVol = AudioManager.CurrentVolume; }
        AudioManager.PlayMusic("musique_combat", 0.6f, 0.8f);
        GameUI.Instance.PlayTitle(bossName, "Mini-boss");
    }

    void ResetFight()
    {
        fighting = false;
        if (attacks != null) attacks.ResetFight();
        if (entryLock != null) entryLock.SetLocked(false);
        health.Restore();
        if (slime != null) slime.suspended = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) { rb.velocity = Vector2.zero; rb.position = home; }
        transform.position = home;
        if (barRoot != null) { Destroy(barRoot.gameObject); barRoot = null; bar = null; }
        RestoreMusic();
    }

    void RestoreMusic()
    {
        if (AudioManager.CurrentMusic != "musique_combat") return;
        if (string.IsNullOrEmpty(prevMusic)) AudioManager.StopMusic(1.5f);
        else AudioManager.PlayMusic(prevMusic, prevVol, 1.5f);
    }

    void Died(bool stomped)
    {
        if (dead) return;
        dead = true;
        SaveSystem.SetFlag(flag);
        if (barRoot != null) Destroy(barRoot.gameObject, 0.4f);
        if (GameUI.HasInstance) GameUI.Instance.Toast(bossName + " est vaincu");
        AudioManager.Play("slime_mort", 0.9f, 0.6f);
        if (Camera.main != null)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.Shake(0.1f, 0.6f);
        }
        RewardPickup.Create(transform.position + new Vector3(0f, 0.6f, 0f), rewardId, rewardLabel, transform.parent);
        if (attacks != null) { attacks.active = false; attacks.ClearSummoned(); }
        if (entryLock != null) entryLock.SetLocked(false);
        if (rewardShard != null)
        {
            // le Gluant gardait un éclat de vie : il apparaît là où il est tombé
            rewardShard.transform.position = transform.position + new Vector3(0f, 1.0f, 0f);
            rewardShard.SetActive(true);
        }
        RestoreMusic();
    }

    void BuildBar()
    {
        GameUI ui = GameUI.Instance;
        barRoot = UIFactory.Rect("Barre du mini-boss", ui.Root);
        barRoot.anchorMin = barRoot.anchorMax = new Vector2(0.5f, 1f);
        barRoot.pivot = new Vector2(0.5f, 1f);
        barRoot.anchoredPosition = new Vector2(0f, -40f);
        barRoot.sizeDelta = new Vector2(620f, 40f);

        Text label = UIFactory.Label("Nom", barRoot, ui.Font, 24, new Color(0.85f, 1f, 0.7f), TextAnchor.LowerCenter);
        label.rectTransform.anchorMin = new Vector2(0f, 1f);
        label.rectTransform.anchorMax = new Vector2(1f, 1f);
        label.rectTransform.pivot = new Vector2(0.5f, 0f);
        label.rectTransform.anchoredPosition = new Vector2(0f, 2f);
        label.rectTransform.sizeDelta = new Vector2(0f, 30f);
        label.fontStyle = FontStyle.Bold;
        label.text = bossName;

        Image frame = barRoot.gameObject.AddComponent<Image>();
        frame.color = new Color(0.05f, 0.07f, 0.05f, 0.92f);
        RectTransform inner = UIFactory.Rect("Interieur", barRoot);
        inner.anchorMin = Vector2.zero;
        inner.anchorMax = Vector2.one;
        inner.offsetMin = new Vector2(5f, 5f);
        inner.offsetMax = new Vector2(-5f, -5f);
        inner.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.2f, 0.1f, 1f);
        Image ghost = NewBarImage(inner, new Color(1f, 1f, 1f, 0.8f));
        Image fill = NewBarImage(inner, new Color(0.5f, 0.95f, 0.3f));
        bar = barRoot.gameObject.AddComponent<HealthBar>();
        bar.ghost = ghost;
        bar.fill = fill;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(new Color(0.9f, 0.3f, 0.2f), 0f), new GradientColorKey(new Color(0.5f, 0.95f, 0.3f), 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        bar.gradient = g;
        bar.SetMaxHealth(health.maxHealth);
        bar.SetHealth(health.Health);
    }

    static Image NewBarImage(Transform parent, Color color)
    {
        RectTransform rt = UIFactory.Rect("Barre", parent);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }
}
