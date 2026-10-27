using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Page du livre qui apparaît quand le gardien est vaincu. Elle s'affiche en grand : un texte sur le héros,
// puis elle améliore la magie que le joueur possède (attaque chargée : maintenir l'attaque, puis relâcher).
public class BossUpgradePage : Interactable
{
    public string requiredFlag = "boss_moustique";
    public string takenFlag = "amelioration_prise";
    public GameObject visual;
    public SpriteRenderer pageRenderer, glowRenderer;
    public Sprite glowSprite, fragmentSprite;
    public Material additiveMaterial;
    public string playerName = "???";

    private bool shown, busy;
    private Vector3 basePos;

    static readonly string Title = "Le Mage de Lumière";
    static readonly string[] Pages =
    {
        "On dit qu'il est venu un soir, d'au-delà des montagnes, avec un livre vide pour tout bagage et une toute petite bête sur l'épaule.\n\n" +
        "Il n'a demandé ni toit ni récompense. Il a seulement posé la main sur l'eau noire du marécage, et l'eau s'est mise à briller.",
        "Chaque soir, il écrivait une page. « La lumière, disait-il, ne dure que si quelqu'un la raconte. »\n\n" +
        "Il parlait à son bâton comme on parle à un vieil ami. Et parfois, tard dans la nuit, à quelqu'un qui n'était pas là.",
        "Ceux qui l'ont connu disent que son sourire était la seule chose qu'il n'a jamais su écrire."
    };

    protected override void Awake()
    {
        base.Awake();
        promptText = "Lire la page";
        promptOffset = new Vector3(0f, 1.1f, 0f);
        basePos = transform.position;
        if (visual != null) visual.SetActive(false);
        canInteract = false;
    }

    protected override void Update()
    {
        bool ready = SaveSystem.HasFlag(requiredFlag) && !SaveSystem.HasFlag(takenFlag);
        if (ready && !shown && !busy) StartCoroutine(Appear());
        if (shown && !SaveSystem.HasFlag(takenFlag))
        {
            transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.1f;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 1.3f) * 6f);
            if (glowRenderer != null)
            {
                Color c = new Color(1f, 0.95f, 0.75f, 0.5f);
                c.a *= 0.7f + 0.3f * Mathf.Sin(Time.time * 3f);
                glowRenderer.color = c;
            }
        }
        base.Update();
    }

    IEnumerator Appear()
    {
        shown = true;
        if (Time.timeSinceLevelLoad > 3f) yield return new WaitForSeconds(4f); // laisse passer l'explosion
        while (CaveCollapse.Busy) yield return null; // ... et l'éboulement
        if (visual != null)
        {
            visual.SetActive(true);
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.2f)
            {
                float s = Ease.OutBack(Mathf.Clamp01(t));
                visual.transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            visual.transform.localScale = Vector3.one;
        }
        AudioManager.Play("fragment", 0.5f, 0.8f);
        canInteract = true;
    }

    protected override void Interact()
    {
        if (busy) return;
        busy = true;
        canInteract = false;
        StartCoroutine(Read(player));
    }

    IEnumerator Read(PlayerMovement pm)
    {
        GameUI ui = GameUI.Instance;
        pm.SetCutscene(true);
        StartCoroutine(ui.Letterbox(true));
        PlayerMagic magic = pm.GetComponent<PlayerMagic>();
        MagicType current = magic != null ? magic.magic : MagicType.Aucune;
        Color col = current == MagicType.Aucune ? new Color(1f, 0.95f, 0.75f) : PlayerMagic.ColorOf(current);

        yield return ui.Say("Une nouvelle page. Les lettres s'y dessinent d'elles-mêmes, comme si elle m'attendait.", playerName);

        // --- la page s'affiche en grand
        RectTransform root = UIFactory.Rect("Page lue", ui.Root);
        UIFactory.Stretch(root, 0);
        root.SetAsLastSibling();
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        Image dim = UIFactory.Box("Voile", root, Vector2.zero, Vector2.zero, new Color(0.02f, 0.01f, 0.05f, 0.9f));
        UIFactory.Stretch(dim.rectTransform, 0);
        if (glowSprite != null)
        {
            Image halo = UIFactory.SpriteImage("Lueur", root, glowSprite, new Vector2(0, 0), 1f, new Color(col.r, col.g, col.b, 0.25f));
            halo.rectTransform.sizeDelta = new Vector2(1500, 1100);
        }
        Image paper = UIFactory.Box("Page", root, new Vector2(0, 0), new Vector2(1000, 800), new Color(0.86f, 0.79f, 0.62f, 1f));
        Outline ol = paper.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(0.35f, 0.25f, 0.12f, 1f);
        ol.effectDistance = new Vector2(6, 6);
        Image inner = UIFactory.Box("Marge", root, new Vector2(0, 0), new Vector2(940, 740), new Color(0.9f, 0.84f, 0.68f, 1f));
        Text title = UIFactory.Label("Titre", root, ui.Font, 58, new Color(0.25f, 0.15f, 0.08f), TextAnchor.MiddleCenter);
        title.fontStyle = FontStyle.Bold;
        title.rectTransform.anchoredPosition = new Vector2(0, 290);
        title.rectTransform.sizeDelta = new Vector2(860, 80);
        title.text = Title;
        Text body = UIFactory.Label("Texte", root, ui.Font, 36, new Color(0.2f, 0.12f, 0.07f), TextAnchor.UpperLeft);
        body.rectTransform.anchoredPosition = new Vector2(0, -20);
        body.rectTransform.sizeDelta = new Vector2(820, 520);
        body.lineSpacing = 1.15f;
        Text hint = UIFactory.Label("Aide", root, ui.Font, 28, new Color(0.35f, 0.25f, 0.15f, 0.9f), TextAnchor.MiddleCenter);
        hint.rectTransform.anchoredPosition = new Vector2(0, -350);
        hint.rectTransform.sizeDelta = new Vector2(860, 40);

        for (float t = 0; t < 1f; t += Time.deltaTime / 0.7f) { group.alpha = Ease.Out(t); yield return null; }
        group.alpha = 1f;
        AudioManager.Play("fragment", 0.5f, 0.9f);

        for (int i = 0; i < Pages.Length; i++)
        {
            hint.text = "";
            yield return GameUI.TypeText(body, Pages[i], 38f, "voix_livre", 0.9f);
            hint.text = i < Pages.Length - 1 ? "[E] Tourner la page" : "[E] Refermer";
            yield return null;
            while (!(GameInput.ConfirmDown() || GameInput.InteractDown())) yield return null;
            AudioManager.Play("menu_valider", 0.5f, 0.8f);
        }
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.5f) { group.alpha = 1f - t; yield return null; }
        Destroy(root.gameObject);

        // --- la page résonne dans le héros : elle pulse, de plus en plus fort, au rythme de son cœur
        CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        Vector3 pageScale = visual != null ? visual.transform.localScale : Vector3.one;
        yield return ui.Say("Les mots résonnent en moi... comme l'écho de ma propre voix.", playerName);
        for (float t = 0f; t < 2.4f; t += Time.deltaTime)
        {
            float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(t * (3f + t * 2f))), 4f);
            if (visual != null) visual.transform.localScale = pageScale * (1f + 0.25f * beat * (t / 2.4f + 0.3f));
            if (glowRenderer != null) glowRenderer.color = new Color(col.r, col.g, col.b, 0.4f + 0.5f * beat);
            if (pageRenderer != null) pageRenderer.color = Color.Lerp(Color.white, col, Mathf.Clamp01(t / 2.4f) * 0.6f);
            if (cam != null && beat > 0.9f) cam.Shake(0.03f + 0.04f * (t / 2.4f), 0.12f);
            if (beat > 0.95f && Random.value < 0.5f) AudioManager.Play("fragment", 0.4f, 0.6f + 0.3f * (t / 2.4f));
            yield return null;
        }
        if (visual != null) visual.transform.localScale = pageScale;

        // --- le héros absorbe la page : elle se dissout en fragments qui rejoignent le joueur
        AudioManager.Play("choix", 0.8f);
        if (pageRenderer != null)
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.7f) { pageRenderer.color = Color.Lerp(Color.white, col, t * 0.6f); yield return null; }
        int count = 20, arrived = 0;
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            MemoryFragment.Spawn(fragmentSprite, additiveMaterial, transform.position, dir * Random.Range(2f, 4.5f),
                pm.transform, new Vector3(0f, 0.1f, 0f), Random.Range(0.3f, 0.7f), col, Random.value < 0.3f ? 1.4f : 1f, () => { arrived++; });
        }
        if (visual != null) visual.SetActive(false);
        float timeout = 5f;
        while (arrived < count && timeout > 0) { timeout -= Time.deltaTime; yield return null; }

        SaveSystem.Session.chargeUnlocked = true;
        SaveSystem.SetFlag(takenFlag);
        yield return ui.Flash(new Color(col.r, col.g, col.b, 0.7f), 0.6f);
        yield return ui.Say("Ma magie gronde, plus profonde... Si je retiens mon souffle avant de lancer le sort...", playerName);
        ui.Toast("Maintiens l'attaque pour charger ton sort, puis relâche");
        yield return ui.Letterbox(false);
        yield return null;
        pm.SetCutscene(false);
        GetComponent<Collider2D>().enabled = false;
        busy = false;
    }
}
