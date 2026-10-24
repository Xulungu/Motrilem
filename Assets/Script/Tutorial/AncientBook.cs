using System.Collections;
using UnityEngine;

// Le livre ancien posé sur son socle. En interagissant :
//  1. le livre s'ouvre et raconte la légende des deux frères (LegendBook)
//  2. le protagoniste retrouve la mémoire un instant... puis lévite
//  3. des fragments de souvenirs jaillissent de son corps et sont absorbés par le livre
//  4. le livre se disperse en pages ; une seule reste, devant le joueur (LastPage)
[RequireComponent(typeof(LegendBook))]
public class AncientBook : Interactable
{
    [Header("Visuel du livre")]
    public SpriteRenderer bookRenderer;
    public Sprite closedSprite;
    public Sprite openSprite;
    public SpriteRenderer glowRenderer;
    public Color glowColor = new Color(0.56f, 0.94f, 1f, 0.55f);

    [Header("Effets")]
    public Sprite fragmentSprite;
    public Sprite pageSprite;
    public Sprite glowSprite;
    public Sprite raysSprite;
    public Material additiveMaterial;
    public int fragmentCount = 40;
    public int scatteredPages = 26;
    public float levitationHeight = 1.4f;

    [Header("Suite")]
    public LastPage lastPage;

    [Header("Textes")]
    public string playerName = "???";

    private Vector3 restPosition;
    private bool cutscenePlaying;
    private float glowBoost;
    private float idleBob = 1f;
    private int absorbed;
    private Vector3 punch;

    protected override void Awake()
    {
        base.Awake();
        if (bookRenderer == null) bookRenderer = GetComponent<SpriteRenderer>();
        restPosition = transform.position;
        if (lastPage != null) lastPage.gameObject.SetActive(false);
        promptText = "Lire le livre";
    }

    void Start()
    {
        // Partie reprise : le livre a déjà été lu et s'est dispersé
        if (!SaveSystem.HasFlag("livre_lu")) return;
        cutscenePlaying = false;
        idleBob = 0f;
        canInteract = false;
        if (bookRenderer != null) bookRenderer.enabled = false;
        if (glowRenderer != null) glowRenderer.enabled = false;
        glowColor.a = 0f;
        GetComponent<Collider2D>().enabled = false;
        // La dernière page attend encore devant le socle si la magie n'a pas été choisie
        if (lastPage != null && !SaveSystem.HasFlag("magie_choisie"))
        {
            lastPage.transform.position = restPosition + new Vector3(-1.6f, -1.6f, 0f);
            lastPage.gameObject.SetActive(true);
            lastPage.canInteract = true;
        }
    }

    protected override void Update()
    {
        base.Update();
        // Le livre flotte doucement au-dessus du socle
        if (!cutscenePlaying || idleBob > 0f)
        {
            transform.position = restPosition + Vector3.up * Mathf.Sin(Time.time * 1.8f) * 0.08f * idleBob;
        }
        if (glowRenderer != null)
        {
            Color c = glowColor;
            c.a = glowColor.a * (0.75f + 0.25f * Mathf.Sin(Time.time * 2.5f)) + glowBoost;
            glowRenderer.color = c;
            float s = 1f + glowBoost * 0.8f;
            glowRenderer.transform.localScale = new Vector3(s, s, 1f) * 1.6f;
        }
        punch = Vector3.Lerp(punch, Vector3.zero, Time.deltaTime * 10f);
        if (bookRenderer != null) bookRenderer.transform.localScale = Vector3.one + punch;
    }

    protected override void Interact()
    {
        if (cutscenePlaying) return;
        canInteract = false;
        StartCoroutine(Cutscene(player));
    }

    // ================================================================== cinématique
    IEnumerator Cutscene(PlayerMovement pm)
    {
        cutscenePlaying = true;
        GameUI ui = GameUI.Instance;
        CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        PlayerHealth health = pm.GetComponent<PlayerHealth>();
        if (health != null) health.invulnerable = true;
        SpriteRenderer playerSprite = pm.spriteRenderer;

        pm.SetCutscene(true);
        // Le joueur se tourne vers le livre
        playerSprite.flipX = transform.position.x < pm.transform.position.x;
        float facing = playerSprite.flipX ? -1f : 1f;

        StartCoroutine(ui.Letterbox(true));
        if (cam != null)
        {
            cam.SetFocus((pm.transform.position + transform.position) * 0.5f + Vector3.up * 0.5f);
            cam.SetZoom(4.5f);
        }

        // --- Le livre réagit
        yield return new WaitForSeconds(0.6f);
        yield return ui.Say("Un vieux livre... Il flotte au-dessus de ce socle depuis mon réveil.", playerName);
        yield return ui.Say("Les symboles sur la couverture... On dirait qu'ils m'appellent.", playerName);

        glowBoost = 0.3f;
        if (cam != null) cam.Shake(0.05f, 0.4f);
        if (bookRenderer != null && openSprite != null) bookRenderer.sprite = openSprite;
        AudioManager.Play("livre_ouvre", 0.8f);
        punch = Vector3.one * 0.25f;
        yield return new WaitForSeconds(0.5f);

        // --- La légende
        yield return GetComponent<LegendBook>().Play();
        glowBoost = 0.5f;

        // --- Le souvenir revient... et repart
        yield return ui.Say("Ces deux frères... Je... je les connais.", playerName);
        if (cam != null) cam.Shake(0.08f, 0.6f);
        yield return ui.Say("Je me souviens ! Mon nom est...", playerName);

        // Voile sombre sur le décor (le joueur et les effets restent lumineux)
        SpriteRenderer veil = FxUtil.MakeSprite("Voile", FxUtil.WhiteSprite, cam != null ? cam.transform.position + Vector3.forward * 10f : transform.position, 9980, new Color(0.02f, 0.02f, 0.06f, 0f));
        veil.transform.localScale = new Vector3(60f, 40f, 1f);
        StartCoroutine(FadeSprite(veil, 0.6f, 1.2f));

        // Lévitation
        AudioManager.Play("levitation", 0.8f);
        AudioManager.FadeMusicVolume(0.12f, 1.5f);
        Vector3 groundPos = pm.transform.position;
        Coroutine levitate = StartCoroutine(Levitate(pm, groundPos));
        if (cam != null)
        {
            cam.SetFocus((groundPos + Vector3.up * levitationHeight + transform.position) * 0.5f + Vector3.up * 0.3f);
            cam.Shake(0.06f, 2.5f);
        }

        // Rayons de lumière et aura derrière le joueur (comme quand on obtient un pouvoir)
        Transform body = pm.transform;
        SpriteRenderer rays1 = FxUtil.MakeSprite("Rayons", raysSprite, body.position, 9991, new Color(0.75f, 0.95f, 1f, 0f), additiveMaterial);
        SpriteRenderer rays2 = FxUtil.MakeSprite("Rayons", raysSprite, body.position, 9991, new Color(0.75f, 0.95f, 1f, 0f), additiveMaterial);
        SpriteRenderer aura = FxUtil.MakeSprite("Aura", glowSprite, body.position, 9992, new Color(0.75f, 0.95f, 1f, 0f), additiveMaterial);
        bool effectsAlive = true;
        StartCoroutine(AnimateHalo(body, rays1, rays2, aura, () => effectsAlive));

        yield return new WaitForSeconds(1.2f);
        yield return ui.Flash(new Color(1f, 1f, 1f, 0.8f), 0.35f);

        // --- Les fragments de souvenirs jaillissent et sont absorbés par le livre
        absorbed = 0;
        Material mat = additiveMaterial;
        for (int i = 0; i < fragmentCount; i++)
        {
            Vector3 from = body.position + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.4f, 0.4f), 0f);
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.y < -0.3f) dir.y = -dir.y; // surtout vers le haut et les côtés
            float big = Random.value < 0.2f ? 1.6f : 1f;
            Color col = Color.Lerp(new Color(0.75f, 0.95f, 1f), new Color(1f, 0.95f, 0.8f), Random.value);
            MemoryFragment.Spawn(fragmentSprite, mat, from, dir * Random.Range(3f, 7f), transform, Vector3.zero,
                Random.Range(0.6f, 1.3f), col, big, OnFragmentAbsorbed);
            if (i % 6 == 0 && playerSprite != null) StartCoroutine(PulseSprite(playerSprite));
            yield return new WaitForSeconds(Random.Range(0.03f, 0.09f));
        }

        float timeout = 6f;
        while (absorbed < fragmentCount && timeout > 0f) { timeout -= Time.deltaTime; yield return null; }

        // --- Le livre déborde de souvenirs
        if (cam != null) cam.Shake(0.12f, 1.4f);
        for (float t = 0; t < 1.4f; t += Time.deltaTime)
        {
            glowBoost = Mathf.Lerp(0.8f, 2f, t / 1.4f);
            if (bookRenderer != null)
            {
                bookRenderer.color = Color.Lerp(Color.white, new Color(0.8f, 1f, 1f), t / 1.4f);
                bookRenderer.transform.localPosition = Random.insideUnitCircle * 0.06f * (t / 1.4f);
            }
            yield return null;
        }

        // --- Explosion : le livre se disperse
        AudioManager.Play("explosion_livre", 1f);
        for (int k = 0; k < 4; k++) AudioManager.Play("page", 0.5f, Random.Range(0.8f, 1.3f));
        Vector3 burst = transform.position;
        StartCoroutine(ui.Flash(Color.white, 1.2f));
        if (cam != null) cam.Shake(0.25f, 0.6f);
        for (int i = 0; i < scatteredPages; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            dir.y = Mathf.Abs(dir.y) * 0.8f + 0.2f;
            FlyingPage.Spawn(pageSprite, burst, dir * Random.Range(5f, 12f), Random.Range(1.8f, 3f), 10005);
        }
        if (bookRenderer != null) bookRenderer.enabled = false;
        if (glowRenderer != null) glowRenderer.enabled = false;
        glowBoost = 0f;
        idleBob = 0f;

        yield return new WaitForSeconds(0.8f);

        // --- Une seule page reste, et vient se placer devant le joueur
        Vector3 pageTarget = new Vector3(groundPos.x + facing * 1.6f, groundPos.y + 0.6f, 0f);
        if (lastPage != null)
        {
            lastPage.transform.position = burst;
            lastPage.gameObject.SetActive(true);
            lastPage.canInteract = false;
            yield return lastPage.FloatTo(pageTarget, 2.2f);
        }

        SaveSystem.SetFlag("livre_lu");

        // --- Le protagoniste redescend
        effectsAlive = false;
        StartCoroutine(FadeSprite(veil, 0f, 1.5f, true));
        StopCoroutine(levitate);
        yield return Descend(pm, groundPos);
        if (cam != null) cam.SetFocus((groundPos + pageTarget) * 0.5f + Vector3.up * 0.5f);

        yield return new WaitForSeconds(0.6f);
        yield return ui.Say("...", playerName);
        yield return ui.Say("Qu'est-ce que... Ça m'échappe. Le nom, les visages... tout s'efface.", playerName);
        yield return ui.Say("Le livre a absorbé mes souvenirs... puis il s'est dispersé aux quatre vents.", playerName);
        yield return ui.Say("Il ne reste qu'une page. Elle brille faiblement.", playerName);

        if (cam != null) { cam.ClearFocus(); cam.ResetZoom(); }
        AudioManager.FadeMusicVolume(0.5f, 3f);
        yield return ui.Letterbox(false);
        if (health != null) health.invulnerable = false;
        yield return null;
        pm.SetCutscene(false);
        if (lastPage != null) lastPage.canInteract = true;
        cutscenePlaying = false;
        // Le livre n'existe plus : on coupe sa zone d'interaction
        GetComponent<Collider2D>().enabled = false;
    }

    void OnFragmentAbsorbed()
    {
        absorbed++;
        if (absorbed % 2 == 0) AudioManager.Play("fragment", 0.35f, Random.Range(0.8f, 1.5f));
        punch = Vector3.one * 0.18f;
        glowBoost = Mathf.Min(0.9f, 0.4f + absorbed * 0.015f);
    }

    // ------------------------------------------------------------------ lévitation
    IEnumerator Levitate(PlayerMovement pm, Vector3 groundPos)
    {
        Rigidbody2D rb = pm.rb;
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        pm.externalControl = true;
        float t = 0f;
        while (true)
        {
            t += Time.fixedDeltaTime;
            float up = Ease.InOut(t / 1.6f) * levitationHeight;
            float bob = Mathf.Sin(t * 2.2f) * 0.07f * Mathf.Clamp01(t - 1.6f);
            rb.MovePosition(groundPos + Vector3.up * (up + bob));
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator Descend(PlayerMovement pm, Vector3 groundPos)
    {
        Rigidbody2D rb = pm.rb;
        Vector3 start = rb.position;
        for (float t = 0; t < 1f; t += Time.fixedDeltaTime / 1.4f)
        {
            rb.MovePosition(Vector3.Lerp(start, groundPos + Vector3.up * 0.05f, Ease.InOut(t)));
            yield return new WaitForFixedUpdate();
        }
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.velocity = Vector2.zero;
        pm.externalControl = false;
        if (pm.spriteRenderer != null) pm.spriteRenderer.color = Color.white;
    }

    // ------------------------------------------------------------------ effets
    IEnumerator AnimateHalo(Transform body, SpriteRenderer r1, SpriteRenderer r2, SpriteRenderer aura, System.Func<bool> alive)
    {
        float strength = 0f;
        float t = 0f;
        while (strength > 0.001f || alive())
        {
            t += Time.deltaTime;
            strength = Mathf.MoveTowards(strength, alive() ? 1f : 0f, Time.deltaTime * (alive() ? 0.8f : 1.2f));
            Vector3 p = body.position;
            r1.transform.position = p; r2.transform.position = p; aura.transform.position = p;
            r1.transform.rotation = Quaternion.Euler(0, 0, t * 25f);
            r2.transform.rotation = Quaternion.Euler(0, 0, -t * 18f + 20f);
            float s = 2.2f * strength;
            r1.transform.localScale = new Vector3(s, s, 1f);
            r2.transform.localScale = new Vector3(s * 0.8f, s * 0.8f, 1f);
            float a = 0.45f * strength;
            r1.color = new Color(0.75f, 0.95f, 1f, a);
            r2.color = new Color(1f, 0.95f, 0.85f, a * 0.8f);
            float auraSize = (1.8f + 0.2f * Mathf.Sin(t * 5f)) * strength;
            aura.transform.localScale = new Vector3(auraSize, auraSize, 1f);
            aura.color = new Color(0.75f, 0.95f, 1f, 0.7f * strength);
            yield return null;
        }
        Destroy(r1.gameObject); Destroy(r2.gameObject); Destroy(aura.gameObject);
    }

    IEnumerator PulseSprite(SpriteRenderer sr)
    {
        Color bright = new Color(0.75f, 0.95f, 1f);
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.25f)
        {
            sr.color = Color.Lerp(bright, Color.white, t);
            yield return null;
        }
        sr.color = Color.white;
    }

    IEnumerator FadeSprite(SpriteRenderer sr, float alpha, float duration, bool destroyAfter = false)
    {
        Color start = sr.color;
        Color end = new Color(start.r, start.g, start.b, alpha);
        for (float t = 0; t < 1f; t += Time.deltaTime / duration)
        {
            sr.color = Color.Lerp(start, end, t);
            yield return null;
        }
        sr.color = end;
        if (destroyAfter) Destroy(sr.gameObject);
    }
}
