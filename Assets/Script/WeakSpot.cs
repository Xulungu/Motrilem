using UnityEngine;

// Point faible sur le dessus de l'ennemi : le joueur qui RETOMBE dessus le tue et rebondit.
// La zone est volontairement petite : il faut viser le centre de la tête.
public class WeakSpot : MonoBehaviour
{
    public GameObject objectToDestroy;
    [Tooltip("Vitesse verticale donnée au joueur quand il rebondit sur l'ennemi")]
    public float bounceVelocity = 6f;

    [Header("Zone de la tête (appliquée au démarrage)")]
    public bool overrideHitbox = true;
    public Vector2 hitboxSize = new Vector2(0.26f, 0.07f);
    public Vector2 hitboxOffset = new Vector2(0f, 0.17f);
    [Tooltip("Le joueur doit tomber au moins à cette vitesse")]
    public float minFallSpeed = 0.8f;
    [Tooltip("Écart horizontal maximal entre le centre du joueur et la tête")]
    public float maxHorizontalOffset = 0.32f;

    private bool killed;

    void Awake()
    {
        if (!overrideHitbox) return;
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = hitboxSize;
            box.offset = hitboxOffset;
        }
    }

    /// <summary>Le joueur retombe-t-il franchement sur la tête, bien centré ?</summary>
    public bool CanStomp(Rigidbody2D playerRb)
    {
        if (playerRb == null) return false;
        if (playerRb.velocity.y > -minFallSpeed) return false;
        return Mathf.Abs(playerRb.position.x - transform.position.x) <= maxHorizontalOffset;
    }

    private void OnTriggerEnter2D(Collider2D collision) { TryStomp(collision); }
    private void OnTriggerStay2D(Collider2D collision) { TryStomp(collision); }

    void TryStomp(Collider2D collision)
    {
        if (killed || !collision.CompareTag("Player")) return;

        Rigidbody2D playerRb = collision.attachedRigidbody;
        if (!CanStomp(playerRb)) return;

        killed = true;

        PlayerMovement movement = collision.GetComponentInParent<PlayerMovement>();
        if (movement != null) movement.Bounce(bounceVelocity);

        EnemyHealth health = GetComponentInParent<EnemyHealth>();
        if (health != null) health.Kill(true);
        else
        {
            AudioManager.Play("ecrase", 0.6f);
            Destroy(objectToDestroy);
        }
    }
}
