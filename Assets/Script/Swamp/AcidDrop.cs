using UnityEngine;

// Goutte d'acide qui tombe du corps du gardien (ou du plafond) : elle fait mal si elle touche le joueur,
// et laisse une petite flaque dangereuse en s'écrasant.
public class AcidDrop : MonoBehaviour
{
    public int damage = 10;
    public float gravity = 16f;
    public float maxFall = 14f;

    private float vy;
    private bool done;

    public static AcidDrop Spawn(Vector3 position, float scale = 1f, int damage = 10)
    {
        GameObject go = new GameObject("Goutte d'acide");
        go.transform.position = position;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FxUtil.LoadSprite("goutte_acide");
        sr.sortingOrder = 9998;
        go.transform.localScale = Vector3.one * scale;
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        CircleCollider2D c = go.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = 0.2f;
        Sprite g = FxUtil.LoadSprite("lueur_douce");
        if (g != null)
        {
            SpriteRenderer glow = FxUtil.MakeSprite("Lueur", g, position, 9997, new Color(0.5f, 1f, 0.25f, 0.45f), FxUtil.Additive, go.transform);
            glow.transform.localScale = Vector3.one * 0.5f;
        }
        AcidDrop d = go.AddComponent<AcidDrop>();
        d.damage = damage;
        return d;
    }

    void Update()
    {
        if (done) return;
        vy = Mathf.Min(maxFall, vy + gravity * Time.deltaTime);
        transform.position += new Vector3(0f, -vy * Time.deltaTime, 0f);
        if (transform.position.y < -120f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (done) return;
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
            if (ph != null && ph.CanTakeDamage && !AcidImmunity.Has) ph.TakeDamage(damage);
            Land(false);
            return;
        }
        if (other.isTrigger) return;
        if (other.GetComponentInParent<EnemyHealth>() != null) return;
        Land(true);
    }

    void Land(bool ground)
    {
        done = true;
        AcidSplash.Spawn(transform.position + new Vector3(0f, -0.1f, 0f), 0.85f, 2.6f, 8, ground);
        Destroy(gameObject);
    }
}
