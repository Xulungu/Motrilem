using UnityEngine;

// Lancer de sorts : J, K ou clic gauche. Le sort dépend de l'affinité choisie (PlayerMagic).
// Visée en 8 directions : haut / bas (+ gauche / droite pour les diagonales) au moment du lancer.
public class PlayerCaster : MonoBehaviour
{
    public float fireCooldown = 0.55f;
    public float lightningCooldown = 0.4f;
    public float iceCooldown = 0.45f;

    private PlayerMovement pm;
    private PlayerMagic magic;
    private PlayerVisuals visuals;
    private float cooldown;

    void Awake()
    {
        pm = GetComponent<PlayerMovement>();
        visuals = GetComponent<PlayerVisuals>();
    }

    void Update()
    {
        cooldown -= Time.deltaTime;
        if (magic == null) magic = GetComponent<PlayerMagic>();
        if (magic == null || magic.magic == MagicType.Aucune) return;
        if (pm == null || !pm.controlsEnabled || pm.externalControl) return;

        if (GameInput.AttackDown() && cooldown <= 0f) Cast();
    }

    void Cast()
    {
        MagicType type = magic.magic;
        PlayerProfile.Record(type == MagicType.Feu ? PlayerProfile.Event.CastFire : type == MagicType.Foudre ? PlayerProfile.Event.CastLightning : PlayerProfile.Event.CastIce);
        cooldown = type == MagicType.Feu ? fireCooldown : type == MagicType.Foudre ? lightningCooldown : iceCooldown;

        Vector2 dir = AimDirection();
        Vector3 origin = transform.position + new Vector3(dir.x * 0.45f, 0.05f + dir.y * 0.55f, 0f);
        MagicProjectile.Spawn(type, origin, dir, gameObject);
        if (visuals == null) visuals = GetComponent<PlayerVisuals>();
        if (visuals != null) visuals.CastPunch();

        // Éclat lumineux dans la main
        Sprite glow = FxUtil.LoadSprite("lueur_douce");
        if (glow != null)
            SimpleParticle.Spawn(glow, origin, Vector2.zero, 0.2f, PlayerMagic.ColorOf(type), 1.2f, 10003, FxUtil.Additive);

        PlayCastSound(type);
    }

    /// <summary>Direction du sort : devant soi par défaut, vers le haut / bas, ou en diagonale.</summary>
    Vector2 AimDirection()
    {
        float h = GameInput.Horizontal();
        float v = GameInput.Vertical();
        if (v == 0f) return new Vector2(pm.Facing, 0f);
        if (h == 0f) return new Vector2(0f, v);
        return new Vector2(h, v).normalized;
    }

    static void PlayCastSound(MagicType type)
    {
        switch (type)
        {
            case MagicType.Feu: AudioManager.Play("sort_feu", 0.7f, Random.Range(0.95f, 1.05f)); break;
            case MagicType.Foudre: AudioManager.Play("sort_foudre", 0.6f, Random.Range(0.95f, 1.08f)); break;
            default: AudioManager.Play("sort_glace", 0.6f, Random.Range(0.95f, 1.05f)); break;
        }
    }
}
