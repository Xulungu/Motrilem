using UnityEngine;

public enum MagicType { Aucune = 0, Feu = 1, Foudre = 2, Glace = 3 }

// Affinité magique du joueur (sauvegardée entre les scènes et les parties)
public class PlayerMagic : MonoBehaviour
{
    public const string SaveKey = "Motrilem.Magie";

    public MagicType magic = MagicType.Aucune;
    [Tooltip("Recharger l'affinité sauvegardée au démarrage (désactivé dans le tutoriel, qui repart de zéro)")]
    public bool loadSavedMagic = true;

    [Header("Aura")]
    public Sprite auraSprite;
    public Material auraMaterial;
    public float auraSize = 2.2f;

    private SpriteRenderer aura;
    private float auraStrength;

    public event System.Action<MagicType> OnMagicChanged;

    void Start()
    {
        // La partie en cours fait foi ; sinon (scène lancée directement dans l'éditeur) on lit la dernière magie choisie
        if (SaveSystem.Session.magic != 0) magic = (MagicType)SaveSystem.Session.magic;
        else if (loadSavedMagic) magic = (MagicType)PlayerPrefs.GetInt(SaveKey, 0);
        auraStrength = magic == MagicType.Aucune ? 0f : 1f;
        UpdateAura();
    }

    public void SetMagic(MagicType type)
    {
        magic = type;
        SaveSystem.Session.magic = (int)type;
        PlayerPrefs.SetInt(SaveKey, (int)type);
        PlayerPrefs.Save();
        auraStrength = 2.5f; // petit pic de lumière au moment du choix
        UpdateAura();
        if (OnMagicChanged != null) OnMagicChanged(type);
    }

    void Update()
    {
        if (aura == null) return;
        auraStrength = Mathf.MoveTowards(auraStrength, magic == MagicType.Aucune ? 0f : 1f, Time.deltaTime);
        Color c = ColorOf(magic);
        c.a = 0.28f * auraStrength * (0.8f + 0.2f * Mathf.Sin(Time.time * 3f));
        aura.color = c;
        float s = auraSize * (1f + 0.05f * Mathf.Sin(Time.time * 2f));
        aura.transform.localScale = new Vector3(s / transform.lossyScale.x, s / transform.lossyScale.y, 1f);
    }

    void UpdateAura()
    {
        if (magic == MagicType.Aucune && aura == null) return;
        if (aura == null)
        {
            Sprite s = auraSprite;
            if (s == null) return;
            aura = FxUtil.MakeSprite("Aura magique", s, transform.position, 9990, Color.clear, auraMaterial, transform);
            aura.transform.localPosition = new Vector3(0f, 0f, 0f);
        }
    }

    // ------------------------------------------------------------------ infos
    public static Color ColorOf(MagicType t)
    {
        switch (t)
        {
            case MagicType.Feu: return new Color(1f, 0.42f, 0.16f);
            case MagicType.Foudre: return new Color(1f, 0.88f, 0.3f);
            case MagicType.Glace: return new Color(0.62f, 0.91f, 1f);
            default: return Color.white;
        }
    }

    public static string NameOf(MagicType t)
    {
        switch (t)
        {
            case MagicType.Feu: return "Feu";
            case MagicType.Foudre: return "Foudre";
            case MagicType.Glace: return "Glace";
            default: return "Aucune";
        }
    }
}
