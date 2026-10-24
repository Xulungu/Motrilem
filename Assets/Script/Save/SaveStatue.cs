using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Statue d'ange : point de sauvegarde. La première fois qu'on l'utilise, elle déploie ses ailes
// (et les garde déployées ensuite : on voit d'un coup d'œil les statues déjà activées).
// Prier devant la statue sauvegarde la partie, rend toute la vie et place le point de réapparition.
public class SaveStatue : Interactable
{
    [Tooltip("Identifiant unique (laisser vide : calculé à partir de la scène et de la position)")]
    public string statueId;
    public SpriteRenderer statueRenderer;
    public SpriteRenderer glowRenderer;
    [Tooltip("Images de l'animation : ailes repliées -> déployées")]
    public Sprite[] frames;

    private bool activated;
    private bool busy;
    private float glowStrength;

    string Id
    {
        get
        {
            if (!string.IsNullOrEmpty(statueId)) return statueId;
            return SceneManager.GetActiveScene().name + "_" + Mathf.RoundToInt(transform.position.x) + "_" + Mathf.RoundToInt(transform.position.y);
        }
    }

    protected override void Awake()
    {
        base.Awake();
        promptText = "Prier (sauvegarder)";
        if (statueRenderer == null) statueRenderer = GetComponentInChildren<SpriteRenderer>();
        if (frames == null || frames.Length == 0)
        {
            frames = new Sprite[4];
            for (int i = 0; i < 4; i++) frames[i] = FxUtil.LoadSprite("statue_" + i);
        }
    }

    void Start()
    {
        activated = SaveSystem.IsStatueActivated(Id);
        SetFrame(activated ? frames.Length - 1 : 0);
        glowStrength = activated ? 1f : 0f;
    }

    void SetFrame(int i)
    {
        if (statueRenderer != null && frames != null && i >= 0 && i < frames.Length && frames[i] != null)
            statueRenderer.sprite = frames[i];
    }

    protected override void Update()
    {
        base.Update();
        if (glowRenderer != null)
        {
            float a = glowStrength * (0.3f + 0.1f * Mathf.Sin(Time.time * 2f));
            glowRenderer.color = new Color(0.6f, 0.9f, 1f, a);
        }
    }

    protected override void Interact()
    {
        if (busy) return;
        StartCoroutine(Use(player));
    }

    IEnumerator Use(PlayerMovement pm)
    {
        busy = true;
        canInteract = false;
        pm.SetCutscene(true);
        PlayerHealth guard = pm.GetComponent<PlayerHealth>();
        if (guard != null) guard.invulnerable = true;
        GameUI ui = GameUI.Instance;
        CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        PlayerProfile.Record(PlayerProfile.Event.Interact);

        if (!activated)
        {
            // --- Première prière : la statue s'éveille et déploie ses ailes
            AudioManager.Play("ailes", 0.8f);
            if (cam != null) cam.Shake(0.04f, 1.2f);
            Vector3 basePos = statueRenderer != null ? statueRenderer.transform.localPosition : Vector3.zero;
            for (int f = 1; f < frames.Length; f++)
            {
                SetFrame(f);
                for (float t = 0; t < 0.28f; t += Time.deltaTime)
                {
                    if (statueRenderer != null) statueRenderer.transform.localPosition = basePos + (Vector3)(Random.insideUnitCircle * 0.02f);
                    glowStrength = Mathf.Min(1f, glowStrength + Time.deltaTime * 1.5f);
                    yield return null;
                }
                Feathers(5);
            }
            if (statueRenderer != null) statueRenderer.transform.localPosition = basePos;
            activated = true;
            SaveSystem.ActivateStatue(Id);
            StartCoroutine(ui.Flash(new Color(0.85f, 0.95f, 1f, 0.6f), 0.6f));
        }

        // --- Sauvegarde : la vie est restaurée
        AudioManager.Play("sauvegarde", 0.7f);
        glowStrength = 2.5f;
        PlayerHealth health = pm.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.Heal(health.maxHealth);
            health.SetSpawnPoint(pm.transform.position);
        }
        Feathers(8);
        bool ok = SaveSystem.Save(pm.transform.position, health != null ? health.CurrentHealth : 100);
        ui.Toast(ok ? "Partie sauvegardée" : "La sauvegarde a échoué...");

        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            glowStrength = Mathf.Lerp(2.5f, 1f, t);
            yield return null;
        }
        glowStrength = 1f;
        yield return null;
        if (guard != null) guard.invulnerable = false;
        pm.SetCutscene(false);
        canInteract = true;
        busy = false;
    }

    void Feathers(int count)
    {
        Sprite s = FxUtil.LoadSprite("eclat");
        if (s == null) return;
        for (int i = 0; i < count; i++)
        {
            Vector3 p = transform.position + new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(0.2f, 1.4f), 0f);
            SimpleParticle.Spawn(s, p, new Vector2(Random.Range(-0.8f, 0.8f), Random.Range(0.5f, 1.5f)), Random.Range(0.8f, 1.4f),
                new Color(0.75f, 0.95f, 1f), Random.Range(0.8f, 1.3f), 10006, FxUtil.Additive, -0.3f, 1f);
        }
    }
}
