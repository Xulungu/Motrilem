using UnityEngine;
using UnityEngine.UI;

// Barre de vie progressive :
//  - la barre colorée descend en douceur jusqu'à la nouvelle valeur
//  - une barre blanche "fantôme" reste un instant derrière puis rattrape (montre les dégâts subis)
//  - la couleur passe du vert au rouge selon la vie restante
public class HealthBar : MonoBehaviour
{
    public Image fill;
    public Image ghost;
    public Gradient gradient = DefaultGradient();

    [Tooltip("Vitesse de la barre principale (fraction de la barre par seconde)")]
    public float fillSpeed = 1.5f;
    [Tooltip("Délai avant que la barre fantôme rattrape")]
    public float ghostDelay = 0.4f;
    public float ghostSpeed = 0.8f;

    private float maxHealth = 100f;
    private float targetRatio = 1f;
    private float fillRatio = 1f;
    private float ghostRatio = 1f;
    private float ghostTimer;

    static Gradient DefaultGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(new Color(0.85f, 0.15f, 0.15f), 0f),
                new GradientColorKey(new Color(0.95f, 0.75f, 0.1f), 0.5f),
                new GradientColorKey(new Color(0.25f, 0.8f, 0.3f), 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }

    public void SetMaxHealth(int max)
    {
        maxHealth = Mathf.Max(1, max);
        targetRatio = fillRatio = ghostRatio = 1f;
        Apply();
    }

    public void SetHealth(int health)
    {
        float newRatio = Mathf.Clamp01(health / maxHealth);
        if (newRatio < targetRatio) ghostTimer = ghostDelay;      // dégâts : le fantôme attend
        else ghostRatio = Mathf.Max(ghostRatio, newRatio);         // soin : pas de fantôme
        targetRatio = newRatio;
    }

    void Update()
    {
        fillRatio = Mathf.MoveTowards(fillRatio, targetRatio, fillSpeed * Time.deltaTime);

        if (ghostTimer > 0f) ghostTimer -= Time.deltaTime;
        else ghostRatio = Mathf.MoveTowards(ghostRatio, fillRatio, ghostSpeed * Time.deltaTime);

        Apply();
    }

    void Apply()
    {
        if (fill != null)
        {
            fill.rectTransform.anchorMax = new Vector2(fillRatio, 1f);
            fill.color = gradient.Evaluate(fillRatio);
        }
        if (ghost != null) ghost.rectTransform.anchorMax = new Vector2(Mathf.Max(ghostRatio, fillRatio), 1f);
    }

    // ---------- Création automatique de l'interface ----------
    public static HealthBar CreateDefault()
    {
        GameObject canvasGO = new GameObject("HUD (Health)");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Cadre
        RectTransform frame = NewRect("HealthBar", canvasGO.transform);
        frame.anchorMin = frame.anchorMax = new Vector2(0f, 1f);
        frame.pivot = new Vector2(0f, 1f);
        frame.anchoredPosition = new Vector2(40f, -40f);
        frame.sizeDelta = new Vector2(420f, 42f);
        frame.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.9f);

        // Zone intérieure (marge de 5 px)
        RectTransform inner = NewRect("Inner", frame);
        Stretch(inner, 5f);
        inner.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.05f, 0.05f, 1f);

        Image ghost = NewBar("Ghost", inner, new Color(1f, 1f, 1f, 0.85f));
        Image fill = NewBar("Fill", inner, Color.green);

        HealthBar bar = frame.gameObject.AddComponent<HealthBar>();
        bar.ghost = ghost;
        bar.fill = fill;
        return bar;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt, float margin)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);
    }

    static Image NewBar(string name, Transform parent, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;   // anchorMax.x sert de "remplissage"
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }
}
