using UnityEngine;

// Fragment de souvenir : jaillit d'un point, tourbillonne un instant,
// puis est aspiré vers une cible (le livre, ou le joueur) en laissant une traînée.
public class MemoryFragment : MonoBehaviour
{
    private Transform target;
    private Vector3 targetOffset;
    private Vector2 velocity;
    private float delay;
    private float age;
    private float homingAge;
    private float spin;
    private float twinkle;
    private float swirl;
    private System.Action onArrive;
    private SpriteRenderer sr;
    private Color baseColor;
    private bool done;

    public static MemoryFragment Spawn(Sprite sprite, Material material, Vector3 position, Vector2 burstVelocity,
                                       Transform target, Vector3 targetOffset, float delay, Color color,
                                       float scale, System.Action onArrive)
    {
        SpriteRenderer sr = FxUtil.MakeSprite("Fragment de souvenir", sprite, position, 10010, color, material);
        sr.transform.localScale = Vector3.one * scale;
        sr.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        // Traînée lumineuse
        TrailRenderer trail = sr.gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.35f;
        trail.minVertexDistance = 0.05f;
        trail.widthMultiplier = 0.16f * scale;
        trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
        trail.sharedMaterial = material != null ? material : FxUtil.SpriteMaterial;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
                  new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = g;
        trail.sortingOrder = 10009;

        MemoryFragment f = sr.gameObject.AddComponent<MemoryFragment>();
        f.sr = sr;
        f.baseColor = color;
        f.target = target;
        f.targetOffset = targetOffset;
        f.velocity = burstVelocity;
        f.delay = delay;
        f.onArrive = onArrive;
        f.spin = Random.Range(-540f, 540f);
        f.twinkle = Random.Range(0f, 10f);
        f.swirl = Random.value < 0.5f ? -1f : 1f;
        return f;
    }

    void Update()
    {
        if (done) return;
        float dt = Time.deltaTime;
        age += dt;
        Vector2 pos = transform.position;

        if (age < delay || target == null)
        {
            // Phase 1 : éjection puis ralentissement, avec un léger tourbillon
            velocity *= Mathf.Exp(-2.2f * dt);
            Vector2 perp = new Vector2(-velocity.y, velocity.x) * 0.9f * swirl;
            velocity += perp * dt;
            velocity += Vector2.up * 0.4f * dt;
        }
        else
        {
            // Phase 2 : aspiration vers la cible, de plus en plus rapide
            homingAge += dt;
            Vector2 to = (Vector2)(target.position + targetOffset) - pos;
            float dist = to.magnitude;
            float speed = Mathf.Lerp(2f, 16f, Mathf.Clamp01(homingAge / 1.2f));
            Vector2 desired = to.normalized * speed + new Vector2(-to.y, to.x).normalized * swirl * Mathf.Min(3f, dist) * 0.6f;
            velocity = Vector2.MoveTowards(velocity, desired, (12f + homingAge * 30f) * dt);

            if (dist < 0.25f || homingAge > 3f)
            {
                Arrive();
                return;
            }
        }

        transform.position = new Vector3(pos.x + velocity.x * dt, pos.y + velocity.y * dt, transform.position.z);
        transform.Rotate(0f, 0f, spin * dt);

        Color c = Color.Lerp(baseColor, Color.white, 0.5f + 0.5f * Mathf.Sin((age + twinkle) * 14f));
        c.a = Mathf.Clamp01(age * 6f);
        if (target == null)
        {
            // Sans cible : simple étincelle qui s'éteint au bout de "delay" secondes
            c.a *= 1f - Mathf.Clamp01((age - delay * 0.6f) / (delay * 0.4f));
            if (age >= delay) { done = true; Destroy(gameObject); }
        }
        sr.color = c;
    }

    void Arrive()
    {
        done = true;
        if (onArrive != null) onArrive();
        // On laisse la traînée s'estomper avant de détruire
        sr.enabled = false;
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null) trail.emitting = false;
        Destroy(gameObject, 0.4f);
    }
}
