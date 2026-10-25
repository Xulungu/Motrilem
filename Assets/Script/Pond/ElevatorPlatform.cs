using UnityEngine;

// Ascenseur : une plateforme qui monte et descend dans un puits en s'arrêtant à chaque niveau desservi.
// (Remplace les longues échelles de planches : on monte à bord, on attend, on arrive.)
[RequireComponent(typeof(BoxCollider2D))]
public class ElevatorPlatform : MonoBehaviour
{
    [Tooltip("Positions (centre de la plateforme) de chaque arrêt, du plus bas au plus haut")]
    public Vector3[] stops;
    public float speed = 4.5f;
    public float pause = 2.2f;

    private Rigidbody2D body;
    private BoxCollider2D box;
    private PlayerMovement player;
    private int index, dir = 1;
    private float wait;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        body = GetComponent<Rigidbody2D>();
        if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Start()
    {
        player = FindObjectOfType<PlayerMovement>();
        wait = pause;
        if (stops != null && stops.Length > 0) body.position = stops[0];
    }

    void FixedUpdate()
    {
        if (stops == null || stops.Length < 2) return;
        if (wait > 0f) { wait -= Time.fixedDeltaTime; return; }

        Vector2 target = stops[index + dir];
        // ralentit un peu à l'approche de l'arrêt
        float dist = Vector2.Distance(body.position, target);
        float v = Mathf.Min(speed, Mathf.Max(1.2f, dist * 3f));
        Vector2 next = Vector2.MoveTowards(body.position, target, v * Time.fixedDeltaTime);
        Vector2 delta = next - body.position;
        body.MovePosition(next);

        // Le joueur debout dessus est emporté avec la plateforme
        if (player != null && player.GroundCollider == box && player.rb != null)
            player.rb.position += delta;

        if (Vector2.Distance(next, target) < 0.01f)
        {
            index += dir;
            if (index == stops.Length - 1) dir = -1;
            else if (index == 0) dir = 1;
            wait = pause;
        }
    }

    void OnDrawGizmos()
    {
        if (stops == null) return;
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.8f);
        for (int i = 0; i < stops.Length; i++)
        {
            Gizmos.DrawWireSphere(stops[i], 0.25f);
            if (i > 0) Gizmos.DrawLine(stops[i - 1], stops[i]);
        }
    }
}
