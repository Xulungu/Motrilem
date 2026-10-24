using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu pause (Échap) : Reprendre / Paramètres / Menu principal / Quitter le jeu.
// Créé automatiquement au lancement du jeu ; il ne s'ouvre que dans les scènes où il y a un joueur.
public class PauseMenu : MonoBehaviour
{
    public const string MenuSceneName = "Menu";

    public static bool IsPaused { get; private set; }
    private static int resumedFrame = -100;
    /// <summary>Les touches du jeu sont ignorées pendant la pause (et l'image où on la quitte).</summary>
    public static bool BlocksInput { get { return IsPaused || Time.frameCount - resumedFrame <= 1; } }

    private static PauseMenu instance;
    private GameObject canvasGO;
    private RectTransform mainPanel, settingsPanel, controlsPanel, openPanel;
    private readonly List<Text> items = new List<Text>();
    private readonly List<float> scales = new List<float>();
    private int selected;
    private bool inSettings;
    private Font font;

    static readonly string[] Labels = { "Reprendre", "Commandes", "Paramètres", "Menu principal", "Quitter le jeu" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (instance != null) return;
        GameObject go = new GameObject("Menu pause");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PauseMenu>();
        SceneManager.sceneLoaded += (s, m) => { if (IsPaused) instance.Resume(); };
    }

    void Update()
    {
        if (!IsPaused)
        {
            if (Input.GetKeyDown(KeyCode.Escape) && CanPause()) Open();
            return;
        }

        if (inSettings)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.E) ||
                Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(1))
                ShowPanel(null);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape)) { Resume(); return; }

        int before = selected;
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) selected = (selected + items.Count - 1) % items.Count;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) selected = (selected + 1) % items.Count;
        int hovered = -1;
        for (int i = 0; i < items.Count; i++)
            if (RectTransformUtility.RectangleContainsScreenPoint(items[i].rectTransform, Input.mousePosition, null)) hovered = i;
        if (hovered >= 0 && (Input.GetAxisRaw("Mouse X") != 0f || Input.GetAxisRaw("Mouse Y") != 0f)) selected = hovered;
        if (selected != before) AudioManager.Play("menu_deplacer", 0.6f);

        bool click = Input.GetMouseButtonDown(0) && hovered >= 0;
        if (click) selected = hovered;
        if (click || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            Choose();

        for (int i = 0; i < items.Count; i++)
        {
            bool sel = i == selected;
            scales[i] = Mathf.Lerp(scales[i], sel ? 1.12f : 1f, Time.unscaledDeltaTime * 12f);
            items[i].rectTransform.localScale = Vector3.one * scales[i];
            items[i].color = sel ? new Color(0.56f, 0.94f, 1f) : new Color(1f, 1f, 1f, 0.6f);
            items[i].text = sel ? "›  " + Labels[i] + "  ‹" : Labels[i];
        }
    }

    static bool CanPause()
    {
        if (SceneManager.GetActiveScene().name == MenuSceneName) return false;
        if (FindObjectOfType<PlayerMovement>() == null) return false;
        if (MagicChoiceUI.BlocksPause) return false; // Échap sert à quitter le choix de magie
        return true;
    }

    void Open()
    {
        if (canvasGO == null) Build();
        IsPaused = true;
        Time.timeScale = 0f;
        selected = 0;
        inSettings = false;
        canvasGO.SetActive(true);
        mainPanel.gameObject.SetActive(true);
        settingsPanel.gameObject.SetActive(false);
        controlsPanel.gameObject.SetActive(false);
        openPanel = null;
        AudioManager.Play("menu_valider", 0.5f, 0.8f);
    }

    public void Resume()
    {
        IsPaused = false;
        resumedFrame = Time.frameCount;
        Time.timeScale = 1f;
        if (canvasGO != null) canvasGO.SetActive(false);
    }

    void Choose()
    {
        AudioManager.Play("menu_valider", 0.6f);
        switch (selected)
        {
            case 0: Resume(); break;
            case 1: ShowPanel(controlsPanel); break;
            case 2: ShowPanel(settingsPanel); break;
            case 3:
                Resume();
                AudioManager.StopMusic(0.8f);
                SceneManager.LoadScene(MenuSceneName);
                break;
            default:
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                break;
        }
    }

    /// <summary>Ouvre un sous-panneau (commandes, paramètres) ou revient au menu (null).</summary>
    void ShowPanel(RectTransform panel)
    {
        if (openPanel != null) openPanel.gameObject.SetActive(false);
        openPanel = panel;
        inSettings = panel != null;
        if (panel != null) panel.gameObject.SetActive(true);
        mainPanel.gameObject.SetActive(panel == null);
    }

    // ------------------------------------------------------------------ interface
    void Build()
    {
        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        canvasGO = new GameObject("Pause UI", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform root = (RectTransform)canvasGO.transform;

        Image dim = UIFactory.Box("Voile", root, Vector2.zero, Vector2.zero, new Color(0.02f, 0.02f, 0.05f, 0.82f));
        UIFactory.Stretch(dim.rectTransform, 0);

        Text title = UIFactory.Label("Titre", root, font, 84, new Color(0.93f, 0.9f, 0.82f), TextAnchor.MiddleCenter);
        title.rectTransform.anchoredPosition = new Vector2(0, 280);
        title.rectTransform.sizeDelta = new Vector2(1200, 120);
        title.fontStyle = FontStyle.Bold;
        title.text = "PAUSE";

        mainPanel = UIFactory.Rect("Principal", root);
        UIFactory.Stretch(mainPanel, 0);
        for (int i = 0; i < Labels.Length; i++)
        {
            Text t = UIFactory.Label(Labels[i], mainPanel, font, 50, Color.white, TextAnchor.MiddleCenter);
            t.rectTransform.anchoredPosition = new Vector2(0, 140 - i * 85);
            t.rectTransform.sizeDelta = new Vector2(900, 80);
            t.text = Labels[i];
            items.Add(t);
            scales.Add(1f);
        }
        Text help = UIFactory.Label("Aide", mainPanel, font, 26, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleCenter);
        help.rectTransform.anchoredPosition = new Vector2(0, -400);
        help.rectTransform.sizeDelta = new Vector2(1600, 40);
        help.text = "Z / S : choisir     E / Entrée : valider     Échap : reprendre";

        settingsPanel = UIFactory.Rect("Paramètres", root);
        UIFactory.Stretch(settingsPanel, 0);
        Image box = UIFactory.Box("Cadre", settingsPanel, new Vector2(0, -20), new Vector2(900, 380), new Color(0.03f, 0.03f, 0.07f, 0.95f));
        Outline ol = box.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(0.56f, 0.94f, 1f, 0.5f);
        ol.effectDistance = new Vector2(3, 3);
        Text st = UIFactory.Label("Titre", settingsPanel, font, 50, new Color(0.56f, 0.94f, 1f), TextAnchor.MiddleCenter);
        st.rectTransform.anchoredPosition = new Vector2(0, 110);
        st.rectTransform.sizeDelta = new Vector2(800, 70);
        st.fontStyle = FontStyle.Bold;
        st.text = "PARAMÈTRES";
        Text sb = UIFactory.Label("Vide", settingsPanel, font, 32, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter);
        sb.rectTransform.anchoredPosition = new Vector2(0, -10);
        sb.rectTransform.sizeDelta = new Vector2(800, 100);
        sb.text = "Rien à régler pour l'instant...";
        Text back = UIFactory.Label("Retour", settingsPanel, font, 34, Color.white, TextAnchor.MiddleCenter);
        back.rectTransform.anchoredPosition = new Vector2(0, -130);
        back.rectTransform.sizeDelta = new Vector2(800, 60);
        back.text = "› Retour (Échap) ‹";
        settingsPanel.gameObject.SetActive(false);

        // Panneau des commandes
        controlsPanel = UIFactory.Rect("Commandes", root);
        UIFactory.Stretch(controlsPanel, 0);
        Image cbox = UIFactory.Box("Cadre", controlsPanel, new Vector2(0, -60), new Vector2(1300, 660), new Color(0.03f, 0.03f, 0.07f, 0.95f));
        Outline col = cbox.gameObject.AddComponent<Outline>();
        col.effectColor = new Color(0.56f, 0.94f, 1f, 0.5f);
        col.effectDistance = new Vector2(3, 3);
        Text ct = UIFactory.Label("Titre", controlsPanel, font, 50, new Color(0.56f, 0.94f, 1f), TextAnchor.MiddleCenter);
        ct.rectTransform.anchoredPosition = new Vector2(0, 180);
        ct.rectTransform.sizeDelta = new Vector2(900, 70);
        ct.fontStyle = FontStyle.Bold;
        ct.text = "COMMANDES";
        string[,] keys =
        {
            { "Se déplacer", "Q / D   ou   ← / →" },
            { "Sauter", "Espace (maintenir = plus haut)" },
            { "Regarder en haut / en bas", "Z / ↑  ou  S / ↓ (maintenir)" },
            { "Descendre des planches", "S ou ↓" },
            { "Viser en haut / bas / diagonale", "Z / S (+ Q / D) en lançant" },
            { "Interagir", "E" },
            { "Lancer un sort", "J, K ou clic gauche" },
            { "Pause", "Échap" }
        };
        for (int i = 0; i < keys.GetLength(0); i++)
        {
            Text l = UIFactory.Label("Action", controlsPanel, font, 32, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleRight);
            l.rectTransform.anchoredPosition = new Vector2(-280, 110 - i * 52);
            l.rectTransform.sizeDelta = new Vector2(560, 50);
            l.text = keys[i, 0];
            Text r = UIFactory.Label("Touches", controlsPanel, font, 32, Color.white, TextAnchor.MiddleLeft);
            r.rectTransform.anchoredPosition = new Vector2(300, 110 - i * 52);
            r.rectTransform.sizeDelta = new Vector2(560, 50);
            r.fontStyle = FontStyle.Bold;
            r.text = keys[i, 1];
        }
        Text cback = UIFactory.Label("Retour", controlsPanel, font, 34, Color.white, TextAnchor.MiddleCenter);
        cback.rectTransform.anchoredPosition = new Vector2(0, -330);
        cback.rectTransform.sizeDelta = new Vector2(800, 60);
        cback.text = "› Retour (Échap) ‹";
        controlsPanel.gameObject.SetActive(false);
    }
}
