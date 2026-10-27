using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu d'accueil : Jouer / Paramètres / Quitter.
// Clavier (Z S ou flèches, E / Entrée pour valider, Échap pour revenir) ou souris.
public class MainMenu : MonoBehaviour
{
    public string firstScene = "Tutorial";

    private RectTransform root, mainPanel, settingsPanel;
    private CanvasGroup mainGroup, settingsGroup;
    private readonly List<Text> items = new List<Text>();
    private readonly List<float> itemScale = new List<float>();
    private int selected;
    private bool inSettings, starting;
    private readonly VolumeSliders sliders = new VolumeSliders();
    private Image fade;
    private RectTransform book, glowRect;
    private readonly List<RectTransform> fragments = new List<RectTransform>();
    private readonly List<Vector3> fragData = new List<Vector3>();
    private Text title;
    private Font font;

    // Entrées du menu : "Continuer" n'apparaît que s'il existe une sauvegarde
    private readonly List<string> labels = new List<string>();
    private readonly List<string> actions = new List<string>();
    private bool confirmNewGame;
    private SaveData existingSave;

    void Start()
    {
        Build();
        StartCoroutine(FadeIn());
    }

    // ------------------------------------------------------------------ construction
    void Build()
    {
        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        GameObject go = new GameObject("Menu UI", typeof(RectTransform));
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        root = (RectTransform)go.transform;

        Image bg = UIFactory.Box("Fond", root, Vector2.zero, Vector2.zero, new Color(0.055f, 0.045f, 0.09f));
        UIFactory.Stretch(bg.rectTransform, 0);

        // Lueur et fragments qui flottent
        Sprite glow = FxUtil.LoadSprite("lueur_douce");
        Image g = UIFactory.SpriteImage("Lueur", root, glow, new Vector2(0, 160), 1f, new Color(0.45f, 0.85f, 1f, 0.22f));
        glowRect = g.rectTransform;
        glowRect.sizeDelta = new Vector2(1100, 1100);
        Sprite frag = FxUtil.LoadSprite("fragment");
        for (int i = 0; i < 26; i++)
        {
            Image f = UIFactory.SpriteImage("Fragment", root, frag, Vector2.zero, Random.Range(3f, 6f), new Color(0.8f, 0.95f, 1f, Random.Range(0.25f, 0.7f)));
            fragments.Add(f.rectTransform);
            fragData.Add(new Vector3(Random.Range(-950f, 950f), Random.Range(-560f, 560f), Random.Range(0.3f, 1f)));
        }

        Image bk = UIFactory.SpriteImage("Livre", root, FxUtil.LoadSprite("livre_ferme"), new Vector2(0, 250), 9f, Color.white);
        book = bk.rectTransform;

        title = UIFactory.Label("Titre", root, font, 120, new Color(0.93f, 0.9f, 0.82f), TextAnchor.MiddleCenter);
        title.rectTransform.anchoredPosition = new Vector2(0, 60);
        title.rectTransform.sizeDelta = new Vector2(1600, 160);
        title.fontStyle = FontStyle.Bold;
        title.text = "MOTRILEM";
        Shadow sh = title.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0.2f, 0.6f, 0.8f, 0.6f);
        sh.effectDistance = new Vector2(4, -4);

        // Panneau principal
        mainPanel = UIFactory.Rect("Principal", root);
        UIFactory.Stretch(mainPanel, 0);
        mainGroup = mainPanel.gameObject.AddComponent<CanvasGroup>();
        existingSave = SaveSystem.ReadSave();
        if (existingSave != null && !string.IsNullOrEmpty(existingSave.scene))
        {
            labels.Add("Continuer"); actions.Add("continuer");
            labels.Add("Nouvelle partie"); actions.Add("nouvelle");
        }
        else
        {
            labels.Add("Jouer"); actions.Add("nouvelle");
        }
        labels.Add("Paramètres"); actions.Add("parametres");
        labels.Add("Quitter"); actions.Add("quitter");

        float top = labels.Count > 3 ? -90 : -120;
        for (int i = 0; i < labels.Count; i++)
        {
            Text t = UIFactory.Label(labels[i], mainPanel, font, 52, Color.white, TextAnchor.MiddleCenter);
            t.rectTransform.anchoredPosition = new Vector2(0, top - i * 90);
            t.rectTransform.sizeDelta = new Vector2(1200, 80);
            t.text = labels[i];
            items.Add(t);
            itemScale.Add(1f);
        }
        if (existingSave != null && !string.IsNullOrEmpty(existingSave.date))
        {
            Text info = UIFactory.Label("Info sauvegarde", mainPanel, font, 24, new Color(0.56f, 0.94f, 1f, 0.6f), TextAnchor.MiddleCenter);
            info.rectTransform.anchoredPosition = new Vector2(0, top + 50);
            info.rectTransform.sizeDelta = new Vector2(1200, 40);
            info.text = "Dernière sauvegarde : " + existingSave.date;
        }
        Text help = UIFactory.Label("Aide", mainPanel, font, 26, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleCenter);
        help.rectTransform.anchoredPosition = new Vector2(0, -490);
        help.rectTransform.sizeDelta = new Vector2(1600, 40);
        help.text = "Z / S ou ↑ / ↓ : choisir        E / Entrée : valider";

        // Panneau des paramètres (vide pour l'instant)
        settingsPanel = UIFactory.Rect("Paramètres", root);
        UIFactory.Stretch(settingsPanel, 0);
        settingsGroup = settingsPanel.gameObject.AddComponent<CanvasGroup>();
        Image box = UIFactory.Box("Cadre", settingsPanel, new Vector2(0, -270), new Vector2(1000, 540), new Color(0.03f, 0.03f, 0.07f, 0.92f));
        Outline ol = box.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(0.56f, 0.94f, 1f, 0.5f);
        ol.effectDistance = new Vector2(3, 3);
        Text st = UIFactory.Label("Titre", settingsPanel, font, 54, new Color(0.56f, 0.94f, 1f), TextAnchor.MiddleCenter);
        st.rectTransform.anchoredPosition = new Vector2(0, -90);
        st.rectTransform.sizeDelta = new Vector2(800, 80);
        st.fontStyle = FontStyle.Bold;
        st.text = "PARAMÈTRES";
        sliders.Build(settingsPanel, font, -180f);
        Text back = UIFactory.Label("Retour", settingsPanel, font, 36, Color.white, TextAnchor.MiddleCenter);
        back.rectTransform.anchoredPosition = new Vector2(0, -470);
        back.rectTransform.sizeDelta = new Vector2(800, 60);
        back.text = "Z / S : choisir     Q / D : régler     Échap : retour";
        back.fontSize = 28;
        settingsGroup.alpha = 0f;
        settingsPanel.gameObject.SetActive(false);

        fade = UIFactory.Box("Fondu", root, Vector2.zero, Vector2.zero, Color.black);
        UIFactory.Stretch(fade.rectTransform, 0);
    }

    // ------------------------------------------------------------------ boucle
    void Update()
    {
        float t = Time.time;
        // Animation d'ambiance
        book.anchoredPosition = new Vector2(0, 250 + Mathf.Sin(t * 1.5f) * 12f);
        book.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.8f) * 3f);
        float gs = 1100f + Mathf.Sin(t * 1.2f) * 80f;
        glowRect.sizeDelta = new Vector2(gs, gs);
        for (int i = 0; i < fragments.Count; i++)
        {
            Vector3 d = fragData[i];
            float y = Mathf.Repeat(d.y + 560f + t * 25f * d.z, 1120f) - 560f;
            fragments[i].anchoredPosition = new Vector2(d.x + Mathf.Sin(t * d.z + i) * 20f, y);
            fragments[i].localRotation = Quaternion.Euler(0, 0, t * 40f * d.z + i * 20f);
        }
        title.color = Color.Lerp(new Color(0.93f, 0.9f, 0.82f), new Color(0.75f, 0.95f, 1f), 0.5f + 0.5f * Mathf.Sin(t * 1.3f));

        if (starting) return;

        if (inSettings)
        {
            sliders.Tick();
            if (GameInput.CancelDown() || Input.GetKeyDown(KeyCode.Backspace) || Input.GetMouseButtonDown(1)) StartCoroutine(ShowSettings(false));
            return;
        }

        int before = selected;
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) selected = (selected + items.Count - 1) % items.Count;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) selected = (selected + 1) % items.Count;
        int hovered = -1;
        for (int i = 0; i < items.Count; i++)
            if (RectTransformUtility.RectangleContainsScreenPoint(items[i].rectTransform, Input.mousePosition, null)) hovered = i;
        if (hovered >= 0 && (Input.GetAxis("Mouse X") != 0f || Input.GetAxis("Mouse Y") != 0f)) selected = hovered;
        if (selected != before) { AudioManager.Play("menu_deplacer", 0.6f); confirmNewGame = false; }

        bool click = Input.GetMouseButtonDown(0) && hovered >= 0;
        if (click) selected = hovered;
        if (click || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            Choose();

        for (int i = 0; i < items.Count; i++)
        {
            bool sel = i == selected;
            itemScale[i] = Mathf.Lerp(itemScale[i], sel ? 1.15f : 1f, Time.deltaTime * 12f);
            items[i].rectTransform.localScale = Vector3.one * itemScale[i];
            items[i].color = sel ? new Color(0.56f, 0.94f, 1f) : new Color(1f, 1f, 1f, 0.6f);
            string label = labels[i];
            if (sel && confirmNewGame && actions[i] == "nouvelle") label = "Nouvelle partie ? La sauvegarde sera effacée (valider)";
            items[i].text = sel ? "›  " + label + "  ‹" : label;
        }
    }

    void Choose()
    {
        AudioManager.Play("menu_valider", 0.7f);
        switch (actions[selected])
        {
            case "continuer": StartCoroutine(Play(false)); break;
            case "nouvelle":
                if (existingSave != null && !confirmNewGame) { confirmNewGame = true; break; }
                StartCoroutine(Play(true));
                break;
            case "parametres": StartCoroutine(ShowSettings(true)); break;
            default:
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                break;
        }
    }

    IEnumerator FadeIn()
    {
        for (float t = 0; t < 1f; t += Time.deltaTime / 1.5f)
        {
            fade.color = new Color(0, 0, 0, 1f - t);
            yield return null;
        }
        fade.color = new Color(0, 0, 0, 0);
        fade.raycastTarget = false;
    }

    IEnumerator Play(bool newGame)
    {
        starting = true;
        AudioManager.StopMusic(1.2f);
        for (float t = 0; t < 1f; t += Time.deltaTime / 1.2f)
        {
            fade.color = new Color(0, 0, 0, t);
            yield return null;
        }
        if (!newGame && SaveSystem.Continue()) yield break;
        SaveSystem.NewGame();
        if (Application.CanStreamedLevelBeLoaded(firstScene)) SceneManager.LoadScene(firstScene);
        else
        {
            Debug.LogWarning("La scène '" + firstScene + "' n'est pas dans les Build Settings.");
            fade.color = new Color(0, 0, 0, 0);
            starting = false;
        }
    }

    IEnumerator ShowSettings(bool show)
    {
        inSettings = show;
        if (show) { sliders.Reset(); settingsPanel.gameObject.SetActive(true); }
        else mainPanel.gameObject.SetActive(true);
        CanvasGroup from = show ? mainGroup : settingsGroup;
        CanvasGroup to = show ? settingsGroup : mainGroup;
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.25f)
        {
            from.alpha = 1f - t;
            to.alpha = t;
            yield return null;
        }
        from.alpha = 0f;
        to.alpha = 1f;
        from.gameObject.SetActive(false);
    }
}
