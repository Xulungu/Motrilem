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

    // Retour d'une scène vers une autre (ex. : sortie du donjon) : où replacer le joueur
    private static string returnScene = "";
    private static Vector2 returnPos;

    public static void SetReturnSpawn(string scene, Vector2 pos) { returnScene = scene; returnPos = pos; skipIntro = true; }

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
        if (data == null || string.IsNullOrEmpty(data.scene) || !Application.CanStreamedLevelBeLoaded(data.scene)) return false;
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
