using UnityEngine;

// Animation "procédurale" du personnage, par-dessus les animations de sprites :
//  - écrasement/étirement au saut et à l'atterrissage, respiration à l'arrêt
//  - inclinaison dans la course, cadence d'animation selon la vitesse
//  - poussière, bruits de pas, images rémanentes quand l'élan est au maximum
// Le sprite est déplacé dans un objet enfant "Visuel" pour pouvoir le déformer sans toucher aux hitbox.
[DefaultExecutionOrder(-100)]
public class PlayerVisuals : MonoBehaviour
{
    public Transform visual;

    private PlayerMovement pm;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private float halfHeight = 0.42f;

    private Vector2 squash = Vector2.one;   // déformation actuelle
    private Vector2 squashVel;
    private float lean;
    private float stepTimer;
    private float ghostTimer;
    private float castPunch;

    void Awake()
    {
        pm = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
        Detach();
    }

    void OnEnable()
    {
        if (pm != null) { pm.OnJumped += Jumped; pm.OnLanded += Landed; }
    }

    void OnDisable()
    {
        if (pm != null) { pm.OnJumped -= Jumped; pm.OnLanded -= Landed; }
    }

    void Detach()
    {
        SpriteRenderer rootSr = GetComponent<SpriteRenderer>();
        Animator rootAnim = GetComponent<Animator>();
        if (rootSr == null)
        {
            // déjà détaché (ou monté à la main)
            sr = GetComponentInChildren<SpriteRenderer>();
            anim = GetComponentInChildren<Animator>();
            if (sr != null) visual = sr.transform;
            return;
        }

        GameObject v = new GameObject("Visuel");
        v.transform.SetParent(transform, false);
        visual = v.transform;
        sr = v.AddComponent<SpriteRenderer>();
        sr.sprite = rootSr.sprite;
        sr.color = rootSr.color;
        sr.flipX = rootSr.flipX;
        sr.flipY = rootSr.flipY;
        sr.sortingLayerID = rootSr.sortingLayerID;
        sr.sortingOrder = rootSr.sortingOrder;
        sr.sharedMaterial = rootSr.sharedMaterial;
        if (rootSr.sprite != null) halfHeight = rootSr.sprite.bounds.extents.y;

        if (rootAnim != null)
        {
            anim = v.AddComponent<Animator>();
            anim.runtimeAnimatorController = rootAnim.runtimeAnimatorController;
            Destroy(rootAnim);
        }
        Destroy(rootSr);

        // Les autres scripts utilisent désormais le sprite enfant
        if (pm != null)
        {
            pm.spriteRenderer = sr;
            if (anim != null) pm.animator = anim;
        }
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null) health.graphics = sr;
    }

    // ------------------------------------------------------------------ événements
    void Jumped()
    {
        squash = new Vector2(0.78f, 1.25f);
        Dust(4, 1.2f);
    }

    void Landed(float impactVy)
    {
        float k = Mathf.Clamp01(-impactVy / 12f);
        if (k < 0.15f) return;
        squash = new Vector2(1f + 0.35f * k, 1f - 0.3f * k);
        Dust(Mathf.RoundToInt(3 + 5 * k), 1.5f + 2f * k);
        AudioManager.Play("atterrissage", 0.25f + 0.35f * k, Random.Range(0.9f, 1.1f));
    }

    /// <summary>Petit à-coup quand un sort est lancé.</summary>
    public void CastPunch()
    {
        squash = new Vector2(1.15f, 0.9f);
        castPunch = 1f;
    }

    // ------------------------------------------------------------------ chaque image
    void LateUpdate()
    {
        if (visual == null || sr == null) return;
        float dt = Time.deltaTime;
        float vx = rb.velocity.x, vy = rb.velocity.y;
        bool grounded = pm.isGrounded;
        bool frozen = pm.externalControl;

        // Cible de déformation selon l'état
        Vector2 target = Vector2.one;
        if (!grounded && !frozen)
        {
            float s = Mathf.Clamp(Mathf.Abs(vy) * 0.02f, 0f, 0.12f);
            target = new Vector2(1f - s, 1f + s);
        }
        else if (Mathf.Abs(vx) < 0.2f)
        {
            float b = Mathf.Sin(Time.time * 2.6f) * 0.018f; // respiration
            target = new Vector2(1f - b * 0.5f, 1f + b);
        }

        // Ressort amorti vers la cible
        squash = Vector2.SmoothDamp(squash, target, ref squashVel, 0.08f);

        // Inclinaison dans le sens de la course (plus forte avec l'élan)
        float targetLean = grounded ? -vx * (1.2f + 1.5f * pm.Momentum) : -vx * 0.6f;
        targetLean = Mathf.Clamp(targetLean, -14f, 14f);
        lean = Mathf.Lerp(lean, frozen ? 0f : targetLean, dt * 10f);

        castPunch = Mathf.MoveTowards(castPunch, 0f, dt * 5f);

        visual.localScale = new Vector3(squash.x, squash.y, 1f);
        // On garde les pieds au sol pendant la déformation
        visual.localPosition = new Vector3(0f, -(1f - squash.y) * halfHeight, 0f);
        visual.localRotation = Quaternion.Euler(0f, 0f, lean);

        // Cadence d'animation selon la vitesse
        if (anim != null) anim.speed = Mathf.Abs(vx) > 0.3f ? Mathf.Lerp(0.8f, 1.7f, Mathf.Abs(vx) / Mathf.Max(1f, pm.runSpeed)) : 1f;

        // Pas et poussière
        if (grounded && Mathf.Abs(vx) > 1f && !frozen)
        {
            stepTimer -= dt * Mathf.Abs(vx) / 5f;
            if (stepTimer <= 0f)
            {
                stepTimer = 0.27f;
                AudioManager.Play("pas", 0.3f, Random.Range(0.85f, 1.15f));
                if (pm.Momentum > 0.5f) Dust(2, 0.8f);
            }
        }

        // Images rémanentes quand on court à pleine vitesse
        if (pm.Momentum > 0.75f && Mathf.Abs(vx) > pm.moveSpeed)
        {
            ghostTimer -= dt;
            if (ghostTimer <= 0f)
            {
                ghostTimer = 0.05f;
                Ghost();
            }
        }
    }

    void Dust(int count, float speed)
    {
        Sprite dust = FxUtil.LoadSprite("poussiere");
        if (dust == null) return;
        Vector3 feet = transform.position + Vector3.down * (halfHeight * transform.lossyScale.y * 0.95f);
        for (int i = 0; i < count; i++)
        {
            Vector2 v = new Vector2(Random.Range(-1f, 1f) * speed, Random.Range(0.1f, 0.6f) * speed * 0.5f);
            SimpleParticle.Spawn(dust, feet + new Vector3(Random.Range(-0.2f, 0.2f), 0f, 0f), v,
                Random.Range(0.3f, 0.55f), new Color(0.85f, 0.8f, 0.9f, 0.7f), Random.Range(1.2f, 2.2f), 10002, null, -0.5f, 2.5f);
        }
    }

    void Ghost()
    {
        SpriteRenderer g = FxUtil.MakeSprite("Image rémanente", sr.sprite, visual.position, sr.sortingOrder - 1,
            new Color(1f, 0.55f, 0.45f, 0.45f), FxUtil.Additive);
        g.flipX = sr.flipX;
        g.transform.rotation = visual.rotation;
        g.transform.localScale = visual.lossyScale;
        SimpleParticle p = g.gameObject.AddComponent<SimpleParticle>();
        p.Init(Vector2.zero, 0.25f, g.color, 1f, 0f, 0f);
    }
}
