using UnityEngine;

// Page arrachée qui s'envole quand le livre se disperse.
public class FlyingPage : MonoBehaviour
{
    private Vector2 velocity;
    private float angularVelocity;
    private float life;
    private float age;
    private SpriteRenderer sr;
    private float flutter;

    public static FlyingPage Spawn(Sprite sprite, Vector3 position, Vector2 velocity, float life, int order)
    {
        SpriteRenderer sr = FxUtil.MakeSprite("Page envolée", sprite, position, order, Color.white);
        FlyingPage p = sr.gameObject.AddComponent<FlyingPage>();
        p.sr = sr;
        p.velocity = velocity;
        p.angularVelocity = Random.Range(-500f, 500f);
        p.life = life;
        p.flutter = Random.Range(0f, 10f);
        sr.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
        return p;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        velocity *= Mathf.Exp(-1.2f * dt);
        velocity += new Vector2(Mathf.Sin((age + flutter) * 6f) * 2f, -1.5f) * dt; // la page "vole" en zigzag
        angularVelocity *= Mathf.Exp(-0.8f * dt);
        transform.position += (Vector3)(velocity * dt);
        transform.Rotate(0, 0, angularVelocity * dt);

        // Effet de retournement (la page tourne sur elle-même)
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.y) * Mathf.Cos((age + flutter) * 7f);
        transform.localScale = s;

        float a = 1f - Mathf.Clamp01((age - life * 0.6f) / (life * 0.4f));
        sr.color = new Color(1f, 1f, 1f, a);
        if (age >= life) Destroy(gameObject);
    }
}
