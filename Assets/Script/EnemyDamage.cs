using UnityEngine;

// Hitbox du corps de l'ennemi : blesse le joueur au contact.
[RequireComponent(typeof(Collider2D))]
public class EnemyDamage : MonoBehaviour
{
    public int damage = 20;
    [Tooltip("Vitesse du recul appliqué au joueur (x = horizontal, y = vertical)")]
    public Vector2 knockback = new Vector2(4f, 4f);
    public float knockbackDuration = 0.25f;

    private Collider2D bodyCollider;
    private Collider2D weakSpotCollider;
    private WeakSpot weakSpot;

    void Awake()
    {
        bodyCollider = GetComponent<Collider2D>();
        weakSpot = GetComponentInChildren<WeakSpot>();
        if (weakSpot != null) weakSpotCollider = weakSpot.GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision) { TryHurt(collision); }
    // Stay : si le joueur reste collé à l'ennemi, il reprend des dégâts à la fin de son invincibilité
    private void OnTriggerStay2D(Collider2D collision) { TryHurt(collision); }

    void TryHurt(Collider2D collision)
    {
        if (!enabled || !collision.CompareTag("Player")) return;
        // Le Rigidbody reçoit aussi les contacts des enfants (point faible, trompe) : seul le corps fait mal ici
        if (!bodyCollider.IsTouching(collision)) return;

        PlayerHealth health = collision.GetComponentInParent<PlayerHealth>();
        if (health == null || !health.CanTakeDamage) return;

        // Juste après un écrasement, le joueur remonte : l'ennemi ne le blesse pas
        if (weakSpot != null && weakSpot.RecentlyStomped) return;

        // Pas de dégâts seulement si le joueur est en train de retomber pile sur le point faible
        Rigidbody2D playerRb = collision.attachedRigidbody;
        if (weakSpotCollider != null && weakSpotCollider.enabled && playerRb != null &&
            weakSpot.CanStomp(playerRb) && weakSpotCollider.IsTouching(collision))
        {
            return;
        }

        health.TakeDamage(damage);

        PlayerMovement movement = collision.GetComponentInParent<PlayerMovement>();
        if (movement != null && health.CurrentHealth > 0)
        {
            float side = Mathf.Sign(collision.transform.position.x - transform.position.x);
            if (side == 0f) side = 1f;
            movement.ApplyKnockback(new Vector2(knockback.x * side, knockback.y), knockbackDuration);
        }
    }
}
