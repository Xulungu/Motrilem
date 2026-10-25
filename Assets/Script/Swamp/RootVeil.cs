using UnityEngine;
using UnityEngine.Tilemaps;

// Voile de racines : cache un passage. Il devient presque transparent quand le joueur est tout près
// (et reste un peu transparent une fois le passage découvert).
[RequireComponent(typeof(BoxCollider2D))]
public class RootVeil : MonoBehaviour
{
    public string veilId = "voile";
    public Tilemap tilemap;

    private int inside;
    private float alpha = 1f;
    private bool found;

    string Key { get { return "voile_" + veilId; } }

    void Awake() { GetComponent<BoxCollider2D>().isTrigger = true; }

    void Start()
    {
        found = SaveSystem.HasFlag(Key);
        if (found) alpha = 0.35f;
        Apply();
    }

    void Update()
    {
        float target = inside > 0 ? 0.1f : (found ? 0.35f : 1f);
        alpha = Mathf.MoveTowards(alpha, target, Time.deltaTime * 1.6f);
        Apply();
    }

    void Apply()
    {
        if (tilemap != null) tilemap.color = new Color(1f, 1f, 1f, alpha);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inside++;
        if (!found)
        {
            found = true;
            SaveSystem.SetFlag(Key);
            AudioManager.Play("sceau", 0.35f, 1.4f);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inside = Mathf.Max(0, inside - 1);
    }
}
