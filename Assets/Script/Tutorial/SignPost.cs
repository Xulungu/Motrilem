using UnityEngine;

// Panneau en bois : une petite bulle apparaît au-dessus quand le joueur passe devant.
// (C'est un élément du décor : rien ne s'affiche en bas de l'écran.)
[RequireComponent(typeof(BoxCollider2D))]
public class SignPost : MonoBehaviour
{
    [TextArea] public string message = "...";
    public Vector3 bubbleOffset = new Vector3(0f, 1.9f, 0f);

    private int inside;
    private float alpha;
    private GameObject bubble;
    private TextMesh text, shadow;
    private SpriteRenderer panel;
    private MeshRenderer textRenderer;

    void Reset() { GetComponent<BoxCollider2D>().isTrigger = true; }

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        Build();
    }

    void Build()
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        bubble = new GameObject("Bulle du panneau");
        bubble.transform.SetParent(transform, false);

        panel = new GameObject("Fond").AddComponent<SpriteRenderer>();
        panel.transform.SetParent(bubble.transform, false);
        panel.sprite = FxUtil.WhiteSprite;
        panel.sortingOrder = 10058;

        shadow = MakeText(bubble.transform, font, new Color(0f, 0f, 0f, 0.8f), 10059, new Vector3(0.02f, -0.02f, 0f));
        text = MakeText(bubble.transform, font, Color.white, 10060, Vector3.zero);
        textRenderer = text.GetComponent<MeshRenderer>();
        bubble.SetActive(false);
    }

    TextMesh MakeText(Transform parent, Font font, Color color, int order, Vector3 offset)
    {
        GameObject go = new GameObject("Texte");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = offset;
        TextMesh tm = go.AddComponent<TextMesh>();
        tm.font = font;
        tm.fontSize = 64;
        tm.characterSize = 0.04f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        tm.text = message;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material;
        mr.sortingOrder = order;
        return tm;
    }

    void Update()
    {
        // Pas de bulle pendant un dialogue ni en pause
        bool talking = GameUI.HasInstance && GameUI.Instance.IsTalking;
        bool show = inside > 0 && !talking && !PauseMenu.IsPaused;
        alpha = Mathf.MoveTowards(alpha, show ? 1f : 0f, Time.unscaledDeltaTime * 5f);

        if (bubble.activeSelf != (alpha > 0.001f)) bubble.SetActive(alpha > 0.001f);
        if (!bubble.activeSelf) return;

        text.text = message;
        shadow.text = message;
        bubble.transform.position = transform.position + bubbleOffset + Vector3.up * Mathf.Sin(Time.time * 2.5f) * 0.03f;
        bubble.transform.rotation = Quaternion.identity;
        Vector3 ls = transform.lossyScale;
        bubble.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, Mathf.Abs(ls.x)), 1f / Mathf.Max(0.0001f, Mathf.Abs(ls.y)), 1f);

        // Le fond s'adapte à la taille du texte
        Vector3 size = textRenderer.bounds.size;
        Vector3 s = bubble.transform.lossyScale;
        panel.transform.localScale = new Vector3((size.x + 0.5f) / Mathf.Max(0.0001f, s.x), (size.y + 0.35f) / Mathf.Max(0.0001f, s.y), 1f);
        panel.color = new Color(0.05f, 0.04f, 0.08f, 0.78f * alpha);
        text.color = new Color(1f, 0.96f, 0.85f, alpha);
        shadow.color = new Color(0f, 0f, 0f, 0.8f * alpha);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) inside++;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) inside = Mathf.Max(0, inside - 1);
    }

    void OnDisable() { inside = 0; }
}
