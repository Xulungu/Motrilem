using UnityEngine;

// Levier : on l'actionne une seule fois ; il ouvre la trappe ou la grille qui porte le même drapeau.
// L'état est mémorisé dans la sauvegarde (il reste actionné après un rechargement).
public class Lever : Interactable
{
    public string flag = "levier";
    public SpriteRenderer render;
    public Sprite offSprite, onSprite;

    private bool pulled;

    public static string Key(string f) { return "levier_" + f; }

    protected override void Awake()
    {
        base.Awake();
        promptText = "Actionner";
        promptOffset = new Vector3(0f, 1.3f, 0f);
    }

    void Start()
    {
        pulled = SaveSystem.HasFlag(Key(flag));
        canInteract = !pulled;
        Refresh();
    }

    void Refresh()
    {
        if (render != null) render.sprite = pulled ? onSprite : offSprite;
    }

    protected override void Interact()
    {
        if (pulled) return;
        pulled = true;
        canInteract = false;
        SaveSystem.SetFlag(Key(flag));
        AudioManager.Play("porte", 0.8f, 0.75f);
        Refresh();
        if (Camera.main != null)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.Shake(0.05f, 0.6f);
        }
        if (GameUI.HasInstance) GameUI.Instance.Toast("Quelque chose s'est ouvert, quelque part...");
    }
}
