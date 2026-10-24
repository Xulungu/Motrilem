using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Le livre ancien ouvert à l'écran : chaque double page raconte un passage de la légende
// des deux frères, avec une illustration animée à gauche et le texte à droite.
public class LegendBook : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite bookSprite;      // livre_ui
    public Sprite mageLight;       // frère aîné (le héros)
    public Sprite mageDark;        // frère cadet (l'antagoniste)
    public Sprite glow;
    public Sprite spark;
    public Sprite closedBook;
    public Sprite fragment;

    [Header("Texte")]
    public float charactersPerSecond = 38f;

    // Le texte est dans le code (et non dans l'Inspector) pour que les mises à jour s'appliquent toujours
    private static readonly string[] pages =
    {
        "Il y a cinq mille ans, deux frères naquirent sous la même étoile.\n\nTous deux reçurent le don de la magie.",
        "L'aîné mit son pouvoir au service des royaumes, et les peuples l'aimèrent.\n\nLe cadet, dans son ombre, ne cessait d'envier sa lumière.",
        "Au fond des ténèbres, le cadet trouva une magie interdite.\n\nElle lui offrit une puissance sans limite... au prix de son âme.",
        "Alors les deux frères s'affrontèrent.\n\nSept jours et sept nuits durant, le ciel brûla, la terre gela et la foudre ne cessa de tomber.",
        "À l'aube du huitième jour, l'aîné rassembla ses dernières forces et scella son frère au plus profond de la terre.\n\nMais le héros ne survécut pas à ce dernier sort.",
        "Les peuples élevèrent des statues à son image, pour que jamais son visage ne soit oublié."
    };

    private const float PX = 7f;   // 1 pixel du sprite = 7 pixels à l'écran
    private RectTransform root, book, illus, flipRight, flipLeft;
    private CanvasGroup group;
    private Image dim;
    private Text text, pageNumber, hint;
    private System.Action<float> animate;
    private float pageTime;

    // ------------------------------------------------------------------ construction
    void BuildUI()
    {
        GameUI ui = GameUI.Instance;
        root = UIFactory.Rect("Livre de la légende", ui.Root);
        UIFactory.Stretch(root, 0);
        group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        dim = UIFactory.Box("Voile", root, Vector2.zero, Vector2.zero, new Color(0.02f, 0.01f, 0.04f, 0.85f));
        UIFactory.Stretch(dim.rectTransform, 0);

        Image bookImg = UIFactory.SpriteImage("Livre", root, bookSprite, new Vector2(0, 20), PX, Color.white);
        book = bookImg.rectTransform;
        if (bookSprite == null) { bookImg.color = new Color(0.86f, 0.77f, 0.59f); book.sizeDelta = new Vector2(1400, 896); }

        // Zones des pages (coordonnées du sprite livre_ui, 200x128 px)
        illus = UIFactory.Rect("Illustration", book);
        illus.anchoredPosition = new Vector2(-329, 0);
        illus.sizeDelta = new Vector2(560, 680);

        text = UIFactory.Label("Texte", book, ui.Font, 38, new Color(0.23f, 0.14f, 0.08f), TextAnchor.MiddleCenter);
        text.rectTransform.anchoredPosition = new Vector2(322, 10);
        text.rectTransform.sizeDelta = new Vector2(520, 640);
        text.lineSpacing = 1.15f;

        pageNumber = UIFactory.Label("Numéro", book, ui.Font, 26, new Color(0.48f, 0.35f, 0.23f), TextAnchor.MiddleCenter);
        pageNumber.rectTransform.anchoredPosition = new Vector2(322, -330);
        pageNumber.rectTransform.sizeDelta = new Vector2(300, 40);

        hint = UIFactory.Label("Suite", root, ui.Font, 28, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        hint.rectTransform.anchoredPosition = new Vector2(0, 50);
        hint.rectTransform.sizeDelta = new Vector2(800, 50);

        // Feuilles qui servent à l'animation de "page tournée"
        Color parchment = new Color(0.86f, 0.77f, 0.59f);
        flipRight = UIFactory.Box("Feuille droite", book, new Vector2(0, 7), new Vector2(630, 770), parchment).rectTransform;
        flipRight.pivot = new Vector2(0f, 0.5f);
        flipRight.anchoredPosition = new Vector2(7, 7);
        flipRight.gameObject.SetActive(false);
        flipLeft = UIFactory.Box("Feuille gauche", book, new Vector2(0, 7), new Vector2(630, 770), parchment).rectTransform;
        flipLeft.pivot = new Vector2(1f, 0.5f);
        flipLeft.anchoredPosition = new Vector2(-7, 7);
        flipLeft.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ déroulé
    public IEnumerator Play()
    {
        if (root == null) BuildUI();
        root.SetAsLastSibling();
        root.gameObject.SetActive(true);
        text.text = "";
        pageNumber.text = "";
        hint.text = "";

        // Ouverture
        AudioManager.Play("livre_ouvre", 0.8f);
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.7f)
        {
            group.alpha = Ease.Out(t);
            float s = Mathf.Lerp(0.6f, 1f, Ease.OutBack(t));
            book.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        group.alpha = 1f;
        book.localScale = Vector3.one;

        for (int i = 0; i < pages.Length; i++)
        {
            if (i > 0) yield return FlipPage(i);
            else ShowSpread(i);

            yield return GameUI.TypeText(text, pages[i], charactersPerSecond, "voix_livre", 0.9f);
            hint.text = i < pages.Length - 1 ? "[E] Tourner la page" : "[E] ...";
            yield return GameInput.WaitForConfirm();
            hint.text = "";
        }

        // Le livre "aspire" ses propres mots : le texte se brouille et disparaît
        yield return Glitch();

        // Fermeture
        AudioManager.Play("livre_ouvre", 0.6f, 0.85f);
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.5f)
        {
            group.alpha = 1f - t;
            float s = Mathf.Lerp(1f, 0.85f, t);
            book.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        group.alpha = 0f;
        ClearIllustration();
        root.gameObject.SetActive(false);
    }

    void Update()
    {
        if (animate != null)
        {
            pageTime += Time.deltaTime;
            animate(pageTime);
        }
    }

    IEnumerator FlipPage(int next)
    {
        // La page de droite se soulève vers la reliure...
        AudioManager.Play("page", 0.8f, Random.Range(0.92f, 1.08f));
        flipRight.gameObject.SetActive(true);
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.28f)
        {
            flipRight.localScale = new Vector3(1f - Ease.In(t), 1f, 1f);
            yield return null;
        }
        flipRight.gameObject.SetActive(false);
        ShowSpread(next);
        // ...puis retombe sur la gauche
        flipLeft.gameObject.SetActive(true);
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.28f)
        {
            flipLeft.localScale = new Vector3(1f - Ease.Out(t), 1f, 1f);
            yield return null;
        }
        flipLeft.gameObject.SetActive(false);
    }

    void ShowSpread(int index)
    {
        ClearIllustration();
        text.text = "";
        pageNumber.text = "— " + (index + 1) + " —";
        pageTime = 0f;
        switch (index)
        {
            case 0: animate = IllusBirth(); break;
            case 1: animate = IllusEnvy(); break;
            case 2: animate = IllusForbidden(); break;
            case 3: animate = IllusBattle(); break;
            case 4: animate = IllusFall(); break;
            default: animate = IllusBook(); break;
        }
    }

    void ClearIllustration()
    {
        animate = null;
        if (illus == null) return;
        for (int i = illus.childCount - 1; i >= 0; i--) Destroy(illus.GetChild(i).gameObject);
    }

    IEnumerator Glitch()
    {
        string original = text.text;
        AudioManager.Play("levitation", 0.5f, 1.3f);
        const string noise = "#%&?!*¤§ø∆¥ΞΨ";
        Vector2 basePos = book.anchoredPosition;
        for (float t = 0; t < 1.6f; t += Time.deltaTime)
        {
            float k = t / 1.6f;
            char[] chars = original.ToCharArray();
            for (int c = 0; c < chars.Length; c++)
            {
                if (chars[c] == '\n' || chars[c] == ' ') continue;
                if (Random.value < k) chars[c] = Random.value < 0.6f ? ' ' : noise[Random.Range(0, noise.Length)];
            }
            text.text = new string(chars);
            text.color = Color.Lerp(new Color(0.23f, 0.14f, 0.08f), new Color(0.3f, 0.75f, 0.9f), k);
            book.anchoredPosition = basePos + Random.insideUnitCircle * 8f * k;
            dim.color = Color.Lerp(new Color(0.02f, 0.01f, 0.04f, 0.85f), new Color(0.1f, 0.25f, 0.3f, 0.9f), k);
            yield return null;
        }
        book.anchoredPosition = basePos;
        text.text = "";
        text.color = new Color(0.23f, 0.14f, 0.08f);
        dim.color = new Color(0.02f, 0.01f, 0.04f, 0.85f);
        yield return GameUI.Instance.Flash(new Color(0.85f, 0.97f, 1f, 1f), 0.5f);
    }

    // ------------------------------------------------------------------ illustrations
    Image Img(string name, Sprite s, Vector2 pos, float scale, Color c)
    {
        return UIFactory.SpriteImage(name, illus, s, pos, scale, c);
    }

    Image Glow(Vector2 pos, float size, Color c)
    {
        Image g = UIFactory.SpriteImage("Lueur", illus, glow, pos, 1f, c);
        g.rectTransform.sizeDelta = new Vector2(size, size);
        return g;
    }

    Image Ground()
    {
        return UIFactory.Box("Sol", illus, new Vector2(0, -200), new Vector2(440, 6), new Color(0.48f, 0.35f, 0.23f, 0.7f));
    }

    System.Action<float> IllusBirth()
    {
        Ground();
        Image gl = Glow(new Vector2(-110, -90), 260, new Color(1f, 0.9f, 0.5f, 0.35f));
        Image gd = Glow(new Vector2(110, -90), 260, new Color(0.7f, 0.5f, 1f, 0.35f));
        Image a = Img("Aîné", mageLight, new Vector2(-110, -88), 8, Color.white);
        Image b = Img("Cadet", mageDark, new Vector2(110, -88), 8, Color.white);
        Image star = Img("Étoile", spark, new Vector2(0, 210), 14, new Color(1f, 0.95f, 0.7f));
        Image starGlow = Glow(new Vector2(0, 210), 200, new Color(1f, 0.95f, 0.7f, 0.5f));
        return t =>
        {
            a.rectTransform.anchoredPosition = new Vector2(-110, -88 + Mathf.Sin(t * 2f) * 4f);
            b.rectTransform.anchoredPosition = new Vector2(110, -88 + Mathf.Sin(t * 2f + 1.5f) * 4f);
            float tw = 0.8f + 0.2f * Mathf.Sin(t * 5f);
            star.rectTransform.localScale = new Vector3(tw, tw, 1f);
            starGlow.color = new Color(1f, 0.95f, 0.7f, 0.35f + 0.2f * Mathf.Sin(t * 3f));
            float pulse = 0.3f + 0.1f * Mathf.Sin(t * 2.5f);
            gl.color = new Color(1f, 0.9f, 0.5f, pulse);
            gd.color = new Color(0.7f, 0.5f, 1f, pulse);
        };
    }

    System.Action<float> IllusEnvy()
    {
        Ground();
        Image gl = Glow(new Vector2(-90, -60), 420, new Color(1f, 0.85f, 0.4f, 0.5f));
        Image a = Img("Aîné", mageLight, new Vector2(-90, -88), 9, Color.white);
        Image b = Img("Cadet", mageDark, new Vector2(150, -100), 7, new Color(0.75f, 0.7f, 0.8f));
        b.rectTransform.localScale = new Vector3(-1f, 1f, 1f); // il se détourne
        List<Image> people = new List<Image>();
        for (int i = 0; i < 4; i++)
        {
            people.Add(UIFactory.Box("Foule", illus, new Vector2(-230 + i * 40, -180), new Vector2(18, 30), new Color(0.48f, 0.35f, 0.23f, 0.5f)));
        }
        return t =>
        {
            gl.color = new Color(1f, 0.85f, 0.4f, 0.4f + 0.15f * Mathf.Sin(t * 2f));
            a.rectTransform.anchoredPosition = new Vector2(-90, -88 + Mathf.Sin(t * 2f) * 4f);
            // le cadet jette des regards vers son frère
            b.rectTransform.localScale = new Vector3(Mathf.Sin(t * 0.9f) > 0.6f ? 1f : -1f, 1f, 1f);
            for (int i = 0; i < people.Count; i++)
                people[i].rectTransform.anchoredPosition = new Vector2(-230 + i * 40, -180 + Mathf.Abs(Mathf.Sin(t * 4f + i)) * 6f);
        };
    }

    System.Action<float> IllusForbidden()
    {
        Image dark = Glow(new Vector2(0, -20), 300, new Color(0.35f, 0.05f, 0.45f, 0.7f));
        Image red = Glow(new Vector2(0, -40), 200, new Color(1f, 0.15f, 0.2f, 0.4f));
        Image b = Img("Cadet", mageDark, new Vector2(0, -60), 8, Color.white);
        List<Image> sparks = new List<Image>();
        for (int i = 0; i < 8; i++) sparks.Add(Img("Étincelle", spark, Vector2.zero, 5, new Color(0.85f, 0.4f, 1f)));
        return t =>
        {
            float grow = Mathf.Clamp01(t / 4f);
            dark.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(300, 620, grow);
            red.color = new Color(1f, 0.15f, 0.2f, 0.3f + 0.25f * Mathf.Abs(Mathf.Sin(t * 3f)));
            float s = Mathf.Lerp(1f, 1.35f, Ease.InOut(grow));
            b.rectTransform.localScale = new Vector3(s, s, 1f);
            b.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(t * 30f) * grow * 2f, -60);
            for (int i = 0; i < sparks.Count; i++)
            {
                float ang = t * 1.6f + i * Mathf.PI * 2f / sparks.Count;
                float r = 150f + 20f * Mathf.Sin(t * 3f + i);
                sparks[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(ang) * r, -40 + Mathf.Sin(ang) * r * 0.7f);
            }
        };
    }

    System.Action<float> IllusBattle()
    {
        Ground();
        Image a = Img("Aîné", mageLight, new Vector2(-170, -88), 8, Color.white);
        Image b = Img("Cadet", mageDark, new Vector2(170, -88), 8, Color.white);
        b.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        Image clash = Glow(new Vector2(0, -40), 260, Color.white);
        Image core = Img("Choc", spark, new Vector2(0, -40), 12, Color.white);
        Color[] elements = { new Color(1f, 0.42f, 0.16f), new Color(1f, 0.88f, 0.3f), new Color(0.62f, 0.91f, 1f) };
        List<Image> sparks = new List<Image>();
        List<Vector2> dirs = new List<Vector2>();
        for (int i = 0; i < 10; i++)
        {
            sparks.Add(Img("Étincelle", spark, Vector2.zero, 4, Color.white));
            dirs.Add(Random.insideUnitCircle.normalized);
        }
        return t =>
        {
            // Le choc change de couleur : feu, foudre, glace
            float phase = t * 0.9f;
            int k = Mathf.FloorToInt(phase) % 3;
            Color c = Color.Lerp(elements[k], elements[(k + 1) % 3], phase - Mathf.Floor(phase));
            clash.color = new Color(c.r, c.g, c.b, 0.75f);
            float pulse = 1f + 0.25f * Mathf.Sin(t * 12f);
            clash.rectTransform.sizeDelta = Vector2.one * 240f * pulse;
            core.color = Color.Lerp(Color.white, c, 0.3f);
            a.rectTransform.anchoredPosition = new Vector2(-170 + Mathf.Sin(t * 25f) * 2f, -88);
            b.rectTransform.anchoredPosition = new Vector2(170 + Mathf.Sin(t * 23f) * 2f, -88);
            for (int i = 0; i < sparks.Count; i++)
            {
                float life = (t * 1.5f + i * 0.17f) % 1f;
                sparks[i].rectTransform.anchoredPosition = new Vector2(0, -40) + dirs[i] * life * 190f;
                sparks[i].color = new Color(c.r, c.g, c.b, 1f - life);
            }
        };
    }

    System.Action<float> IllusFall()
    {
        Ground();
        Image a = Img("Aîné", mageLight, new Vector2(-150, -88), 8, Color.white);
        Image sealGlow = Glow(new Vector2(130, -190), 60, new Color(0.65f, 0.5f, 1f, 0f));
        // Le cadet est placé dans une zone masquée qui s'arrête au niveau du sol :
        // quand il s'enfonce, tout ce qui passe sous le sol disparaît
        RectTransform aboveGround = UIFactory.Rect("Au-dessus du sol", illus);
        aboveGround.anchoredPosition = new Vector2(0, 70);
        aboveGround.sizeDelta = new Vector2(560, 540);
        aboveGround.gameObject.AddComponent<RectMask2D>();
        Image b = UIFactory.SpriteImage("Cadet", aboveGround, mageDark, new Vector2(130, -158), 8, Color.white);
        b.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        List<Image> runes = new List<Image>();
        for (int i = 0; i < 6; i++) runes.Add(Img("Rune", spark, Vector2.zero, 4, new Color(0.75f, 0.6f, 1f, 0f)));
        Image beam = Glow(new Vector2(-10, -60), 40, new Color(1f, 0.95f, 0.7f, 0f));
        Image soul = Img("Âme", spark, new Vector2(-150, -40), 6, new Color(1f, 0.9f, 0.6f, 0f));
        return t =>
        {
            // L'aîné lance le sceau : un rayon de lumière vers son frère
            float cast = Mathf.Clamp01(t / 0.8f);
            beam.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-120, 110, cast), -60);
            beam.color = new Color(1f, 0.95f, 0.7f, cast < 1f ? 0.9f : Mathf.Max(0f, 0.9f - (t - 0.8f)));
            beam.rectTransform.sizeDelta = Vector2.one * (60 + 40 * Mathf.Sin(t * 20f));
            // Un cercle de runes apparaît sous le cadet, qui s'enfonce dans la terre
            float seal = Mathf.Clamp01((t - 0.8f) / 2.2f);
            sealGlow.color = new Color(0.65f, 0.5f, 1f, Mathf.Sin(seal * Mathf.PI) * 0.8f);
            sealGlow.rectTransform.sizeDelta = new Vector2(320, 120) * (0.5f + seal);
            for (int i = 0; i < runes.Count; i++)
            {
                float ang = t * 2.5f + i * Mathf.PI * 2f / runes.Count;
                runes[i].rectTransform.anchoredPosition = new Vector2(130 + Mathf.Cos(ang) * 110f, -190 + Mathf.Sin(ang) * 25f);
                runes[i].color = new Color(0.75f, 0.6f, 1f, Mathf.Sin(seal * Mathf.PI));
            }
            b.rectTransform.anchoredPosition = new Vector2(130 + Mathf.Sin(t * 30f) * 3f * (1f - seal), -158 - Ease.In(seal) * 240f);
            // Puis l'aîné s'éteint, et une petite lumière s'élève
            float fade = Mathf.Clamp01((t - 2.6f) / 1.4f);
            a.color = new Color(1f, 1f, 1f, 1f - fade);
            float rise = Mathf.Clamp01((t - 3f) / 3f);
            soul.rectTransform.anchoredPosition = new Vector2(-150 + Mathf.Sin(t * 2f) * 10f, -40 + rise * 220f);
            soul.color = new Color(1f, 0.9f, 0.6f, Mathf.Sin(rise * Mathf.PI));
        };
    }

    System.Action<float> IllusBook()
    {
        Image g = Glow(new Vector2(-90, 0), 360, new Color(1f, 0.92f, 0.7f, 0.35f));
        // Statue de pierre du héros sur son piédestal
        UIFactory.Box("Piédestal", illus, new Vector2(-90, -205), new Vector2(150, 40), new Color(0.55f, 0.53f, 0.5f));
        UIFactory.Box("Piédestal haut", illus, new Vector2(-90, -180), new Vector2(120, 14), new Color(0.68f, 0.66f, 0.62f));
        Img("Statue du héros", mageLight, new Vector2(-90, -60), 8, new Color(0.62f, 0.62f, 0.66f));
        Image bg = Glow(new Vector2(150, -40), 220, new Color(0.56f, 0.94f, 1f, 0.45f));
        Image bk = Img("Livre", closedBook, new Vector2(150, -40), 7, Color.white);
        List<Image> frags = new List<Image>();
        for (int i = 0; i < 5; i++) frags.Add(Img("Fragment", fragment, Vector2.zero, 4, Color.white));
        return t =>
        {
            g.color = new Color(1f, 0.92f, 0.7f, 0.3f + 0.1f * Mathf.Sin(t * 1.5f));
            bk.rectTransform.anchoredPosition = new Vector2(150, -40 + Mathf.Sin(t * 2f) * 8f);
            bg.color = new Color(0.56f, 0.94f, 1f, 0.35f + 0.15f * Mathf.Sin(t * 3f));
            for (int i = 0; i < frags.Count; i++)
            {
                float ang = t * 1.4f + i * Mathf.PI * 2f / frags.Count;
                frags[i].rectTransform.anchoredPosition = new Vector2(150 + Mathf.Cos(ang) * 110f, -40 + Mathf.Sin(ang) * 60f);
            }
        };
    }
}
