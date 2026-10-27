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
    [Tooltip("Identifiant : permet de reprendre l'ascenseur là où il était quand on change de carte")]
    public string liftId = "";

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
        // Changement de carte pendant un trajet : l'ascenseur reprend exactement là où il était
        float[] st = SaveSystem.ConsumeLiftState(liftId);
        if (st != null && st.Length >= 5 && stops != null && stops.Length > 1)
        {
            index = Mathf.Clamp((int)st[2], 0, stops.Length - 1);
            dir = st[3] >= 0f ? 1 : -1;
            wait = st[4];
            body.position = new Vector2(st[0], st[1]);
            transform.position = new Vector3(st[0], st[1], transform.position.z);
        }
    }

    /// <summary>x, y, index, sens, attente : de quoi reprendre l'ascenseur sur l'autre carte.</summary>
    public float[] ExportState()
    {
        return new[] { body.position.x, body.position.y, (float)index, (float)dir, wait };
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
