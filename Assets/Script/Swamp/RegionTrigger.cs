using System.Collections.Generic;
using UnityEngine;

// Zone qui annonce le nom de la région quand le joueur y entre (au plus une fois toutes les 40 secondes).
[RequireComponent(typeof(BoxCollider2D))]
public class RegionTrigger : MonoBehaviour
{
    public string title = "Région";
    public string subtitle = "";
    [Tooltip("Vrai : ne rien afficher (région de départ, déjà annoncée par l'intro)")]
    public bool silent;

    private static readonly Dictionary<string, float> lastShown = new Dictionary<string, float>();
    private static string current = "";

    void Awake() { GetComponent<BoxCollider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (current == title) return;
        current = title;
        if (silent || !GameUI.HasInstance) return;
        float last;
        if (lastShown.TryGetValue(title, out last) && Time.realtimeSinceStartup - last < 40f) return;
        lastShown[title] = Time.realtimeSinceStartup;
        GameUI.Instance.PlayTitle(title, subtitle);
    }
}
