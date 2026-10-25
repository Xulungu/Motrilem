using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Interface générale du jeu, créée automatiquement au premier usage :
//  - fondu / flash plein écran
//  - bandes noires cinématiques
//  - boîte de dialogue (texte qui s'écrit lettre par lettre)
//  - bulle d'aide du tutoriel
public class GameUI : MonoBehaviour
{
    private static GameUI instance;
    public static GameUI Instance
    {
        get
        {
            if (instance == null) Create();
            return instance;
        }
    }

    public static bool HasInstance { get { return instance != null; } }

    public Font Font { get; private set; }
    public RectTransform Root { get; private set; }

    private Image fade;
    private RectTransform barTop, barBottom;
    private CanvasGroup dialogueGroup;
    private RectTransform dialoguePanel;
    private Text dialogueText, dialogueSpeaker, dialogueContinue;
    private CanvasGroup hintGroup;
    private Text hintText;
    private object hintOwner;
    private float hintTargetAlpha;

    public bool IsTalking { get; private set; }

    public float charactersPerSecond = 45f;

    // ------------------------------------------------------------------ création
    static void Create()
    {
        GameObject go = new GameObject("GameUI", typeof(RectTransform));
        instance = go.AddComponent<GameUI>();
        instance.Build();
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Build()
    {
        Font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        Root = (RectTransform)transform;

        // Bandes cinématiques
        barTop = UIFactory.Rect("Bande haut", Root);
        barTop.anchorMin = new Vector2(0, 1); barTop.anchorMax = new Vector2(1, 1); barTop.pivot = new Vector2(0.5f, 1);
        barTop.sizeDelta = new Vector2(0, 0);
        barTop.gameObject.AddComponent<Image>().color = Color.black;
        barBottom = UIFactory.Rect("Bande bas", Root);
        barBottom.anchorMin = new Vector2(0, 0); barBottom.anchorMax = new Vector2(1, 0); barBottom.pivot = new Vector2(0.5f, 0);
        barBottom.sizeDelta = new Vector2(0, 0);
        barBottom.gameObject.AddComponent<Image>().color = Color.black;

        // Bulle d'aide
        RectTransform hint = UIFactory.Rect("Aide", Root);
        hint.anchorMin = hint.anchorMax = new Vector2(0.5f, 0f);
        hint.pivot = new Vector2(0.5f, 0f);
        hint.anchoredPosition = new Vector2(0, 70);
        hint.sizeDelta = new Vector2(1000, 90);
        hint.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.09f, 0.8f);
        hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
        hintGroup.alpha = 0f;
        hintGroup.blocksRaycasts = false;
        hintText = UIFactory.Label("Texte", hint, Font, 32, new Color(0.95f, 0.92f, 0.85f), TextAnchor.MiddleCenter);
        UIFactory.Stretch(hintText.rectTransform, 20);

        // Dialogue
        dialoguePanel = UIFactory.Rect("Dialogue", Root);
        dialoguePanel.anchorMin = dialoguePanel.anchorMax = new Vector2(0.5f, 0f);
        dialoguePanel.pivot = new Vector2(0.5f, 0f);
        dialoguePanel.anchoredPosition = new Vector2(0, 150);
        dialoguePanel.sizeDelta = new Vector2(1300, 210);
        Image frame = dialoguePanel.gameObject.AddComponent<Image>();
        frame.color = new Color(0.75f, 0.85f, 1f, 0.35f);
        RectTransform inner = UIFactory.Rect("Fond", dialoguePanel);
        UIFactory.Stretch(inner, 3);
        inner.gameObject.AddComponent<Image>().color = new Color(0.03f, 0.03f, 0.07f, 0.92f);
        dialogueGroup = dialoguePanel.gameObject.AddComponent<CanvasGroup>();
        dialogueGroup.alpha = 0f;
        dialogueGroup.blocksRaycasts = false;
        dialogueSpeaker = UIFactory.Label("Nom", dialoguePanel, Font, 28, new Color(0.56f, 0.94f, 1f), TextAnchor.UpperLeft);
        dialogueSpeaker.rectTransform.anchorMin = new Vector2(0, 1); dialogueSpeaker.rectTransform.anchorMax = new Vector2(1, 1);
        dialogueSpeaker.rectTransform.pivot = new Vector2(0.5f, 1);
        dialogueSpeaker.rectTransform.offsetMin = new Vector2(40, -60); dialogueSpeaker.rectTransform.offsetMax = new Vector2(-40, -18);
        dialogueSpeaker.fontStyle = FontStyle.Bold;
        dialogueText = UIFactory.Label("Texte", dialoguePanel, Font, 36, Color.white, TextAnchor.MiddleLeft);
        UIFactory.Stretch(dialogueText.rectTransform, 40);
        dialogueText.rectTransform.offsetMax = new Vector2(-40, -50);
        dialogueText.lineSpacing = 1.1f;
        dialogueContinue = UIFactory.Label("Suite", dialoguePanel, Font, 24, new Color(1f, 1f, 1f, 0.7f), TextAnchor.LowerRight);
        UIFactory.Stretch(dialogueContinue.rectTransform, 16);
        dialogueContinue.text = "[E] Continuer";

        // Fondu (au-dessus de tout)
        RectTransform fadeRect = UIFactory.Rect("Fondu", Root);
        UIFactory.Stretch(fadeRect, 0);
        fade = fadeRect.gameObject.AddComponent<Image>();
        fade.color = new Color(0, 0, 0, 0);
        fade.raycastTarget = false;
    }

    void Update()
    {
        if (hintGroup != null)
            hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, hintTargetAlpha, Time.unscaledDeltaTime * 4f);
        if (dialogueContinue != null && IsTalking)
        {
            Color c = dialogueContinue.color;
            c.a = 0.4f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f));
            dialogueContinue.color = c;
        }
    }

    // ------------------------------------------------------------------ fondu / flash
    public void SetFade(Color color)
    {
        fade.transform.SetAsLastSibling();
        fade.color = color;
    }

    public IEnumerator FadeTo(Color color, float duration)
    {
        fade.transform.SetAsLastSibling();
        Color start = fade.color;
        if (start.a <= 0.001f) start = new Color(color.r, color.g, color.b, 0f);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            fade.color = Color.Lerp(start, color, Mathf.Clamp01(t / duration));
            yield return null;
        }
        fade.color = color;
    }

    public IEnumerator Flash(Color color, float duration)
    {
        fade.transform.SetAsLastSibling();
        Color transparent = new Color(color.r, color.g, color.b, 0f);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            fade.color = Color.Lerp(color, transparent, Mathf.Clamp01(t / duration));
            yield return null;
        }
        fade.color = transparent;
    }

    // ------------------------------------------------------------------ bandes noires
    public IEnumerator Letterbox(bool on, float duration = 0.6f)
    {
        float from = barTop.sizeDelta.y;
        float to = on ? 120f : 0f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float h = Mathf.Lerp(from, to, Ease.InOut(t / duration));
            barTop.sizeDelta = new Vector2(0, h);
            barBottom.sizeDelta = new Vector2(0, h);
            yield return null;
        }
        barTop.sizeDelta = new Vector2(0, to);
        barBottom.sizeDelta = new Vector2(0, to);
    }

    // ------------------------------------------------------------------ dialogue
    public IEnumerator Say(string text, string speaker = null)
    {
        IsTalking = true;
        dialoguePanel.SetAsLastSibling();
        dialogueSpeaker.text = speaker ?? "";
        dialogueText.text = "";
        dialogueContinue.enabled = false;

        // Apparition
        for (float t = 0; t < 1f; t += Time.deltaTime * 6f) { dialogueGroup.alpha = t; yield return null; }
        dialogueGroup.alpha = 1f;

        yield return TypeText(dialogueText, text, charactersPerSecond);

        dialogueContinue.enabled = true;
        yield return GameInput.WaitForConfirm();
        dialogueContinue.enabled = false;

        for (float t = 1f; t > 0; t -= Time.deltaTime * 6f) { dialogueGroup.alpha = t; yield return null; }
        dialogueGroup.alpha = 0f;
        IsTalking = false;
    }

    /// <summary>Écrit un texte lettre par lettre. Appuyer sur Confirmer affiche tout d'un coup.</summary>
    public static IEnumerator TypeText(Text target, string text, float cps, string voice = "voix", float voicePitch = 1f)
    {
        target.text = "";
        yield return null; // évite que la touche qui a lancé le texte le saute directement
        float shown = 0f;
        int lastShown = 0;
        int voiced = 0;
        while (shown < text.Length)
        {
            if (GameInput.ConfirmDown()) break;
            shown += cps * Time.deltaTime;
            int count = Mathf.Min(text.Length, Mathf.FloorToInt(shown));
            // petit bruitage de "voix" toutes les deux lettres
            for (int i = lastShown; i < count; i++)
            {
                char c = text[i];
                if (char.IsLetterOrDigit(c) && (voiced++ % 2 == 0) && !string.IsNullOrEmpty(voice))
                    AudioManager.PlayVoice(voice, 0.35f, voicePitch * Random.Range(0.9f, 1.12f));
            }
            lastShown = count;
            target.text = text.Substring(0, count);
            yield return null;
        }
        target.text = text;
        yield return null;
    }

    /// <summary>Affiche un grand titre ; l'animation tourne sur l'interface elle-même
    /// (elle continue même si l'objet qui l'a demandée disparaît).</summary>
    public void PlayTitle(string title, string subtitle = null)
    {
        StartCoroutine(ShowTitle(title, subtitle));
    }

    /// <summary>Petite notification discrète en haut à droite (ex. : « Partie sauvegardée »).</summary>
    public void Toast(string text)
    {
        StartCoroutine(ToastRoutine(text));
    }

    IEnumerator ToastRoutine(string text)
    {
        RectTransform rt = UIFactory.Rect("Notification", Root);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-40, -40);
        rt.sizeDelta = new Vector2(520, 60);
        CanvasGroup g = rt.gameObject.AddComponent<CanvasGroup>();
        g.blocksRaycasts = false;
        Text t = UIFactory.Label("Texte", rt, Font, 30, new Color(0.85f, 0.95f, 1f), TextAnchor.MiddleRight);
        UIFactory.Stretch(t.rectTransform, 0);
        t.fontStyle = FontStyle.Italic;
        t.text = text;
        Shadow sh = t.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.7f);
        for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / 0.4f) { g.alpha = k; yield return null; }
        g.alpha = 1f;
        for (float k = 0; k < 1.8f; k += Time.unscaledDeltaTime) yield return null;
        for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / 0.8f) { g.alpha = 1f - k; yield return null; }
        Destroy(rt.gameObject);
    }

    // ------------------------------------------------------------------ titre de zone
    /// <summary>Grand titre au centre de l'écran (nom de la zone), qui apparaît puis s'efface.</summary>
    public IEnumerator ShowTitle(string title, string subtitle = null)
    {
        RectTransform rt = UIFactory.Rect("Titre de zone", Root);
        UIFactory.Stretch(rt, 0);
        CanvasGroup g = rt.gameObject.AddComponent<CanvasGroup>();
        g.alpha = 0f;
        g.blocksRaycasts = false;
        Text t = UIFactory.Label("Titre", rt, Font, 86, new Color(0.95f, 0.93f, 0.85f), TextAnchor.MiddleCenter);
        t.rectTransform.anchoredPosition = new Vector2(0, 160);
        t.rectTransform.sizeDelta = new Vector2(1600, 120);
        t.fontStyle = FontStyle.Bold;
        t.text = title;
        Shadow sh = t.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.7f);
        sh.effectDistance = new Vector2(4, -4);
        Image line = UIFactory.Box("Trait", rt, new Vector2(0, 95), new Vector2(0, 3), new Color(1f, 1f, 1f, 0.7f));
        if (!string.IsNullOrEmpty(subtitle))
        {
            Text st = UIFactory.Label("Sous-titre", rt, Font, 32, new Color(0.8f, 0.95f, 0.75f), TextAnchor.MiddleCenter);
            st.rectTransform.anchoredPosition = new Vector2(0, 55);
            st.rectTransform.sizeDelta = new Vector2(1600, 50);
            st.fontStyle = FontStyle.Italic;
            st.text = subtitle;
        }
        for (float k = 0; k < 1f; k += Time.deltaTime / 1f)
        {
            g.alpha = k;
            line.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(0, 700, Ease.Out(k)), 3);
            yield return null;
        }
        g.alpha = 1f;
        yield return new WaitForSeconds(2.2f);
        for (float k = 0; k < 1f; k += Time.deltaTime / 1.2f)
        {
            g.alpha = 1f - k;
            yield return null;
        }
        Destroy(rt.gameObject);
    }

    // ------------------------------------------------------------------ aide
    public void ShowHint(object owner, string text)
    {
        hintOwner = owner;
        hintText.text = text;
        hintTargetAlpha = 1f;
    }

    public void HideHint(object owner)
    {
        if (owner == null || owner == hintOwner) hintTargetAlpha = 0f;
    }

    public void ShowHintFor(string text, float seconds)
    {
        object owner = new object();
        ShowHint(owner, text);
        StartCoroutine(HideLater(owner, seconds));
    }

    IEnumerator HideLater(object owner, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        HideHint(owner);
    }
}

// ---------------------------------------------------------------------- outils
public static class GameInput
{
    // Clavier AZERTY (ZQSD) et QWERTY (WASD) + flèches
    public static bool LeftHeld() { if (PauseMenu.BlocksInput) return false; return Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow); }
    public static bool RightHeld() { if (PauseMenu.BlocksInput) return false; return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow); }
    public static bool DownHeld() { if (PauseMenu.BlocksInput) return false; return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow); }
    public static bool UpHeld() { if (PauseMenu.BlocksInput) return false; return Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow); }

    /// <summary>-1 (bas), 0 ou 1 (haut) : sert à regarder et à viser.</summary>
    public static float Vertical()
    {
        float v = 0f;
        if (UpHeld()) v += 1f;
        if (DownHeld()) v -= 1f;
        return v;
    }

    public static float Horizontal()
    {
        float h = 0f;
        if (LeftHeld()) h -= 1f;
        if (RightHeld()) h += 1f;
        return h;
    }

    public static bool JumpDown()
    {
        if (PauseMenu.BlocksInput) return false;
        return Input.GetKeyDown(KeyCode.Space); // seul Espace fait sauter (Z / ↑ servent à regarder et viser)
    }

    public static bool JumpHeld()
    {
        if (PauseMenu.BlocksInput) return false;
        return Input.GetKey(KeyCode.Space);
    }

    public static bool DownPressed() { if (PauseMenu.BlocksInput) return false; return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow); }
    public static bool UpPressed() { if (PauseMenu.BlocksInput) return false; return Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow); }

    public static bool AttackDown()
    {
        if (PauseMenu.BlocksInput) return false;
        return Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K) || Input.GetMouseButtonDown(0);
    }

    /// <summary>Changer de magie (si le joueur en possède deux).</summary>
    public static bool SwitchMagicDown()
    {
        if (PauseMenu.BlocksInput) return false;
        return Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.R);
    }

    public static bool AttackHeld()
    {
        if (PauseMenu.BlocksInput) return false;
        return Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.K) || Input.GetMouseButton(0);
    }

    public static bool ConfirmDown()
    {
        if (PauseMenu.BlocksInput) return false;
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
    }

    public static bool InteractDown()
    {
        if (PauseMenu.BlocksInput) return false;
        return Input.GetKeyDown(KeyCode.E);
    }

    public static bool CancelDown()
    {
        if (PauseMenu.BlocksInput) return false;
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetMouseButtonDown(1);
    }

    public static bool LeftDown()
    {
        if (PauseMenu.BlocksInput) return false;
        return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Q);
    }

    public static bool RightDown()
    {
        if (PauseMenu.BlocksInput) return false;
        return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
    }

    public static IEnumerator WaitForConfirm()
    {
        yield return null;
        while (!ConfirmDown()) yield return null;
        yield return null;
    }
}

public static class Ease
{
    public static float InOut(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
    public static float Out(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t); }
    public static float In(float t) { t = Mathf.Clamp01(t); return t * t; }
    public static float OutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}

public static class UIFactory
{
    public static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static void Stretch(RectTransform rt, float margin)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);
    }

    public static Text Label(string name, Transform parent, Font font, int size, Color color, TextAnchor anchor)
    {
        RectTransform rt = Rect(name, parent);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    public static Image SpriteImage(string name, Transform parent, Sprite sprite, Vector2 pos, float pixelScale, Color color)
    {
        RectTransform rt = Rect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sprite != null ? sprite.rect.size * pixelScale : new Vector2(10, 10) * pixelScale;
        return img;
    }

    public static Image Box(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform rt = Rect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return img;
    }
}

public static class FxUtil
{
    private static Sprite white;
    private static Material spriteMat;

    /// <summary>Sprite blanc carré de 1 unité (pour voiles, éclairs...).</summary>
    public static Sprite WhiteSprite
    {
        get
        {
            if (white == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                white = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }
            return white;
        }
    }

    private static Material additive;
    private static bool additiveLoaded;

    /// <summary>Matériau additif (lueurs), dans Assets/Resources/Additif.mat. Repli : matériau de sprite normal.</summary>
    public static Material Additive
    {
        get
        {
            if (!additiveLoaded)
            {
                additiveLoaded = true;
                additive = Resources.Load<Material>("Additif");
            }
            return additive != null ? additive : SpriteMaterial;
        }
    }

    private static readonly System.Collections.Generic.Dictionary<string, Sprite> spriteCache = new System.Collections.Generic.Dictionary<string, Sprite>();

    /// <summary>Charge un sprite de Assets/Resources/Sprites.</summary>
    public static Sprite LoadSprite(string name)
    {
        Sprite s;
        if (spriteCache.TryGetValue(name, out s) && s != null) return s;
        s = Resources.Load<Sprite>("Sprites/" + name);
        spriteCache[name] = s;
        return s;
    }

    public static Material SpriteMaterial
    {
        get
        {
            if (spriteMat == null) spriteMat = new Material(Shader.Find("Sprites/Default"));
            return spriteMat;
        }
    }

    public static SpriteRenderer MakeSprite(string name, Sprite sprite, Vector3 position, int order, Color color, Material material = null, Transform parent = null)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = position;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        sr.color = color;
        if (material != null) sr.sharedMaterial = material;
        return sr;
    }
}
