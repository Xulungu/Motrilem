using UnityEngine;

// Fait vaciller une lueur (feu de camp, torche...)
[RequireComponent(typeof(SpriteRenderer))]
public class LightFlicker : MonoBehaviour
{
    public float baseAlpha = 0.45f;
    public float amount = 0.12f;
    public float speed = 6f;

    private SpriteRenderer sr;
    private Vector3 baseScale;
    private float seed;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        seed = Random.Range(0f, 100f);
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(Time.time * speed, seed) - 0.5f;
        Color c = sr.color;
        c.a = baseAlpha + n * 2f * amount;
        sr.color = c;
        transform.localScale = baseScale * (1f + n * 0.1f);
    }
}
