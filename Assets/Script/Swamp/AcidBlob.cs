using UnityEngine;

// Globule d'acide poursuivant : lancé par le gardien. Il suit lentement le joueur, mais un sort le fait éclater.
public class AcidBlob : MonoBehaviour
{
    public int damage = 15;
    public float speed = 3.2f;
    public float life = 8f;

    private Transform target;
    private float age;
    private Vector2 vel;
    private EnemyHealth health;

    public static AcidBlob Spawn(Vector3 position, Vector2 initialVelocity)
    {
        GameObject go = new GameObject("Globule d'acide");
        go.transform.position = position;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FxUtil.LoadSprite("goutte_acide");
        sr.sortingOrder = 10000;
        sr.color = new Color(0.75f, 1f, 0.5f);
        go.transform.localScale = Vector3.one * 2.1f;

        Sprite g = FxUtil.LoadSprite("lueur_douce");
        if (g != null)
        {
            SpriteRenderer glow = FxUtil.MakeSprite("Lueur", g, position, 9999, new Color(0.55f, 1f, 0.25f, 0.5f), FxUtil.Additive, go.transform);
            glow.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        }

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        CircleCollider2D c = go.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = 0.32f;

        EnemyHealth eh = go.AddComponent<EnemyHealth>();
        eh.maxHealth = 2;
        eh.rootToDestroy = go;
        eh.countsAsEnemy = false;
        eh.deathStyle = EnemyHealth.DeathStyle.Eclatement;
        eh.deathSound = "acide";
        eh.shardColor = new Color(0.6f, 1f, 0.3f);

        AcidBlob b = go.AddComponent<AcidBlob>();
        b.vel = initialVelocity;
        b.health = eh;
        return b;
    }

    void Start()
    {
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) target = pm.transform;
    }

    void Update()
    {
        if (health != null && health.IsDead) return;
        age += Time.deltaTime;
        if (age >= life) { Pop(); return; }
        float mult = health != null ? health.SpeedMultiplier : 1f;
        Vector2 want = Vector2.zero;
        if (target != null)
        {
            Vector2 to = (Vector2)target.position + new Vector2(0f, 0.2f) - (Vector2)transform.position;
            want = to.normalized * speed * mult;
        }
        vel = Vector2.Lerp(vel, want, Time.deltaTime * 2.2f);
        transform.position += (Vector3)(vel * Time.deltaTime) + new Vector3(0f, Mathf.Sin(age * 6f) * 0.01f, 0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (health != null && health.IsDead) return;
        if (!other.CompareTag("Player")) return;
        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null && ph.CanTakeDamage)
        {
            ph.TakeDamage(damage);
            PlayerMovement pm = other.GetComponentInParent<PlayerMovement>();
            if (pm != null && ph.CurrentHealth > 0)
            {
                float side = Mathf.Sign(other.transform.position.x - transform.position.x);
                if (side == 0f) side = 1f;
                pm.ApplyKnockback(new Vector2(4f * side, 4f), 0.25f);
            }
        }
        Pop();
    }

    void Pop()
    {
        AcidSplash.Spawn(transform.position, 0.9f, 1.6f, 8, false);
        Destroy(gameObject);
    }
}
