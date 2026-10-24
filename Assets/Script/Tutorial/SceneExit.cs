using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Sortie de zone : fondu au noir puis chargement de la scène suivante.
[RequireComponent(typeof(BoxCollider2D))]
public class SceneExit : MonoBehaviour
{
    public string sceneToLoad = "SampleScene";
    private bool used;

    void Reset() { GetComponent<BoxCollider2D>().isTrigger = true; }
    void Awake() { GetComponent<BoxCollider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !other.CompareTag("Player")) return;
        used = true;
        StartCoroutine(Exit(other.GetComponentInParent<PlayerMovement>()));
    }

    IEnumerator Exit(PlayerMovement pm)
    {
        if (pm != null) pm.controlsEnabled = false;
        yield return GameUI.Instance.FadeTo(Color.black, 0.8f);
        if (Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("La scène '" + sceneToLoad + "' n'est pas dans les Build Settings.");
            yield return GameUI.Instance.FadeTo(new Color(0, 0, 0, 0), 0.5f);
            if (pm != null) pm.controlsEnabled = true;
            used = false;
        }
    }
}
