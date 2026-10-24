using System.Collections;
using UnityEngine;

// Sceau magique qui bloque le passage tant que le joueur n'a pas choisi sa magie.
public class MagicBarrier : MonoBehaviour
{
    public SpriteRenderer barrierRenderer;
    public Color color = new Color(0.65f, 0.5f, 1f, 0.85f);
    public Sprite sparkSprite;
    public Material additiveMaterial;

    private bool opening;

    void Awake()
    {
        if (barrierRenderer == null) barrierRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        // Partie reprise après le choix de la magie : le sceau est déjà brisé
        if (SaveSystem.HasFlag("magie_choisie")) gameObject.SetActive(false);
    }

    void Update()
    {
        if (opening || barrierRenderer == null) return;
        Color c = color;
        c.a = color.a * (0.75f + 0.25f * Mathf.Sin(Time.time * 4f));
        barrierRenderer.color = c;
    }

    public IEnumerator Open(Color magicColor)
    {
        opening = true;
        AudioManager.Play("sceau", 0.8f);
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>()) col.enabled = false;

        Vector3 baseScale = barrierRenderer != null ? barrierRenderer.transform.localScale : Vector3.one;
        // Le sceau se colore, se fissure, puis s'effondre en étincelles
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.6f)
        {
            if (barrierRenderer != null)
            {
                barrierRenderer.color = Color.Lerp(color, new Color(magicColor.r, magicColor.g, magicColor.b, 1f), t);
                barrierRenderer.transform.localPosition = Random.insideUnitCircle * 0.05f * t;
            }
            yield return null;
        }

        if (sparkSprite != null && barrierRenderer != null)
        {
            Bounds b = barrierRenderer.bounds;
            for (int i = 0; i < 30; i++)
            {
                Vector3 p = new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), 0f);
                Vector2 v = new Vector2(Random.Range(-3f, 3f), Random.Range(-1f, 3f));
                // sans cible : l'étincelle s'envole puis s'éteint au bout de "delay" secondes
                MemoryFragment.Spawn(sparkSprite, additiveMaterial, p, v, null, Vector3.zero, Random.Range(0.6f, 1.2f), magicColor, Random.Range(0.8f, 1.5f), null);
            }
        }

        for (float t = 0; t < 1f; t += Time.deltaTime / 0.5f)
        {
            if (barrierRenderer != null)
            {
                barrierRenderer.transform.localScale = new Vector3(baseScale.x * (1f - t), baseScale.y, baseScale.z);
                Color c = barrierRenderer.color; c.a = 1f - t; barrierRenderer.color = c;
            }
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
