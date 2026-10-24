using System.Collections;
using UnityEngine;

// Objet qui déclenche quelques répliques quand on interagit avec (panneau, passage scellé...).
public class TalkInteractable : Interactable
{
    [TextArea(2, 4)] public string[] lines = { "..." };
    public string speaker = "???";
    public bool shakeCamera = false;

    private bool busy;

    protected override void Interact()
    {
        if (busy) return;
        StartCoroutine(Talk(player));
    }

    IEnumerator Talk(PlayerMovement pm)
    {
        busy = true;
        canInteract = false;
        pm.SetCutscene(true);
        GameUI ui = GameUI.Instance;
        if (shakeCamera && Camera.main != null)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.Shake(0.06f, 1.2f);
            AudioManager.Play("bzz", 0.8f, 0.55f);
        }
        foreach (string l in lines) yield return ui.Say(l, speaker);
        yield return null;
        pm.SetCutscene(false);
        canInteract = true;
        busy = false;
    }
}
