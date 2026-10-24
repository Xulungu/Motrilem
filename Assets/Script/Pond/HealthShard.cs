using System.Collections;
using UnityEngine;

// Éclat de vie : objet caché à trouver en explorant. Le ramasser augmente la vie maximale de 20.
// Chaque éclat ne peut être ramassé qu'une fois (mémorisé dans la sauvegarde).
[RequireComponent(typeof(CircleCollider2D))]
public class HealthShard : MonoBehaviour
{
    public string shardId = "eclat";
    public int bonus = 20;

    private SpriteRenderer sr, glow;
    private Vector3 home;
    private bool taken;

    string Flag { get { return "eclat_vie_" + shardId; } }

    void Awake()
    {
        GetComponent<CircleCollider2D>().isTrigger = true;
        home = transform.position;
        SpriteRenderer[] rs = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer r in rs) { if (r.name == "Lueur") glow = r; else sr = r; }
    }

    void Start()
    {
        if (SaveSystem.HasFlag(Flag)) gameObject.SetActive(false);
    }

    void Update()
    {
        if (taken) return;
        transform.position = home + Vector3.up * Mathf.Sin(Time.time * 2.2f) * 0.12f;
        if (sr != null) sr.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 1.5f) * 8f);
        if (glow != null) glow.color = new Color(1f, 0.4f, 0.55f, 0.45f + 0.15f * Mathf.Sin(Time.time * 3f));
        if (Random.value < Time.deltaTime * 3f)
        {
            Sprite s = FxUtil.LoadSprite("eclat");
            if (s != null)
                SimpleParticle.Spawn(s, transform.position + (Vector3)Random.insideUnitCircle * 0.4f, new Vector2(0f, 0.6f), 0.8f,
                    new Color(1f, 0.6f, 0.75f), 0.7f, 10006, FxUtil.Additive, -0.2f, 1f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (taken || !other.CompareTag("Player")) return;
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;
        taken = true;
        StartCoroutine(Collect(health));
    }

    IEnumerator Collect(PlayerHealth health)
    {
        SaveSystem.SetFlag(Flag);
        SaveSystem.Session.maxHealthBonus += bonus;
        health.IncreaseMaxHealth(bonus);
        AudioManager.Play("choix", 0.8f, 1.1f);
        GameUI ui = GameUI.Instance;
        // Lancées sur l'interface : elles continuent même quand l'éclat disparaît
        ui.StartCoroutine(ui.Flash(new Color(1f, 0.6f, 0.7f, 0.5f), 0.5f));
        ui.PlayTitle("Éclat de vie", "Vie maximale : " + health.maxHealth);

        Sprite s = FxUtil.LoadSprite("eclat");
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.5f)
        {
            transform.localScale = Vector3.one * (1f + t);
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 1f - t);
            if (glow != null) glow.color = new Color(1f, 0.4f, 0.55f, 0.6f * (1f - t));
            if (s != null && Random.value < 0.5f)
                SimpleParticle.Spawn(s, transform.position, Random.insideUnitCircle.normalized * 3f, 0.5f,
                    new Color(1f, 0.6f, 0.75f), 1f, 10006, FxUtil.Additive, 0f, 2f);
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
