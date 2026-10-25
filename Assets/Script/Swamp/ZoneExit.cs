using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sortie vers une région pas encore disponible : fondu, message « À suivre », retour au menu.
[RequireComponent(typeof(BoxCollider2D))]
public class ZoneExit : MonoBehaviour
{
    public string message = "À suivre...";
    public string sub = "La suite de l'aventure arrive bientôt.";
    public string sceneToLoad = "Menu";

    private bool used;

    void Awake() { GetComponent<BoxCollider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !other.CompareTag("Player")) return;
        used = true;
        StartCoroutine(Go(other.GetComponentInParent<PlayerMovement>()));
    }

    IEnumerator Go(PlayerMovement pm)
    {
        if (pm != null) pm.SetCutscene(true);
        GameUI ui = GameUI.Instance;
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
