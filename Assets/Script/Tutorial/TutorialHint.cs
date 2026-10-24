using UnityEngine;

// Zone qui affiche un message d'aide tant que le joueur est dedans.
[RequireComponent(typeof(BoxCollider2D))]
public class TutorialHint : MonoBehaviour
{
    [TextArea] public string message = "← → : se déplacer";
    private int inside;

    void Reset() { GetComponent<BoxCollider2D>().isTrigger = true; }
    void Awake() { GetComponent<BoxCollider2D>().isTrigger = true; }

    /// <summary>Les messages d'aide sont désactivés (les joueurs découvrent par eux-mêmes).
    /// Les commandes restent consultables dans le menu pause.</summary>
    public static bool Enabled = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!Enabled) return;
        if (!other.CompareTag("Player")) return;
        inside++;
        GameUI.Instance.ShowHint(this, message);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inside = Mathf.Max(0, inside - 1);
        if (inside == 0 && GameUI.HasInstance) GameUI.Instance.HideHint(this);
    }

    void Update()
    {
        if (!Enabled) return;
        // Pas de bulle d'aide pendant les dialogues
        if (inside > 0 && GameUI.Instance.IsTalking) GameUI.Instance.HideHint(this);
        else if (inside > 0) GameUI.Instance.ShowHint(this, message);
    }

    void OnDrawGizmos()
    {
        BoxCollider2D b = GetComponent<BoxCollider2D>();
        if (b == null) return;
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.25f);
        Gizmos.DrawCube(transform.position + (Vector3)b.offset, b.size);
    }
}
