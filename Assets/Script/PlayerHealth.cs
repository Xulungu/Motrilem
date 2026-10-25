using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int CurrentHealth { get; private set; }

    [Header("Invincibilité après un coup")]
    public float invincibilityDuration = 1f;
    public float blinkInterval = 0.1f;

    [Header("Références")]
    [Tooltip("Laisser vide : une barre de vie est créée automatiquement en haut à gauche de l'écran")]
    public HealthBar healthBar;
    public SpriteRenderer graphics;

    [Header("Mort")]
    public float respawnDelay = 1.5f;

    [HideInInspector] public bool invulnerable; // pendant les cinématiques

    [Header("Chute dans l'acide ou le vide")]
    [Tooltip("Vrai : on réapparaît sur le dernier sol sûr (loin de l'acide) au lieu du dernier checkpoint")]
    public bool safeGroundRespawn;
    private Vector3 safePos;
    private bool hasSafe;
    private float groundedTime;

    private bool isInvincible;
    private bool isDead;
    private Vector3 spawnPosition;
    private PlayerMovement movement;
    private Rigidbody2D rb;

    public bool CanTakeDamage { get { return !isInvincible && !isDead && !invulnerable; } }

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
        if (graphics == null) graphics = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        spawnPosition = transform.position;
        // Bonus des éclats de vie ramassés pendant la partie
        maxHealth += SaveSystem.Session.maxHealthBonus;
        CurrentHealth = maxHealth;

        if (healthBar == null) healthBar = HealthBar.CreateDefault();
        healthBar.SetMaxHealth(maxHealth);
    }

    void Update()
    {
        if (!safeGroundRespawn || isDead || movement == null || rb == null) return;
        if (movement.isGrounded && Mathf.Abs(rb.velocity.y) < 0.1f)
        {
            groundedTime += Time.deltaTime;
            if (groundedTime > 0.3f && !AcidPool.NearAcid(transform.position, 2.2f))
            {
                Collider2D g = movement.GroundCollider;
                if (g == null || (g.GetComponentInParent<LilyPad>() == null && g.GetComponentInParent<MovingPlatform>() == null))
                {
                    safePos = transform.position;
                    hasSafe = true;
                }
            }
        }
        else groundedTime = 0f;
    }

    public void TakeDamage(int amount)
    {
        if (!CanTakeDamage || amount <= 0) return;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        healthBar.SetHealth(CurrentHealth);
        PlayerProfile.RecordDamage(amount);
        AudioManager.Play("degat", 0.7f, Random.Range(0.95f, 1.05f));
        if (Camera.main != null)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.Shake(0.12f, 0.2f);
        }

        if (CurrentHealth == 0) StartCoroutine(Die());
        else StartCoroutine(Invincibility());
    }

    /// <summary>Change l'endroit où le joueur réapparaît (checkpoints).</summary>
    public void SetSpawnPoint(Vector3 position)
    {
        spawnPosition = position;
    }

    /// <summary>Chute dans le vide : perte de vie puis retour au dernier checkpoint.</summary>
    public void FallIntoVoid(int damage)
    {
        if (isDead) return;
        PlayerProfile.Record(PlayerProfile.Event.Fall);
        CurrentHealth = Mathf.Max(CurrentHealth - Mathf.Max(0, damage), 0);
        healthBar.SetHealth(CurrentHealth);
        if (CurrentHealth == 0) StartCoroutine(Die());
        else StartCoroutine(RespawnAfterFall(safeGroundRespawn && hasSafe ? safePos : spawnPosition));
    }

    IEnumerator RespawnAfterFall(Vector3 target)
    {
        isDead = true; // bloque les dégâts pendant la téléportation
        if (movement != null) movement.controlsEnabled = false;
        yield return GameUI.Instance.FadeTo(Color.black, 0.25f);
        transform.position = target;
        if (rb != null) rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(0.15f);
        yield return GameUI.Instance.FadeTo(new Color(0, 0, 0, 0), 0.3f);
        if (movement != null) movement.controlsEnabled = true;
        isDead = false;
        if (!isInvincible) StartCoroutine(Invincibility());
    }

    /// <summary>Fixe directement la vie (chargement d'une sauvegarde).</summary>
    public void SetHealth(int hp)
    {
        CurrentHealth = Mathf.Clamp(hp, 1, maxHealth);
        if (healthBar != null) healthBar.SetHealth(CurrentHealth);
    }

    /// <summary>Augmente la vie maximale (éclat de vie) et soigne complètement.</summary>
    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        CurrentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(CurrentHealth);
        }
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        healthBar.SetHealth(CurrentHealth);
    }

    IEnumerator Invincibility()
    {
        isInvincible = true;
        float t = 0f;
        while (t < invincibilityDuration)
        {
            if (graphics != null) graphics.enabled = !graphics.enabled;
            yield return new WaitForSeconds(blinkInterval);
            t += blinkInterval;
        }
        if (graphics != null) graphics.enabled = true;
        isInvincible = false;
    }

    // Pour l'instant : le joueur réapparaît à son point de départ avec toute sa vie
    IEnumerator Die()
    {
        PlayerProfile.Record(PlayerProfile.Event.Death);
        isDead = true;
        if (movement != null) movement.controlsEnabled = false;
        if (graphics != null) graphics.color = new Color(1f, 0.4f, 0.4f, 1f);

        yield return new WaitForSeconds(respawnDelay * 0.6f);
        yield return GameUI.Instance.FadeTo(Color.black, respawnDelay * 0.4f);

        transform.position = spawnPosition;
        if (rb != null) rb.velocity = Vector2.zero;
        if (graphics != null) { graphics.color = Color.white; graphics.enabled = true; }

        CurrentHealth = maxHealth;
        healthBar.SetHealth(CurrentHealth);
        yield return GameUI.Instance.FadeTo(new Color(0, 0, 0, 0), 0.4f);
        if (movement != null) movement.controlsEnabled = true;
        isDead = false;

        StartCoroutine(Invincibility());
    }
}
