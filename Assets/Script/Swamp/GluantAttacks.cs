using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attaques spéciales du Gros Gluant (en plus des bonds d'un slime normal) :
//  - Écrasement : il bondit très haut, retombe lourdement et envoie deux ondes gluantes au ras du sol (à sauter) ;
//  - Crachat : il gonfle puis lance des boules de glu en cloche vers le joueur (un sort les fait éclater) ;
//  - Appel : à moins de 2/3 puis 1/3 de vie, il fait tomber des petits slimes du plafond.
// Plus il est blessé, plus il enchaîne vite et plus les attaques sont nombreuses.
[RequireComponent(typeof(Slime))]
public class GluantAttacks : MonoBehaviour
{
    public Vector2 arenaMin, arenaMax;
    [HideInInspector] public bool active;

    private Slime slime;
    private Rigidbody2D rb;
    private EnemyHealth health;
    private Transform player;
    private SpriteRenderer sr;
    private float cooldown = 2.5f;
    private bool busy;
    private int lastAttack = -1;
    private int summonsDone;
    private readonly List<GameObject> summoned = new List<GameObject>();

    void Awake()
    {
        slime = GetComponent<Slime>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
    }

    void Start()
    {
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) player = pm.transform;
        sr = slime.BodyRenderer;
    }

    float Ratio { get { return health != null ? (float)health.Health / Mathf.Max(1, health.maxHealth) : 1f; } }
    int Phase { get { float r = Ratio; return r > 0.66f ? 1 : r > 0.33f ? 2 : 3; } }

    public void ResetFight()
    {
        StopAllCoroutines();
        busy = false;
        active = false;
        slime.suspended = false;
        summonsDone = 0;
        cooldown = 2.5f;
        if (sr != null) sr.color = Color.white;
        ClearSummoned();
    }

    public void ClearSummoned()
    {
        foreach (GameObject g in summoned) if (g != null) Destroy(g);
        summoned.Clear();
    }

    void Update()
    {
        if (!active || busy || player == null || health == null || health.IsDead) return;
        if (health.SpeedMultiplier <= 0f) return;   // gelé / étourdi
        cooldown -= Time.deltaTime;
        if (cooldown > 0f || !slime.Grounded || slime.Preparing) return;
        StartCoroutine(Attack());
    }

    IEnumerator Attack()
    {
        busy = true;
        slime.suspended = true;
        int phase = Phase;
        summoned.RemoveAll(g => g == null);

        // choix de l'attaque (jamais deux fois la même d'affilée)
        List<int> pool = new List<int> { 0, 1 };
        if ((phase >= 2 && summonsDone < 1 || phase >= 3 && summonsDone < 2) && summoned.Count < 2) pool = new List<int> { 2 };
        if (pool.Count > 1 && lastAttack >= 0 && lastAttack < 2) pool.Remove(lastAttack);
        int choice = pool[Random.Range(0, pool.Count)];
        lastAttack = choice;

        if (choice == 0) yield return Slam(phase);
        else if (choice == 1) yield return Spit(phase);
        else { summonsDone++; yield return Summon(phase); }

        if (sr != null) sr.color = Color.white;
        slime.suspended = false;
        cooldown = phase == 1 ? 2.8f : phase == 2 ? 2.2f : 1.6f;
        busy = false;
    }

    IEnumerator Telegraph(Color tint, float time, Vector2 squashTo)
    {
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            if (health == null || health.IsDead) yield break;
            if (sr != null) sr.color = Color.Lerp(Color.white, tint, Mathf.PingPong(t * 6f, 1f) * 0.6f + 0.4f);
            slime.Punch(squashTo);
            yield return null;
        }
    }

    // ------------------------------------------------------------------ écrasement
    IEnumerator Slam(int phase)
    {
        yield return Telegraph(new Color(1f, 0.45f, 0.35f), 0.75f, new Vector2(1.45f, 0.55f));
        if (health.IsDead) yield break;
        float dx = Mathf.Clamp(player.position.x - transform.position.x, -7f, 7f);
        float x = Mathf.Clamp(transform.position.x + dx, arenaMin.x + 2f, arenaMax.x - 2f);
        rb.velocity = new Vector2((x - transform.position.x) * 0.85f, 17.5f);
        slime.Punch(new Vector2(0.65f, 1.45f));
        AudioManager.Play("slime_saut", 0.7f, 0.6f);
        float timeout = 3f;
        yield return new WaitForSeconds(0.25f);
        while (!slime.Grounded && timeout > 0f && !health.IsDead) { timeout -= Time.deltaTime; yield return null; }
        if (health.IsDead) yield break;

        // impact
        slime.Punch(new Vector2(1.7f, 0.5f));
        AudioManager.Play("slime_mort", 0.7f, 0.5f);
        if (Camera.main != null)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.Shake(0.12f, 0.35f);
        }
        Vector3 feet = new Vector3(transform.position.x, transform.position.y - 1.0f, 0f);
        feet.y = GroundY(feet);
        float speed = phase == 3 ? 8f : 6.5f;
        GooShot.Wave(feet, -1, speed, arenaMin.x, arenaMax.x, transform.parent);
        GooShot.Wave(feet, +1, speed, arenaMin.x, arenaMax.x, transform.parent);
        if (phase >= 2)
        {
            yield return new WaitForSeconds(0.55f);
            if (health.IsDead) yield break;
            GooShot.Wave(feet, -1, speed * 0.8f, arenaMin.x, arenaMax.x, transform.parent);
            GooShot.Wave(feet, +1, speed * 0.8f, arenaMin.x, arenaMax.x, transform.parent);
        }
        yield return new WaitForSeconds(0.6f);
    }

    float GroundY(Vector3 from)
    {
        RaycastHit2D[] rays = Physics2D.RaycastAll(from + Vector3.up * 1.5f, Vector2.down, 6f);
        foreach (RaycastHit2D r in rays)
        {
            if (r.collider == null || r.collider.isTrigger || r.collider.CompareTag("Player")) continue;
            if (r.collider.GetComponentInParent<Slime>() != null) continue;
            return r.point.y;
        }
        return from.y;
    }

    // ------------------------------------------------------------------ crachat
    IEnumerator Spit(int phase)
    {
        yield return Telegraph(new Color(1f, 0.95f, 0.4f), 0.6f, new Vector2(1.3f, 0.75f));
        if (health.IsDead) yield break;
        int count = phase == 1 ? 3 : phase == 2 ? 4 : 5;
        for (int i = 0; i < count; i++)
        {
            if (health.IsDead) yield break;
            Vector3 from = transform.position + new Vector3(0f, 1.4f, 0f);
            float spread = (i - (count - 1) * 0.5f) * 1.7f;
            Vector3 target = new Vector3(Mathf.Clamp(player.position.x + spread, arenaMin.x + 1f, arenaMax.x - 1f), player.position.y + 0.3f, 0f);
            GooShot.Ball(from, target, Random.Range(0.8f, 1.15f), transform.parent);
            slime.Punch(new Vector2(0.8f, 1.25f));
            AudioManager.Play("bulle", 0.6f, Random.Range(0.8f, 1.1f));
            yield return new WaitForSeconds(phase == 3 ? 0.22f : 0.3f);
        }
        yield return new WaitForSeconds(0.5f);
    }

    // ------------------------------------------------------------------ appel des petits
    IEnumerator Summon(int phase)
    {
        yield return Telegraph(new Color(0.6f, 1f, 0.7f), 0.8f, new Vector2(0.8f, 1.3f));
        if (health.IsDead) yield break;
        if (GameUI.HasInstance) GameUI.Instance.Toast("Le Gros Gluant appelle ses petits !");
        int n = phase == 3 ? 3 : 2;
        // Les petits se forment au sol, loin du joueur ; ils n'existent qu'une fois la flaque devenue slime
        Vector3[] pts = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float x = Mathf.Lerp(arenaMin.x + 3f, arenaMax.x - 3f, (i + Random.Range(0.2f, 0.8f)) / n);
            for (int tries = 0; tries < 6 && player != null && Mathf.Abs(x - player.position.x) < 3.5f; tries++)
                x = Random.Range(arenaMin.x + 3f, arenaMax.x - 3f);
            float ground = SpawnFx.GroundBelow(new Vector3(x, arenaMax.y - 1f, 0f), arenaMin.y);
            pts[i] = new Vector3(x, ground + 0.5f, 0f);
            SpawnFx.Telegraph(pts[i], false, 0.6f);
            yield return new WaitForSeconds(0.25f);
        }
        yield return new WaitForSeconds(SpawnFx.SlimeTime - 0.1f);
        for (int i = 0; i < n; i++)
        {
            yield return SpawnFx.WaitClear(pts[i], 1.8f, 1.2f);
            if (health.IsDead) yield break;
            Slime s = SpawnFx.CreateSlime(pts[i], false, transform.parent);
            summoned.Add(s.gameObject);
        }
        yield return new WaitForSeconds(0.6f);
    }
}
