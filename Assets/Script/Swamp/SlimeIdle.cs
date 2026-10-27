using UnityEngine;

// Petit slime posé en décor (inoffensif) : il respire et se dandine. Sert à montrer qu'un endroit grouille de slimes.
public class SlimeIdle : MonoBehaviour
{
    public float scale = 0.8f;
    private SpriteRenderer sr;
    private Sprite[] frames;
    private float phase;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        frames = new[] { FxUtil.LoadSprite("slime_0"), FxUtil.LoadSprite("slime_1") };
        phase = Random.Range(0f, 6.28f);
    }

    void Update()
    {
        if (sr == null) return;
        float s = Mathf.Sin(Time.time * 2.6f + phase);
        transform.localScale = new Vector3(scale * (1f + s * 0.06f), scale * (1f - s * 0.06f), 1f);
        if (frames[0] != null && frames[1] != null) sr.sprite = frames[Mathf.FloorToInt(Time.time * 2.2f + phase) % 2 == 0 ? 0 : 1];
    }
}
