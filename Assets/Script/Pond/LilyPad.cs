using UnityEngine;

// Nénuphar flottant sur l'acide (plateforme traversable par-dessous).
// Les nénuphars bruns ("sinks") s'enfoncent quand on reste dessus, puis remontent un peu plus tard.
[RequireComponent(typeof(BoxCollider2D))]
public class LilyPad : MonoBehaviour
{
    public bool sinks = false;
    public float timeBeforeSinking = 0.6f;
    public float sinkDuration = 0.9f;
    public float respawnDelay = 2.5f;

    enum State { Flotte, Tremble, Coule, Absent, Remonte }

    private State state = State.Flotte;
    private float timer;
    private BoxCollider2D box;
    private Rigidbody2D body;
    private SpriteRenderer sr;
    private Vector3 home;
    private float phase;
    private PlayerMovement player;

    void Start() { player = FindObjectOfType<PlayerMovement>(); }

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        box.usedByEffector = true;
        PlatformEffector2D eff = GetComponent<PlatformEffector2D>();
        if (eff == null) eff = gameObject.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.surfaceArc = 160f;
        body = GetComponent<Rigidbody2D>();
        if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        sr = GetComponentInChildren<SpriteRenderer>();
        home = transform.position;
        phase = Random.Range(0f, 10f);
    }

    bool PlayerOnMe()
    {
        return player != null && player.GroundCollider == box;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        timer += dt;
        Vector3 target = home;
        switch (state)
        {
            case State.Flotte:
                target = home + Vector3.up * Mathf.Sin(Time.time * 1.6f + phase) * 0.04f;
                if (sinks && PlayerOnMe()) { state = State.Tremble; timer = 0f; }
                break;
            case State.Tremble:
                target = home + new Vector3(Mathf.Sin(timer * 60f) * 0.03f, -0.05f, 0f);
                if (timer >= timeBeforeSinking) { state = State.Coule; timer = 0f; AudioManager.Play("bulle", 0.6f, 0.7f); }
                break;
            case State.Coule:
                target = home + Vector3.down * Mathf.Lerp(0.05f, 1f, timer / sinkDuration);
                if (sr != null) sr.color = new Color(1f, 1f, 1f, 1f - timer / sinkDuration);
                if (timer >= sinkDuration) { state = State.Absent; timer = 0f; box.enabled = false; }
                break;
            case State.Absent:
                target = home + Vector3.down;
                if (timer >= respawnDelay) { state = State.Remonte; timer = 0f; }
                break;
            case State.Remonte:
                target = home + Vector3.down * (1f - timer / 0.8f);
                if (sr != null) sr.color = new Color(1f, 1f, 1f, timer / 0.8f);
                if (timer >= 0.8f) { state = State.Flotte; timer = 0f; box.enabled = true; if (sr != null) sr.color = Color.white; }
                break;
        }
        body.MovePosition(target);
    }
}
