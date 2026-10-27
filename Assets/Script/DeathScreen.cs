using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Mort du joueur : le personnage se fige, un éclat blanc, puis il s'effondre et se dissout en lueurs ;
// l'écran s'éteint en fondu sur noir, puis l'écran « Vous êtes mort » propose de réapparaître ou de quitter vers le menu.
public static class DeathScreen
{
    /// <summary>Vrai pendant toute la séquence (empêche d'ouvrir le menu pause).</summary>
    public static bool Active { get; private set; }
    /// <summary>Choix du joueur sur l'écran de mort.</summary>
    public static bool ChoseMenu { get; private set; }

    static readonly string[] Lines =
    {
        "Le marécage ne t'a pas encore gardé.",
        "Respire. Recommence.",
        "L'acide n'oublie rien, mais toi, tu peux apprendre.",
        "Ce n'était qu'un faux pas."
    };

    public static IEnumerator Play(PlayerHealth owner, SpriteRenderer graphics, Rigidbody2D rb)
    {
        Active = true;
        ChoseMenu = false;
        Transform root = owner.transform;
        float gravity = rb != null ? rb.gravityScale : 1f;
        if (rb != null) { rb.velocity = Vector2.zero; rb.gravityScale = 0f; }
        PlayerSkin skin = root.GetComponent<PlayerSkin>();
        if (skin != null) skin.enabled = false;

        // ---- animation : une « dépouille » copie le dernier aspect du personnage, le vrai sprite se cache
        GameObject corpse = null;
        SpriteRenderer csr = null;
        float dir = 1f;
        if (graphics != null)
        {
            corpse = new GameObject("Mort du joueur");
            corpse.transform.position = graphics.transform.position;
            corpse.transform.localScale = graphics.transform.lossyScale;
            csr = corpse.AddComponent<SpriteRenderer>();
            csr.sprite = graphics.sprite;
            csr.flipX = graphics.flipX;
            csr.sortingLayerID = graphics.sortingLayerID;
            csr.sortingOrder = graphics.sortingOrder + 1;
            csr.sharedMaterial = graphics.sharedMaterial;
            dir = graphics.flipX ? 1f : -1f;      // il bascule vers l'arrière
            graphics.enabled = false;
        }
        Vector3 basePos = corpse != null ? corpse.transform.position : root.position;
        AudioManager.Play("degat", 0.9f, 0.7f);
        CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cam != null) cam.Shake(0.18f, 0.35f);

        // 1) l'impact : le personnage devient tout blanc et reste figé un instant
        if (csr != null) csr.color = new Color(3f, 3f, 3f, 1f);
        for (float t = 0f; t < 0.18f; t += Time.deltaTime)
        {
            if (corpse != null) corpse.transform.position = basePos + new Vector3(Random.Range(-0.04f, 0.04f), Random.Range(-0.02f, 0.02f), 0f);
            yield return null;
        }

        // 2) il s'effondre et se dissout en lueurs
        Sprite spark = FxUtil.LoadSprite("eclat");
        Sprite glow = FxUtil.LoadSprite("lueur");
        float dur = 1.3f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float k = t / dur;
            float e = k * k * (3f - 2f * k);
            if (corpse != null)
            {
                corpse.transform.position = basePos + new Vector3(dir * 0.25f * e, -0.35f * e, 0f);
                corpse.transform.rotation = Quaternion.Euler(0f, 0f, dir * 82f * e);
                Color c = Color.Lerp(new Color(1f, 0.92f, 0.92f), new Color(0.35f, 0.12f, 0.18f), Mathf.Clamp01(k * 2f));
                c.a = k < 0.55f ? 1f : 1f - (k - 0.55f) / 0.45f;
                csr.color = c;
            }
            if (spark != null && Random.value < 0.7f)
                SimpleParticle.Spawn(spark, basePos + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.5f, 0.4f), 0f),
                    new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(0.6f, 2.2f)), Random.Range(0.6f, 1.2f), new Color(0.65f, 1f, 0.55f, 0.9f),
                    Random.Range(0.5f, 1f), 10006, FxUtil.Additive, -0.4f, 0.8f);
            if (glow != null && Random.value < 0.12f)
                SimpleParticle.Spawn(glow, basePos + new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f), new Vector2(0f, Random.Range(0.8f, 1.6f)), 1.1f,
                    new Color(0.55f, 1f, 0.5f, 0.5f), Random.Range(0.8f, 1.5f), 10005, FxUtil.Additive, 0f, 1f);
            yield return null;
        }
        if (corpse != null) Object.Destroy(corpse);
        yield return new WaitForSeconds(0.25f);

        // 3) fondu sur noir
        yield return GameUI.Instance.FadeTo(Color.black, 1.0f);

        // 4) écran de mort
        yield return Screen();

        // retour à la normale (l'écran reste noir : la suite se charge de la réapparition)
        if (graphics != null) graphics.enabled = true;
        if (rb != null) rb.gravityScale = gravity;
        if (skin != null) skin.enabled = true;
        Active = false;
    }

    static IEnumerator Screen()
    {
        GameObject go = new GameObject("Écran de mort");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        CanvasGroup group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        RectTransform root = (RectTransform)go.transform;

        RectTransform bg = UIFactory.Rect("Fond", root);
        UIFactory.Stretch(bg, 0);
        bg.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.01f, 0.02f, 1f);

        Text title = UIFactory.Label("Titre", root, font, 104, new Color(0.82f, 0.2f, 0.22f), TextAnchor.MiddleCenter);
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        title.rectTransform.sizeDelta = new Vector2(1500f, 160f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 120f);
        title.fontStyle = FontStyle.Bold;
        title.text = "VOUS ÊTES MORT";

        RectTransform line = UIFactory.Rect("Trait", root);
        line.anchorMin = line.anchorMax = new Vector2(0.5f, 0.5f);
        line.sizeDelta = new Vector2(760f, 3f);
        line.anchoredPosition = new Vector2(0f, 30f);
        line.gameObject.AddComponent<Image>().color = new Color(0.7f, 0.2f, 0.22f, 0.7f);

        Text sub = UIFactory.Label("Phrase", root, font, 34, new Color(0.75f, 0.72f, 0.7f, 0.9f), TextAnchor.MiddleCenter);
        sub.rectTransform.anchorMin = sub.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        sub.rectTransform.sizeDelta = new Vector2(1500f, 60f);
        sub.rectTransform.anchoredPosition = new Vector2(0f, -20f);
        sub.fontStyle = FontStyle.Italic;
        sub.text = Lines[Random.Range(0, Lines.Length)];

        Text retry = UIFactory.Label("Réapparaître", root, font, 40, Color.white, TextAnchor.MiddleCenter);
        retry.rectTransform.anchorMin = retry.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        retry.rectTransform.sizeDelta = new Vector2(900f, 60f);
        retry.rectTransform.anchoredPosition = new Vector2(0f, -170f);
        retry.text = "[Entrée]  Réapparaître";
        Text menu = UIFactory.Label("Menu", root, font, 32, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleCenter);
        menu.rectTransform.anchorMin = menu.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        menu.rectTransform.sizeDelta = new Vector2(900f, 50f);
        menu.rectTransform.anchoredPosition = new Vector2(0f, -240f);
        menu.text = "[M]  Menu principal";

        // apparition douce
        for (float t = 0f; t < 1.2f; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Clamp01(t / 1.0f);
            title.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.08f, 1f, Mathf.Clamp01(t / 1.2f));
            yield return null;
        }
        group.alpha = 1f;

        float wait = 0f;
        int choice = -1;
        while (choice < 0)
        {
            wait += Time.unscaledDeltaTime;
            float pulse = 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.5f));
            Color rc = retry.color; rc.a = pulse; retry.color = rc;
            bool overRetry = RectTransformUtility.RectangleContainsScreenPoint(retry.rectTransform, Input.mousePosition, null);
            bool overMenu = RectTransformUtility.RectangleContainsScreenPoint(menu.rectTransform, Input.mousePosition, null);
            menu.color = overMenu ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            if (wait > 0.6f)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E)
                    || (Input.GetMouseButtonDown(0) && overRetry)) choice = 0;
                else if (Input.GetKeyDown(KeyCode.M) || (Input.GetMouseButtonDown(0) && overMenu)) choice = 1;
            }
            yield return null;
        }
        ChoseMenu = choice == 1;
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / 0.35f;
            yield return null;
        }
        Object.Destroy(go);
    }
}
