using System.Collections.Generic;
using UnityEngine;

// Projectile magique :
//  - Feu    : boule de feu, explose en petite zone et fait brûler (dégâts sur la durée)
//  - Foudre : éclair très rapide qui rebondit sur 2 ennemis proches (aucun étourdissement)
//  - Glace  : éclat de glace, ralentit ; 3 coups rapprochés gèlent l'ennemi
// Le projectile vole tout droit (8 directions possibles) jusqu'à toucher un mur ou un ennemi.
public class MagicProjectile : MonoBehaviour
{
    private MagicType type;
    private Vector2 direction;
    private float speed, traveled;
    private int damage;
    private GameObject owner;
    private Rigidbody2D body;
    private SpriteRenderer sr;
    private bool done;
    private float age;
    private float baseAngle;
    private float scaleMul = 1f;

    public static MagicProjectile Spawn(MagicType type, Vector3 position, int dir, GameObject owner)
    {
        return Spawn(type, position, new Vector2(dir < 0 ? -1f : 1f, 0f), owner);
    }

    /// <summary>Lance un sort dans n'importe quelle direction (horizontale, verticale ou diagonale).</summary>
    public static MagicProjectile Spawn(MagicType type, Vector3 position, Vector2 dir, GameObject owner, float charge = 0f)
    {
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        // Le sprite pointe vers la droite : retourné vers la gauche, puis incliné selon la visée
        bool left = dir.x < -0.01f;
        float angle = left ? Mathf.Atan2(-dir.y, -dir.x) * Mathf.Rad2Deg : Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        string spriteName = type == MagicType.Feu ? "boule_feu" : type == MagicType.Foudre ? "eclair" : "eclat_glace";
        Color col = PlayerMagic.ColorOf(type);

        GameObject go = new GameObject("Sort - " + PlayerMagic.NameOf(type));
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FxUtil.LoadSprite(spriteName);
        sr.sortingOrder = 10004;
        sr.flipX = left;

        // Halo
        Sprite glow = FxUtil.LoadSprite("lueur_douce");
        if (glow != null)
        {
            SpriteRenderer halo = FxUtil.MakeSprite("Halo", glow, position, 10003, new Color(col.r, col.g, col.b, 0.6f), FxUtil.Additive, go.transform);
            halo.transform.localScale = Vector3.one * (type == MagicType.Foudre ? 0.9f : 0.7f);
        }

        // Traînée
        TrailRenderer trail = go.AddComponent<TrailRenderer>();
        trail.time = type == MagicType.Foudre ? 0.08f : 0.18f;
        trail.widthMultiplier = type == MagicType.Feu ? 0.3f : 0.18f;
        trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
        trail.sharedMaterial = FxUtil.Additive;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(col, 1f) },
                  new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = g;
        trail.sortingOrder = 10002;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        // Pas d'interpolation : le sprite est affiché exactement là où se trouve la hitbox
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CircleCollider2D col2d = go.AddComponent<CircleCollider2D>();
        col2d.isTrigger = true;
        col2d.radius = type == MagicType.Feu ? 0.22f : 0.16f;

        MagicProjectile p = go.AddComponent<MagicProjectile>();
        p.type = type;
        p.direction = dir;
        p.baseAngle = angle;
        p.owner = owner;
        p.body = rb;
        p.sr = sr;
        switch (type)
        {
            // Équilibrage : le feu fait peu de dégâts directs mais brûle et éclabousse ;
            // la foudre frappe fort et rebondit ; la glace frappe fort et contrôle (gel).
            case MagicType.Feu: p.speed = 11f; p.damage = 1; break;
            case MagicType.Foudre: p.speed = 26f; p.damage = 2; break;
            default: p.speed = 14f; p.damage = 2; break;
        }
        // Sort chargé (0..1) : plus gros, plus de dégâts
        charge = Mathf.Clamp01(charge);
        if (charge > 0f)
        {
            p.damage = Mathf.RoundToInt(p.damage * (1f + 2f * charge));
            p.scaleMul = 1f + charge * 0.9f;
            go.transform.localScale = Vector3.one * p.scaleMul;
            col2d.radius *= 1f + charge * 0.5f;
        }
        return p;
    }

    void FixedUpdate()
    {
        if (done) return;
        float step = speed * Time.fixedDeltaTime;
        body.MovePosition(body.position + direction * step);
        traveled += step;
        // Le sort ne s'arrête que contre un mur ou un ennemi (sécurité : 6 s ou 120 unités max)
        if (traveled >= 120f || age > 6f) Fizzle();
    }

    void Update()
    {
        age += Time.deltaTime;
        if (sr == null) return;
        switch (type)
        {
            case MagicType.Feu:
                float f = 1f + 0.15f * Mathf.Sin(age * 40f);
                transform.localScale = new Vector3(f * scaleMul, f * scaleMul, 1f);
                break;
            case MagicType.Foudre:
                transform.localScale = new Vector3(scaleMul, (Random.value < 0.5f ? 1f : -1f) * scaleMul, 1f);
                break;
            default:
                transform.rotation = Quaternion.Euler(0, 0, baseAngle + Mathf.Sin(age * 25f) * 6f);
                break;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (done) return;
        if (owner != null && other.attachedRigidbody != null && other.attachedRigidbody.gameObject == owner) return;

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (other.isTrigger && enemy == null) return; // zones d'aide, checkpoints...
        if (other.usedByEffector && other.GetComponent<PlatformEffector2D>() != null) return; // les planches ne bloquent pas les sorts
        if (enemy != null && enemy.IsDead) return;

        SnapToContact(other);
        if (enemy != null) HitEnemy(enemy);
        else Impact();
    }

    /// <summary>Place le projectile pile au point de contact (la pointe du sprite touche la cible).</summary>
    void SnapToContact(Collider2D other)
    {
        Vector2 center = body.position;
        Vector2 contact = other.ClosestPoint(center - direction * 1.5f);
        float tip = sr != null && sr.sprite != null ? sr.sprite.bounds.extents.x * Mathf.Abs(transform.localScale.x) : 0.2f;
        Vector2 snapped = contact - direction * tip * 0.8f;
        // On n'avance que vers l'avant (jamais de retour en arrière visible)
        if (Vector2.Dot(snapped - center, direction) > 0f) transform.position = new Vector3(snapped.x, snapped.y, transform.position.z);
    }

    void HitEnemy(EnemyHealth enemy)
    {
        done = true;
        Vector2 point = transform.position;
        PlayerProfile.Record(PlayerProfile.Event.SpellHit);
        PlayerProfile.RecordAttackDistance(traveled);
        enemy.TakeDamage(damage, type, direction);

        switch (type)
        {
            case MagicType.Feu:
                // Petite explosion : touche aussi les ennemis proches
                foreach (EnemyHealth e in FindNear(point, 1.1f, enemy)) e.TakeDamage(1, type, (Vector2)e.transform.position - point);
                Burst(14, 3.5f);
                break;
            case MagicType.Foudre:
                // L'éclair rebondit sur deux ennemis proches
                Vector3 from = enemy.transform.position;
                List<EnemyHealth> hit = new List<EnemyHealth> { enemy };
                for (int i = 0; i < 2; i++)
                {
                    EnemyHealth nextTarget = Closest(from, 4.5f, hit);
                    if (nextTarget == null) break;
                    LightningArc.Draw(from, nextTarget.transform.position, PlayerMagic.ColorOf(type));
                    nextTarget.TakeDamage(1, type, (Vector2)(nextTarget.transform.position - from));
                    hit.Add(nextTarget);
                    from = nextTarget.transform.position;
                }
                Burst(10, 5f);
                break;
            default:
                Burst(10, 3f);
                break;
        }
        AudioManager.Play("impact", 0.5f, Random.Range(0.9f, 1.15f));
        End();
    }

    void Impact()
    {
        done = true;
        Burst(8, 2.5f);
        AudioManager.Play("impact", 0.35f, Random.Range(0.8f, 1f));
        End();
    }

    void Fizzle()
    {
        done = true;
        Burst(5, 1.5f);
        End();
    }

    void End()
    {
        // Le sprite reste visible un court instant au point d'impact, puis disparaît
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        TrailRenderer tr = GetComponent<TrailRenderer>();
        if (tr != null) tr.emitting = false;
        Invoke("Hide", 0.05f);
        Destroy(gameObject, 0.35f);
    }

    void Hide()
    {
        if (sr != null) sr.enabled = false;
        foreach (SpriteRenderer child in GetComponentsInChildren<SpriteRenderer>()) child.enabled = false;
        Collider2D c = GetComponent<Collider2D>();
        if (c != null) c.enabled = false;
    }

    void Burst(int count, float force)
    {
        Sprite s = FxUtil.LoadSprite("eclat");
        if (s == null) return;
        Color col = PlayerMagic.ColorOf(type);
        for (int i = 0; i < count; i++)
        {
            Vector2 v = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1f) * force - direction * force * 0.3f;
            SimpleParticle.Spawn(s, transform.position, v, Random.Range(0.25f, 0.5f), Color.Lerp(col, Color.white, Random.value * 0.5f),
                Random.Range(0.8f, 1.4f), 10006, FxUtil.Additive, type == MagicType.Glace ? 6f : 0f, 3f);
        }
    }

    static List<EnemyHealth> FindNear(Vector2 point, float radius, EnemyHealth exclude)
    {
        List<EnemyHealth> list = new List<EnemyHealth>();
        foreach (EnemyHealth e in FindObjectsOfType<EnemyHealth>())
        {
            if (e == exclude || e.IsDead) continue;
            if (Vector2.Distance(point, e.transform.position) <= radius) list.Add(e);
        }
        return list;
    }

    static EnemyHealth Closest(Vector3 point, float radius, List<EnemyHealth> exclude)
    {
        EnemyHealth best = null;
        float bestD = radius;
        foreach (EnemyHealth e in FindObjectsOfType<EnemyHealth>())
        {
            if (exclude.Contains(e) || e.IsDead) continue;
            float d = Vector2.Distance(point, e.transform.position);
            if (d < bestD) { bestD = d; best = e; }
        }
        return best;
    }
}
