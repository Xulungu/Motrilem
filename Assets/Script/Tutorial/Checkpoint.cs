using UnityEngine;

// Point de réapparition : le joueur revient ici après une chute ou une mort.
[RequireComponent(typeof(BoxCollider2D))]
public class Checkpoint : MonoBehaviour
{
    public Vector3 spawnOffset = new Vector3(0f, 0.8f, 0f);

    void Reset() { GetComponent<BoxCollider2D>().isTrigger = true; }
    void Awake() { GetComponent<BoxCollider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null) health.SetSpawnPoint(transform.position + spawnOffset);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.6f);
        Gizmos.DrawWireSphere(transform.position + spawnOffset, 0.3f);
    }
}
