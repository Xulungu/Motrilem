using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Page du livre qui apparaît quand le gardien est vaincu : elle améliore la magie du joueur.
// Choix 1 : magie chargée (maintenir l'attaque). Choix 2 : une seconde magie (parmi les deux autres).
public class BossUpgradePage : Interactable
{
    public string requiredFlag = "boss_moustique";
    public string takenFlag = "amelioration_prise";
    public GameObject visual;
    public SpriteRenderer pageRenderer, glowRenderer;
    public Sprite iconFire, iconLightning, iconIce, glowSprite, fragmentSprite;
    public Material additiveMaterial;
    public string playerName = "???";

    private bool shown, busy;
    private Vector3 basePos;

    protected override void Awake()
    {
        base.Awake();
        promptText = "Examiner la page";
        promptOffset = new Vector3(0f, 1.1f, 0f);
        basePos = transform.position;
        if (visual != null) visual.SetActive(false);
        canInteract = false;
    }

    protected override void Update()
    {
        bool ready = SaveSystem.HasFlag(requiredFlag) && !SaveSystem.HasFlag(takenFlag);
        if (ready && !shown && !busy) StartCoroutine(Appear());
        if (shown)
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
        // si le gardien vient de tomber, on laisse passer un instant avant de la faire apparaître
        if (Time.timeSinceLevelLoad > 3f) yield return new WaitForSeconds(2.5f);
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
        StartCoroutine(Choose(player));
    }

    IEnumerator Choose(PlayerMovement pm)
    {
        GameUI ui = GameUI.Instance;
        pm.SetCutscene(true);
        StartCoroutine(ui.Letterbox(true));
        PlayerMagic magic = pm.GetComponent<PlayerMagic>();
        MagicType current = magic != null ? magic.magic : MagicType.Aucune;

        yield return ui.Say("Une nouvelle page. Les lettres s'y dessinent d'elles-mêmes, comme si elle m'attendait.", playerName);

        Color col = PlayerMagic.ColorOf(current);
        int kind = -1; // 0 = magie chargée, 1 = seconde magie
        MagicType second = MagicType.Aucune;
        while (true)
        {
            UpgradeChoiceUI ch = gameObject.AddComponent<UpgradeChoiceUI>();
            ch.glow = glowSprite;
            var opts = new List<UpgradeChoiceUI.Option>();
            opts.Add(new UpgradeChoiceUI.Option { title = "Magie chargée", tag = "Puissance", color = col, icon = IconOf(current),
                description = "Maintiens l'attaque pour charger ton sort ( " + PlayerMagic.NameOf(current) + " ), puis relâche : il grossit et frappe bien plus fort." });
            opts.Add(new UpgradeChoiceUI.Option { title = "Seconde magie", tag = "Polyvalence", color = new Color(0.9f, 0.88f, 0.95f), icon = glowSprite,
                description = "Apprends une deuxième magie, parmi celles que tu n'as pas choisies. Change de magie à tout moment avec Tab ou R." });
            int pick = -1;
            yield return ch.Run("La page se réécrit...", "Choisis comment ta magie évolue", opts, i => pick = i);
            Destroy(ch);
            if (pick < 0) break;
            if (pick == 0) { kind = 0; break; }

            // seconde magie : choisir parmi les deux autres
            var others = new List<MagicType>();
            foreach (MagicType t in new[] { MagicType.Feu, MagicType.Foudre, MagicType.Glace }) if (t != current) others.Add(t);
            UpgradeChoiceUI ch2 = gameObject.AddComponent<UpgradeChoiceUI>();
            ch2.glow = glowSprite;
            var opts2 = new List<UpgradeChoiceUI.Option>();
            foreach (MagicType t in others)
                opts2.Add(new UpgradeChoiceUI.Option { title = PlayerMagic.NameOf(t), tag = TagOf(t), color = PlayerMagic.ColorOf(t), icon = IconOf(t), description = DescOf(t) });
            int pick2 = -1;
            yield return ch2.Run("Une seconde affinité", "Laquelle réveiller ?", opts2, i => pick2 = i);
            Destroy(ch2);
            if (pick2 >= 0) { kind = 1; second = others[pick2]; break; }
            // Échap : retour au premier choix
        }

        if (kind < 0)
        {
            yield return ui.Letterbox(false, 0.3f);
            yield return null;
            pm.SetCutscene(false);
            canInteract = true;
            busy = false;
            yield break;
        }

        Color fx = kind == 0 ? col : PlayerMagic.ColorOf(second);
        AudioManager.Play("choix", 0.8f);
        if (pageRenderer != null)
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.7f) { pageRenderer.color = Color.Lerp(Color.white, fx, t * 0.6f); yield return null; }

        int count = 20, arrived = 0;
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            MemoryFragment.Spawn(fragmentSprite, additiveMaterial, transform.position, dir * Random.Range(2f, 4.5f),
                pm.transform, new Vector3(0f, 0.1f, 0f), Random.Range(0.3f, 0.7f), fx, Random.value < 0.3f ? 1.4f : 1f, () => { arrived++; });
        }
        if (visual != null) visual.SetActive(false);
        float timeout = 5f;
        while (arrived < count && timeout > 0) { timeout -= Time.deltaTime; yield return null; }

        if (kind == 0) SaveSystem.Session.chargeUnlocked = true;
        else if (magic != null) magic.SetSecondMagic(second);
        SaveSystem.SetFlag(takenFlag);

        yield return ui.Flash(new Color(fx.r, fx.g, fx.b, 0.7f), 0.6f);
        if (kind == 0)
        {
            yield return ui.Say("Ma magie gronde, plus profonde. Si je retiens mon souffle avant de lancer le sort...", playerName);
            ui.Toast("Maintiens l'attaque pour charger ton sort");
        }
        else
        {
            yield return ui.Say("Une deuxième flamme s'éveille en moi. Deux affinités... comme si elles avaient toujours été là.", playerName);
            ui.Toast("Tab / R : changer de magie");
        }
        yield return ui.Letterbox(false);
        yield return null;
        pm.SetCutscene(false);
        GetComponent<Collider2D>().enabled = false;
        busy = false;
    }

    Sprite IconOf(MagicType t) { return t == MagicType.Feu ? iconFire : t == MagicType.Foudre ? iconLightning : iconIce; }
    static string TagOf(MagicType t) { return t == MagicType.Feu ? "Puissance" : t == MagicType.Foudre ? "Vitesse" : "Contrôle"; }
    static string DescOf(MagicType t)
    {
        return t == MagicType.Feu ? "Une magie brute et dévastatrice.\nTes sorts embrasent les ennemis et les brûlent sur la durée."
             : t == MagicType.Foudre ? "Une magie vive et précise.\nTes sorts frappent en un éclair et rebondissent d'ennemi en ennemi."
             : "Une magie froide et maîtrisée.\nTes sorts ralentissent les ennemis jusqu'à les figer sur place.";
    }
}
