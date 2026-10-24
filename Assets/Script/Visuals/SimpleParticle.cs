using UnityEngine;

// Petite particule (poussière, étincelle, débris) : vitesse, gravité, rétrécissement/grossissement et fondu.
public class SimpleParticle : MonoBehaviour
{
    private Vector2 velocity;
    private float life, age;
    private Color color;
    private float startScale, endScaleMul;
    private float gravity, drag;
    private float spin;
    private SpriteRenderer sr;

    public static SimpleParticle Spawn(Sprite sprite, Vector3 position, Vector2 velocity, float life, Color color,
                                       float scale, int order, Material material = null, float gravity = 0f, float drag = 1f)
    {
        SpriteRenderer sr = FxUtil.MakeSprite("Particule", sprite, position, order, color, material);
        sr.transform.localScale = Vector3.one * scale;
        SimpleParticle p = sr.gameObject.AddComponent<SimpleParticle>();
        p.Init(velocity, life, color, 0.4f, gravity, drag);
        p.spin = Random.Range(-180f, 180f);
        return p;
    }

    public void Init(Vector2 velocity, float life, Color color, float endScaleMul, float gravity, float drag)
    {
        sr = GetComponent<SpriteRenderer>();
        this.velocity = velocity;
        this.life = Mathf.Max(0.01f, life);
        this.color = color;
        this.endScaleMul = endScaleMul;
        this.gravity = gravity;
        this.drag = drag;
        startScale = transform.localScale.x;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        float k = age / life;
        velocity *= Mathf.Exp(-drag * dt);
        velocity.y -= gravity * dt;
        transform.position += (Vector3)(velocity * dt);
        if (spin != 0f) transform.Rotate(0f, 0f, spin * dt);
        float s = startScale * Mathf.Lerp(1f, endScaleMul, k);
        transform.localScale = new Vector3(s * Mathf.Sign(transform.localScale.x == 0 ? 1 : transform.localScale.x), s, 1f);
        Color c = color;
        c.a = color.a * (1f - k);
        if (sr != null) sr.color = c;
        if (age >= life) Destroy(gameObject);
    }
}
