using System.Collections;
using UnityEngine;

// La dernière page du livre. En interagissant avec elle, le joueur choisit son affinité magique.
public class LastPage : Interactable
{
    [Header("Visuel")]
    public SpriteRenderer pageRenderer;
    public SpriteRenderer glowRenderer;
    public Color glowColor = new Color(1f, 0.95f, 0.75f, 0.5f);

    [Header("Choix de magie")]
    public Sprite iconFire;
    public Sprite iconLightning;
    public Sprite iconIce;
    public Sprite glowSprite;
    public Sprite fragmentSprite;
    public Material additiveMaterial;

    [Header("Suite")]
    public MagicBarrier barrier;
    public string playerName = "???";

    private Vector3 hoverPosition;
    private bool hovering;
    private float tint;
    private Color tintColor = Color.white;
    private bool introSeen;

    protected override void Awake()
    {
        base.Awake();
        promptText = "Examiner la page";
        promptOffset = new Vector3(0f, 0.9f, 0f);
        hoverPosition = transform.position;
        hovering = true;
    }

    protected override void Update()
    {
        base.Update();
        if (hovering)
        {
            transform.position = hoverPosition + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.1f;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 1.3f) * 6f);
        }
        if (glowRenderer != null)
        {
            Color c = Color.Lerp(glowColor, tintColor, tint);
            c.a = glowColor.a * (0.7f + 0.3f * Mathf.Sin(Time.time * 3f)) + tint * 0.4f;
            glowRenderer.color = c;
        }
    }

    /// <summary>La page vole jusqu'à une position (utilisé par la cinématique du livre).</summary>
    public IEnumerator FloatTo(Vector3 target, float duration)
    {
        hovering = false;
        Vector3 start = transform.position;
        Vector3 control = (start + target) * 0.5f + Vector3.up * 1.5f;
        transform.localScale = Vector3.zero;
        for (float t = 0; t < 1f; t += Time.deltaTime / duration)
        {
            float e = Ease.InOut(t);
            // courbe de Bézier : la page plane en arc de cercle
            Vector3 a = Vector3.Lerp(start, control, e);
            Vector3 b = Vector3.Lerp(control, target, e);
            transform.position = Vector3.Lerp(a, b, e) + Vector3.right * Mathf.Sin(t * 9f) * 0.15f * (1f - t);
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 7f) * 25f * (1f - t));
            float s = Ease.OutBack(Mathf.Clamp01(t * 3f));
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        transform.position = target;
        transform.localScale = Vector3.one;
        hoverPosition = target;
        hovering = true;
    }

    protected override void Interact()
    {
        canInteract = false;
        StartCoroutine(ChooseMagic(player));
    }

    IEnumerator ChooseMagic(PlayerMovement pm)
    {
        GameUI ui = GameUI.Instance;
        CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        pm.SetCutscene(true);
        StartCoroutine(ui.Letterbox(true));

        if (!introSeen)
        {
            introSeen = true;
            yield return ui.Say("Sur la page, trois symboles brillent faiblement : une flamme, un éclair, un cristal.", playerName);
            yield return ui.Say("Un seul d'entre eux résonne encore en moi...", playerName);
        }

        // --- Choix
        MagicType chosen = MagicType.Aucune;
        MagicChoiceUI choice = gameObject.AddComponent<MagicChoiceUI>();
        choice.iconFire = iconFire;
        choice.iconLightning = iconLightning;
        choice.iconIce = iconIce;
        choice.glow = glowSprite;
        yield return choice.Run(m => chosen = m);
        Destroy(choice);

        // Échap : le joueur n'a rien choisi, la page reste là jusqu'à ce qu'il revienne
        if (chosen == MagicType.Aucune)
        {
            yield return ui.Letterbox(false, 0.3f);
            yield return null;
            pm.SetCutscene(false);
            canInteract = true;
            yield break;
        }

        Color col = PlayerMagic.ColorOf(chosen);
        tintColor = col;
        AudioManager.Play("choix", 0.8f);

        // --- La page s'illumine de la couleur choisie...
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.8f)
        {
            tint = t;
            if (pageRenderer != null) pageRenderer.color = Color.Lerp(Color.white, col, t * 0.6f);
            yield return null;
        }
        if (cam != null) cam.Shake(0.06f, 0.8f);

        // ...puis se dissout en fragments qui rejoignent le joueur
        int count = 22;
        int arrived = 0;
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            MemoryFragment.Spawn(fragmentSprite, additiveMaterial, transform.position, dir * Random.Range(2f, 4.5f),
                pm.transform, new Vector3(0f, 0.1f, 0f), Random.Range(0.3f, 0.7f), col, Random.value < 0.3f ? 1.4f : 1f,
                () => { arrived++; StartCoroutine(Flicker(pm.spriteRenderer, col)); if (arrived % 3 == 0) AudioManager.Play("fragment", 0.3f, Random.Range(1.2f, 1.8f)); });
        }
        if (pageRenderer != null) pageRenderer.enabled = false;
        if (glowRenderer != null) glowRenderer.enabled = false;

        float timeout = 5f;
        while (arrived < count && timeout > 0) { timeout -= Time.deltaTime; yield return null; }

        PlayerMagic magic = pm.GetComponent<PlayerMagic>();
        if (magic == null) magic = pm.gameObject.AddComponent<PlayerMagic>();
        if (magic.auraSprite == null) magic.auraSprite = glowSprite;
        if (magic.auraMaterial == null) magic.auraMaterial = additiveMaterial;
        magic.SetMagic(chosen);
        SaveSystem.SetFlag("magie_choisie");

        yield return ui.Flash(new Color(col.r, col.g, col.b, 0.7f), 0.6f);
        if (pm.spriteRenderer != null) pm.spriteRenderer.color = Color.white;

        switch (chosen)
        {
            case MagicType.Feu:
                yield return ui.Say("Cette chaleur... Oui. Le feu a toujours brûlé en moi.", playerName); break;
            case MagicType.Foudre:
                yield return ui.Say("Ce grondement... La foudre coule encore dans mes veines.", playerName); break;
            default:
                yield return ui.Say("Ce froid, calme et tranchant... La glace m'obéit encore.", playerName); break;
        }
        yield return ui.Say("Si je retrouve les autres pages du livre... peut-être retrouverai-je qui j'étais.", playerName);

        // --- Le sceau qui bloquait la sortie se brise
        if (barrier != null && cam != null)
        {
            cam.SetFocus(barrier.transform.position);
            yield return new WaitForSeconds(1f);
            yield return barrier.Open(col);
            yield return new WaitForSeconds(0.6f);
            cam.ClearFocus();
        }
        else if (barrier != null)
        {
            yield return barrier.Open(col);
        }

        yield return ui.Letterbox(false);
        yield return null;
        pm.SetCutscene(false);
        GetComponent<Collider2D>().enabled = false;
    }

    IEnumerator Flicker(SpriteRenderer sr, Color col)
    {
        if (sr == null) yield break;
        sr.color = Color.Lerp(Color.white, col, 0.7f);
        yield return new WaitForSeconds(0.06f);
        sr.color = Color.white;
    }
}
