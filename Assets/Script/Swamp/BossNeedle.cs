using UnityEngine;

// Trompe lancée par le gardien : comme celle des moustiques piqueurs, mais plus grosse et pleine d'acide.
// Quand elle touche le joueur ou un mur, de l'acide jaillit sur une petite zone (AcidSplash).
public class BossNeedle : MonoBehaviour
{
    public int damage = 30;
    public float speed = 12f;
    public float maxDistance = 40f;

    private Vector2 dir;
    private float traveled;
    private bool done;
    private Rigidbody2D body;

    public static BossNeedle Spawn(Vector3 position, Vector2 direction, int damage, float speed)
    {
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.left;
        GameObject go = new GameObject("Trompe du gardien");
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        go.transform.localScale = Vector3.one * 1.9f;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FxUtil.LoadSprite("trompe");
        sr.sortingOrder = 10001;
        sr.color = new Color(0.65f, 1f, 0.45f);

        Sprite g = FxUtil.LoadSprite("lueur_douce");
        if (g != null)
        {
            SpriteRenderer glow = FxUtil.MakeSprite("Lueur", g, position, 10000, new Color(0.5f, 1f, 0.25f, 0.5f), FxUtil.Additive, go.transform);
            glow.transform.localPosition = new Vector3(0.35f, 0f, 0f);
            glow.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        }

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.8f, 0.2f);

        TrailRenderer trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.25f;
        trail.widthMultiplier = 0.14f;
        trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
        trail.sharedMaterial = FxUtil.Additive;
        Gradient gr = new Gradient();
        gr.SetKeys(new[] { new GradientColorKey(new Color(0.7f, 1f, 0.4f), 0f), new GradientColorKey(new Color(0.15f, 0.5f, 0.1f), 1f) },
                   new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gr;
        trail.sortingOrder = 9999;

        BossNeedle n = go.AddComponent<BossNeedle>();
        n.dir = direction;
        n.damage = damage;
        n.speed = speed;
        n.body = rb;
        AudioManager.Play("trompe", 0.7f, Random.Range(0.7f, 0.85f));
        return n;
    }

    void FixedUpdate()
    {
        if (done) return;
        float step = speed * Time.fixedDeltaTime;
        body.MovePosition(body.position + dir * step);
        traveled += step;
        if (traveled >= maxDistance) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (done) return;
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
            if (ph != null && ph.CanTakeDamage)
            {
                ph.TakeDamage(damage);
                PlayerMovement pm = other.GetComponentInParent<PlayerMovement>();
                if (pm != null && ph.CurrentHealth > 0)
                    pm.ApplyKnockback(new Vector2(Mathf.Sign(dir.x == 0f ? 1f : dir.x) * 5.5f, 4.5f), 0.3f);
                PlayerProfile.Record(PlayerProfile.Event.HitByStinger);
            }
            Burst(true);
            return;
        }
        if (other.isTrigger) return;
        if (other.usedByEffector && other.GetComponent<PlatformEffector2D>() != null) return; // les planches ne bloquent pas les trompes
        if (other.GetComponentInParent<EnemyHealth>() != null) return;
        if (other.GetComponentInParent<BossMosquito>() != null) return;
        // Mur, sol ou plafond : la trompe se plante et l'acide jaillit
        Burst(false);
    }

    void Burst(bool hitPlayer)
    {
        done = true;
        if (!hitPlayer) PlayerProfile.Record(PlayerProfile.Event.DodgedStinger);
        AcidSplash.Spawn(transform.position, 1.4f, 3.5f, 12, !hitPlayer && Mathf.Abs(dir.y) > 0.5f);
        AudioManager.Play("impact", 0.5f, 0.6f);
        if (Camera.main != null)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.Shake(0.05f, 0.2f);
        }
        Destroy(gameObject);
    }
}
