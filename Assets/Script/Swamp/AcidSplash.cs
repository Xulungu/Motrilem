using UnityEngine;

// Éclaboussure d'acide : petite zone dangereuse pendant quelques secondes (trompe du gardien qui s'écrase,
// goutte tombée de son corps...). Le joueur qui reste dedans est brûlé ; elle se résorbe d'elle-même.
public class AcidSplash : MonoBehaviour
{
    public int damage = 12;
    public float life = 3.5f;

    private float age;
    private SpriteRenderer glow, puddle;
    private float radius;

    public static AcidSplash Spawn(Vector3 position, float radius = 1.3f, float life = 3.5f, int damage = 12, bool onGround = true)
    {
        GameObject go = new GameObject("Éclaboussure d'acide");
        go.transform.position = position;
        CircleCollider2D c = go.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = radius;
        AcidSplash s = go.AddComponent<AcidSplash>();
        s.radius = radius;
        s.life = life;
        s.damage = damage;

        Sprite g = FxUtil.LoadSprite("lueur_douce");
        if (g != null)
        {
            s.glow = FxUtil.MakeSprite("Lueur", g, position, 9995, new Color(0.55f, 1f, 0.25f, 0.55f), FxUtil.Additive, go.transform);
            s.glow.transform.localScale = Vector3.one * (radius * 1.5f);
        }
        Sprite p = FxUtil.LoadSprite("flaque_acide");
        if (p != null && onGround)
        {
            s.puddle = FxUtil.MakeSprite("Flaque", p, position + new Vector3(0f, 0.05f, 0f), 9996, Color.white, null, go.transform);
            s.puddle.transform.localScale = new Vector3(radius * 2f / 1.5f, 1.1f, 1f);
        }
        Sprite e = FxUtil.LoadSprite("eclat");
        if (e != null)
            for (int i = 0; i < 9; i++)
                SimpleParticle.Spawn(e, position, new Vector2(Random.Range(-3f, 3f), Random.Range(1.5f, 5f)), Random.Range(0.4f, 0.7f),
                    new Color(0.6f, 1f, 0.3f), Random.Range(0.8f, 1.4f), 10006, FxUtil.Additive, 12f, 0.5f);
        AudioManager.Play("acide", 0.45f, Random.Range(1.1f, 1.4f));
        return s;
    }

    void Update()
    {
        age += Time.deltaTime;
        float fade = Mathf.Clamp01((life - age) / 0.8f);
        float pulse = 1f + 0.08f * Mathf.Sin(age * 9f);
        if (glow != null)
        {
            glow.color = new Color(0.55f, 1f, 0.25f, 0.5f * fade);
            glow.transform.localScale = Vector3.one * (radius * 1.5f * pulse);
        }
        if (puddle != null) puddle.color = new Color(1f, 1f, 1f, fade);
        if (age >= life) Destroy(gameObject);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (damage <= 0 || !other.CompareTag("Player") || AcidImmunity.Has) return;
        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph == null || !ph.CanTakeDamage) return;
        ph.TakeDamage(damage);
        PlayerMovement pm = other.GetComponentInParent<PlayerMovement>();
        if (pm != null && ph.CurrentHealth > 0)
        {
            float side = Mathf.Sign(other.transform.position.x - transform.position.x);
            if (side == 0f) side = 1f;
            pm.ApplyKnockback(new Vector2(3.5f * side, 4.5f), 0.25f);
        }
    }
}
