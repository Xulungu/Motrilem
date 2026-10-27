using UnityEngine;

// Projectiles gluants du Gros Gluant : vague rasante qui court sur le sol (à sauter) et boule de glu lancée en cloche.
// Les deux blessent et ralentissent le joueur ; un sort fait éclater les boules.
public class GooShot : MonoBehaviour
{
    private enum Kind { Wave, Ball }
    private Kind kind;
    private Vector2 vel;
    private float life, age;
    private float minX = -9999f, maxX = 9999f;
    private Rigidbody2D rb;
    private readonly Collider2D[] hits = new Collider2D[6];

    static void StyleDamage(GameObject go, int damage)
    {
        EnemyDamage d = go.AddComponent<EnemyDamage>();
        d.damage = damage;
        d.knockback = new Vector2(5f, 5f);
        d.slowFactor = 0.5f;
        d.slowDuration = 2f;
    }

    /// <summary>Onde de choc au ras du sol : se déplace horizontalement jusqu'au bord de l'arène.</summary>
    public static GooShot Wave(Vector3 feet, int dir, float speed, float minX, float maxX, Transform parent)
    {
        GameObject go = new GameObject("Onde gluante");
        if (parent != null) go.transform.SetParent(parent, true);
        go.transform.position = feet + new Vector3(dir * 0.8f, 0.4f, 0f);
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.0f, 0.8f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FxUtil.LoadSprite("slime_0");
        sr.sortingOrder = 9996;
        sr.color = new Color(0.65f, 1f, 0.4f, 0.95f);
        sr.flipX = dir > 0;
        go.transform.localScale = new Vector3(1.0f, 0.75f, 1f);
        Sprite g = FxUtil.LoadSprite("lueur_douce");
        if (g != null)
        {
            SpriteRenderer glow = FxUtil.MakeSprite("Lueur", g, go.transform.position, 9995, new Color(0.55f, 1f, 0.25f, 0.45f), FxUtil.Additive, go.transform);
            glow.transform.localScale = new Vector3(1.8f, 1.0f, 1f);
        }
        StyleDamage(go, 12);
        GooShot s = go.AddComponent<GooShot>();
        s.kind = Kind.Wave; s.vel = new Vector2(dir * speed, 0f); s.life = 4f; s.minX = minX; s.maxX = maxX; s.rb = rb;
        return s;
    }

    /// <summary>Boule de glu lancée vers 'target' (tir en cloche).</summary>
    public static GooShot Ball(Vector3 from, Vector3 target, float flightTime, Transform parent)
    {
        GameObject go = new GameObject("Boule de glu");
        if (parent != null) go.transform.SetParent(parent, true);
        go.transform.position = from;
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.34f;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FxUtil.LoadSprite("goutte_acide");
        sr.sortingOrder = 10000;
        sr.color = new Color(0.7f, 1f, 0.45f);
        go.transform.localScale = Vector3.one * 1.8f;
        Sprite g = FxUtil.LoadSprite("lueur_douce");
        if (g != null)
        {
            SpriteRenderer glow = FxUtil.MakeSprite("Lueur", g, from, 9999, new Color(0.55f, 1f, 0.25f, 0.5f), FxUtil.Additive, go.transform);
            glow.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        }
        StyleDamage(go, 10);
        EnemyHealth eh = go.AddComponent<EnemyHealth>();
        eh.maxHealth = 1;
        eh.rootToDestroy = go;
        eh.countsAsEnemy = false;
        eh.deathStyle = EnemyHealth.DeathStyle.Eclatement;
        eh.deathSound = "slime_mort";
        eh.shardColor = new Color(0.6f, 1f, 0.3f);
        float t = Mathf.Max(0.4f, flightTime);
        float g0 = -Physics2D.gravity.y * rb.gravityScale;
        Vector2 d = target - from;
        rb.velocity = new Vector2(d.x / t, d.y / t + 0.5f * g0 * t);
        GooShot s = go.AddComponent<GooShot>();
        s.kind = Kind.Ball; s.life = 6f; s.rb = rb;
        return s;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= life) { Pop(); return; }
        if (kind == Kind.Wave)
        {
            transform.position += (Vector3)(vel * Time.deltaTime);
            if (transform.position.x < minX || transform.position.x > maxX) { Pop(); return; }
            // une onde s'arrête sur un mur
            Vector2 p = (Vector2)transform.position + new Vector2(Mathf.Sign(vel.x) * 0.55f, 0f);
            int n = Physics2D.OverlapCircleNonAlloc(p, 0.2f, hits);
            for (int i = 0; i < n; i++)
            {
                Collider2D h = hits[i];
                if (h == null || h.isTrigger || h.CompareTag("Player") || h.GetComponentInParent<Slime>() != null) continue;
                Pop();
                return;
            }
            transform.localScale = new Vector3(1.0f, 0.75f + 0.08f * Mathf.Sin(age * 18f), 1f);
        }
        else
        {
            EnemyHealth eh = GetComponent<EnemyHealth>();
            if (eh != null && eh.IsDead) return;
            int n = Physics2D.OverlapCircleNonAlloc(transform.position, 0.3f, hits);
            for (int i = 0; i < n; i++)
            {
                Collider2D h = hits[i];
                if (h == null || h.isTrigger || h.CompareTag("Player") || h.GetComponentInParent<Slime>() != null) continue;
                Pop();
                return;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // EnemyDamage s'occupe des dégâts ; la boule éclate au contact du joueur
        if (kind == Kind.Ball && other.CompareTag("Player")) Invoke("Pop", 0.02f);
    }

    void Pop()
    {
        if (this == null) return;
        AcidSplash.Spawn(transform.position, 0.8f, 1.4f, 7, false);
        Destroy(gameObject);
    }
}
