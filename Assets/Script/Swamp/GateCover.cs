using UnityEngine;

// Cache ce qui se trouve sous une trappe fermée (un raccourci) : tant que le levier n'est pas actionné,
// le joueur ne voit qu'un sol plein. Si le joueur est déjà du côté du raccourci (en dessous), rien n'est caché.
public class GateCover : MonoBehaviour
{
    public string flag = "levier";
    public bool byX;                       // vrai : visible si le joueur est à gauche du seuil ; faux : visible s'il est au-dessus
    public float threshold = 0f;
    public SpriteRenderer[] renders;

    private Transform player;
    private float alpha = 1f;
    private bool opened;

    void Start()
    {
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) player = pm.transform;
        if (SaveSystem.HasFlag(Lever.Key(flag))) { opened = true; alpha = 0f; Apply(); }
    }

    void Update()
    {
        if (!opened && SaveSystem.HasFlag(Lever.Key(flag))) opened = true;
        float target = 0f;
        if (!opened && player != null && (byX ? player.position.x <= threshold : player.position.y >= threshold)) target = 1f;
        else if (!opened && player == null) target = 1f;
        alpha = Mathf.MoveTowards(alpha, target, Time.deltaTime / (opened ? 1.2f : 0.25f));
        Apply();
    }

    void Apply()
    {
        if (renders == null) return;
        foreach (SpriteRenderer r in renders)
        {
            if (r == null) continue;
            Color c = r.color; c.a = alpha; r.color = c;
            r.enabled = alpha > 0.01f;
        }
    }
}
