using UnityEngine;

// Plateforme de pierre qui fait des allers-retours entre deux points et transporte le joueur.
[RequireComponent(typeof(BoxCollider2D))]
public class MovingPlatform : MonoBehaviour
{
    public Vector3 pointA;
    public Vector3 pointB;
    [Tooltip("Durée d'un trajet A -> B (secondes)")]
    public float travelTime = 3f;
    [Tooltip("Pause à chaque extrémité")]
    public float pause = 0.6f;

    private Rigidbody2D body;
    private BoxCollider2D box;
    private float t;
    private PlayerMovement player;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        body = GetComponent<Rigidbody2D>();
        if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (pointA == Vector3.zero && pointB == Vector3.zero) { pointA = transform.position; pointB = transform.position + Vector3.right * 5f; }
    }

    void Start() { player = FindObjectOfType<PlayerMovement>(); }

    void FixedUpdate()
    {
        float cycle = 2f * (travelTime + pause);
        t = (t + Time.fixedDeltaTime) % cycle;
        float k;
        if (t < pause) k = 0f;
        else if (t < pause + travelTime) k = Ease.InOut((t - pause) / travelTime);
        else if (t < 2f * pause + travelTime) k = 1f;
        else k = 1f - Ease.InOut((t - 2f * pause - travelTime) / travelTime);

        Vector2 next = Vector3.Lerp(pointA, pointB, k);
        Vector2 delta = next - body.position;
        body.MovePosition(next);

        // Le joueur debout dessus est emporté avec la plateforme
        if (player != null && player.GroundCollider == box && player.rb != null)
            player.rb.position += delta;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.8f);
        Vector3 a = Application.isPlaying ? pointA : (pointA == Vector3.zero ? transform.position : pointA);
        Vector3 b = Application.isPlaying ? pointB : (pointB == Vector3.zero ? transform.position + Vector3.right * 5f : pointB);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawWireSphere(a, 0.2f);
        Gizmos.DrawWireSphere(b, 0.2f);
    }
}
