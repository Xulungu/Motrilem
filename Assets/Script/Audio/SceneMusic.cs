using UnityEngine;

// Lance une musique au démarrage de la scène (Assets/Resources/Audio/<musique>).
public class SceneMusic : MonoBehaviour
{
    public string music = "musique_grotte";
    [Range(0f, 1f)] public float volume = 0.5f;
    public float fadeTime = 2f;

    void Start()
    {
        if (string.IsNullOrEmpty(music)) AudioManager.StopMusic(fadeTime);
        else AudioManager.PlayMusic(music, volume, fadeTime);
    }
}
