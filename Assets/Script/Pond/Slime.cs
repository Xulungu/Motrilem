using System.Collections;
using UnityEngine;

// Slime d'acide : se déplace par petits bonds. Quand le joueur approche, il bondit vers lui.
// Il fait demi-tour devant les trous et les bassins d'acide. On peut l'écraser (point faible
// assez large sur le dessus) ou le détruire à la magie. Les gros slimes se divisent en deux petits.
public class Slime : MonoBehaviour
{
    public bool big = true;
    public float patrolRange = 3f;
    public float detectRange = 6f;
    public Vector2 hop = new Vector2(2.2f, 5f);
    public Vector2 chaseHop = new Vector2(3.8f, 6.5f);
    public float hopInterval = 1.3f;

    private Rigidbody2D rb;
    private Collider2D body;
    private EnemyHealth health;
    private Transform visual;
    private SpriteRenderer sr;
    private Sprite[] idle;
    private Sprite jump;
    private Transform player;
    private Vector3 home;
    private int dir = 1;
    private float timer;
    private bool grounded, wasGrounded, preparing;
    private Vector2 squash = Vector2.one, squashVel;
    private float halfHeight = 0.5f;
    private readonly Collider2D[] hits = new Collider2D[8];

    // ------------------------------------------------------------------ fabrication
    /// <summary>Crée un slime complet (corps, visuel, hitbox de dégâts, point faible, points de vie).</summary>
    public static Slime Create(Vector3 position, bool big, Transform parent = null)
    {
        float scale = big ? 1f : 0.6f;
        GameObject go = new GameObject(big ? "Gros slime" : "Petit slime");
        if (parent != null) go.transform.SetParent(parent, true);
        go.transform.position = position;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.2f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D bodyCol = go.AddComponent<BoxCollider2D>();
        bodyCol.size = new Vector2(1.0f, 0.72f) * scale;
        bodyCol.offset = new Vector2(0f, -0.1f * scale);

        GameObject v = new GameObject("Visuel");
        v.transform.SetParent(go.transform, false);
        SpriteRenderer sr = v.AddComponent<SpriteRenderer>();
        sr.sprite = FxUtil.LoadSprite("slime_0");
        sr.sortingOrder = 9997;
        v.transform.localScale = Vector3.one * scale;

        // Hitbox de dégâts (corps) avec le point faible en enfant
        GameObject hit = new GameObject("Hitbox");
        hit.transform.SetParent(go.transform, false);
        BoxCollider2D hitCol = hit.AddComponent<BoxCollider2D>();
        hitCol.isTrigger = true;
        hitCol.size = new Vector2(0.9f, 0.6f) * scale;
        hitCol.offset = new Vector2(0f, -0.12f * scale);

        GameObject weak = new GameObject("WeakSpot");
        weak.transform.SetParent(hit.transform, false);
        BoxCollider2D weakCol = weak.AddComponent<BoxCollider2D>();
        weakCol.isTrigger = true;
        WeakSpot ws = weak.AddComponent<WeakSpot>();
        ws.overrideHitbox = false;
        ws.objectToDestroy = go;
        ws.maxHorizontalOffset = 0.5f * scale + 0.1f;
        ws.minFallSpeed = 0.5f;
        ws.bounceVelocity = 6.5f;
        weakCol.size = new Vector2(0.8f, 0.14f) * scale;
        weakCol.offset = new Vector2(0f, 0.27f * scale);

        EnemyDamage dmg = hit.AddComponent<EnemyDamage>();
        dmg.damage = big ? 15 : 10;
        dmg.knockback = new Vector2(4f, 3.5f);

        EnemyHealth eh = go.AddComponent<EnemyHealth>();
        eh.maxHealth = big ? 3 : 1;
        eh.rootToDestroy = go;
        eh.deathSound = "slime_mort";
        eh.shardColor = new Color(0.5f, 1f, 0.3f);

        Slime slime = go.AddComponent<Slime>();
        slime.big = big;
        if (!big)
        {
            slime.hop = new Vector2(2.6f, 4.5f);
            slime.chaseHop = new Vector2(4.2f, 5.5f);
            slime.hopInterval = 0.9f;
        }
        return slime;
    }

    // ------------------------------------------------------------------ cycle de vie
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
        health = GetComponent<EnemyHealth>();
        sr = GetComponentInChildren<SpriteRenderer>();
        visual = sr != null ? sr.transform : transform;
        idle = new[] { FxUtil.LoadSprite("slime_0"), FxUtil.LoadSprite("slime_1") };
        jump = FxUtil.LoadSprite("slime_saut");
        if (sr != null && sr.sprite != null) halfHeight = sr.sprite.bounds.extents.y * visual.localScale.y;
    }

    void OnEnable() { if (health != null) health.OnDeath += Died; }
    void OnDisable() { if (health != null) health.OnDeath -= Died; }

    void Start()
    {
        home = transform.position;
        timer = Random.Range(0.3f, hopInterval);
        dir = Random.value < 0.5f ? -1 : 1;
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
            // Le corps du slime ne pousse pas le joueur (seule la hitbox de dégâts compte)
            foreach (Collider2D c in pm.GetComponentsInChildren<Collider2D>()) Physics2D.IgnoreCollision(body, c, true);
        }
    }

    void FixedUpdate()
    {
        grounded = CheckGround();
        if (grounded && !wasGrounded && rb.velocity.y <= 0.1f)
        {
            squash = new Vector2(1.35f, 0.7f);
            if (IsVisible()) AudioManager.Play("bulle", 0.25f, Random.Range(0.6f, 0.9f));
        }
        wasGrounded = grounded;
        // Frottement au sol (pas de glissade après un bond)
        if (grounded && !preparing) rb.velocity = new Vector2(Mathf.MoveTowards(rb.velocity.x, 0f, 25f * Time.fixedDeltaTime), rb.velocity.y);
    }

    void Update()
    {
        if (health == null || health.IsDead) return;
        float mult = health.SpeedMultiplier;
        float dt = Time.deltaTime;

        if (mult > 0f)
        {
            timer -= dt * mult;
            bool chasing = IsChasing();
            if (grounded && timer <= 0f && !preparing) StartCoroutine(Hop(chasing, mult));
        }

        // ---- visuel : écrasement / étirement, respiration, images
        Vector2 target = Vector2.one;
        if (preparing) target = new Vector2(1.25f, 0.75f);
        else if (!grounded) target = new Vector2(0.85f, 1.18f);
        else target = new Vector2(1f + Mathf.Sin(Time.time * 4f) * 0.04f, 1f - Mathf.Sin(Time.time * 4f) * 0.04f);
        if (mult <= 0f) target = Vector2.one; // gelé / étourdi
        squash = Vector2.SmoothDamp(squash, target, ref squashVel, 0.07f);

        float baseScale = big ? 1f : 0.6f;
        visual.localScale = new Vector3(squash.x * baseScale, squash.y * baseScale, 1f);
        visual.localPosition = new Vector3(0f, -(1f - squash.y) * halfHeight, 0f);
        if (sr != null)
        {
            sr.flipX = dir > 0;
            if (!grounded && jump != null) sr.sprite = jump;
            else if (idle[0] != null) sr.sprite = idle[Mathf.FloorToInt(Time.time * 2.5f + home.x) % 2];
        }
    }

    bool IsChasing()
    {
        if (player == null) return false;
        Vector2 d = player.position - transform.position;
        return Mathf.Abs(d.x) < detectRange && Mathf.Abs(d.y) < 3f;
    }

    IEnumerator Hop(bool chasing, float mult)
    {
        preparing = true;
        if (chasing) dir = player.position.x > transform.position.x ? 1 : -1;
        else
        {
            if (transform.position.x > home.x + patrolRange) dir = -1;
            else if (transform.position.x < home.x - patrolRange) dir = 1;
        }
        bool safe = SafeAhead(dir);
        if (!safe && !chasing) { dir = -dir; safe = SafeAhead(dir); }

        yield return new WaitForSeconds(0.25f); // anticipation (il s'écrase avant de sauter)
        if (health == null || health.IsDead) yield break;

        Vector2 force = chasing ? chaseHop : hop;
        if (!safe) force.x = 0f; // trou ou acide devant : il saute sur place
        rb.velocity = new Vector2(dir * force.x * mult, force.y * Mathf.Lerp(0.6f, 1f, mult));
        squash = new Vector2(0.75f, 1.3f);
        if (IsVisible()) AudioManager.Play("slime_saut", 0.45f, Random.Range(0.9f, 1.15f) * (big ? 1f : 1.3f));
        timer = hopInterval * (chasing ? 0.7f : 1f) * Random.Range(0.8f, 1.2f);
        preparing = false;
    }

    bool CheckGround()
    {
        Bounds b = body.bounds;
        Vector2 a = new Vector2(b.min.x + 0.05f, b.min.y - 0.06f);
        Vector2 c = new Vector2(b.max.x - 0.05f, b.min.y + 0.02f);
        int n = Physics2D.OverlapAreaNonAlloc(a, c, hits);
        for (int i = 0; i < n; i++)
        {
            Collider2D h = hits[i];
            if (h.isTrigger || h.attachedRigidbody == rb || h.CompareTag("Player")) continue;
            return true;
        }
        return false;
    }

    /// <summary>Y a-t-il du sol (et pas un bassin d'acide) là où le slime va retomber ?</summary>
    bool SafeAhead(int direction)
    {
        Vector2 probe = (Vector2)transform.position + new Vector2(direction * (big ? 1.8f : 1.4f), 0.5f);
        RaycastHit2D[] rays = Physics2D.RaycastAll(probe, Vector2.down, 2.6f);
        foreach (RaycastHit2D r in rays)
        {
            if (r.collider == null || r.collider.attachedRigidbody == rb) continue;
            if (r.collider.GetComponent<AcidPool>() != null) return false;
            if (r.collider.isTrigger || r.collider.CompareTag("Player")) continue;
            return true;
        }
        return false;
    }

    bool IsVisible()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Vector3 v = cam.WorldToViewportPoint(transform.position);
        return v.x > -0.1f && v.x < 1.1f && v.y > -0.1f && v.y < 1.1f;
    }

    // ------------------------------------------------------------------ mort
    void Died(bool stomped)
    {
        if (!big) return;
        // Le gros slime se divise en deux petits qui sautent de chaque côté
        for (int i = -1; i <= 1; i += 2)
        {
            Slime s = Create(transform.position + new Vector3(i * 0.3f, 0.2f, 0f), false, transform.parent);
            s.StartCoroutine(s.Pop(new Vector2(i * 2.5f, 5f)));
        }
    }

    IEnumerator Pop(Vector2 velocity)
    {
        yield return null;
        rb.velocity = velocity;
        squash = new Vector2(0.6f, 1.4f);
    }
}
