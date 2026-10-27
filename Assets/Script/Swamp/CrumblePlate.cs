using System.Collections;
using UnityEngine;

// Plaque de sol de l'arène du gardien : elle se fissure puis s'effondre sur un bassin d'acide.
// Le combat qui s'éternise laisse de moins en moins de sol : il faut utiliser les planches.
[RequireComponent(typeof(BoxCollider2D))]
public class CrumblePlate : MonoBehaviour
{
    public SpriteRenderer render;
    public bool IsBroken { get; private set; }
    public bool IsBreaking { get; private set; }
    public float CenterX { get { return transform.position.x; } }

    private BoxCollider2D col;
    private Vector3 home;
    private SpriteRenderer[] parts;     // la plaque est faite de plusieurs tuiles (même aspect que le reste du sol)
    private Color[] baseColors;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        home = transform.position;
        parts = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[parts.Length];
        for (int i = 0; i < parts.Length; i++) baseColors[i] = parts[i].color;
    }

    void Tint(Color flash, float amount)
    {
        for (int i = 0; i < parts.Length; i++) parts[i].color = Color.Lerp(baseColors[i], flash, amount);
    }

    public void Break()
    {
        if (IsBroken || IsBreaking) return;
        StartCoroutine(Crumble());
    }

    IEnumerator Crumble()
    {
        IsBreaking = true;
        Sprite dust = FxUtil.LoadSprite("poussiere");
        Sprite chip = FxUtil.LoadSprite("eclat");
        float halfW = col.size.x * 0.5f;
        AudioManager.Play("impact", 0.5f, 0.6f);
        // Tremblement et fissures : le joueur a le temps de s'écarter
        for (float t = 0f; t < 1.4f; t += Time.deltaTime)
        {
            transform.position = home + new Vector3(Mathf.Sin(t * 70f) * 0.06f, 0f, 0f);
            Tint(new Color(1f, 0.6f, 0.4f, 1f), Mathf.PingPong(t * 5f, 1f) * 0.5f);
            if (dust != null && Random.value < 0.5f)
                SimpleParticle.Spawn(dust, new Vector3(home.x + Random.Range(-halfW, halfW), home.y + col.size.y * 0.5f, 0f), new Vector2(0f, Random.Range(0.5f, 1.5f)),
                    0.6f, new Color(0.7f, 0.7f, 0.65f, 0.7f), Random.Range(0.8f, 1.4f), 9999, null, 0f, 1f);
            yield return null;
        }
        // Effondrement
        IsBroken = true;
        IsBreaking = false;
        col.enabled = false;
        AudioManager.Play("explosion_livre", 0.45f, 0.9f);
        if (Camera.main != null)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.Shake(0.07f, 0.4f);
        }
        if (chip != null)
            for (int i = 0; i < 18; i++)
                SimpleParticle.Spawn(chip, new Vector3(home.x + Random.Range(-halfW, halfW), home.y + col.size.y * 0.5f, 0f), new Vector2(Random.Range(-3f, 3f), Random.Range(1f, 5f)),
                    Random.Range(0.5f, 0.9f), new Color(0.55f, 0.58f, 0.55f), Random.Range(1f, 2f), 10006, null, 12f, 0.5f);
        float vy = 0f;
        for (float t = 0f; t < 0.7f; t += Time.deltaTime)
        {
            vy -= 25f * Time.deltaTime;
            transform.position += new Vector3(0f, vy * Time.deltaTime, 0f);
            for (int i = 0; i < parts.Length; i++) parts[i].color = new Color(baseColors[i].r, baseColors[i].g, baseColors[i].b, 1f - t / 0.7f);
            yield return null;
        }
        for (int i = 0; i < parts.Length; i++) parts[i].enabled = false;
        transform.position = home;
    }

    /// <summary>Remet la plaque en place (fin ou reprise du combat).</summary>
    public void Restore()
    {
        StopAllCoroutines();
        IsBroken = false;
        IsBreaking = false;
        transform.position = home;
        if (col != null) col.enabled = true;
        for (int i = 0; i < parts.Length; i++) { parts[i].enabled = true; parts[i].color = baseColors[i]; }
    }
}
