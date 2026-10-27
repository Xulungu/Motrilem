using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sons et musiques du jeu. Les fichiers sont dans Assets/Resources/Audio.
//   AudioManager.Play("page");                 -> bruitage
//   AudioManager.PlayMusic("musique_grotte");  -> musique en boucle (fondu enchaîné)
// L'objet survit aux changements de scène pour que la musique ne coupe pas.
public class AudioManager : MonoBehaviour
{
    public const string MusicVolumeKey = "Motrilem.VolumeMusique";
    public const string SfxVolumeKey = "Motrilem.VolumeSons";
    public const string VoiceVolumeKey = "Motrilem.VolumeDialogues";

    private static AudioManager instance;
    private readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    private AudioSource[] pool;
    private int next;
    private AudioSource musicA, musicB;
    private string currentMusic;
    private Coroutine fade;

    public static float MusicVolume
    {
        get { return PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f); }
        set { PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value)); Instance.ApplyMusicVolume(); }
    }

    public static float VoiceVolume
    {
        get { return PlayerPrefs.GetFloat(VoiceVolumeKey, 1f); }
        set { PlayerPrefs.SetFloat(VoiceVolumeKey, Mathf.Clamp01(value)); }
    }

    public static float SfxVolume
    {
        get { return PlayerPrefs.GetFloat(SfxVolumeKey, 1f); }
        set { PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value)); }
    }

    static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("AudioManager");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<AudioManager>();
                instance.Init();
            }
            return instance;
        }
    }

    void Init()
    {
        pool = new AudioSource[16];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
        }
        musicA = gameObject.AddComponent<AudioSource>();
        musicB = gameObject.AddComponent<AudioSource>();
        foreach (AudioSource m in new[] { musicA, musicB })
        {
            m.loop = true;
            m.playOnAwake = false;
            m.volume = 0f;
        }
    }

    AudioClip Get(string name)
    {
        AudioClip clip;
        if (!cache.TryGetValue(name, out clip))
        {
            clip = Resources.Load<AudioClip>("Audio/" + name);
            if (clip == null) Debug.LogWarning("Son introuvable : Resources/Audio/" + name);
            cache[name] = clip;
        }
        return clip;
    }

    // ------------------------------------------------------------------ bruitages
    public static void Play(string name, float volume = 1f, float pitch = 1f)
    {
        AudioManager am = Instance;
        AudioClip clip = am.Get(name);
        if (clip == null) return;
        AudioSource src = am.pool[am.next];
        am.next = (am.next + 1) % am.pool.Length;
        src.pitch = pitch;
        src.volume = volume * SfxVolume;
        src.clip = clip;
        src.Play();
    }

    /// <summary>Petit bruitage de voix (dialogues, livres) : réglé par le volume « Dialogues ».</summary>
    public static void PlayVoice(string name, float volume = 1f, float pitch = 1f)
    {
        AudioManager am = Instance;
        AudioClip clip = am.Get(name);
        if (clip == null) return;
        AudioSource src = am.pool[am.next];
        am.next = (am.next + 1) % am.pool.Length;
        src.pitch = pitch;
        src.volume = volume * VoiceVolume;
        src.clip = clip;
        src.Play();
    }

    // ------------------------------------------------------------------ musique
    private float musicTarget = 0.5f;

    public static void PlayMusic(string name, float volume = 0.5f, float fadeTime = 2f)
    {
        AudioManager am = Instance;
        if (am.currentMusic == name) return;
        AudioClip clip = am.Get(name);
        if (clip == null) return;
        am.currentMusic = name;
        am.musicTarget = volume;
        if (am.fade != null) am.StopCoroutine(am.fade);
        am.fade = am.StartCoroutine(am.CrossFade(clip, fadeTime));
    }

    public static string CurrentMusic { get { return Instance.currentMusic; } }
    public static float CurrentVolume { get { return Instance.musicTarget; } }

    public static void StopMusic(float fadeTime = 1.5f)
    {
        AudioManager am = Instance;
        am.currentMusic = null;
        if (am.fade != null) am.StopCoroutine(am.fade);
        am.fade = am.StartCoroutine(am.CrossFade(null, fadeTime));
    }

    /// <summary>Change en douceur le volume de la musique en cours (ex. : baisser pendant une cinématique).</summary>
    public static void FadeMusicVolume(float volume, float fadeTime = 1f)
    {
        AudioManager am = Instance;
        am.musicTarget = volume;
        if (am.fade != null) return; // un fondu enchaîné est en cours : il utilisera la nouvelle cible
        am.fade = am.StartCoroutine(am.VolumeFade(fadeTime));
    }

    IEnumerator VolumeFade(float time)
    {
        AudioSource src = musicA.volume >= musicB.volume ? musicA : musicB;
        float start = src.volume;
        float target = musicTarget * MusicVolume;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, time))
        {
            src.volume = Mathf.Lerp(start, target, t);
            yield return null;
        }
        src.volume = target;
        fade = null;
    }

    IEnumerator CrossFade(AudioClip clip, float time)
    {
        AudioSource from = musicA.isPlaying && musicA.volume >= musicB.volume ? musicA : musicB;
        AudioSource to = from == musicA ? musicB : musicA;
        float fromStart = from.volume;
        if (clip != null)
        {
            to.clip = clip;
            to.volume = 0f;
            to.Play();
        }
        float target = musicTarget * MusicVolume;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, time))
        {
            from.volume = Mathf.Lerp(fromStart, 0f, t);
            if (clip != null) to.volume = Mathf.Lerp(0f, target, t);
            yield return null;
        }
        from.volume = 0f;
        from.Stop();
        if (clip != null) to.volume = target;
        fade = null;
    }

    void ApplyMusicVolume()
    {
        float v = musicTarget * MusicVolume;
        if (musicA.isPlaying) musicA.volume = v;
        if (musicB.isPlaying) musicB.volume = v;
    }
}
