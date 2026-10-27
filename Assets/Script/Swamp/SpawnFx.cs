using System.Collections;
using UnityEngine;

// Apparition animée des créatures : on voit la créature SE FORMER à l'endroit où elle va surgir, avant qu'elle existe vraiment.
// Le joueur a le temps de s'écarter (plus de monstre qui apparaît sur lui).
//   - slime : une flaque de glu s'étale, des bulles montent, puis un corps se dresse hors de la flaque ;
//   - moustique : des lueurs convergent en tourbillon, un corps fantôme s'y dessine.
// La créature elle-même n'est créée qu'à la fin (SpawnFx.CreateSlime / CreateMinion) et reste inoffensive une fraction de seconde.
public static class SpawnFx
{
    public const float SlimeTime = 1.0f;
    public const float BigSlimeTime = 1.3f;
    public const float MinionTime = 0.9f;

    /// <summary>Hauteur du sol sous un point (premier solide rencontré), ou 'fallback' si rien.</summary>
    public static float GroundBelow(Vector3 from, float fallback)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(from, Vector2.down, 40f);
        foreach (RaycastHit2D h in hits)
        {
            if (h.collider == null || h.collider.isTrigger || h.collider.CompareTag("Player")) continue;
            if (h.collider.attachedRigidbody != null && h.collider.attachedRigidbody.bodyType != RigidbodyType2D.Static) continue;
            return h.point.y;
        }
        return fallback;
    }

    /// <summary>Lance l'animation de formation. La créature ne doit être créée qu'après 'duration' secondes.</summary>
    public static float Telegraph(Vector3 pos, bool flying, float scale, bool big = false)
    {
        float duration = flying ? MinionTime : (big ? BigSlimeTime : SlimeTime);
        float groundY = pos.y - 0.46f * scale;
        if (!flying)
        {
            groundY = GroundBelow(pos + Vector3.up * 0.6f, pos.y - 0.46f * scale);
        }
        GameObject go = new GameObject(flying ? "Formation (moustique)" : "Formation (slime)");
        go.transform.position = pos;
        SpawnFxAnim a = go.AddComponent<SpawnFxAnim>();
        a.flying = flying;
        a.scale = scale;
        a.groundY = groundY;
        a.duration = duration;
        return duration;
    }

    /// <summary>Attend (au plus 'maxWait' s) que le joueur ne soit plus à l'endroit où la créature va surgir.</summary>
    public static IEnumerator WaitClear(Vector3 pos, float radius, float maxWait)
    {
        PlayerMovement pm = Object.FindObjectOfType<PlayerMovement>();
        if (pm == null) yield break;
        float t = 0f;
        while (t < maxWait && Vector2.Distance(pm.transform.position, pos) < radius)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    public static Slime CreateSlime(Vector3 pos, bool big, Transform parent)
    {
        Slime s = Slime.Create(pos, big, parent);
        Grace(s.gameObject, 0.45f);
        return s;
    }

    public static FlyingMinion CreateMinion(Vector3 pos, bool shooter, Transform parent)
    {
        FlyingMinion m = FlyingMinion.Create(pos, shooter, parent);
        Grace(m.gameObject, 0.45f);
        return m;
    }

    /// <summary>La créature ne fait pas de dégâts pendant un court instant après son apparition.</summary>
    public static void Grace(GameObject mob, float time)
    {
        SpawnGrace g = mob.AddComponent<SpawnGrace>();
        g.time = time;
    }
}

/// <summary>Animation de formation (flaque de glu + corps qui se dresse, ou tourbillon de lueurs).</summary>
public class SpawnFxAnim : MonoBehaviour
{
    public bool flying;
    public float scale = 0.6f;
    public float groundY;
    public float duration = 1f;

    private float t;
    private SpriteRenderer puddle, puddleCore, body, glow;
    private Transform[] orbs;
    private float[] orbAngle, orbRadius;
    private bool sound2;

    void Start()
    {
        Sprite soft = FxUtil.LoadSprite("lueur_douce");
        Vector3 p = transform.position;
        if (!flying)
        {
            puddle = FxUtil.MakeSprite("Flaque", soft, new Vector3(p.x, groundY + 0.05f, 0f), 9996, new Color(0.35f, 0.85f, 0.2f, 0f), null, transform);
            puddleCore = FxUtil.MakeSprite("Coeur de flaque", soft, new Vector3(p.x, groundY + 0.07f, 0f), 9996, new Color(0.65f, 1f, 0.35f, 0f), FxUtil.Additive, transform);
            body = FxUtil.MakeSprite("Corps", FxUtil.LoadSprite("slime_0"), new Vector3(p.x, groundY, 0f), 9997, new Color(0.8f, 1f, 0.7f, 0f), null, transform);
            body.transform.localScale = new Vector3(scale, 0.01f, 1f);
            AudioManager.Play("bulle", 0.55f, Random.Range(0.55f, 0.7f));
        }
        else
        {
            Sprite corps = FxUtil.LoadSprite("boss_corps");
            if (corps == null) corps = FxUtil.LoadSprite("boss_moustique");
            body = FxUtil.MakeSprite("Corps fantôme", corps, p, 9996, new Color(0.7f, 1f, 0.45f, 0f), null, transform);
            body.transform.localScale = Vector3.one * (scale * 0.4f);
            glow = FxUtil.MakeSprite("Lueur", soft, p, 9995, new Color(0.6f, 1f, 0.3f, 0f), FxUtil.Additive, transform);
            int n = 9;
            orbs = new Transform[n];
            orbAngle = new float[n];
            orbRadius = new float[n];
            for (int i = 0; i < n; i++)
            {
                SpriteRenderer o = FxUtil.MakeSprite("Lueur " + i, soft, p, 9998, new Color(0.65f, 1f, 0.35f, 0.9f), FxUtil.Additive, transform);
                o.transform.localScale = Vector3.one * 0.3f;
                orbs[i] = o.transform;
                orbAngle[i] = i * 6.283f / n + Random.Range(-0.3f, 0.3f);
                orbRadius[i] = Random.Range(1.9f, 2.7f);
            }
            AudioManager.Play("bzz", 0.35f, Random.Range(1.3f, 1.6f));
        }
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        if (!flying) UpdateSlime(k); else UpdateMinion(k);
        if (t >= duration + 0.05f) { Burst(); Destroy(gameObject); }
    }

    void UpdateSlime(float k)
    {
        Vector3 p = transform.position;
        // phase 1 (0 -> 0,5) : la flaque s'étale, des bulles montent ; phase 2 (0,5 -> 1) : le corps se dresse
        float spread = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k / 0.5f));
        float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - 0.45f) / 0.55f));
        float w = (1.0f + 2.6f * scale) * spread;
        if (puddle != null)
        {
            puddle.transform.localScale = new Vector3(w * 1.5f, 0.55f * Mathf.Max(0.2f, spread), 1f);
            puddle.color = new Color(0.35f, 0.85f, 0.2f, 0.9f * Mathf.Min(1f, spread * 2f) * (1f - Mathf.Clamp01((k - 0.9f) / 0.1f)));
        }
        if (puddleCore != null)
        {
            puddleCore.transform.localScale = new Vector3(w * 0.9f, 0.35f * Mathf.Max(0.2f, spread), 1f);
            puddleCore.color = new Color(0.65f, 1f, 0.35f, (0.35f + 0.15f * Mathf.Sin(t * 14f)) * spread);
        }
        if (body != null)
        {
            float h = Mathf.Lerp(0.05f, 1f, rise);
            float wobble = 1f + Mathf.Sin(t * 22f) * 0.08f * (1f - rise);
            float baseH = 0.5625f * scale * 2f;   // hauteur du sprite à l'échelle 'scale'
            body.transform.localScale = new Vector3(scale * Mathf.Lerp(1.5f, 1f, rise) * wobble, scale * h, 1f);
            // les pieds restent dans la flaque, le corps s'élève : au final le centre est à la hauteur d'un vrai slime au repos
            body.transform.position = new Vector3(p.x, groundY - 0.1f * scale + baseH * 0.5f * h, 0f);
            body.color = new Color(0.8f, 1f, 0.7f, 0.85f * Mathf.Clamp01(rise * 3f));
        }
        if (k > 0.45f && !sound2) { sound2 = true; AudioManager.Play("slime_saut", 0.35f, 0.8f); }
        // bulles qui éclatent
        if (Random.value < 6f * Time.deltaTime * (1f + spread))
        {
            Sprite b = FxUtil.LoadSprite("bulle");
            if (b != null)
                SimpleParticle.Spawn(b, new Vector3(p.x + Random.Range(-w * 0.5f, w * 0.5f), groundY + 0.2f, 0f), new Vector2(0f, Random.Range(0.8f, 1.6f)),
                    Random.Range(0.35f, 0.6f), new Color(0.8f, 1f, 0.6f, 0.9f), Random.Range(0.8f, 1.2f), 9998, null, -0.2f, 0.5f);
        }
    }

    void UpdateMinion(float k)
    {
        float ease = k * k;
        for (int i = 0; i < orbs.Length; i++)
        {
            float a = orbAngle[i] + k * 7f;
            float r = orbRadius[i] * (1f - ease);
            orbs[i].localPosition = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.8f, 0f);
            orbs[i].localScale = Vector3.one * Mathf.Lerp(0.3f, 0.12f, k);
        }
        if (body != null)
        {
            float g = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - 0.25f) / 0.75f));
            body.transform.localScale = Vector3.one * (scale * Mathf.Lerp(0.4f, 1f, g));
            body.color = new Color(0.7f, 1f, 0.45f, 0.75f * g);
        }
        if (glow != null)
        {
            glow.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 2.2f, k);
            glow.color = new Color(0.6f, 1f, 0.3f, 0.45f * (0.5f + 0.5f * Mathf.Sin(t * 18f)) * k);
        }
    }

    void Burst()
    {
        Sprite s = FxUtil.LoadSprite("eclat");
        if (s == null) return;
        Vector3 p = flying ? transform.position : new Vector3(transform.position.x, groundY + 0.4f, 0f);
        for (int i = 0; i < 8; i++)
            SimpleParticle.Spawn(s, p, new Vector2(Random.Range(-2f, 2f), Random.Range(0.5f, 3f)), Random.Range(0.3f, 0.5f),
                new Color(0.6f, 1f, 0.3f), Random.Range(0.8f, 1.2f), 10006, FxUtil.Additive, 8f, 0.5f);
    }
}

/// <summary>Rend une créature inoffensive juste après son apparition.</summary>
public class SpawnGrace : MonoBehaviour
{
    public float time = 0.4f;

    IEnumerator Start()
    {
        EnemyDamage[] ds = GetComponentsInChildren<EnemyDamage>();
        foreach (EnemyDamage d in ds) d.enabled = false;
        yield return new WaitForSeconds(time);
        FlyingMinion fm = GetComponent<FlyingMinion>();
        bool pulled = fm != null && fm.IsPulled;
        EnemyHealth h = GetComponent<EnemyHealth>();
        if (!pulled && (h == null || !h.IsDead))
            foreach (EnemyDamage d in ds) if (d != null) d.enabled = true;
        Destroy(this);
    }
}
