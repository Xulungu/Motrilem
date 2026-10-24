using UnityEngine;

// Vide : le joueur perd un peu de vie et revient au dernier point de réapparition.
[RequireComponent(typeof(BoxCollider2D))]
public class FallZone : MonoBehaviour
{
    public int damage = 20;

    void Reset() { GetComponent<BoxCollider2D>().isTrigger = true; }
    void Awake() { GetComponent<BoxCollider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null) health.FallIntoVoid(damage);
    }

    void OnDrawGizmos()
    {
        BoxCollider2D b = GetComponent<BoxCollider2D>();
        if (b == null) return;
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawCube(transform.position + (Vector3)b.offset, b.size);
    }
}
