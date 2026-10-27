using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Porte vers la suite de l'aventure (pas encore disponible) : écran noir « À suivre », puis retour au menu.
public class ToBeContinuedDoor : Interactable
{
    public string message = "À suivre...";
    public string sub = "La suite de l'aventure arrive bientôt.";
    public string sceneToLoad = "Menu";
    private bool busy;

    protected override void Awake()
    {
        base.Awake();
        promptText = "Franchir la porte";
        promptOffset = new Vector3(0f, 2.8f, 0f);
    }

    protected override void Interact()
    {
        if (busy) return;
        busy = true;
        canInteract = false;
        StartCoroutine(Go(player));
    }

    IEnumerator Go(PlayerMovement pm)
    {
        if (pm != null) pm.SetCutscene(true);
        GameUI ui = GameUI.Instance;
        AudioManager.StopMusic(1.2f);
        AudioManager.Play("porte", 0.8f, 0.7f);
        yield return ui.FadeTo(Color.black, 1.2f);
        RectTransform rt = UIFactory.Rect("À suivre", ui.Root);
        UIFactory.Stretch(rt, 0);
        Text t = UIFactory.Label("Titre", rt, ui.Font, 80, new Color(0.95f, 0.93f, 0.85f), TextAnchor.MiddleCenter);
        t.rectTransform.anchoredPosition = new Vector2(0, 40);
        t.rectTransform.sizeDelta = new Vector2(1600, 120);
        t.text = message;
        Text s = UIFactory.Label("Sous-titre", rt, ui.Font, 34, new Color(0.8f, 0.9f, 0.8f), TextAnchor.MiddleCenter);
        s.rectTransform.anchoredPosition = new Vector2(0, -50);
        s.rectTransform.sizeDelta = new Vector2(1600, 60);
        s.fontStyle = FontStyle.Italic;
        s.text = sub;
        yield return new WaitForSeconds(4.5f);
        if (Application.CanStreamedLevelBeLoaded(sceneToLoad)) SceneManager.LoadScene(sceneToLoad);
    }
}
