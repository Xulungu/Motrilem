using UnityEngine;

// Geyser d'acide : une lueur annonce l'éruption, puis une colonne d'acide jaillit du sol et brûle tout sur son passage.
public class AcidColumn : MonoBehaviour
{
    public int damage = 22;
    public float warn = 0.9f;
    public float active = 0.55f;
    public float height = 5.5f;
    public float width = 1.6f;

    private float age;
    private SpriteRenderer glow, column;
    private BoxCollider2D box;
    private bool erupted;

    public static AcidColumn Spawn(Vector3 floorPos, float warn = 0.9f, float height = 5.5f, int damage = 22)
    {
        GameObject go = new GameObject("Geyser d'acide");
        go.transform.position = floorPos;
        AcidColumn c = go.AddComponent<AcidColumn>();
        c.warn = warn;
        c.height = height;
        c.damage = damage;

        c.box = go.AddComponent<BoxCollider2D>();
        c.box.isTrigger = true;
        c.box.size = new Vector2(c.width, height);
        c.box.offset = new Vector2(0f, height * 0.5f);
        c.box.enabled = false;

        Sprite g = FxUtil.LoadSprite("lueur_douce");
        if (g != null)
        {
            c.glow = FxUtil.MakeSprite("Alerte", g, floorPos + new Vector3(0f, 0.1f, 0f), 9990, new Color(0.6f, 1f, 0.25f, 0.2f), FxUtil.Additive, go.transform);
            c.glow.transform.localScale = new Vector3(c.width * 1.3f, 0.7f, 1f);
            c.column = FxUtil.MakeSprite("Colonne", g, floorPos + new Vector3(0f, height * 0.5f, 0f), 9991, new Color(0.55f, 1f, 0.2f, 0f), FxUtil.Additive, go.transform);
            c.column.transform.localScale = new Vector3(c.width * 1.5f, height * 0.75f, 1f);
        }
        return c;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age < warn)
        {
            // Avertissement : la lueur au sol grossit et clignote
            float k = age / warn;
            if (glow != null)
            {
                glow.color = new Color(0.6f, 1f, 0.25f, 0.25f + 0.5f * Mathf.Abs(Mathf.Sin(age * 18f)) * k);
                glow.transform.localScale = new Vector3(width * (1.1f + 0.5f * k), 0.5f + 0.5f * k, 1f);
            }
            if (Random.value < Time.deltaTime * 14f)
            {
                Sprite b = FxUtil.LoadSprite("bulle");
                if (b != null)
                    SimpleParticle.Spawn(b, transform.position + new Vector3(Random.Range(-0.6f, 0.6f), 0.1f, 0f), new Vector2(0f, Random.Range(1f, 2.5f)),
                        0.4f, new Color(0.8f, 1f, 0.5f, 0.9f), Random.Range(0.6f, 1f), 9992, null, 0f, 0.5f);
            }
            return;
        }
        if (!erupted)
        {
            erupted = true;
            if (box != null) box.enabled = true;
            AudioManager.Play("acide", 0.7f, Random.Range(0.8f, 1f));
            if (Camera.main != null)
            {
                CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
                if (cam != null) cam.Shake(0.05f, 0.2f);
            }
            Sprite e = FxUtil.LoadSprite("eclat");
            if (e != null)
                for (int i = 0; i < 14; i++)
                    SimpleParticle.Spawn(e, transform.position + new Vector3(Random.Range(-0.6f, 0.6f), 0.2f, 0f), new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(5f, 11f)),
                        Random.Range(0.4f, 0.8f), new Color(0.6f, 1f, 0.3f), Random.Range(0.8f, 1.4f), 10006, FxUtil.Additive, 12f, 0.5f);
        }
        float t = (age - warn) / active;
        float a = t < 0.2f ? t / 0.2f : 1f - Mathf.Clamp01((t - 0.2f) / 0.8f);
        if (column != null) column.color = new Color(0.55f, 1f, 0.2f, 0.75f * a);
        if (glow != null) glow.color = new Color(0.6f, 1f, 0.25f, 0.6f * a);
        if (t >= 1f) Destroy(gameObject);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!erupted || !other.CompareTag("Player")) return;
        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph == null || !ph.CanTakeDamage) return;
        ph.TakeDamage(damage);
        PlayerMovement pm = other.GetComponentInParent<PlayerMovement>();
        if (pm != null && ph.CurrentHealth > 0)
        {
            float side = Mathf.Sign(other.transform.position.x - transform.position.x);
            if (side == 0f) side = 1f;
            pm.ApplyKnockback(new Vector2(4f * side, 7f), 0.3f);
        }
    }
}
