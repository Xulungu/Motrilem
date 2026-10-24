using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Écran de choix de l'affinité magique : Feu, Foudre ou Glace.
// Clavier (← → puis E) ou souris (survol + clic). Une confirmation est demandée car le choix est définitif.
public class MagicChoiceUI : MonoBehaviour
{
    public Sprite iconFire;
    public Sprite iconLightning;
    public Sprite iconIce;
    public Sprite glow;

    class Card
    {
        public MagicType type;
        public RectTransform rect;
        public CanvasGroup group;
        public Image frame;
        public Image icon;
        public Image iconGlow;
        public float scale = 1f;
    }

    private RectTransform root;
    private CanvasGroup rootGroup;
    private Text prompt;
    private Card[] cards;
    private int selected = 0;
    private bool confirming;

    static readonly string[] Names = { "FEU", "FOUDRE", "GLACE" };
    static readonly string[] Tags = { "Puissance", "Vitesse", "Contrôle" };
    static readonly string[] Descriptions =
    {
        "Une magie brute et dévastatrice.\nTes sorts embrasent les ennemis et les brûlent sur la durée.",
        "Une magie vive et précise.\nTes sorts frappent en un éclair et rebondissent d'ennemi en ennemi.",
        "Une magie froide et maîtrisée.\nTes sorts ralentissent les ennemis jusqu'à les figer sur place."
    };
    static readonly string[] Articles = { "du feu", "de la foudre", "de la glace" };
    static readonly MagicType[] Types = { MagicType.Feu, MagicType.Foudre, MagicType.Glace };

    // Tant que l'écran est ouvert (et juste après), Échap ne doit pas ouvrir le menu pause
    private static int openCount;
    private static int closedFrame = -100;
    public static bool BlocksPause { get { return openCount > 0 || Time.frameCount - closedFrame <= 2; } }

    public IEnumerator Run(System.Action<MagicType> onChosen)
    {
        openCount++;
        bool cancelled = false;
        Build();
        selected = 1; // la carte du milieu est présélectionnée

        // Apparition : les cartes montent une à une
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.8f)
        {
            rootGroup.alpha = Ease.Out(t * 1.5f);
            for (int i = 0; i < cards.Length; i++)
            {
                float k = Ease.OutBack(Mathf.Clamp01(t * 1.6f - i * 0.2f));
                cards[i].rect.anchoredPosition = new Vector2(CardX(i), Mathf.Lerp(-300f, -20f, k));
                cards[i].group.alpha = Mathf.Clamp01(t * 1.6f - i * 0.2f);
            }
            yield return null;
        }
        for (int i = 0; i < cards.Length; i++) cards[i].rect.anchoredPosition = new Vector2(CardX(i), -20f);

        // Sélection
        yield return null;
        while (true)
        {
            int hovered = MouseOverCard();
            if (!confirming)
            {
                // Échap : on referme sans choisir (la page reste là)
                if (GameInput.CancelDown()) { cancelled = true; break; }
                int before = selected;
                if (GameInput.LeftDown()) selected = (selected + cards.Length - 1) % cards.Length;
                if (GameInput.RightDown()) selected = (selected + 1) % cards.Length;
                if (hovered >= 0 && (Input.GetAxis("Mouse X") != 0f || Input.GetAxis("Mouse Y") != 0f)) selected = hovered;
                if (selected != before) AudioManager.Play("menu_deplacer", 0.6f);

                bool click = Input.GetMouseButtonDown(0) && hovered >= 0;
                if (click) selected = hovered;
                if (click || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                {
                    confirming = true;
                    AudioManager.Play("menu_valider", 0.6f);
                    yield return null;
                }
            }
            else
            {
                bool click = Input.GetMouseButtonDown(0) && hovered == selected;
                if (click || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                    break;
                if (GameInput.CancelDown() || GameInput.LeftDown() || GameInput.RightDown() || (Input.GetMouseButtonDown(0) && hovered != selected))
                {
                    confirming = false;
                    yield return null;
                }
            }

            prompt.text = confirming
                ? "Choisir la magie " + Articles[selected] + " ? Ce choix est définitif.\n[E] Confirmer        [Échap] Revenir"
                : "[← →] Choisir        [E] Valider        [Échap] Plus tard";
            Animate();
            yield return null;
        }

        if (cancelled)
        {
            AudioManager.Play("menu_deplacer", 0.6f, 0.8f);
            prompt.text = "";
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.35f)
            {
                rootGroup.alpha = 1f - t;
                yield return null;
            }
            Destroy(root.gameObject);
            openCount = Mathf.Max(0, openCount - 1);
            closedFrame = Time.frameCount;
            onChosen(MagicType.Aucune);
            yield break;
        }

        // Validation : la carte choisie s'avance, les autres s'effacent
        Card chosen = cards[selected];
        Vector2 from = chosen.rect.anchoredPosition;
        prompt.text = "";
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.9f)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (i == selected) continue;
                cards[i].group.alpha = 1f - t;
            }
            chosen.rect.anchoredPosition = Vector2.Lerp(from, new Vector2(0, 0), Ease.InOut(t));
            float s = Mathf.Lerp(1.08f, 1.25f, Ease.Out(t));
            chosen.rect.localScale = new Vector3(s, s, 1f);
            chosen.iconGlow.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(320f, 700f, t);
            yield return null;
        }
        yield return new WaitForSeconds(0.3f);
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.5f)
        {
            rootGroup.alpha = 1f - t;
            yield return null;
        }
        Destroy(root.gameObject);
        openCount = Mathf.Max(0, openCount - 1);
        closedFrame = Time.frameCount;
        onChosen(chosen.type);
    }

    float CardX(int i) { return (i - 1) * 470f; }

    int MouseOverCard()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(cards[i].rect, Input.mousePosition, null)) return i;
        }
        return -1;
    }

    void Animate()
    {
        float time = Time.time;
        for (int i = 0; i < cards.Length; i++)
        {
            Card c = cards[i];
            bool sel = i == selected;
            float targetScale = sel ? (confirming ? 1.12f : 1.08f) : 0.92f;
            c.scale = Mathf.Lerp(c.scale, targetScale, Time.deltaTime * 12f);
            c.rect.localScale = new Vector3(c.scale, c.scale, 1f);
            c.group.alpha = Mathf.Lerp(c.group.alpha, sel ? 1f : 0.5f, Time.deltaTime * 10f);

            Color col = PlayerMagic.ColorOf(c.type);
            float pulse = sel ? 0.75f + 0.25f * Mathf.Sin(time * (confirming ? 9f : 4f)) : 0.35f;
            c.frame.color = new Color(col.r, col.g, col.b, pulse);
            c.icon.rectTransform.anchoredPosition = new Vector2(0, 130 + (sel ? Mathf.Sin(time * 3f) * 8f : 0f));
            c.iconGlow.color = new Color(col.r, col.g, col.b, sel ? 0.45f + 0.15f * Mathf.Sin(time * 4f) : 0.15f);
        }
    }

    // ------------------------------------------------------------------ construction
    void Build()
    {
        GameUI ui = GameUI.Instance;
        Font font = ui.Font;
        root = UIFactory.Rect("Choix de magie", ui.Root);
        UIFactory.Stretch(root, 0);
        root.SetAsLastSibling();
        rootGroup = root.gameObject.AddComponent<CanvasGroup>();
        rootGroup.alpha = 0f;

        Image dim = UIFactory.Box("Voile", root, Vector2.zero, Vector2.zero, new Color(0.02f, 0.01f, 0.05f, 0.88f));
        UIFactory.Stretch(dim.rectTransform, 0);

        Text title = UIFactory.Label("Titre", root, font, 58, new Color(0.95f, 0.92f, 0.85f), TextAnchor.MiddleCenter);
        title.rectTransform.anchoredPosition = new Vector2(0, 410);
        title.rectTransform.sizeDelta = new Vector2(1600, 80);
        title.text = "Un seul souvenir subsiste...";
        Text sub = UIFactory.Label("Sous-titre", root, font, 30, new Color(0.7f, 0.68f, 0.75f), TextAnchor.MiddleCenter);
        sub.rectTransform.anchoredPosition = new Vector2(0, 350);
        sub.rectTransform.sizeDelta = new Vector2(1600, 50);
        sub.text = "Choisis l'affinité magique de ton personnage";

        prompt = UIFactory.Label("Commandes", root, font, 30, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter);
        prompt.rectTransform.anchoredPosition = new Vector2(0, -420);
        prompt.rectTransform.sizeDelta = new Vector2(1600, 100);

        Sprite[] icons = { iconFire, iconLightning, iconIce };
        cards = new Card[3];
        for (int i = 0; i < 3; i++)
        {
            Card c = new Card();
            c.type = Types[i];
            Color col = PlayerMagic.ColorOf(c.type);

            c.rect = UIFactory.Rect("Carte " + Names[i], root);
            c.rect.sizeDelta = new Vector2(410, 560);
            c.rect.anchoredPosition = new Vector2(CardX(i), -20);
            c.group = c.rect.gameObject.AddComponent<CanvasGroup>();

            c.frame = c.rect.gameObject.AddComponent<Image>();
            c.frame.color = col;
            c.frame.raycastTarget = false;
            Image inner = UIFactory.Box("Fond", c.rect, Vector2.zero, Vector2.zero, new Color(0.07f, 0.06f, 0.11f, 0.97f));
            UIFactory.Stretch(inner.rectTransform, 5);
            UIFactory.Box("Bandeau", c.rect, new Vector2(0, 130), new Vector2(400, 240), new Color(col.r, col.g, col.b, 0.08f));

            c.iconGlow = UIFactory.SpriteImage("Lueur", c.rect, glow, new Vector2(0, 130), 1f, new Color(col.r, col.g, col.b, 0.3f));
            c.iconGlow.rectTransform.sizeDelta = new Vector2(320, 320);
            c.icon = UIFactory.SpriteImage("Icône", c.rect, icons[i], new Vector2(0, 130), 7f, Color.white);
            if (icons[i] == null) { c.icon.color = col; c.icon.rectTransform.sizeDelta = new Vector2(120, 120); }

            Text name = UIFactory.Label("Nom", c.rect, font, 50, col, TextAnchor.MiddleCenter);
            name.fontStyle = FontStyle.Bold;
            name.rectTransform.anchoredPosition = new Vector2(0, -30);
            name.rectTransform.sizeDelta = new Vector2(380, 60);
            name.text = Names[i];

            Text tag = UIFactory.Label("Style", c.rect, font, 26, new Color(0.75f, 0.73f, 0.8f), TextAnchor.MiddleCenter);
            tag.fontStyle = FontStyle.Italic;
            tag.rectTransform.anchoredPosition = new Vector2(0, -80);
            tag.rectTransform.sizeDelta = new Vector2(380, 40);
            tag.text = "— " + Tags[i] + " —";

            Text desc = UIFactory.Label("Description", c.rect, font, 25, new Color(0.9f, 0.88f, 0.85f), TextAnchor.UpperCenter);
            desc.rectTransform.anchoredPosition = new Vector2(0, -185);
            desc.rectTransform.sizeDelta = new Vector2(350, 160);
            desc.lineSpacing = 1.1f;
            desc.text = Descriptions[i];

            cards[i] = c;
        }
    }
}
