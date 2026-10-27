using UnityEngine;

// Annonce le nom de la zone : à l'arrivée par un passage entre deux cartes, et quand le joueur entre dans une nouvelle région.
// (Même principe que dans Hollow Knight : le titre s'affiche en grand, puis disparaît.)
public class ZoneBanner : MonoBehaviour
{
    /// <summary>Vrai juste avant de changer de carte par un passage : la carte d'arrivée annonce alors sa zone.</summary>
    public static bool announce;

    private Transform player;
    private string current;
    private string pending;
    private float stable, cooldown;

    void Start()
    {
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) player = pm.transform;
        current = Title();
        if (announce)
        {
            announce = false;
            if (current != null) StartCoroutine(ShowLater(current, 1.0f));
        }
    }

    string Title()
    {
        if (player == null) return null;
        string sub;
        string t = SwampZones.TitleAt(player.position.x, player.position.y, out sub);
        return t == null ? null : t + "|" + sub;
    }

    System.Collections.IEnumerator ShowLater(string key, float delay)
    {
        yield return new WaitForSeconds(delay);
        Show(key);
    }

    void Show(string key)
    {
        if (!GameUI.HasInstance || key == null) return;
        string[] p = key.Split('|');
        GameUI.Instance.PlayTitle(p[0], p.Length > 1 ? p[1] : null);
        cooldown = 8f;
    }

    void Update()
    {
        cooldown -= Time.deltaTime;
        string t = Title();
        if (t == null || t == current) { stable = 0f; pending = null; return; }
        if (t != pending) { pending = t; stable = 0f; return; }
        stable += Time.deltaTime;
        if (stable > 1.2f && cooldown <= 0f)
        {
            current = t;
            Show(t);
        }
    }
}
