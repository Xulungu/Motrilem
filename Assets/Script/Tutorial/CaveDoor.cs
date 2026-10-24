using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Porte au bout de la grotte : elle s'ouvre sur la lumière du jour et mène à la scène suivante.
public class CaveDoor : Interactable
{
    public SpriteRenderer doorRenderer;
    public Sprite closedSprite;
    public Sprite openSprite;
    public SpriteRenderer lightRenderer;
    public string sceneToLoad = "SampleScene";
    public string playerName = "???";

    protected override void Awake()
    {
        base.Awake();
        promptText = "Ouvrir la porte";
        promptOffset = new Vector3(0f, 2.1f, 0f);
        if (lightRenderer != null) lightRenderer.color = new Color(1f, 0.95f, 0.8f, 0f);
    }

    protected override void Interact()
    {
        canInteract = false;
        StartCoroutine(Open(player));
    }

    IEnumerator Open(PlayerMovement pm)
    {
        GameUI ui = GameUI.Instance;
        CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        pm.SetCutscene(true);
        PlayerHealth health = pm.GetComponent<PlayerHealth>();
        if (health != null) health.invulnerable = true;
        StartCoroutine(ui.Letterbox(true));

        // La lourde porte tremble puis s'ouvre
        AudioManager.Play("porte", 0.9f);
        if (cam != null) cam.Shake(0.05f, 1.5f);
        Vector3 basePos = doorRenderer != null ? doorRenderer.transform.localPosition : Vector3.zero;
        for (float t = 0; t < 1.5f; t += Time.deltaTime)
        {
            if (doorRenderer != null) doorRenderer.transform.localPosition = basePos + (Vector3)(Random.insideUnitCircle * 0.03f);
            yield return null;
        }
        if (doorRenderer != null)
        {
            doorRenderer.transform.localPosition = basePos;
            if (openSprite != null) doorRenderer.sprite = openSprite;
        }
        StartCoroutine(ui.Flash(new Color(1f, 0.97f, 0.85f, 0.8f), 0.6f));
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.8f)
        {
            if (lightRenderer != null) lightRenderer.color = new Color(1f, 0.95f, 0.8f, 0.7f * t);
            yield return null;
        }

        yield return ui.Say("De la lumière... La lumière du jour !", playerName);
        yield return ui.Say("Après un an dans cette grotte... Il est temps de partir à la recherche des pages.", playerName);

        // Le personnage marche vers la sortie
        pm.externalControl = true;
        Rigidbody2D rb = pm.rb;
        if (pm.spriteRenderer != null) pm.spriteRenderer.flipX = transform.position.x < pm.transform.position.x;
        AudioManager.StopMusic(1.5f);
        StartCoroutine(ui.FadeTo(new Color(1f, 0.98f, 0.92f, 1f), 1.4f));
        Vector2 start = rb.position;
        Vector2 end = new Vector2(transform.position.x, rb.position.y);
        for (float t = 0; t < 1f; t += Time.fixedDeltaTime / 1.4f)
        {
            rb.MovePosition(Vector2.Lerp(start, end, t));
            if (pm.animator != null) pm.animator.SetFloat("Speed", 3f);
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForSeconds(0.3f);

        if (Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("La scène '" + sceneToLoad + "' n'est pas dans les Build Settings.");
            pm.externalControl = false;
            pm.SetCutscene(false);
            yield return ui.FadeTo(new Color(1f, 1f, 1f, 0f), 0.5f);
        }
    }
}
