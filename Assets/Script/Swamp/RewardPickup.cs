using UnityEngine;

// Récompense à ramasser. Pour l'instant il n'y a ni coffres ni objets : c'est un cristal flottant « de préparation »
// qui mémorise la récompense dans la sauvegarde (drapeau « recompense_<id> ») et affiche un message.
// Plus tard, on branchera ici l'inventaire / la compétence réelle.
[RequireComponent(typeof(CircleCollider2D))]
public class RewardPickup : MonoBehaviour
{
    public string rewardId = "recompense";
    public string label = "Récompense";
    public Color color = new Color(1f, 0.85f, 0.35f);

    private Transform gem;
    private SpriteRenderer glow, core;
    private float seed;
    private bool taken;

    public static string Key(string id) { return "recompense_" + id; }

    public static RewardPickup Create(Vector3 position, string id, string label, Transform parent = null)
    {
        if (SaveSystem.HasFlag(Key(id))) return null;
        GameObject go = new GameObject("Récompense : " + label);
        if (parent != null) go.transform.SetParent(parent, true);
        go.transform.position = position;
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.9f;
        RewardPickup r = go.AddComponent<RewardPickup>();
        r.rewardId = id;
        r.label = label;
        return r;
    }

    void Awake()
    {
        GetComponent<CircleCollider2D>().isTrigger = true;
        seed = Random.value * 10f;
        Sprite white = FxUtil.WhiteSprite;
        core = FxUtil.MakeSprite("Cristal", white, transform.position, 9996, color, null, transform);
        core.transform.localScale = new Vector3(0.38f, 0.38f, 1f);
        core.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        gem = core.transform;
        glow = FxUtil.MakeSprite("Lueur", FxUtil.LoadSprite("lueur"), transform.position, 9995, new Color(color.r, color.g, color.b, 0.55f), FxUtil.Additive, transform);
        glow.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
    }

    void Start()
    {
        if (SaveSystem.HasFlag(Key(rewardId))) Destroy(gameObject);
    }

    void Update()
    {
        if (taken || gem == null) return;
        float t = Time.time + seed;
        Vector3 p = transform.position + new Vector3(0f, 0.15f * Mathf.Sin(t * 2f), 0f);
        gem.position = p;
        glow.transform.position = p;
        gem.rotation = Quaternion.Euler(0f, 0f, 45f + Mathf.Sin(t * 1.3f) * 12f);
        Color c = glow.color; c.a = 0.45f + 0.15f * Mathf.Sin(t * 3f); glow.color = c;
        if (Random.value < 0.04f)
            SimpleParticle.Spawn(FxUtil.WhiteSprite, p + (Vector3)Random.insideUnitCircle * 0.5f, new Vector2(0f, Random.Range(0.4f, 0.9f)),
                0.8f, new Color(color.r, color.g, color.b, 0.8f), 0.1f, 9997, FxUtil.Additive, 0f, 1f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (taken || !other.CompareTag("Player")) return;
        taken = true;
        SaveSystem.SetFlag(Key(rewardId));
        AudioManager.Play("fragment", 0.8f, 1.1f);
        if (GameUI.HasInstance) GameUI.Instance.Toast(label + " obtenu(e)");
        for (int i = 0; i < 12; i++)
            SimpleParticle.Spawn(FxUtil.WhiteSprite, transform.position, Random.insideUnitCircle * 3f, 0.7f,
                new Color(color.r, color.g, color.b, 0.9f), 0.12f, 9997, FxUtil.Additive, 0f, 2f);
        Destroy(gameObject);
    }
}
