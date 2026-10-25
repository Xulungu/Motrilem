using UnityEngine;

// Zone qui change la musique quand le joueur y entre (fondu enchaîné).
[RequireComponent(typeof(BoxCollider2D))]
public class MusicZone : MonoBehaviour
{
    public string music = "musique_etang";
    [Range(0f, 1f)] public float volume = 0.5f;
    public float fadeTime = 3f;

    void Reset() { GetComponent<BoxCollider2D>().isTrigger = true; }

    void Awake() { GetComponent<BoxCollider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        AudioManager.PlayMusic(music, volume, fadeTime);
    }
}
