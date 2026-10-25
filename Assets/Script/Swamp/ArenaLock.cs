using UnityEngine;

// Grille qui se ferme derrière le joueur quand le combat de boss commence, et se rouvre quand il est fini
// (ou si le joueur est mort et que le combat recommence).
[RequireComponent(typeof(BoxCollider2D))]
public class ArenaLock : MonoBehaviour
{
    public SpriteRenderer render;

    private BoxCollider2D col;
    private bool locked;
    private float k;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        col.enabled = false;
        if (render != null) render.color = new Color(1f, 1f, 1f, 0f);
    }

    public void SetLocked(bool v)
    {
        if (locked == v) return;
        locked = v;
        if (v) AudioManager.Play("porte", 0.7f, 0.7f);
        col.enabled = v;
    }

    void Update()
    {
        k = Mathf.MoveTowards(k, locked ? 1f : 0f, Time.deltaTime * 2.5f);
        if (render != null) render.color = new Color(1f, 1f, 1f, k);
    }
}
