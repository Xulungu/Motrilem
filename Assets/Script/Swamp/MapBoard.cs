using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Panneau-carte au début de la zone : montre où l'on peut aller, comme les plans dans les parcs.
// (Il ne montre que les grands chemins : pas les raccourcis, pas les secrets.)
public class MapBoard : Interactable
{
    public string mapSprite = "carte_marais";
    public string title = "Le Marécage Acide";

    private bool open;

    protected override void Awake()
    {
        base.Awake();
        promptText = "Lire la carte";
        promptOffset = new Vector3(0f, 2.4f, 0f);
    }

    protected override void Interact()
    {
        if (open) return;
        StartCoroutine(Show());
    }

    IEnumerator Show()
    {
        open = true;
        canInteract = false;
        PlayerMovement pm = player;
        if (pm != null) pm.SetCutscene(true);
        AudioManager.Play("page", 0.8f, 0.9f);
        GameUI ui = GameUI.Instance;

        RectTransform root = UIFactory.Rect("Carte du marécage", ui.Root);
        UIFactory.Stretch(root, 0);
        Image bg = root.gameObject.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.05f, 0.04f, 0.94f);
        CanvasGroup g = root.gameObject.AddComponent<CanvasGroup>();
        g.alpha = 0f;

        Text t = UIFactory.Label("Titre", root, ui.Font, 54, new Color(0.9f, 0.95f, 0.8f), TextAnchor.MiddleCenter);
        t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        t.rectTransform.anchoredPosition = new Vector2(0, -70);
        t.rectTransform.sizeDelta = new Vector2(1600, 80);
        t.fontStyle = FontStyle.Bold;
        t.text = title;

        RectTransform mp = UIFactory.Rect("Carte", root);
        mp.anchorMin = new Vector2(0.04f, 0.14f);
        mp.anchorMax = new Vector2(0.96f, 0.84f);
        mp.offsetMin = mp.offsetMax = Vector2.zero;
        Image img = mp.gameObject.AddComponent<Image>();
        img.sprite = FxUtil.LoadSprite(mapSprite);
        img.preserveAspect = true;
        img.color = Color.white;

        Text help = UIFactory.Label("Aide", root, ui.Font, 28, new Color(0.7f, 0.8f, 0.7f), TextAnchor.MiddleCenter);
        help.rectTransform.anchorMin = help.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        help.rectTransform.anchoredPosition = new Vector2(0, 60);
        help.rectTransform.sizeDelta = new Vector2(1600, 50);
        help.fontStyle = FontStyle.Italic;
        help.text = "Les chemins tracés sont ceux des anciens pèlerins. Le marécage cache sûrement d'autres passages.      [E] Fermer";

        for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / 0.25f) { g.alpha = k; yield return null; }
        g.alpha = 1f;
        yield return null;
        while (!(GameInput.InteractDown() || GameInput.JumpDown())) yield return null;
        AudioManager.Play("page", 0.6f, 1.1f);
        for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / 0.2f) { g.alpha = 1f - k; yield return null; }
        Destroy(root.gameObject);
        yield return null;
        yield return null;
        if (pm != null) pm.SetCutscene(false);
        canInteract = true;
        open = false;
    }
}
