using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    public float speed;
    public Transform[] waypoints;
    public SpriteRenderer graphics;
    [Tooltip("Distance à laquelle un waypoint est considéré comme atteint")]
    public float reachDistance = 0.3f;

    [Header("Santé")]
    public int health = 3;

    /// <summary>Mis à true par une attaque (le moustique s'arrête pour viser).</summary>
    [HideInInspector] public bool paused;

    private Transform target;
    private int destPoint = 0;
    private Rigidbody2D rb;
    private EnemyHealth enemyHealth;

    void Awake()
    {
        // Composants ajoutés automatiquement (animations, points de vie)
        if (GetComponent<EnemyVisuals>() == null) gameObject.AddComponent<EnemyVisuals>();
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            enemyHealth = gameObject.AddComponent<EnemyHealth>();
            enemyHealth.maxHealth = health;
            WeakSpot ws = GetComponentInChildren<WeakSpot>();
            if (ws != null && ws.objectToDestroy != null) enemyHealth.rootToDestroy = ws.objectToDestroy;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning(name + " : aucun waypoint assigné, l'ennemi reste immobile.", this);
            enabled = false;
            return;
        }

        destPoint = Random.Range(0, waypoints.Length);
        target = waypoints[destPoint];
    }

    void FixedUpdate()
    {
        if (target == null || paused) return;

        float mult = enemyHealth != null ? enemyHealth.SpeedMultiplier : 1f;
        if (mult <= 0f) return;

        Vector2 current = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 dir = (Vector2)target.position - current;

        // Mouvement (via le Rigidbody2D cinématique pour que les collisions/triggers soient fiables)
        Vector2 next = Vector2.MoveTowards(current, target.position, speed * mult * Time.fixedDeltaTime);
        if (rb != null) rb.MovePosition(next);
        else transform.position = new Vector3(next.x, next.y, transform.position.z);

        // Flip selon la direction X
        if (graphics != null)
        {
            if (dir.x > 0.01f) graphics.flipX = true;
            else if (dir.x < -0.01f) graphics.flipX = false;
        }

        // Si proche du waypoint → choisir un nouveau (différent du précédent)
        if (dir.magnitude < reachDistance && waypoints.Length > 1)
        {
            destPoint = (destPoint + Random.Range(1, waypoints.Length)) % waypoints.Length;
            target = waypoints[destPoint];
        }
    }
}
