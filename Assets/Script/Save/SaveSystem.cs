using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Données d'une partie (gardées en mémoire entre les scènes, écrites sur le disque aux statues).
[System.Serializable]
public class SaveData
{
    public string scene = "";
    public float x, y;
    public int health = -1;
    public int magic = 0;
    public int maxHealthBonus = 0;
    public int magic2 = 0;          // seconde magie (0 = aucune)
    public bool chargeUnlocked = false; // attaque chargée débloquée
    public List<string> flags = new List<string>();
    public List<string> statues = new List<string>();
    public PlayerProfileData profile = new PlayerProfileData();
    public string date = "";
}

// Système de sauvegarde : un fichier JSON dans le dossier de données du jeu
// (Windows : %USERPROFILE%/AppData/LocalLow/<entreprise>/Motrilem/sauvegarde.json).
public static class SaveSystem
{
    private static SaveData session;
    private static bool pendingApply;
    private static bool skipIntro;

    // Retour d'une scène vers une autre (ex. : sortie du donjon, passage entre deux cartes) : où replacer le joueur
    private static string returnScene = "";
    private static Vector2 returnPos;
    private static int returnHealth = -1;
    private static Vector2 returnVelocity;
    private static bool hasReturnSafe;
    private static Vector2 returnSafe;
    private static string returnLift = "";
    private static float[] returnLiftState;

    public static void SetReturnSpawn(string scene, Vector2 pos) { returnScene = scene; returnPos = pos; returnHealth = -1; returnVelocity = Vector2.zero; hasReturnSafe = false; returnLift = ""; skipIntro = true; }

    /// <summary>Changement de zone : le joueur garde sa vie.</summary>
    public static void SetReturnSpawn(string scene, Vector2 pos, int health) { SetReturnSpawn(scene, pos); returnHealth = health; }

    /// <summary>Passage entre deux cartes : même endroit du monde, même élan, et un sol sûr où réapparaître en cas de chute.</summary>
    public static void SetTransit(string scene, Vector2 pos, int health, Vector2 velocity, Vector2 safeGround, string liftId, float[] liftState)
    {
        SetReturnSpawn(scene, pos, health);
        returnVelocity = velocity;
        hasReturnSafe = true;
        returnSafe = safeGround;
        returnLift = liftId ?? "";
        returnLiftState = liftState;
    }

    public static int ConsumeReturnHealth() { int h = returnHealth; returnHealth = -1; return h; }
    public static Vector2 ConsumeReturnVelocity() { Vector2 v = returnVelocity; returnVelocity = Vector2.zero; return v; }
    public static bool ConsumeReturnSafe(out Vector2 safe) { safe = returnSafe; bool h = hasReturnSafe; hasReturnSafe = false; return h; }

    /// <summary>État d'un ascenseur transmis d'une carte à l'autre (null si ce n'est pas le bon).</summary>
    public static float[] ConsumeLiftState(string liftId)
    {
        if (string.IsNullOrEmpty(returnLift) || returnLift != liftId) return null;
        float[] st = returnLiftState;
        returnLift = ""; returnLiftState = null;
        return st;
    }

    // Dernier point de réapparition (statue) : une mort ramène à la statue, même si elle est sur une autre carte
    public static string CheckpointScene = "";
    public static Vector2 CheckpointPos;
    public static void SetCheckpoint(string scene, Vector2 pos) { CheckpointScene = scene; CheckpointPos = pos; }

    // Objets cassés depuis la dernière sauvegarde (urnes) : ils ne reviennent QUE quand le joueur sauvegarde
    private static readonly HashSet<string> broken = new HashSet<string>();
    public static bool IsBroken(string id) { return broken.Contains(id); }
    public static void MarkBroken(string id) { broken.Add(id); }

    /// <summary>Anciennes sauvegardes : le marécage est découpé en cartes (une scène par groupe de zones).</summary>
    public static string ResolveZoneScene(string scene, float x, float y)
    {
        if (string.IsNullOrEmpty(scene) || !SwampZones.IsSwampScene(scene)) return scene;
        string wanted = SwampZones.SceneAt(x, y);
        return Application.CanStreamedLevelBeLoaded(wanted) ? wanted : scene;
    }

    public static bool ConsumeReturnSpawn(out Vector2 pos)
    {
        pos = returnPos;
        if (string.IsNullOrEmpty(returnScene) || SceneManager.GetActiveScene().name != returnScene) return false;
        returnScene = "";
        return true;
    }

    public static string FilePath { get { return Path.Combine(Application.persistentDataPath, "sauvegarde.json"); } }
    public static bool HasSave { get { return File.Exists(FilePath); } }

    /// <summary>La partie en cours (créée automatiquement si on lance une scène directement dans l'éditeur).</summary>
    public static SaveData Session
    {
        get
        {
            if (session == null) session = new SaveData();
            return session;
        }
    }

    // ------------------------------------------------------------------ partie
    public static void NewGame()
    {
        session = new SaveData();
        pendingApply = false;
        skipIntro = false;
        returnScene = "";
        broken.Clear();
        CheckpointScene = "";
        PlayerPrefs.DeleteKey(PlayerMagic.SaveKey);
        if (HasSave)
        {
            try { File.Delete(FilePath); }
            catch (System.Exception e) { Debug.LogWarning("Impossible d'effacer la sauvegarde : " + e.Message); }
        }
    }

    public static bool HasFlag(string flag) { return Session.flags.Contains(flag); }

    public static void SetFlag(string flag)
    {
        if (!Session.flags.Contains(flag)) Session.flags.Add(flag);
    }

    public static bool IsStatueActivated(string id) { return Session.statues.Contains(id); }

    public static void ActivateStatue(string id)
    {
        if (!Session.statues.Contains(id)) Session.statues.Add(id);
    }

    // ------------------------------------------------------------------ disque
    public static bool Save(Vector3 position, int health)
    {
        SaveData s = Session;
        s.scene = SceneManager.GetActiveScene().name;
        s.x = position.x;
        s.y = position.y;
        s.health = health;
        s.date = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        broken.Clear();   // sauvegarder fait réapparaître les urnes
        PlayerProfile.Record(PlayerProfile.Event.Save);
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(s, true));
            Debug.Log("Partie sauvegardée : " + FilePath + "\n" + PlayerProfile.Describe());
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("La sauvegarde a échoué : " + e.Message);
            return false;
        }
    }

    public static SaveData ReadSave()
    {
        if (!HasSave) return null;
        try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)); }
        catch (System.Exception e) { Debug.LogError("Sauvegarde illisible : " + e.Message); return null; }
    }

    /// <summary>Charge la sauvegarde et ouvre la scène où elle a été faite.</summary>
    public static bool Continue()
    {
        SaveData data = ReadSave();
        if (data == null || string.IsNullOrEmpty(data.scene)) return false;
        data.scene = ResolveZoneScene(data.scene, data.x, data.y);
        broken.Clear();
        CheckpointScene = "";
        if (!Application.CanStreamedLevelBeLoaded(data.scene)) return false;
        session = data;
        if (session.profile == null) session.profile = new PlayerProfileData();
        PlayerPrefs.SetInt(PlayerMagic.SaveKey, data.magic);
        pendingApply = true;
        skipIntro = true;
        SceneManager.LoadScene(data.scene);
        return true;
    }

    /// <summary>Appelé par le joueur au démarrage d'une scène : faut-il le placer à la position sauvegardée ?</summary>
    public static bool ConsumePendingApply()
    {
        if (!pendingApply) return false;
        if (SceneManager.GetActiveScene().name != Session.scene) return false;
        pendingApply = false;
        return true;
    }

    /// <summary>L'intro de la scène doit-elle être sautée (on reprend une partie) ?</summary>
    public static bool ConsumeSkipIntro()
    {
        bool s = skipIntro;
        skipIntro = false;
        return s;
    }
}
