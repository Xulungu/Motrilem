using System.Collections;
using UnityEngine;

// Trappe ou grille qui s'ouvre quand le levier portant le même drapeau est actionné.
[RequireComponent(typeof(BoxCollider2D))]
public class SwampGate : MonoBehaviour
{
    public string flag = "levier";
    [Tooltip("Vrai : trappe horizontale (elle se retire sur le côté). Faux : grille verticale (elle remonte).")]
    public bool hatch = true;
    public SpriteRenderer render;

    private BoxCollider2D col;
    private bool open;
    private Vector3 home;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        home = transform.position;
    }

    void Start()
    {
        if (SaveSystem.HasFlag(Lever.Key(flag))) Open(true);
    }

    void Update()
    {
        if (!open && SaveSystem.HasFlag(Lever.Key(flag))) Open(false);
    }

    void Open(bool instant)
    {
        open = true;
        col.enabled = false;
        if (instant)
        {
            if (render != null) render.enabled = false;
            return;
        }
        StartCoroutine(Anim());
    }

    IEnumerator Anim()
    {
        AudioManager.Play("porte", 0.7f, 0.6f);
        Sprite dust = FxUtil.LoadSprite("poussiere");
        Vector3 size = col.size;
        for (float k = 0f; k < 1f; k += Time.deltaTime / 1.1f)
        {
            float e = Ease.InOut(k);
            if (render != null)
            {
                Color c = render.color;
                c.a = 1f - e;
                render.color = c;
                render.transform.position = home + (hatch ? new Vector3(0f, -0.5f * e, 0f) : new Vector3(0f, size.y * 0.8f * e, 0f));
            }
            if (dust != null && Random.value < 0.4f)
                SimpleParticle.Spawn(dust, home + new Vector3(Random.Range(-size.x, size.x) * 0.5f, Random.Range(-size.y, size.y) * 0.5f, 0f),
                    new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-1f, 0f)), 0.8f, new Color(0.7f, 0.65f, 0.55f, 0.7f), 0.9f, 10005, null, 0.5f, 1f);
            yield return null;
        }
        if (render != null) render.enabled = false;
    }
}
