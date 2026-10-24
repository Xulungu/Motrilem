using System.Collections;
using UnityEngine;

// Points de vie d'un ennemi (ou d'un cristal), effets des sorts et animation de mort.
public class EnemyHealth : MonoBehaviour
{
    public enum DeathStyle { Chute, Eclatement }

    public int maxHealth = 3;
    [Tooltip("Objet détruit à la mort (par défaut : la racine de l'ennemi)")]
    public GameObject rootToDestroy;
    public DeathStyle deathStyle = DeathStyle.Chute;
    public Color shardColor = new Color(0.65f, 0.55f, 1f);
    public string deathSound = "mort_ennemi";
    [Tooltip("Compte comme un ennemi pour le profil du joueur (faux pour les cristaux)")]
    public bool countsAsEnemy = true;

    /// <summary>Appelé à la mort (paramètre : écrasé par le joueur ?).</summary>
    public event System.Action<bool> OnDeath;

    public int Health { get; private set; }
    public bool IsDead { get; private set; }

    /// <summary>Multiplicateur de vitesse (glace = ralenti, gel/étourdissement = 0).</summary>
    public float SpeedMultiplier
    {
        get
        {
            if (freezeTimer > 0f || stunTimer > 0f) return 0f;
            return slowTimer > 0f ? 0.4f : 1f;
        }
    }
    public bool IsStunned { get { return freezeTimer > 0f || stunTimer > 0f; } }

    /// <summary>0..1 : teinte rouge d'alerte (moustique qui va piquer).</summary>
    [HideInInspector] public float alert;

    private SpriteRenderer sr;
    private Color baseColor = Color.white;
    private float flashTimer;
    private float burnTimer, burnTick;
    private float slowTimer, freezeTimer, stunTimer;
    private int iceHits;
    private float iceWindow;
    private EnemyVisuals visuals;

    void Awake()
    {
        if (rootToDestroy == null) rootToDestroy = transform.root.gameObject;
    }

    void Start()
    {
        Health = maxHealth;
        visuals = GetComponent<EnemyVisuals>();
        sr = visuals != null && visuals.Renderer != null ? visuals.Renderer : GetComponentInChildren<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    // ------------------------------------------------------------------ dégâts
    public void TakeDamage(int amount, MagicType element, Vector2 hitDirection)
    {
        if (IsDead) return;
        // Un ennemi gelé se brise plus facilement
        if (freezeTimer > 0f) amount += 1;
        Health -= amount;
        flashTimer = 0.12f;
        if (visuals != null) visuals.Hit(hitDirection);

        switch (element)
        {
            case MagicType.Feu:
                if (burnTimer <= 0f) burnTick = 0.8f;
                burnTimer = 1.6f; // brûle pendant 1,6 s (1 dégât toutes les 0,8 s : 2 dégâts en tout)
                break;
            case MagicType.Foudre:
                stunTimer = Mathf.Max(stunTimer, 0.6f);
                break;
            case MagicType.Glace:
                slowTimer = 3f;
                iceHits = iceWindow > 0f ? iceHits + 1 : 1;
                iceWindow = 4f;
                if (iceHits >= 3) { freezeTimer = 1.6f; iceHits = 0; }
                break;
        }

        if (Health <= 0) Die(false);
    }

    /// <summary>Mort immédiate (écrasé par le joueur...).</summary>
    public void Kill(bool stomped)
    {
        if (IsDead) return;
        Health = 0;
        Die(stomped);
    }

    void Update()
    {
        if (IsDead) return;
        float dt = Time.deltaTime;
        slowTimer -= dt; freezeTimer -= dt; stunTimer -= dt; iceWindow -= dt; flashTimer -= dt;

        if (burnTimer > 0f)
        {
            burnTimer -= dt;
            burnTick -= dt;
            if (burnTick <= 0f)
            {
                burnTick = 0.8f;
                Health -= 1;
                flashTimer = 0.08f;
                Sprite s = FxUtil.LoadSprite("eclat");
                if (s != null)
                    for (int i = 0; i < 3; i++)
                        SimpleParticle.Spawn(s, transform.position + (Vector3)Random.insideUnitCircle * 0.2f, new Vector2(Random.Range(-0.5f, 0.5f), 1.5f),
                            0.5f, new Color(1f, 0.5f, 0.15f), 1f, 10006, FxUtil.Additive, -1f, 1f);
                if (Health <= 0) { Die(false); return; }
            }
        }

        // Couleur selon l'état
        if (sr != null)
        {
            Color c = baseColor;
            if (freezeTimer > 0f) c = new Color(0.55f, 0.85f, 1f);
            else if (slowTimer > 0f) c = Color.Lerp(baseColor, new Color(0.6f, 0.9f, 1f), 0.5f);
            if (burnTimer > 0f) c = Color.Lerp(c, new Color(1f, 0.45f, 0.2f), 0.4f + 0.2f * Mathf.Sin(Time.time * 20f));
            if (stunTimer > 0f && Mathf.Sin(Time.time * 50f) > 0f) c = Color.Lerp(c, new Color(1f, 0.95f, 0.5f), 0.6f);
            if (alert > 0f) c = Color.Lerp(c, new Color(1f, 0.2f, 0.2f), alert);
            if (flashTimer > 0f) c = new Color(1f, 0.35f, 0.35f);
            sr.color = c;
        }
    }

    // ------------------------------------------------------------------ mort
    void Die(bool stomped)
    {
        if (IsDead) return;
        IsDead = true;
        if (countsAsEnemy)
            PlayerProfile.Record(stomped ? PlayerProfile.Event.StompKill : PlayerProfile.Event.MagicKill);
        AudioManager.Play(stomped ? "ecrase" : deathSound, 0.6f, Random.Range(0.9f, 1.1f));

        // On coupe tout ce qui peut encore faire mal ou bouger
        foreach (Collider2D c in rootToDestroy.GetComponentsInChildren<Collider2D>()) c.enabled = false;
        foreach (EnemyPatrol p in rootToDestroy.GetComponentsInChildren<EnemyPatrol>()) p.enabled = false;
        foreach (EnemyDamage d in rootToDestroy.GetComponentsInChildren<EnemyDamage>()) d.enabled = false;
        foreach (MosquitoStinger s in rootToDestroy.GetComponentsInChildren<MosquitoStinger>()) s.Cancel();
        foreach (Slime sl in rootToDestroy.GetComponentsInChildren<Slime>()) sl.enabled = false;
        foreach (Rigidbody2D body in rootToDestroy.GetComponentsInChildren<Rigidbody2D>())
        {
            body.velocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
        }
        if (OnDeath != null) OnDeath(stomped);

        if (deathStyle == DeathStyle.Eclatement) StartCoroutine(Shatter());
        else StartCoroutine(FallAndFade(stomped));
    }

    IEnumerator Shatter()
    {
        AudioManager.Play("cristal", 0.6f, Random.Range(0.95f, 1.1f));
        Sprite s = FxUtil.LoadSprite("eclat");
        Bounds b = sr != null ? sr.bounds : new Bounds(transform.position, Vector3.one);
        if (s != null)
            for (int i = 0; i < 26; i++)
            {
                Vector3 p = new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), 0f);
                SimpleParticle.Spawn(s, p, new Vector2(Random.Range(-4f, 4f), Random.Range(0f, 5f)), Random.Range(0.5f, 0.9f),
                    Color.Lerp(shardColor, Color.white, Random.value * 0.4f), Random.Range(1f, 2f), 10006, null, 12f, 0.5f);
            }
        foreach (SpriteRenderer r in rootToDestroy.GetComponentsInChildren<SpriteRenderer>()) r.enabled = false;
        yield return null;
        Destroy(rootToDestroy);
    }

    IEnumerator FallAndFade(bool stomped)
    {
        Transform v = visuals != null && visuals.Visual != null ? visuals.Visual : (sr != null ? sr.transform : transform);
        if (visuals != null) visuals.enabled = false;
        Sprite s = FxUtil.LoadSprite("eclat");
        if (s != null)
            for (int i = 0; i < 10; i++)
                SimpleParticle.Spawn(s, v.position, Random.insideUnitCircle.normalized * Random.Range(1.5f, 3.5f), 0.4f,
                    new Color(0.7f, 1f, 0.5f), 1f, 10006, null, 4f, 2f);

        Vector3 start = v.localScale;
        if (stomped)
        {
            // Écrasé : aplati puis disparaît
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.35f)
            {
                v.localScale = new Vector3(start.x * (1f + 0.5f * t), start.y * (1f - 0.75f * t), 1f);
                if (sr != null) sr.color = new Color(1f, 1f, 1f, 1f - t);
                yield return null;
            }
        }
        else
        {
            // Touché par la magie : tournoie et tombe
            float vy = 2f, spin = Random.value < 0.5f ? -540f : 540f;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.9f)
            {
                vy -= 14f * Time.deltaTime;
                v.position += new Vector3(0f, vy * Time.deltaTime, 0f);
                v.Rotate(0, 0, spin * Time.deltaTime);
                if (sr != null) sr.color = new Color(1f, 1f, 1f, 1f - t * t);
                yield return null;
            }
        }
        Destroy(rootToDestroy);
    }
}
