using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Passage d'une carte (groupe de zones) à la suivante, façon Hollow Knight : le passage se trouve dans un couloir,
// un puits ou un pont ; fondu rapide, chargement de la carte voisine, et le joueur réapparaît au MÊME endroit du monde
// avec son élan. L'arrivée a toujours un sol sûr enregistré : en cas de chute dans l'acide on y revient.
// Un ascenseur emprunté pendant le passage reprend là où il était.
//
// Le changement se fait à la frontière même : dès que le joueur dépasse un peu la ligne où le noir commence
// (fireDepth), on change de carte. À l'arrivée, il se trouve du bon côté de la frontière de la nouvelle carte (juste avant
// son noir) : il faut revenir de plus de deux fois fireDepth pour repasser, donc aucun va-et-vient.
[RequireComponent(typeof(BoxCollider2D))]
public class ZoneTransition : MonoBehaviour
{
    public string targetScene = "EtangRuines";
    public Vector2 safeGround;          // sol sûr de la carte d'arrivée (pour les chutes et les morts sans statue)
    public string liftId = "";          // ascenseur à synchroniser (vide : aucun)
    public float fadeTime = 0.25f;

    [Header("Frontière")]
    public Vector2 heading = Vector2.right;   // direction du noir (vers la carte voisine) : axe unitaire
    public Vector2 boundary;                  // point de la frontière, au centre du passage
    public float halfSpan = 5f;               // demi-largeur du passage, le long de la frontière
    public float fireDepth = 0.3f;            // on change de carte quand le joueur dépasse la frontière de cette distance

    private Transform player;
    private bool used;

    void Awake() { GetComponent<BoxCollider2D>().isTrigger = true; }

    void Start()
    {
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) player = pm.transform;
    }

    void Update()
    {
        if (used || player == null) return;
        Vector2 d = (Vector2)player.position - boundary;
        float depth = Vector2.Dot(d, heading);
        float lateral = Mathf.Abs(Vector2.Dot(d, new Vector2(-heading.y, heading.x)));
        if (depth > fireDepth && lateral <= halfSpan + 0.5f) TryGo();
    }

    void TryGo()
    {
        if (!Application.CanStreamedLevelBeLoaded(targetScene))
        {
            Debug.LogWarning("La scène '" + targetScene + "' n'est pas dans les Build Settings.");
            used = true;
            return;
        }
        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null && ph.CurrentHealth <= 0) return;   // mort en cours : on ne change pas de carte
        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        used = true;
        StartCoroutine(Go(pm, ph));
    }

    IEnumerator Go(PlayerMovement pm, PlayerHealth ph)
    {
        Rigidbody2D rb = pm != null ? pm.GetComponent<Rigidbody2D>() : null;
        Vector2 velocity = rb != null ? rb.velocity : Vector2.zero;
        // position relevée à la frontière (pas après le fondu : le joueur aurait continué sa course dans le noir)
        Vector2 pos = pm != null ? (Vector2)pm.transform.position : (Vector2)transform.position;
        float[] liftState = null;
        if (!string.IsNullOrEmpty(liftId))
            foreach (ElevatorPlatform e in FindObjectsOfType<ElevatorPlatform>())
                if (e.liftId == liftId) { liftState = e.ExportState(); break; }

        if (pm != null) pm.SetCutscene(true);
        if (GameUI.HasInstance) yield return GameUI.Instance.FadeTo(Color.black, fadeTime);

        ZoneBanner.announce = true;
        SaveSystem.SetTransit(targetScene, pos, ph != null ? ph.CurrentHealth : -1, velocity, safeGround, liftId, liftState);
        SceneManager.LoadScene(targetScene);
    }
}
