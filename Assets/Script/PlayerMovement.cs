using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Déplacement")]
    [Tooltip("Vitesse de marche (unités par seconde)")]
    public float moveSpeed = 5f;
    [Tooltip("Vitesse maximale une fois l'élan pris")]
    public float runSpeed = 8f;
    [Tooltip("Temps de course au sol (même direction) pour atteindre la vitesse maximale")]
    public float momentumBuildTime = 1.2f;
    [HideInInspector] public float jumpForce = 250f; // ancien réglage (remplacé par jumpVelocity)
    [Header("Saut")]
    [Tooltip("Vitesse verticale au moment du saut (≈ 2,5 cases de hauteur avec la gravité ci-dessous)")]
    public float jumpVelocity = 10.2f;
    [Tooltip("Gravité du joueur (plus forte qu'au départ : sauts hauts mais pas flottants)")]
    public float gravity = 2.1f;
    [Tooltip("Relâcher le saut pendant la montée coupe le saut (petits sauts précis)")]
    [Range(0.1f, 1f)] public float jumpCutMultiplier = 0.5f;
    public float maxFallSpeed = 18f;
    private bool canCutJump;
    public Rigidbody2D rb;
    private Vector3 velocity = Vector3.zero;
    public bool isJumping;
    public bool isGrounded;

    [Header("Confort du saut")]
    [Tooltip("On peut encore sauter un court instant après avoir quitté le bord")]
    public float coyoteTime = 0.1f;
    [Tooltip("Un appui sur saut juste avant d'atterrir est mémorisé")]
    public float jumpBufferTime = 0.12f;

    [Header("Détection du sol")]
    public Transform groundCheckLeft;
    public Transform groundCheckRight;
    [Tooltip("Calques considérés comme du sol (par défaut : tout sauf le joueur lui-même)")]
    public LayerMask groundLayers = ~0;

    [Header("Regarder")]
    [Tooltip("Temps d'appui sur haut / bas (immobile au sol) avant que la caméra se décale")]
    public float lookDelay = 1f;
    private float lookTimer;
    private int lookDir;
    private CameraFollow camFollow;

    [Header("Visuel")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    // Contrôle temporairement désactivé (recul après un coup, mort...)
    private float controlLockTimer;
    [HideInInspector] public bool controlsEnabled = true;
    // Quand une cinématique déplace elle-même le joueur (lévitation...), on ne touche plus à sa vitesse
    [HideInInspector] public bool externalControl = false;

    /// <summary>Élan de 0 (marche) à 1 (pleine course).</summary>
    public float Momentum { get; private set; }
    /// <summary>Le collider sur lequel le joueur se tient (plateformes mobiles, nénuphars...).</summary>
    public Collider2D GroundCollider { get; private set; }
    /// <summary>1 = regarde à droite, -1 = à gauche.</summary>
    public int Facing { get { return spriteRenderer != null && spriteRenderer.flipX ? -1 : 1; } }

    public event System.Action OnJumped;
    public event System.Action<float> OnLanded; // vitesse verticale à l'impact

    private Collider2D[] ownColliders;
    private readonly Collider2D[] groundHits = new Collider2D[8];
    private float coyoteTimer, jumpBufferTimer;
    private int momentumDir;
    private float sameDirTime;
    private bool wasGrounded = true;
    private float lowestAirVy;
    private Collider2D droppingThrough;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        ownColliders = GetComponentsInChildren<Collider2D>();

        // Friction nulle : le joueur ne "colle" plus aux murs quand il saute contre eux
        PhysicsMaterial2D noFriction = new PhysicsMaterial2D();
        noFriction.name = "Sans friction";
        noFriction.friction = 0f;
        noFriction.bounciness = 0f;
        rb.sharedMaterial = noFriction;
        rb.gravityScale = gravity;
        foreach (Collider2D c in ownColliders) c.sharedMaterial = noFriction;

        // Composants ajoutés automatiquement (animations améliorées, magie)
        if (GetComponent<PlayerVisuals>() == null) gameObject.AddComponent<PlayerVisuals>();
        if (GetComponent<PlayerCaster>() == null) gameObject.AddComponent<PlayerCaster>();
        if (GetComponent<PlayerBehaviorTracker>() == null) gameObject.AddComponent<PlayerBehaviorTracker>();
    }

    void Update()
    {
        // Les entrées sont lues dans Update pour ne jamais rater un appui
        if (controlsEnabled && controlLockTimer <= 0f && !externalControl)
        {
            if (GameInput.JumpDown()) jumpBufferTimer = jumpBufferTime;
            if (GameInput.DownPressed()) TryDropThrough();
            // Saut variable : relâcher la touche pendant la montée raccourcit le saut
            if (canCutJump && !GameInput.JumpHeld() && rb.velocity.y > 0f)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
                canCutJump = false;
            }
        }
        if (rb.velocity.y <= 0f) canCutJump = false;
        jumpBufferTimer -= Time.deltaTime;
        UpdateLook();
    }

    // Maintenir haut (Z / ↑) ou bas (S / ↓) immobile au sol pendant 1 s : la caméra montre ce qu'il y a au-dessus / en dessous.
    // Sur des planches, bas sert à descendre à travers : on ne regarde pas.
    void UpdateLook()
    {
        int want = 0;
        if (controlsEnabled && controlLockTimer <= 0f && !externalControl && isGrounded
            && GameInput.Horizontal() == 0f && Mathf.Abs(rb.velocity.x) < 1f)
        {
            float v = GameInput.Vertical();
            want = v > 0f ? 1 : v < 0f ? -1 : 0;
            if (want < 0 && GroundCollider != null && GroundCollider.usedByEffector) want = 0;
        }
        if (want != lookDir) { lookDir = want; lookTimer = 0f; }
        if (want != 0) lookTimer += Time.deltaTime;

        if (camFollow == null && Camera.main != null) camFollow = Camera.main.GetComponent<CameraFollow>();
        if (camFollow != null) camFollow.SetLook(want != 0 && lookTimer >= lookDelay ? want : 0);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        isGrounded = CheckGround();

        // Atterrissage
        if (!isGrounded) lowestAirVy = Mathf.Min(lowestAirVy, rb.velocity.y);
        if (isGrounded && !wasGrounded)
        {
            if (OnLanded != null) OnLanded(lowestAirVy);
            lowestAirVy = 0f;
        }
        wasGrounded = isGrounded;
        coyoteTimer = isGrounded ? coyoteTime : coyoteTimer - dt;

        if (externalControl)
        {
            animator.SetFloat("Speed", 0f);
            return;
        }
        if (rb.bodyType == RigidbodyType2D.Dynamic) rb.gravityScale = gravity;
        if (rb.velocity.y < -maxFallSpeed) rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);

        float input = 0f;
        if (controlLockTimer > 0f)
        {
            // Pendant un recul, on laisse la physique faire
            controlLockTimer -= dt;
            Momentum = 0f;
        }
        else
        {
            input = controlsEnabled ? GameInput.Horizontal() : 0f;
            UpdateMomentum(input, dt);
            MovePlayer(input * Mathf.Lerp(moveSpeed, runSpeed, Momentum) * (Time.time < slowUntil ? slowFactor : 1f));

            if (controlsEnabled && jumpBufferTimer > 0f && coyoteTimer > 0f) Jump();
        }

        Flip(input != 0f ? input : rb.velocity.x);
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
    }

    void UpdateMomentum(float input, float dt)
    {
        int dir = input > 0f ? 1 : input < 0f ? -1 : 0;
        if (dir == 0)
        {
            // On lâche la direction : l'élan retombe au sol, mais se conserve en l'air
            if (isGrounded) Momentum = Mathf.MoveTowards(Momentum, 0f, dt * 4f);
            sameDirTime = 0f;
            return;
        }
        if (dir != momentumDir)
        {
            Momentum = 0f;           // demi-tour : on repart de zéro
            momentumDir = dir;
            sameDirTime = 0f;
        }
        sameDirTime += dt;

        if (sameDirTime > 0.15f && Mathf.Abs(rb.velocity.x) < 0.5f)
        {
            Momentum = 0f;           // bloqué contre un mur
        }
        else if (isGrounded && Mathf.Abs(rb.velocity.x) > moveSpeed * 0.8f)
        {
            Momentum = Mathf.MoveTowards(Momentum, 1f, dt / Mathf.Max(0.01f, momentumBuildTime));
        }
    }

    bool CheckGround()
    {
        GroundCollider = null;
        int count = Physics2D.OverlapAreaNonAlloc(groundCheckLeft.position, groundCheckRight.position, groundHits, groundLayers);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = groundHits[i];
            // On ignore les colliders du joueur et les triggers (points faibles, zones de dégâts...)
            if (hit.isTrigger || IsOwnCollider(hit) || hit == droppingThrough) continue;
            // Plateforme traversable : on ne "tient" dessus que si on ne monte pas à travers
            if (hit.usedByEffector && rb.velocity.y > 0.1f) continue;
            GroundCollider = hit;
            return true;
        }
        return false;
    }

    bool IsOwnCollider(Collider2D col)
    {
        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] == col) return true;
        }
        return false;
    }

    void MovePlayer(float targetSpeedX)
    {
        Vector3 targetVelocity = new Vector2(targetSpeedX, rb.velocity.y);
        float smooth = isGrounded ? 0.05f : 0.09f; // un peu moins de contrôle en l'air
        rb.velocity = Vector3.SmoothDamp(rb.velocity, targetVelocity, ref velocity, smooth);
    }

    void Jump()
    {
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        isJumping = false;
        rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);
        canCutJump = true;
        AudioManager.Play("saut", 0.45f, Random.Range(0.95f, 1.05f));
        if (OnJumped != null) OnJumped();
    }

    // ------------------------------------------------------------------ plateformes traversables
    void TryDropThrough()
    {
        if (!isGrounded || droppingThrough != null) return;
        int count = Physics2D.OverlapAreaNonAlloc(groundCheckLeft.position, groundCheckRight.position, groundHits, groundLayers);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = groundHits[i];
            if (!hit.isTrigger && hit.usedByEffector && hit.GetComponent<PlatformEffector2D>() != null)
            {
                StartCoroutine(DropThrough(hit));
                return;
            }
        }
    }

    IEnumerator DropThrough(Collider2D platform)
    {
        droppingThrough = platform;
        foreach (Collider2D c in ownColliders) Physics2D.IgnoreCollision(c, platform, true);
        rb.velocity = new Vector2(rb.velocity.x, -2f);
        yield return new WaitForSeconds(0.35f);
        foreach (Collider2D c in ownColliders) if (c != null && platform != null) Physics2D.IgnoreCollision(c, platform, false);
        droppingThrough = null;
    }

    // ------------------------------------------------------------------ actions externes
    /// <summary>Projette le joueur (recul) et bloque ses contrôles pendant 'duration' secondes.</summary>
    public void ApplyKnockback(Vector2 knockVelocity, float duration)
    {
        rb.velocity = knockVelocity;
        velocity = Vector3.zero;
        isJumping = false;
        jumpBufferTimer = 0f;
        Momentum = 0f;
        controlLockTimer = duration;
    }

    /// <summary>Rebond (après avoir écrasé un ennemi).</summary>
    public void Bounce(float bounceVelocity)
    {
        // Le rebond est réglé pour une gravité de 1 : on le compense pour la gravité actuelle
        rb.velocity = new Vector2(rb.velocity.x, bounceVelocity * Mathf.Sqrt(Mathf.Max(1f, rb.gravityScale)));
        canCutJump = false;
        isJumping = false;
        if (OnJumped != null) OnJumped();
    }

    /// <summary>Active/désactive le mode cinématique : plus de contrôle joueur, le personnage s'arrête.</summary>
    private float slowFactor = 1f, slowUntil;

    /// <summary>Ralentit le déplacement horizontal (slime collant...). Le plus fort ralentissement en cours l'emporte.</summary>
    public void ApplySlow(float factor, float duration)
    {
        if (Time.time < slowUntil) factor = Mathf.Min(factor, slowFactor);
        slowFactor = Mathf.Clamp(factor, 0.2f, 1f);
        slowUntil = Mathf.Max(slowUntil, Time.time + duration);
    }

    public void SetCutscene(bool on)
    {
        controlsEnabled = !on;
        isJumping = false;
        jumpBufferTimer = 0f;
        if (on)
        {
            controlLockTimer = 0f;
            velocity = Vector3.zero;
            Momentum = 0f;
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
    }

    void Flip(float direction)
    {
        if (spriteRenderer == null) return;
        if (direction > 0.1f) spriteRenderer.flipX = false;
        else if (direction < -0.1f) spriteRenderer.flipX = true;
    }
}
