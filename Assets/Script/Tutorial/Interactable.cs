using UnityEngine;

// Base pour tout objet avec lequel le joueur peut interagir (touche E ou flèche Haut).
// Il faut un Collider2D en "Is Trigger" pour définir la zone d'interaction.
[RequireComponent(typeof(Collider2D))]
public abstract class Interactable : MonoBehaviour
{
    [Header("Interaction")]
    public string promptText = "Interagir";
    public Vector3 promptOffset = new Vector3(0f, 1.1f, 0f);
    public bool canInteract = true;

    protected PlayerMovement player;
    private int playerColliders;
    private GameObject prompt;
    private TextMesh promptMesh;

    protected virtual void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    protected virtual void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        // Un Rigidbody2D cinématique pour que l'objet puisse bouger sans souci
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D body = gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
        }
        BuildPrompt();
    }

    void BuildPrompt()
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        prompt = new GameObject("Bulle interaction");
        prompt.transform.SetParent(transform, false);

        promptMesh = MakeText(prompt.transform, font, new Color(1f, 1f, 1f, 1f), 10060, Vector3.zero);
        MakeText(prompt.transform, font, new Color(0f, 0f, 0f, 0.8f), 10059, new Vector3(0.025f, -0.025f, 0f));
        prompt.SetActive(false);
    }

    TextMesh MakeText(Transform parent, Font font, Color color, int order, Vector3 offset)
    {
        GameObject go = new GameObject("Texte");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = offset;
        TextMesh tm = go.AddComponent<TextMesh>();
        tm.font = font;
        tm.fontSize = 64;
        tm.characterSize = 0.045f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        tm.text = "[E] " + promptText;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material;
        mr.sortingOrder = order;
        return tm;
    }

    protected virtual void Update()
    {
        bool show = playerColliders > 0 && canInteract && player != null && player.controlsEnabled;
        if (prompt != null)
        {
            if (prompt.activeSelf != show) prompt.SetActive(show);
            if (show)
            {
                // Garde la bulle droite et à la bonne taille même si l'objet est retourné/redimensionné
                prompt.transform.position = transform.position + promptOffset + Vector3.up * Mathf.Sin(Time.time * 4f) * 0.04f;
                prompt.transform.rotation = Quaternion.identity;
                Vector3 ls = transform.lossyScale;
                prompt.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, Mathf.Abs(ls.x)), 1f / Mathf.Max(0.0001f, Mathf.Abs(ls.y)), 1f);
                foreach (TextMesh tm in prompt.GetComponentsInChildren<TextMesh>()) tm.text = "[E] " + promptText;
            }
        }

        if (show && GameInput.InteractDown())
        {
            Interact();
        }
    }

    protected abstract void Interact();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerMovement pm = other.GetComponentInParent<PlayerMovement>();
        if (pm == null) return;
        player = pm;
        playerColliders++;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerColliders = Mathf.Max(0, playerColliders - 1);
    }

    void OnDisable()
    {
        playerColliders = 0;
        if (prompt != null) prompt.SetActive(false);
    }
}
