using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Écran de choix générique à 2 ou 3 cartes (amélioration de magie après un gardien).
// Clavier (← → puis E) ou souris. Échap : on referme sans choisir (Run renvoie -1).
public class UpgradeChoiceUI : MonoBehaviour
{
    public class Option
    {
        public string title, tag, description;
        public Color color = Color.white;
        public Sprite icon;
    }

    private class Card
    {
        public RectTransform rect;
        public CanvasGroup group;
        public Image frame, icon, iconGlow;
        public float scale = 1f;
    }

    public Sprite glow;

    private RectTransform root;
    private CanvasGroup rootGroup;
    private Text prompt;
    private Card[] cards;
    private List<Option> options;
    private int selected;
    private bool confirming;

    private static int openCount;
    private static int closedFrame = -100;
    public static bool BlocksPause { get { return openCount > 0 || Time.frameCount - closedFrame <= 2; } }

    public IEnumerator Run(string title, string subtitle, List<Option> opts, System.Action<int> onChosen)
    {
        openCount++;
        options = opts;
        confirming = false;
        selected = 0;
        Build(title, subtitle);
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.6f)
        {
            rootGroup.alpha = Ease.Out(t * 1.5f);
            for (int i = 0; i < cards.Length; i++)
            {
                float k = Ease.OutBack(Mathf.Clamp01(t * 1.6f - i * 0.2f));
                cards[i].rect.anchoredPosition = new Vector2(CardX(i), Mathf.Lerp(-300f, -20f, k));
            }
            yield return null;
        }
        for (int i = 0; i < cards.Length; i++) cards[i].rect.anchoredPosition = new Vector2(CardX(i), -20f);
        yield return null;

        int result = -2;
        while (result == -2)
        {
            int hovered = MouseOverCard();
            bool ok = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
            if (!confirming)
            {
                if (GameInput.CancelDown()) { result = -1; break; }
                int before = selected;
                if (GameInput.LeftDown()) selected = (selected + cards.Length - 1) % cards.Length;
                if (GameInput.RightDown()) selected = (selected + 1) % cards.Length;
                if (hovered >= 0 && (Input.GetAxis("Mouse X") != 0f || Input.GetAxis("Mouse Y") != 0f)) selected = hovered;
                if (selected != before) AudioManager.Play("menu_deplacer", 0.6f);
                bool click = Input.GetMouseButtonDown(0) && hovered >= 0;
                if (click) selected = hovered;
                if (click || ok)
                {
                    confirming = true;
                    AudioManager.Play("menu_valider", 0.6f);
                    yield return null;
                }
            }
            else
            {
                bool click = Input.GetMouseButtonDown(0) && hovered == selected;
                if (click || ok) { result = selected; break; }
                if (GameInput.CancelDown() || GameInput.LeftDown() || GameInput.RightDown() || (Input.GetMouseButtonDown(0) && hovered != selected))
                {
                    confirming = false;
                    yield return null;
                }
            }
            prompt.text = confirming
                ? "Choisir « " + options[selected].title + " » ? Ce choix est définitif.\n[E] Confirmer        [Échap] Revenir"
                : "[← →] Choisir        [E] Valider        [Échap] Plus tard";
            Animate();
            yield return null;
        }

        if (result >= 0) AudioManager.Play("choix", 0.7f);
        prompt.text = "";
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.4f)
        {
            rootGroup.alpha = 1f - t;
            yield return null;
        }
        Destroy(root.gameObject);
        openCount = Mathf.Max(0, openCount - 1);
        closedFrame = Time.frameCount;
        onChosen(result);
    }

    float CardX(int i) { return (i - (cards.Length - 1) * 0.5f) * 560f; }

    int MouseOverCard()
    {
        for (int i = 0; i < cards.Length; i++)
            if (RectTransformUtility.RectangleContainsScreenPoint(cards[i].rect, Input.mousePosition, null)) return i;
        return -1;
    }

    void Animate()
    {
        float time = Time.time;
        for (int i = 0; i < cards.Length; i++)
        {
            Card c = cards[i];
            bool sel = i == selected;
            c.scale = Mathf.Lerp(c.scale, sel ? (confirming ? 1.12f : 1.07f) : 0.93f, Time.deltaTime * 12f);
            c.rect.localScale = new Vector3(c.scale, c.scale, 1f);
            c.group.alpha = Mathf.Lerp(c.group.alpha, sel ? 1f : 0.5f, Time.deltaTime * 10f);
            Color col = options[i].color;
            float pulse = sel ? 0.75f + 0.25f * Mathf.Sin(time * (confirming ? 9f : 4f)) : 0.35f;
            c.frame.color = new Color(col.r, col.g, col.b, pulse);
            c.iconGlow.color = new Color(col.r, col.g, col.b, sel ? 0.45f + 0.15f * Mathf.Sin(time * 4f) : 0.15f);
        }
    }

    void Build(string title, string subtitle)
    {
        GameUI ui = GameUI.Instance;
        Font font = ui.Font;
        root = UIFactory.Rect("Choix d'amélioration", ui.Root);
        UIFactory.Stretch(root, 0);
        root.SetAsLastSibling();
        rootGroup = root.gameObject.AddComponent<CanvasGroup>();
        rootGroup.alpha = 0f;

        Image dim = UIFactory.Box("Voile", root, Vector2.zero, Vector2.zero, new Color(0.02f, 0.01f, 0.05f, 0.88f));
        UIFactory.Stretch(dim.rectTransform, 0);

        Text t1 = UIFactory.Label("Titre", root, font, 58, new Color(0.95f, 0.92f, 0.85f), TextAnchor.MiddleCenter);
        t1.rectTransform.anchoredPosition = new Vector2(0, 410);
        t1.rectTransform.sizeDelta = new Vector2(1600, 80);
        t1.text = title;
        Text t2 = UIFactory.Label("Sous-titre", root, font, 30, new Color(0.7f, 0.68f, 0.75f), TextAnchor.MiddleCenter);
        t2.rectTransform.anchoredPosition = new Vector2(0, 350);
        t2.rectTransform.sizeDelta = new Vector2(1600, 50);
        t2.text = subtitle;
        prompt = UIFactory.Label("Commandes", root, font, 30, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter);
        prompt.rectTransform.anchoredPosition = new Vector2(0, -420);
        prompt.rectTransform.sizeDelta = new Vector2(1600, 100);

        cards = new Card[options.Count];
        for (int i = 0; i < cards.Length; i++)
        {
            Option o = options[i];
            Card c = new Card();
            c.rect = UIFactory.Rect("Carte " + o.title, root);
            c.rect.sizeDelta = new Vector2(500, 560);
            c.rect.anchoredPosition = new Vector2(CardX(i), -20);
            c.group = c.rect.gameObject.AddComponent<CanvasGroup>();
            c.frame = c.rect.gameObject.AddComponent<Image>();
            c.frame.color = o.color;
            c.frame.raycastTarget = false;
            Image inner = UIFactory.Box("Fond", c.rect, Vector2.zero, Vector2.zero, new Color(0.07f, 0.06f, 0.11f, 0.97f));
            UIFactory.Stretch(inner.rectTransform, 5);
            UIFactory.Box("Bandeau", c.rect, new Vector2(0, 130), new Vector2(490, 240), new Color(o.color.r, o.color.g, o.color.b, 0.08f));
            c.iconGlow = UIFactory.SpriteImage("Lueur", c.rect, glow, new Vector2(0, 130), 1f, new Color(o.color.r, o.color.g, o.color.b, 0.3f));
            c.iconGlow.rectTransform.sizeDelta = new Vector2(320, 320);
            c.icon = UIFactory.SpriteImage("Icône", c.rect, o.icon, new Vector2(0, 130), 7f, Color.white);
            if (o.icon == null) { c.icon.color = o.color; c.icon.rectTransform.sizeDelta = new Vector2(120, 120); }

            Text name = UIFactory.Label("Nom", c.rect, font, 44, o.color, TextAnchor.MiddleCenter);
            name.fontStyle = FontStyle.Bold;
            name.rectTransform.anchoredPosition = new Vector2(0, -30);
            name.rectTransform.sizeDelta = new Vector2(470, 60);
            name.text = o.title.ToUpper();
            Text tag = UIFactory.Label("Style", c.rect, font, 26, new Color(0.75f, 0.73f, 0.8f), TextAnchor.MiddleCenter);
            tag.fontStyle = FontStyle.Italic;
            tag.rectTransform.anchoredPosition = new Vector2(0, -80);
            tag.rectTransform.sizeDelta = new Vector2(470, 40);
            tag.text = "— " + o.tag + " —";
            Text desc = UIFactory.Label("Description", c.rect, font, 25, new Color(0.9f, 0.88f, 0.85f), TextAnchor.UpperCenter);
            desc.rectTransform.anchoredPosition = new Vector2(0, -185);
            desc.rectTransform.sizeDelta = new Vector2(430, 160);
            desc.lineSpacing = 1.1f;
            desc.text = o.description;
            cards[i] = c;
        }
    }
}
