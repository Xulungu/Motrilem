using UnityEngine;

// Frontière entre deux cartes, côté joueur : une lueur pâle sur la ligne de passage et des volutes de brume qui filent vers
// l'obscurité. On comprend que ce n'est pas un mur mais l'entrée d'une autre zone. (L'obscurité elle-même est le voile peint
// par le constructeur de scène : il cache tout ce qui se trouve de l'autre côté.)
public class BorderMist : MonoBehaviour
{
    public Vector2 heading = Vector2.right;     // direction de la frontière (vers la carte voisine) : axe unitaire
    public float depth = 6f;                    // longueur de la zone de brume, en arrière de la frontière
    public float length = 10f;                  // largeur du passage (le long de la frontière)
    public Color tint = new Color(0.65f, 1f, 0.8f, 1f);

    private SpriteRenderer seam;
    private Transform player;
    private float timer;

    void Start()
    {
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) player = pm.transform;
        Sprite soft = FxUtil.LoadSprite("lueur_douce");
        if (soft == null) return;
        bool horizontal = Mathf.Abs(heading.x) > 0.5f;
        seam = FxUtil.MakeSprite("Ligne de passage", soft, transform.position, 9997, new Color(tint.r, tint.g, tint.b, 0.3f), FxUtil.Additive, transform);
        // fine dans le sens de la frontière, longue sur sa largeur
        seam.transform.localScale = horizontal ? new Vector3(0.7f, Mathf.Max(1f, length * 0.5f), 1f) : new Vector3(Mathf.Max(1f, length * 0.5f), 0.7f, 1f);
    }

    void Update()
    {
        if (player == null) return;
        Vector2 d = player.position - transform.position;
        if (d.magnitude > Mathf.Max(24f, length)) return;      // loin : rien à animer
        if (seam != null)
        {
            Color c = tint;
            c.a = 0.22f + 0.1f * Mathf.Sin(Time.time * 2.2f + transform.position.x);
            seam.color = c;
        }
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = Random.Range(0.12f, 0.28f);
        Sprite soft = FxUtil.LoadSprite("lueur_douce");
        if (soft == null) return;
        bool horizontal = Mathf.Abs(heading.x) > 0.5f;
        Vector2 across = horizontal ? Vector2.up : Vector2.right;
        Vector3 start = transform.position - (Vector3)(heading * Random.Range(0.5f, depth)) + (Vector3)(across * Random.Range(-length * 0.5f, length * 0.5f));
        Vector2 vel = heading * Random.Range(1.2f, 2.4f) + across * Random.Range(-0.2f, 0.2f);
        SimpleParticle.Spawn(soft, start, vel, Random.Range(1.4f, 2.4f), new Color(tint.r, tint.g, tint.b, 0.28f), Random.Range(0.5f, 1.1f), 9996, FxUtil.Additive, 0f, 0.5f);
    }
}
