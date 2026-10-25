using UnityEngine;
using UnityEngine.UI;

// Trois curseurs de volume (Dialogues / Musique / Effets), utilisés par les menus Paramètres.
// Clavier : Z/S (ou ↑/↓) pour choisir, Q/D (ou ←/→) pour régler. Souris : clic ou glisser sur la barre.
public class VolumeSliders
{
    static readonly string[] Names = { "Dialogues", "Musique", "Effets (magie, pas...)" };
    const float BarW = 460f;

    class Row
    {
        public Text label, value;
        public RectTransform bar;
        public Image fill;
    }

    private readonly Row[] rows = new Row[3];
    private int selected;
    private float lastPlay;
    private int dragging = -1;

    static float Get(int i)
    {
        return i == 0 ? AudioManager.VoiceVolume : i == 1 ? AudioManager.MusicVolume : AudioManager.SfxVolume;
    }

    static void Set(int i, float v)
    {
        v = Mathf.Clamp01(v);
        if (i == 0) AudioManager.VoiceVolume = v;
        else if (i == 1) AudioManager.MusicVolume = v;
        else AudioManager.SfxVolume = v;
    }

    /// <summary>Construit les lignes ; y = position verticale de la première ligne.</summary>
    public void Build(Transform parent, Font font, float y)
    {
        for (int i = 0; i < 3; i++)
        {
            Row r = new Row();
            float ry = y - i * 80f;
            r.label = UIFactory.Label("Nom", parent, font, 32, Color.white, TextAnchor.MiddleRight);
            r.label.rectTransform.anchoredPosition = new Vector2(-330, ry);
            r.label.rectTransform.sizeDelta = new Vector2(420, 50);
            r.label.text = Names[i];
            Image back = UIFactory.Box("Barre", parent, new Vector2(60, ry), new Vector2(BarW, 18), new Color(1f, 1f, 1f, 0.15f));
            r.bar = back.rectTransform;
            r.fill = UIFactory.Box("Remplissage", parent, new Vector2(60 - BarW * 0.5f, ry), new Vector2(BarW, 18), new Color(0.56f, 0.94f, 1f, 0.9f));
            r.fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            r.value = UIFactory.Label("Valeur", parent, font, 32, Color.white, TextAnchor.MiddleLeft);
            r.value.rectTransform.anchoredPosition = new Vector2(380, ry);
            r.value.rectTransform.sizeDelta = new Vector2(120, 50);
            rows[i] = r;
        }
        Refresh();
    }

    public void Reset() { selected = 0; dragging = -1; Refresh(); }

    /// <summary>À appeler à chaque image tant que le panneau est ouvert (fonctionne à l'arrêt du temps).</summary>
    public void Tick()
    {
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) selected = (selected + 2) % 3;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) selected = (selected + 1) % 3;
        float step = 0f;
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) step = -0.1f;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) step = 0.1f;
        if (step != 0f) { Set(selected, Mathf.Round((Get(selected) + step) * 10f) / 10f); Preview(selected); }

        if (Input.GetMouseButtonDown(0))
        {
            dragging = -1;
            for (int i = 0; i < 3; i++)
            {
                Vector2 p = rows[i].bar.position;
                Rect hit = rows[i].bar.rect;
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rows[i].bar, Input.mousePosition, null, out local) &&
                    local.x > hit.xMin - 20 && local.x < hit.xMax + 20 && Mathf.Abs(local.y) < 30) { dragging = i; selected = i; }
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
        for (int i = 0; i < 3; i++)
        {
            Row r = rows[i];
            if (r == null) continue;
            float v = Get(i);
            bool sel = i == selected;
            r.fill.rectTransform.sizeDelta = new Vector2(Mathf.Max(0.01f, BarW * v), 18);
            r.value.text = Mathf.RoundToInt(v * 100f) + " %";
            r.label.color = sel ? new Color(0.56f, 0.94f, 1f) : new Color(1f, 1f, 1f, 0.7f);
            r.label.text = sel ? "›  " + Names[i] : Names[i];
        }
    }
}
