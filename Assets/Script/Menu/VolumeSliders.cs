using UnityEngine;
using UnityEngine.UI;

// Trois curseurs de volume (Dialogues / Musique / Effets), utilisés par les menus Paramètres.
// Clavier : Z/S (ou ↑/↓) pour choisir, Q/D (ou ←/→) pour régler. Souris : clic ou glisser sur la barre.
public class VolumeSliders
{
    static readonly string[] Names = { "Dialogues", "Musique", "Effets", "Apparence" };
    const int Count = 4;   // les trois volumes + le choix du skin du personnage
    const float BarW = 400f;
    const float BarX = 40f;     // centre des barres (les noms restent à gauche, sans les toucher)
    const float Spacing = 70f;

    class Row
    {
        public Text label, value;
        public RectTransform bar;
        public Image fill;
        public Image preview;
    }

    private readonly Row[] rows = new Row[Count];
    private int selected;
    private float lastPlay;
    private int dragging = -1;

    static float Get(int i)
    {
        if (i == 3) return PlayerSkin.Current;
        return i == 0 ? AudioManager.VoiceVolume : i == 1 ? AudioManager.MusicVolume : AudioManager.SfxVolume;
    }

    static void Set(int i, float v)
    {
        if (i == 3) { PlayerSkin.Set(Mathf.RoundToInt(v)); return; }
        v = Mathf.Clamp01(v);
        if (i == 0) AudioManager.VoiceVolume = v;
        else if (i == 1) AudioManager.MusicVolume = v;
        else AudioManager.SfxVolume = v;
    }

    /// <summary>Construit les lignes ; y = position verticale de la première ligne.</summary>
    public void Build(Transform parent, Font font, float y)
    {
        for (int i = 0; i < Count; i++)
        {
            Row r = new Row();
            float ry = y - i * Spacing;
            r.label = UIFactory.Label("Nom", parent, font, 32, Color.white, TextAnchor.MiddleRight);
            r.label.rectTransform.anchoredPosition = new Vector2(-340, ry);
            r.label.rectTransform.sizeDelta = new Vector2(300, 50);
            r.label.text = Names[i];
            Image back = UIFactory.Box("Barre", parent, new Vector2(BarX, ry), new Vector2(BarW, 18), new Color(1f, 1f, 1f, 0.15f));
            r.bar = back.rectTransform;
            r.fill = UIFactory.Box("Remplissage", parent, new Vector2(BarX - BarW * 0.5f, ry), new Vector2(BarW, 18), new Color(0.56f, 0.94f, 1f, 0.9f));
            r.fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            r.value = UIFactory.Label("Valeur", parent, font, 32, Color.white, TextAnchor.MiddleLeft);
            r.value.rectTransform.anchoredPosition = new Vector2(380, ry);
            r.value.rectTransform.sizeDelta = new Vector2(170, 50);
            if (i == 3)
            {
                // aperçu du personnage au bout de la barre de couleur
                r.preview = UIFactory.Box("Aperçu", parent, new Vector2(BarX + BarW * 0.5f - 30f, ry), new Vector2(38, 72), Color.white);
                r.preview.preserveAspect = true;
            }
            rows[i] = r;
        }
        Refresh();
    }

    public void Reset() { selected = 0; dragging = -1; Refresh(); }

    /// <summary>À appeler à chaque image tant que le panneau est ouvert (fonctionne à l'arrêt du temps).</summary>
    public void Tick()
    {
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) selected = (selected + Count - 1) % Count;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) selected = (selected + 1) % Count;
        float step = 0f;
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) step = -0.1f;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) step = 0.1f;
        if (step != 0f)
        {
            if (selected == 3) { PlayerSkin.Set(PlayerSkin.Current + (step > 0f ? 1 : -1)); AudioManager.Play("menu_deplacer", 0.8f); }
            else { Set(selected, Mathf.Round((Get(selected) + step) * 10f) / 10f); Preview(selected); }
        }

        if (Input.GetMouseButtonDown(0))
        {
            dragging = -1;
            for (int i = 0; i < Count; i++)
            {
                Vector2 p = rows[i].bar.position;
                Rect hit = rows[i].bar.rect;
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rows[i].bar, Input.mousePosition, null, out local) &&
                    local.x > hit.xMin - 20 && local.x < hit.xMax + 20 && Mathf.Abs(local.y) < 30)
                {
                    selected = i;
                    if (i == 3) { PlayerSkin.Set(PlayerSkin.Current + 1); AudioManager.Play("menu_deplacer", 0.8f); }   // un clic = skin suivant
                    else dragging = i;
                }
            }
        }
        if (Input.GetMouseButtonUp(0) && dragging >= 0) { Preview(dragging); dragging = -1; }
        if (dragging >= 0 && Input.GetMouseButton(0))
        {
            Vector2 local;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rows[dragging].bar, Input.mousePosition, null, out local))
                Set(dragging, Mathf.Round(Mathf.InverseLerp(rows[dragging].bar.rect.xMin, rows[dragging].bar.rect.xMax, local.x) * 20f) / 20f);
        }
        Refresh();
    }

    // petit son témoin pour entendre le réglage
    void Preview(int i)
    {
        if (Time.realtimeSinceStartup - lastPlay < 0.12f) return;
        lastPlay = Time.realtimeSinceStartup;
        if (i == 0) AudioManager.PlayVoice("voix", 0.6f, 1f);
        else if (i == 2) AudioManager.Play("menu_deplacer", 0.8f);
    }

    void Refresh()
    {
        for (int i = 0; i < Count; i++)
        {
            Row r = rows[i];
            if (r == null) continue;
            if (i == 3)
            {
                int k = PlayerSkin.Current;
                r.fill.rectTransform.sizeDelta = new Vector2(BarW - 70f, 18);
                r.fill.color = PlayerSkin.Swatches[k];
                r.value.text = PlayerSkin.Names[k];
                if (r.preview != null) { r.preview.sprite = PlayerSkin.Preview(k); r.preview.enabled = r.preview.sprite != null; }
                bool sel3 = selected == 3;
                r.label.color = sel3 ? new Color(0.56f, 0.94f, 1f) : new Color(1f, 1f, 1f, 0.7f);
                r.label.text = sel3 ? "›  " + Names[i] : Names[i];
                continue;
            }
            float v = Get(i);
            bool sel = i == selected;
            r.fill.rectTransform.sizeDelta = new Vector2(Mathf.Max(0.01f, BarW * v), 18);
            r.value.text = Mathf.RoundToInt(v * 100f) + " %";
            r.label.color = sel ? new Color(0.56f, 0.94f, 1f) : new Color(1f, 1f, 1f, 0.7f);
            r.label.text = sel ? "›  " + Names[i] : Names[i];
        }
    }
}
