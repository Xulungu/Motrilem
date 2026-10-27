using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Trou, terrier, passage secret : on appuie sur E, fondu, et on arrive à un endroit précis d'une autre carte.
public class WarpDoor : Interactable
{
    public string targetScene = "EtangGluant";
    public Vector2 arrival;
    public string label = "Descendre";
    public string toast = "";
    [Tooltip("Vrai : le joueur se glisse dans l'ouverture (il rétrécit et avance vers le centre) avant le fondu")]
    public bool squeeze;

    private bool busy;

    protected override void Awake()
    {
        base.Awake();
        promptText = label;
        promptOffset = new Vector3(0f, 1.4f, 0f);
    }

    protected override void Interact()
    {
        if (busy) return;
        StartCoroutine(Go());
    }

    IEnumerator Go()
    {
        busy = true;
        canInteract = false;
        PlayerMovement pm = player;
        PlayerHealth ph = pm != null ? pm.GetComponent<PlayerHealth>() : null;
        if (pm != null) pm.SetCutscene(true);
        AudioManager.Play("bulle", 0.6f, 0.6f);
        if (squeeze && pm != null)
        {
            // il se faufile dans le trou : il glisse jusqu'au centre en rapetissant
            Rigidbody2D rb = pm.GetComponent<Rigidbody2D>();
            Vector3 start = pm.transform.position, scale0 = pm.transform.localScale;
            float tx = transform.position.x;
            const float T = 0.55f;
            for (float t = 0f; t < T; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / T);
                float x = Mathf.Lerp(start.x, tx, k);
                if (rb != null) { rb.velocity = Vector2.zero; rb.position = new Vector2(x, rb.position.y); }
                pm.transform.position = new Vector3(x, pm.transform.position.y, start.z);
                pm.transform.localScale = scale0 * Mathf.Lerp(1f, 0.55f, k);
                yield return null;
            }
        }
        if (GameUI.HasInstance) yield return GameUI.Instance.FadeTo(Color.black, squeeze ? 0.35f : 0.5f);
        if (Application.CanStreamedLevelBeLoaded(targetScene))
        {
            ZoneBanner.announce = true;
            SaveSystem.SetTransit(targetScene, arrival, ph != null ? ph.CurrentHealth : -1, Vector2.zero, arrival, "", null);
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Debug.LogWarning("La scène '" + targetScene + "' n'est pas dans les Build Settings.");
            if (GameUI.HasInstance) yield return GameUI.Instance.FadeTo(new Color(0, 0, 0, 0), 0.4f);
            if (pm != null) pm.SetCutscene(false);
            canInteract = true;
            busy = false;
        }
    }
}
