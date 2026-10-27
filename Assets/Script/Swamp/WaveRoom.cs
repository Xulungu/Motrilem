using System.Collections;
using UnityEngine;

// Salle de vagues : quand le joueur y entre, les grilles se ferment et des vagues d'ennemis arrivent.
// Une fois toutes les vagues vaincues, les grilles s'ouvrent pour de bon (drapeau sauvegardé).
// Si le joueur meurt, la salle se réinitialise.
public class WaveRoom : MonoBehaviour
{
    [System.Serializable]
    public class Spawn
    {
        public int kind;      // 0 petit slime, 1 gros slime, 2 petit moustique, 3 moustique piqueur
        public float x, y;
    }

    [System.Serializable]
    public class Wave
    {
        public Spawn[] spawns;
    }

    public string roomId = "salle1";
    public string roomName = "Salle des vagues";
    public Vector2 min, max;
    public ArenaLock entryLock, exitLock;
    [Tooltip("Là où le joueur réapparaît s'il tombe dans l'acide pendant le combat (à l'intérieur de la salle, jamais derrière la grille)")]
    public Vector2 respawnPoint;
    public bool hasRespawnPoint;
    public Wave[] waves;

    private Transform player;
    private bool started, done;
    private Coroutine routine;
    private int pending;                  // créatures en cours de formation (pas encore créées)
    private PlayerHealth playerHealth;
    private string prevMusic; private float prevVol = 0.5f;

    void CombatMusicOn()
    {
        string cur = AudioManager.CurrentMusic;
        if (cur != "musique_combat") { prevMusic = cur; prevVol = AudioManager.CurrentVolume; }
        AudioManager.PlayMusic("musique_combat", 0.55f, 0.8f);
    }

    void CombatMusicOff()
    {
        if (AudioManager.CurrentMusic != "musique_combat") return;
        if (string.IsNullOrEmpty(prevMusic)) AudioManager.StopMusic(1.5f);
        else AudioManager.PlayMusic(prevMusic, prevVol, 1.5f);
    }

    string Flag { get { return "vagues_" + roomId; } }

    void Start()
    {
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) { player = pm.transform; playerHealth = pm.GetComponent<PlayerHealth>(); }
        if (SaveSystem.HasFlag(Flag)) done = true;
    }

    bool Inside(Vector3 p, float margin)
    {
        return p.x > min.x + margin && p.x < max.x - margin && p.y > min.y - 1f && p.y < max.y + 1f;
    }

    void Update()
    {
        if (done || player == null) return;
        if (!started)
        {
            if (Inside(player.position, 3f)) { started = true; routine = StartCoroutine(Run()); }
            return;
        }
        // Le joueur est mort (il a réapparu loin de la salle) : on remet tout à zéro
        if (player.position.x < min.x - 6f || player.position.x > max.x + 6f) ResetRoom();
    }

    void SetRoomSafe(bool on)
    {
        if (playerHealth == null || !hasRespawnPoint) return;
        playerHealth.hasRoomSafe = on;
        playerHealth.roomSafe = new Vector3(respawnPoint.x, respawnPoint.y, 0f);
    }

    void ResetRoom()
    {
        StopAllCoroutines();                 // la routine de la salle et les formations en cours
        pending = 0;
        SetRoomSafe(false);
        routine = null;
        started = false;
        ClearEnemies();
        if (entryLock != null) entryLock.SetLocked(false);
        if (exitLock != null) exitLock.SetLocked(false);
        CombatMusicOff();
    }

    void ClearEnemies()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        foreach (BossNeedle n in FindObjectsOfType<BossNeedle>()) Destroy(n.gameObject);
    }

    int AliveCount()
    {
        int n = 0;
        foreach (EnemyHealth e in GetComponentsInChildren<EnemyHealth>())
            if (!e.IsDead) n++;
        return n;
    }

    IEnumerator Run()
    {
        yield return new WaitForSeconds(0.4f);
        if (entryLock != null) entryLock.SetLocked(true);
        if (exitLock != null) exitLock.SetLocked(true);
        SetRoomSafe(true);
        GameUI ui = GameUI.HasInstance ? GameUI.Instance : null;
        if (ui != null) ui.PlayTitle(roomName, waves.Length + (waves.Length > 1 ? " vagues" : " vague"));
        AudioManager.Play("bzz", 0.5f, 0.6f);
        CombatMusicOn();
        yield return new WaitForSeconds(1.6f);

        for (int w = 0; w < waves.Length; w++)
        {
            if (ui != null && w > 0) ui.Toast("Vague " + (w + 1) + " / " + waves.Length);
            foreach (Spawn s in waves[w].spawns)
            {
                StartCoroutine(SpawnOne(s));
                yield return new WaitForSeconds(0.3f);
            }
            yield return new WaitForSeconds(0.5f);
            while (pending > 0 || AliveCount() > 0) yield return new WaitForSeconds(0.4f);
            yield return new WaitForSeconds(0.8f);
        }

        done = true;
        started = false;
        SetRoomSafe(false);
        SaveSystem.SetFlag(Flag);
        AudioManager.Play("choix", 0.6f, 0.8f);
        if (ui != null) ui.Toast("Salle vaincue : le passage est ouvert");
        if (entryLock != null) entryLock.SetLocked(false);
        if (exitLock != null) exitLock.SetLocked(false);
        routine = null;
        CombatMusicOff();
    }

    // Une créature se forme à l'endroit prévu (flaque, tourbillon...), puis surgit : elle n'apparaît jamais d'un coup sur le joueur.
    IEnumerator SpawnOne(Spawn sp)
    {
        pending++;
        Vector3 pos = new Vector3(sp.x, sp.y, 0f);
        bool flying = sp.kind >= 2;
        bool big = sp.kind == 1;
        float scale = flying ? (sp.kind == 3 ? 0.075f : 0.06f) : (big ? 1f : 0.6f);
        float dur = SpawnFx.Telegraph(pos, flying, scale, big);
        yield return new WaitForSeconds(dur);
        yield return SpawnFx.WaitClear(pos, flying ? 1.4f : 1.8f, 1.5f);    // si le joueur est pile dessus, la forme attend un instant
        switch (sp.kind)
        {
            case 0: SpawnFx.CreateSlime(pos, false, transform); break;
            case 1: SpawnFx.CreateSlime(pos, true, transform); break;
            case 2: SpawnFx.CreateMinion(pos, false, transform); break;
            default: SpawnFx.CreateMinion(pos, true, transform); break;
        }
        pending--;
    }
}
