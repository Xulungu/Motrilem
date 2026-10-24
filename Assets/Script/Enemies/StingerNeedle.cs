using UnityEngine;

// Copie "fantôme" de la trompe du moustique piqueur.
//  1. Apparition : la copie se matérialise sur la vraie trompe et suit le joueur du regard
//  2. Armé : elle recule légèrement et sa pointe s'illumine
//  3. Tir : elle file en ligne droite vers le joueur en laissant une traînée
//  4. Impact : sur le joueur elle éclate ; sur un mur elle se plante, vibre puis s'efface
public class StingerNeedle : MonoBehaviour
{
    enum State { Apparition, Arme, Vol, Plantee, Fin }

    public int damage = 30;
    public float speed = 13f;
    public float maxDistance = 10f;
    [Tooltip("Taille de la trompe lancée (1 = taille du sprite)")]
    public float size = 0.65f;

    private State state = State.Apparition;
    private float timer;
    private Transform anchor;      // la bouche du moustique (suivie pendant l'apparition)
    private Vector3 anchorOffset;
    private Transform target;
    private Vector2 dir = Vector2.left;
    private float traveled;
    private SpriteRenderer sr, glow;
    private Rigidbody2D body;
    private Collider2D col;
    private TrailRenderer trail;
    private float appearTime, aimTime;
    private bool missed;
    private Color baseColor = new Color(1f, 0.55f, 0.55f);

    public static StingerNeedle Spawn(Transform anchor, Vector3 anchorOffset, Transform target, float appearTime, float aimTime, int damage)
    {
        GameObject go = new GameObject("Trompe dupliquée");
        go.transform.position = anchor.position + anchorOffset;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FxUtil.LoadSprite("trompe");
        sr.sortingOrder = 10001;
        sr.color = new Color(1f, 0.55f, 0.55f, 0f);

        Sprite g = FxUtil.LoadSprite("lueur_douce");
        SpriteRenderer glow = null;
        if (g != null)
        {
            glow = FxUtil.MakeSprite("Lueur", g, go.transform.position, 10000, new Color(1f, 0.2f, 0.25f, 0f), FxUtil.Additive, go.transform);
            glow.transform.localPosition = new Vector3(0.35f, 0f, 0f);
            glow.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
        }

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.8f, 0.16f);
        box.enabled = false;

        TrailRenderer trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.2f;
        trail.widthMultiplier = 0.065f;
        trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
        trail.sharedMaterial = FxUtil.Additive;
        Gradient gr = new Gradient();
        gr.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.4f, 0.4f), 0f), new GradientColorKey(new Color(0.5f, 0.05f, 0.1f), 1f) },
                   new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gr;
        trail.sortingOrder = 9999;
        trail.emitting = false;

        StingerNeedle n = go.AddComponent<StingerNeedle>();
        n.sr = sr; n.glow = glow; n.body = rb; n.col = box; n.trail = trail;
        n.anchor = anchor; n.anchorOffset = anchorOffset; n.target = target;
        n.appearTime = appearTime; n.aimTime = aimTime; n.damage = damage;
        n.Aim();
        return n;
    }

    /// <summary>Le moustique est mort pendant l'apparition : la copie se dissipe.</summary>
    public void Dissipate()
    {
        if (state == State.Apparition || state == State.Arme) { state = State.Fin; timer = 0f; }
    }

    /// <summary>La copie est-elle déjà partie (le moustique peut reprendre sa patrouille) ?</summary>
    public bool Launched { get { return state != State.Apparition && state != State.Arme; } }

    void Aim()
    {
        if (target == null) return;
        Vector2 to = (Vector2)(target.position + new Vector3(0f, 0.1f, 0f)) - (Vector2)transform.position;
        if (to.sqrMagnitude > 0.001f) dir = to.normalized;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        timer += dt;
        switch (state)
        {
            case State.Apparition:
            {
                // La copie se détache doucement de la vraie trompe en tremblotant
                if (anchor != null) transform.position = anchor.position + anchorOffset;
                Aim();
                float k = Mathf.Clamp01(timer / appearTime);
                sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.85f * k);
                transform.localScale = new Vector3(Mathf.Lerp(0.4f, 1f, k) * size, (1f + Mathf.Sin(timer * 40f) * 0.15f * (1f - k)) * size, 1f);
                transform.position += (Vector3)(Random.insideUnitCircle * 0.015f);
                if (glow != null) glow.color = new Color(1f, 0.2f, 0.25f, 0.3f * k);
                if (timer >= appearTime) { state = State.Arme; timer = 0f; }
                break;
            }
            case State.Arme:
            {
                // Recul d'anticipation, la pointe s'illumine
                float k = Mathf.Clamp01(timer / aimTime);
                if (anchor != null) transform.position = anchor.position + anchorOffset - (Vector3)(dir * 0.22f * Ease.Out(k));
                sr.color = Color.Lerp(baseColor, Color.white, k * 0.6f);
                if (glow != null) glow.color = new Color(1f, 0.6f, 0.6f, 0.4f + 0.5f * k);
                if (timer >= aimTime) Launch();
                break;
            }
            case State.Plantee:
            {
                // Plantée dans le mur : elle vibre puis s'efface
                float vib = Mathf.Sin(timer * 60f) * 6f * Mathf.Max(0f, 1f - timer * 3f);
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + vib);
                if (timer > 0.6f) { state = State.Fin; timer = 0f; }
                break;
            }
            case State.Fin:
            {
                float a = 1f - timer / 0.35f;
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, Mathf.Max(0f, a) * 0.85f);
                if (glow != null) glow.color = new Color(1f, 0.2f, 0.25f, Mathf.Max(0f, a) * 0.3f);
                if (a <= 0f) Destroy(gameObject);
                break;
            }
        }
    }

    void Launch()
    {
        state = State.Vol;
        timer = 0f;
        col.enabled = true;
        trail.emitting = true;
        sr.color = new Color(1f, 0.75f, 0.75f, 1f);
        transform.localScale = new Vector3(1.25f * size, 0.85f * size, 1f); // étirée par la vitesse
        AudioManager.Play("trompe", 0.6f, Random.Range(0.95f, 1.1f));
    }

    void FixedUpdate()
    {
        if (state != State.Vol) return;
        float step = speed * Time.fixedDeltaTime;
        body.MovePosition(body.position + dir * step);
        traveled += step;
        if (traveled >= maxDistance) { missed = true; state = State.Fin; timer = 0f; col.enabled = false; trail.emitting = false; }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (state != State.Vol) return;

        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
            if (ph != null && ph.CanTakeDamage)
            {
                ph.TakeDamage(damage);
                PlayerMovement pm = other.GetComponentInParent<PlayerMovement>();
                if (pm != null && ph.CurrentHealth > 0)
                    pm.ApplyKnockback(new Vector2(Mathf.Sign(dir.x == 0f ? 1f : dir.x) * 5f, 4f), 0.3f);
                PlayerProfile.Record(PlayerProfile.Event.HitByStinger);
            }
            Shatter();
            return;
        }

        if (other.isTrigger) return; // zones, ennemis, points faibles...
        if (other.GetComponentInParent<EnemyHealth>() != null) return;

        // Mur ou sol : la trompe se plante
        missed = true;
        state = State.Plantee;
        timer = 0f;
        col.enabled = false;
        trail.emitting = false;
        transform.localScale = Vector3.one * size;
        AudioManager.Play("impact", 0.35f, 0.7f);
    }

    void Shatter()
    {
        state = State.Fin;
        timer = 0.35f;
        col.enabled = false;
        trail.emitting = false;
        sr.enabled = false;
        if (glow != null) glow.enabled = false;
        Sprite s = FxUtil.LoadSprite("eclat");
        if (s != null)
            for (int i = 0; i < 8; i++)
                SimpleParticle.Spawn(s, transform.position, Random.insideUnitCircle.normalized * Random.Range(1f, 3f) - dir * 2f,
                    0.35f, new Color(1f, 0.35f, 0.35f), 1f, 10006, null, 6f, 2f);
        Destroy(gameObject, 0.05f);
    }

    /// <summary>La trompe a-t-elle raté sa cible (pour mesurer les esquives du joueur) ?</summary>
    void OnDestroy()
    {
        if (missed) PlayerProfile.Record(PlayerProfile.Event.DodgedStinger);
    }
}
