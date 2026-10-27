using System.Collections;
using UnityEngine;

// Petit moustique volant, créé par le code : vagues du donjon et petits invoqués par le boss.
// Il tourne autour du joueur et le percute ; la version « piqueur » crache aussi des trompes.
// Le boss peut les attirer à lui pour les dévorer (Pull).
public class FlyingMinion : MonoBehaviour
{
    public bool shooter;
    public float speed = 3.2f;
    public int contactDamage = 12;

    private EnemyHealth health;
    private Transform player;
    private Transform pullTarget;
    private float phase, shootTimer, wobble;
    private SpriteRenderer sr;
    private Vector3 lastPos;
    private Vector3 spawnScale;
    private Transform[] wings;
    private readonly float[] wingPhase = { 0f, 1.3f, 2.4f };
    private float flapSpeed = 1f, lean, glowPulse;
    private SpriteRenderer belly;

    public bool IsAlive { get { return health != null && !health.IsDead; } }
    public bool IsPulled { get { return pullTarget != null; } }

    public static FlyingMinion Create(Vector3 position, bool shooter, Transform parent)
    {
        GameObject go = new GameObject(shooter ? "Moustique piqueur (vague)" : "Petit moustique");
        if (parent != null) go.transform.SetParent(parent, true);
        go.transform.position = position;

        GameObject v = new GameObject("Visuel");
        v.transform.SetParent(go.transform, false);
        SpriteRenderer sr = v.AddComponent<SpriteRenderer>();
        sr.sprite = FxUtil.LoadSprite("boss_corps") != null ? FxUtil.LoadSprite("boss_corps") : FxUtil.LoadSprite("boss_moustique");
        sr.sortingOrder = 9996;
        float scale = shooter ? 0.075f : 0.06f;
        v.transform.localScale = new Vector3(scale, scale, 1f);
        sr.color = shooter ? new Color(1f, 0.7f, 0.65f) : new Color(0.85f, 1f, 0.8f);

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        CircleCollider2D hurt = go.AddComponent<CircleCollider2D>();
        hurt.isTrigger = true;
        hurt.radius = 0.55f;

        GameObject hit = new GameObject("Hitbox");
        hit.transform.SetParent(go.transform, false);
        BoxCollider2D hb = hit.AddComponent<BoxCollider2D>();
        hb.isTrigger = true;
        hb.size = new Vector2(0.9f, 0.6f);
        EnemyDamage dmg = hit.AddComponent<EnemyDamage>();
        dmg.damage = shooter ? 15 : 12;
        dmg.knockback = new Vector2(4f, 3f);

        EnemyHealth eh = go.AddComponent<EnemyHealth>();
        eh.maxHealth = shooter ? 3 : 2;
        eh.rootToDestroy = go;
        eh.deathStyle = EnemyHealth.DeathStyle.Chute;
        eh.deathSound = "mort_ennemi";
        eh.shardColor = new Color(0.6f, 1f, 0.3f);

        FlyingMinion m = go.AddComponent<FlyingMinion>();
        m.BuildRig(sr);
        m.shooter = shooter;
        m.speed = shooter ? 2.6f : 3.6f;
        return m;
    }

    // Même corps et mêmes ailes que le boss (ailes qui battent, corps qui s'incline)
    void BuildRig(SpriteRenderer body)
    {
        Sprite corps = body.sprite;
        Vector2[] roots = { new Vector2(80f, 68f), new Vector2(126f, 82f), new Vector2(141f, 78f) };
        wings = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            Sprite ws = FxUtil.LoadSprite("boss_aile_" + i);
            if (ws == null) continue;
            Vector3 root = new Vector3((roots[i].x - ws.pivot.x) / ws.pixelsPerUnit, ((ws.rect.height - roots[i].y) - ws.pivot.y) / ws.pixelsPerUnit, 0f);
            GameObject pivot = new GameObject("Aile " + i);
            pivot.transform.SetParent(body.transform, false);
            pivot.transform.localPosition = root;
            GameObject wing = new GameObject("Sprite");
            wing.transform.SetParent(pivot.transform, false);
            wing.transform.localPosition = -root;
            SpriteRenderer r = wing.AddComponent<SpriteRenderer>();
            r.sprite = ws;
            r.sortingOrder = body.sortingOrder - 1;
            r.color = body.color;
            wings[i] = pivot.transform;
        }
        Sprite lg = FxUtil.LoadSprite("lueur");
        if (lg != null && corps != null)
        {
            belly = FxUtil.MakeSprite("Lueur du ventre", lg, Vector3.zero, body.sortingOrder + 1, new Color(0.55f, 1f, 0.2f, 0.3f), FxUtil.Additive, body.transform);
            belly.transform.localPosition = new Vector3((205f - corps.pivot.x) / corps.pixelsPerUnit, ((corps.rect.height - 150f) - corps.pivot.y) / corps.pixelsPerUnit, 0f);
            belly.transform.localScale = new Vector3(5f, 5f, 1f);
        }
    }

    void Awake()
    {
        health = GetComponent<EnemyHealth>();
        sr = GetComponentInChildren<SpriteRenderer>();
        phase = Random.Range(0f, 10f);
        shootTimer = Random.Range(1.2f, 2.6f);
        lastPos = transform.position;
    }

    void Start()
    {
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) player = pm.transform;
        spawnScale = sr != null ? sr.transform.localScale : Vector3.one;
    }

    /// <summary>L'acide dissout le moustique qui le touche (ce n'est pas un coup du joueur : rien dans son profil).</summary>
    public void DieInAcid()
    {
        if (health == null || health.IsDead) return;
        health.countsAsEnemy = false;
        health.Kill(false);
    }

    /// <summary>Séraphel l'attire : il file vers lui sans rien pouvoir faire.</summary>
    public void Pull(Transform target)
    {
        pullTarget = target;
        foreach (EnemyDamage d in GetComponentsInChildren<EnemyDamage>()) d.enabled = false;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
    }

    void Update()
    {
        if (health == null || health.IsDead) return;
        float dt = Time.deltaTime;
        float mult = health.SpeedMultiplier;
        phase += dt * 8f;
        Vector3 pos = transform.position;

        if (pullTarget != null)
        {
            Vector3 d = pullTarget.position - pos;
            float step = 11f * dt;
            if (d.magnitude > step) pos += d.normalized * step; else pos = pullTarget.position;
            transform.position = pos;
        }
        else if (player != null && mult > 0f)
        {
            // Il tourne autour de la tête du joueur, puis plonge sur lui par moments
            wobble += dt * mult;
            Vector3 center = player.position + new Vector3(Mathf.Sin(wobble * 0.9f + phase * 0.05f) * 3.2f, 2.2f + Mathf.Sin(wobble * 1.7f) * 1.1f, 0f);
            if (!shooter && Mathf.Sin(wobble * 0.55f + phase * 0.02f) > 0.82f) center = player.position + Vector3.up * 0.3f;
            Vector3 d = center - pos;
            pos += d.normalized * Mathf.Min(d.magnitude, speed * mult * dt * (shooter ? 1f : 1.3f));
            transform.position = pos;

            if (shooter)
            {
                shootTimer -= dt * mult;
                health.alert = shootTimer < 0.5f ? 0.5f : 0f;
                if (shootTimer <= 0f)
                {
                    shootTimer = Random.Range(2.2f, 3.4f);
                    Vector2 dir = (Vector2)(player.position + Vector3.up * 0.3f) - (Vector2)pos;
                    if (dir.magnitude > 2f)
                    {
                        AudioManager.Play("trompe", 0.35f, Random.Range(1f, 1.2f));
                        BossNeedle.Spawn(pos + (Vector3)(dir.normalized * 0.5f), dir, 14, 8.5f);
                    }
                }
            }
        }

        // visuel : orientation, inclinaison, battement des ailes
        if (sr != null)
        {
            Vector3 vel = transform.position - lastPos;
            float f = sr.transform.localScale.x < 0f ? -1f : 1f;
            if (Mathf.Abs(vel.x) > 0.001f) f = vel.x > 0f ? -1f : 1f;
            bool tell = shooter && shootTimer < 0.5f;
            bool diving = pullTarget == null && !shooter && player != null && Vector3.Distance(transform.position, player.position) < 1.6f;
            float targetLean = Mathf.Clamp(-f * vel.x / Mathf.Max(0.0001f, dt) * 3f, -25f, 25f) + (tell ? 12f * -f : 0f);
            lean = Mathf.Lerp(lean, targetLean, Mathf.Clamp01(8f * dt));
            float breathe = 1f + Mathf.Sin(Time.time * 3f + phase * 0.1f) * 0.03f;
            sr.transform.localScale = new Vector3(f * Mathf.Abs(spawnScale.x) * breathe, spawnScale.y / breathe, 1f);
            sr.transform.localPosition = new Vector3(0f, Mathf.Sin(phase) * 0.08f, 0f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, lean);
            if (wings != null)
            {
                float freq = tell ? 90f : diving ? 70f : 52f;
                float amp = tell ? 7f : 17f;
                float fold = diving ? 0.65f : 1f;
                for (int i = 0; i < wings.Length; i++)
                {
                    if (wings[i] == null) continue;
                    wings[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * freq + wingPhase[i] + phase * 0.3f) * amp);
                    wings[i].localScale = new Vector3(fold, fold, 1f);
                }
            }
            if (belly != null)
                belly.color = new Color(0.55f, 1f, 0.2f, (tell ? 0.7f : 0.3f) * (0.8f + 0.2f * Mathf.Sin(Time.time * 4f + phase)));
        }
        lastPos = transform.position;
    }
}
