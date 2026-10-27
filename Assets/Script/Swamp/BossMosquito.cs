using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Séraphel : moustique géant, presque une divinité. Son corps est gonflé d'acide.
//  - Attaque de base : sa trompe lancée (comme les moustiques piqueurs), en plus gros ; quand elle touche
//    le joueur ou un mur, de l'acide jaillit sur une petite zone.
//  - Plongeon : il s'écrase sur le sol où se tenait le joueur, puis reste étourdi (c'est le moment de frapper).
//  - Pluie d'acide (dès 2/3 de vie) : il balaie le plafond en laissant tomber des gouttes.
//  - Charge : il s'élance d'un bout à l'autre de la salle (une ligne rouge l'annonce) et s'écrase contre le mur.
//  - Geysers : des colonnes d'acide jaillissent du sol (une lueur les annonce) : il faut monter sur les plateformes ou bouger.
//  - Nova : (2e phase+) anneau de trompes dans toutes les directions.
//  - Essaim : des globules d'acide poursuivent le joueur ; un sort les fait éclater.
//  - Vague d'acide : il frappe le sol et une onde d'acide court des deux côtés (à sauter).
//  - Spirale : il tourne en crachant des trompes en spirale.
//  - Harcèlement : entre deux attaques, il crache régulièrement une trompe isolée.
//  - À la moitié de sa vie (une seule fois) : il plonge dans un bassin d'acide et regagne un peu de vie (2 à 5 s), puis revient enragé :
//    attaques plus rapides, et il invoque de petits moustiques. Ceux qui ne sont pas tués en 10 s sont dévorés (1/25 de sa vie chacun).
//  - Plus le combat dure, plus les plaques de sol s'effondrent sur l'acide (il faut alors utiliser les planches).
// Le feu le brûle, la glace le ralentit (ses attaques se préparent plus lentement), la foudre l'étourdit.
[RequireComponent(typeof(EnemyHealth))]
public class BossMosquito : MonoBehaviour
{
    public Vector2 arenaMin = new Vector2(66f, 1f);
    public Vector2 arenaMax = new Vector2(120f, 22f);
    [Tooltip("Le combat commence quand le joueur dépasse cette abscisse")]
    public float triggerX = 70f;
    public string defeatedFlag = "boss_moustique";
    public string exitFlag = "boss1";
    public ArenaLock entranceLock;
    public string bossName = "Séraphel";
    public string bossSubtitle = "Ce qui bourdonne dans la lumière";
    public SpriteRenderer body;
    [Tooltip("Centres (x) des bassins d'acide où il peut plonger ; y = surface")]
    public Vector2[] pitCenters;
    public CaveCollapse collapse;

    private EnemyHealth health;
    private Transform player;
    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private Vector3 homePosition;
    private bool fighting, dead;
    private int phase = 1;
    private bool enraged, wantSubmerge, hidden;
    private readonly List<FlyingMinion> minions = new List<FlyingMinion>();
    private float eatTime;
    private bool summonHint;
    private CrumblePlate[] plates;
    private int lastHealth;
    private float floorY;
    private float dripTimer;
    private float bobPhase;
    private int facing = -1;
    private Coroutine routine;

    // animation du corps (ailes, inclinaison, poses)
    public enum Pose { Idle, Tell, Dash, Stun }
    private Pose pose = Pose.Idle;
    private bool hasSubmerged;
    private Transform[] wingPivots;
    private readonly float[] wingPhase = { 0f, 1.3f, 2.4f };
    private SpriteRenderer abdomenGlow;
    private Vector3 lastPos, smoothVel;
    private float baseScale = 0.17f;
    private float hitKick;

    // interface de la barre de vie
    private RectTransform barRoot;
    private HealthBar bar;

    float Mult { get { return health != null ? health.SpeedMultiplier : 1f; } }

    void Awake()
    {
        health = GetComponent<EnemyHealth>();
        homePosition = transform.position;
        floorY = arenaMin.y;
    }

    void Start()
    {
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
            playerMovement = pm;
            playerHealth = pm.GetComponent<PlayerHealth>();
        }
        BuildRig();
        lastPos = transform.position;
        health.OnDeath += OnDied;
        lastHealth = health.maxHealth;
        if (SaveSystem.HasFlag(defeatedFlag))
        {
            // Déjà vaincu : il ne revient pas
            if (entranceLock != null) entranceLock.SetLocked(false);
            Destroy(gameObject);
        }
        else
        {
            SetVisible(false);
        }
    }

    void SetVisible(bool v)
    {
        foreach (SpriteRenderer r in GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = v;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = v;
    }

    // ------------------------------------------------------------------ cycle de vie
    void Update()
    {
        if (dead || player == null) return;

        if (!fighting)
        {
            if (player.position.x > triggerX && player.position.y < arenaMax.y && routine == null)
                routine = StartCoroutine(Fight());
            return;
        }

        // Le joueur est mort et a réapparu hors de l'arène : le combat recommence
        if (player.position.x < triggerX - 5f) { ResetFight(); return; }

        // Orientation et animation du corps
        bobPhase += Time.deltaTime * 4.5f;
        float dx = player.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.8f && pose != Pose.Dash) facing = dx > 0f ? 1 : -1;
        AnimateRig(Time.deltaTime);

        // Phases selon la vie restante
        int hp = health.Health;
        int newPhase = enraged ? 3 : hp <= health.maxHealth * 3 / 4 ? 2 : 1;
        if (!hasSubmerged && !wantSubmerge && !hidden && hp <= health.maxHealth / 2) wantSubmerge = true;
        if (newPhase != phase)
        {
            phase = newPhase;
            if (phase == 2) StartCoroutine(PhaseChange());
        }

        // Il saigne de l'acide quand il est touché ; des gouttes tombent de son corps
        if (hp < lastHealth) { SpurtAcid(Mathf.Min(8, (lastHealth - hp) * 3)); hitKick = 1f; }
        lastHealth = hp;
        dripTimer -= Time.deltaTime * Mult;
        if (dripTimer <= 0f)
        {
            dripTimer = (phase == 1 ? 2.2f : phase == 2 ? 1.5f : 1.0f) * Random.Range(0.7f, 1.3f);
            AcidDrop.Spawn(transform.position + new Vector3(-facing * 0.7f, -0.8f, 0f), 1f, 8);
        }
        Sprite bub = FxUtil.LoadSprite("bulle");
        if (bub != null && Random.value < Time.deltaTime * 6f)
            SimpleParticle.Spawn(bub, transform.position + new Vector3(-facing * Random.Range(0.2f, 1f), Random.Range(-0.8f, 0.2f), 0f),
                new Vector2(0f, Random.Range(0.5f, 1f)), 0.9f, new Color(0.7f, 1f, 0.4f, 0.8f), Random.Range(0.6f, 1f), 9999, null, -0.3f, 0.5f);

        // Barre de vie
        if (bar != null) bar.SetHealth(hp);
    }

    void SpurtAcid(int n)
    {
        Sprite e = FxUtil.LoadSprite("eclat");
        if (e == null) return;
        for (int i = 0; i < n; i++)
            SimpleParticle.Spawn(e, transform.position + (Vector3)(Random.insideUnitCircle * 0.8f), new Vector2(Random.Range(-3f, 3f), Random.Range(0.5f, 4f)),
                Random.Range(0.4f, 0.8f), new Color(0.6f, 1f, 0.3f), Random.Range(0.9f, 1.5f), 10006, FxUtil.Additive, 10f, 0.5f);
    }

    // ------------------------------------------------------------------ combat
    IEnumerator Fight()
    {
        fighting = true;
        if (entranceLock != null) entranceLock.SetLocked(true);
        // Entrée en scène : il descend du plafond
        transform.position = new Vector3((arenaMin.x + arenaMax.x) * 0.5f + 6f, arenaMax.y + 3f, 0f);
        SetVisible(true);
        // Il est invulnérable pendant l'apparition
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        AudioManager.Play("bzz", 1f, 0.45f);
        CameraShake(0.1f, 1.6f);
        if (GameUI.HasInstance) GameUI.Instance.PlayTitle(bossName, bossSubtitle);
        BuildBar();
        AudioManager.PlayMusic("musique_boss", 0.55f, 1.0f);
        yield return MoveTo(new Vector2((arenaMin.x + arenaMax.x) * 0.5f, arenaMax.y - 7f), 9f);
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = true;
        yield return Wait(0.5f);
        StartCoroutine(Harass());
        plates = FindObjectsOfType<CrumblePlate>();
        StartCoroutine(FloorBreaker());

        int last = -1;
        while (true)
        {
            if (wantSubmerge) { wantSubmerge = false; yield return Submerge(); }
            if (minions.Count > 0 && Time.time >= eatTime) yield return EatMinions();
            int id = PickAttack(last);
            last = id;
            switch (id)
            {
                case 0: yield return Hover(phase == 1 ? 1.0f : 0.6f); break;
                case 1: yield return Volley(phase == 1 ? 1 : 3, phase == 3 ? 18f : 14f); break;
                case 2: yield return Dive(); if (phase == 3 && Random.value < 0.5f) yield return Dive(); break;
                case 3: yield return AcidRain(phase == 3 ? 4.8f : 4.2f); break;
                case 4: yield return Geysers(phase == 1 ? 4 : phase == 2 ? 6 : 8); break;
                case 5: yield return Charge(); break;
                case 6: yield return Nova(); break;
                case 8: yield return AcidWave(); break;
                case 9: yield return Spiral(); break;
                case 10: yield return Summon(); break;
                default: yield return Swarm(phase == 2 ? 2 : 3); break;
            }
            yield return Wait(phase == 3 ? 0.1f : 0.3f);
        }
    }

    // Choix de la prochaine attaque : jamais deux fois la même, et les plus dures n'arrivent qu'avec les phases avancées
    int PickAttack(int last)
    {
        int[] pool;
        if (phase == 1) pool = new[] { 1, 2, 4, 5, 8, 9, 1, 2 };
        else if (phase == 2) pool = new[] { 1, 2, 3, 4, 5, 7, 6, 8, 9, 4 };
        else pool = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 6, 4, 5, 10, 10, 10 };
        int pick = pool[Random.Range(0, pool.Length)];
        // Pas de nouvelle invocation tant que les précédents sont là
        if (pick == 10 && LiveMinions() > 0) pick = pool[Random.Range(0, 12)];
        int guard = 0;
        while (pick == last && guard++ < 8) pick = pool[Random.Range(0, pool.Length)];
        return pick;
    }

    float Speed(float s) { return s * (phase == 3 ? 1.3f : phase == 2 ? 1.12f : 1f); }

    IEnumerator PhaseChange()
    {
        AudioManager.Play("bzz", 0.9f, 0.5f);
        CameraShake(0.08f, 0.8f);
        if (GameUI.HasInstance) GameUI.Instance.Toast(phase == 2 ? "Séraphel se déchaîne !" : "Séraphel est enragé !");
        yield break;
    }

    // ------------------------------------------------------------------ attaques
    IEnumerator Hover(float duration)
    {
        float t = 0f;
        Vector2 target = PickHoverPoint();
        float retarget = 0f;
        while (t < duration)
        {
            float dt = Time.deltaTime * Mult;
            t += dt;
            retarget -= dt;
            if (retarget <= 0f) { target = PickHoverPoint(); retarget = 1.1f; }
            Step(target, Speed(5f), dt);
            yield return null;
        }
    }

    Vector2 PickHoverPoint()
    {
        float side = Random.value < 0.5f ? -1f : 1f;
        float x = player.position.x + side * Random.Range(3f, 7f);
        float y = player.position.y + Random.Range(3.5f, 7f);
        return Clamp(new Vector2(x, y));
    }

    IEnumerator Volley(int count, float spread)
    {
        // Il se place à distance, à peu près à la hauteur du joueur, puis tremble en rougissant
        float side = player.position.x > (arenaMin.x + arenaMax.x) * 0.5f ? -1f : 1f;
        Vector2 spot = Clamp(new Vector2(player.position.x + side * Random.Range(8f, 11f), player.position.y + Random.Range(1.5f, 3.5f)));
        yield return MoveTo(spot, Speed(8f));
        float tele = phase == 3 ? 0.45f : 0.65f;
        pose = Pose.Tell;
        for (float t = 0f; t < tele; t += Time.deltaTime * Mult)
        {
            health.alert = 0.35f + 0.35f * Mathf.Abs(Mathf.Sin(t * 20f));
            transform.position += (Vector3)(Random.insideUnitCircle * 0.02f);
            facing = player.position.x > transform.position.x ? 1 : -1;
            yield return null;
        }
        health.alert = 0f;
        pose = Pose.Idle;
        AudioManager.Play("bzz", 0.6f, 0.7f);
        Vector3 mouth = MouthPosition();
        Vector2 toPlayer = ((Vector2)player.position + new Vector2(0f, 0.2f)) - (Vector2)mouth;
        for (int i = 0; i < count; i++)
        {
            float a = count == 1 ? 0f : Mathf.Lerp(-spread, spread, i / (float)(count - 1));
            Vector2 d = Quaternion.Euler(0f, 0f, a) * toPlayer;
            BossNeedle.Spawn(mouth, d, 30, Speed(12.5f));
        }
        yield return Wait(0.45f);
    }

    IEnumerator Dive()
    {
        // Il se place au-dessus du joueur et tremble
        Vector2 above = Clamp(new Vector2(player.position.x + Random.Range(-2f, 2f), Mathf.Min(arenaMax.y - 2f, player.position.y + 7.5f)));
        yield return MoveTo(above, Speed(8f));
        float tele = phase == 3 ? 0.4f : 0.6f;
        pose = Pose.Tell;
        for (float t = 0f; t < tele; t += Time.deltaTime * Mult)
        {
            health.alert = 0.4f + 0.4f * Mathf.Abs(Mathf.Sin(t * 24f));
            transform.position += (Vector3)(Random.insideUnitCircle * 0.03f);
            yield return null;
        }
        health.alert = 0f;
        // Plongeon vers l'endroit où se trouvait le joueur
        Vector2 target = new Vector2(AvoidPits(Mathf.Clamp(player.position.x, arenaMin.x + 1f, arenaMax.x - 1f)), floorY + 1.1f);
        AudioManager.Play("bzz", 0.8f, 1.3f);
        pose = Pose.Dash;
        yield return MoveTo(target, Speed(17f));
        pose = Pose.Stun;
        // Impact : il éclate un peu, l'acide jaillit, et il reste étourdi
        AcidSplash.Spawn(new Vector3(transform.position.x, floorY, 0f), 1.8f, 4f, 14, true);
        AudioManager.Play("impact", 0.8f, 0.5f);
        CameraShake(0.1f, 0.4f);
        SpurtAcid(10);
        float stun = phase == 3 ? 1.1f : 1.7f;
        for (float t = 0f; t < stun; t += Time.deltaTime)
        {
            health.alert = 0f;
            yield return null;
        }
        pose = Pose.Idle;
        yield return MoveTo(Clamp(new Vector2(transform.position.x, floorY + 5f)), Speed(6f));
    }

    // Les attaques ordinaires ne le font jamais entrer dans l'acide : on décale la cible hors des bassins
    float AvoidPits(float x)
    {
        if (pitCenters == null) return x;
        foreach (Vector2 c in pitCenters)
            if (Mathf.Abs(x - c.x) < 3.6f) return c.x + (x >= c.x ? 3.6f : -3.6f);
        return x;
    }

    IEnumerator AcidRain(float duration)
    {
        // Il remonte vers le plafond puis le balaie d'un bout à l'autre en laissant tomber des gouttes
        float topY = arenaMax.y - 2f;
        float fromX = player.position.x > (arenaMin.x + arenaMax.x) * 0.5f ? arenaMin.x + 3f : arenaMax.x - 3f;
        float toX = fromX < (arenaMin.x + arenaMax.x) * 0.5f ? arenaMax.x - 3f : arenaMin.x + 3f;
        yield return MoveTo(new Vector2(fromX, topY), Speed(9f));
        AudioManager.Play("bzz", 0.7f, 0.6f);
        float t = 0f, drop = 0f;
        while (t < duration)
        {
            float dt = Time.deltaTime * Mult;
            t += dt;
            drop -= dt;
            float k = Mathf.Clamp01(t / duration);
            Vector2 target = new Vector2(Mathf.Lerp(fromX, toX, k), topY + Mathf.Sin(t * 2f) * 0.8f);
            Step(target, Speed(8f), dt);
            if (drop <= 0f)
            {
                drop = phase == 3 ? 0.2f : 0.3f;
                AcidDrop.Spawn(transform.position + new Vector3(Random.Range(-1.2f, 1.2f), -0.9f, 0f), 1.2f, 10);
            }
            yield return null;
        }
        yield return MoveTo(Clamp(new Vector2(transform.position.x, topY - 5f)), Speed(6f));
    }

    // Charge : il se place à un bout de la salle à la hauteur du joueur, puis traverse tout l'écran
    IEnumerator Charge()
    {
        float mid = (arenaMin.x + arenaMax.x) * 0.5f;
        bool fromLeft = (player.position.x - mid) > 0f;
        float startX = fromLeft ? arenaMin.x + 2f : arenaMax.x - 2f;
        float endX = fromLeft ? arenaMax.x - 2f : arenaMin.x + 2f;
        float y = Mathf.Clamp(player.position.y + 0.8f, floorY + 1.4f, arenaMax.y - 3f);
        yield return MoveTo(new Vector2(startX, y), Speed(11f));
        facing = fromLeft ? 1 : -1;

        // Une ligne rouge annonce le couloir de la charge
        Sprite lg = FxUtil.LoadSprite("lueur_douce");
        SpriteRenderer warn = null;
        if (lg != null)
        {
            warn = FxUtil.MakeSprite("Ligne de charge", lg, new Vector3(mid, y, 0f), 9990, new Color(1f, 0.25f, 0.2f, 0.4f), FxUtil.Additive, null);
            warn.transform.localScale = new Vector3((arenaMax.x - arenaMin.x) * 0.55f, 0.9f, 1f);
        }
        float tele = phase == 3 ? 0.55f : 0.8f;
        pose = Pose.Tell;
        AudioManager.Play("bzz", 0.7f, 0.5f);
        for (float t = 0f; t < tele; t += Time.deltaTime * Mult)
        {
            health.alert = 0.4f + 0.4f * Mathf.Abs(Mathf.Sin(t * 22f));
            transform.position += (Vector3)(Random.insideUnitCircle * 0.03f);
            if (warn != null) warn.color = new Color(1f, 0.25f, 0.2f, 0.25f + 0.35f * Mathf.Abs(Mathf.Sin(t * 14f)));
            yield return null;
        }
        health.alert = 0f;
        if (warn != null) Destroy(warn.gameObject);

        AudioManager.Play("bzz", 0.9f, 1.4f);
        pose = Pose.Dash;
        float dirX = fromLeft ? 1f : -1f;
        float dropAt = transform.position.x;
        while ((fromLeft && transform.position.x < endX) || (!fromLeft && transform.position.x > endX))
        {
            float dt = Time.deltaTime * Mult;
            transform.position += new Vector3(dirX * Speed(19f) * dt, 0f, 0f);
            if (Mathf.Abs(transform.position.x - dropAt) > 2.2f)
            {
                dropAt = transform.position.x;
                AcidDrop.Spawn(transform.position + new Vector3(0f, -0.8f, 0f), 1f, 8);
            }
            yield return null;
        }
        // Il s'écrase contre le mur : de l'acide jaillit et il reste étourdi
        AcidSplash.Spawn(new Vector3(endX + dirX * 1.2f, y, 0f), 1.8f, 3.5f, 14, false);
        AudioManager.Play("impact", 0.9f, 0.5f);
        CameraShake(0.12f, 0.5f);
        SpurtAcid(12);
        pose = Pose.Stun;
        yield return Wait(phase == 3 ? 0.9f : 1.5f);
        pose = Pose.Idle;
        yield return MoveTo(Clamp(new Vector2(mid, y + 3f)), Speed(7f));
    }

    // Geysers : des colonnes d'acide jaillissent du sol, d'abord sous le joueur puis de plus en plus loin
    IEnumerator Geysers(int count)
    {
        yield return MoveTo(Clamp(new Vector2(player.position.x, arenaMax.y - 4f)), Speed(9f));
        AudioManager.Play("bzz", 0.7f, 0.6f);
        CameraShake(0.05f, 0.4f);
        float baseX = player.position.x;
        float warn = phase == 3 ? 0.8f : 1.0f;
        float[] offsets = { 0f, 3.4f, -3.4f, 6.8f, -6.8f, 10.2f, -10.2f, 13.6f };
        for (int i = 0; i < count && i < offsets.Length; i++)
        {
            float x = Mathf.Clamp(baseX + offsets[i], arenaMin.x + 1.5f, arenaMax.x - 1.5f);
            AcidColumn.Spawn(new Vector3(x, floorY, 0f), warn, 5.5f, 22);
            for (float t = 0f; t < 0.28f; t += Time.deltaTime * Mult) yield return null;
        }
        pose = Pose.Tell;
        yield return Wait(warn + 0.5f);
        pose = Pose.Idle;
    }

    // Nova : un anneau de trompes dans toutes les directions (on passe entre elles en sautant ou en se baissant sur une plateforme)
    IEnumerator Nova()
    {
        Vector2 center = Clamp(new Vector2((arenaMin.x + arenaMax.x) * 0.5f, floorY + 8f));
        yield return MoveTo(center, Speed(9f));
        pose = Pose.Tell;
        for (float t = 0f; t < 0.7f; t += Time.deltaTime * Mult)
        {
            health.alert = 0.4f + 0.4f * Mathf.Abs(Mathf.Sin(t * 20f));
            transform.position += (Vector3)(Random.insideUnitCircle * 0.03f);
            yield return null;
        }
        health.alert = 0f;
        pose = Pose.Idle;
        int rings = phase == 3 ? 3 : 2;
        for (int r = 0; r < rings; r++)
        {
            AudioManager.Play("bzz", 0.7f, 0.8f);
            CameraShake(0.05f, 0.25f);
            int n = 10;
            for (int i = 0; i < n; i++)
            {
                float a = (i / (float)n) * 360f + r * (360f / n) * 0.5f;
                Vector2 d = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                BossNeedle.Spawn(transform.position + (Vector3)(d * 1.1f), d, 20, Speed(8.5f));
            }
            for (float t = 0f; t < 0.65f; t += Time.deltaTime * Mult) yield return null;
        }
        yield return Wait(0.5f);
    }

    // Essaim : des globules d'acide poursuivent le joueur ; un sort les fait éclater
    IEnumerator Swarm(int count)
    {
        yield return MoveTo(Clamp(new Vector2(player.position.x + (player.position.x > (arenaMin.x + arenaMax.x) * 0.5f ? -6f : 6f), floorY + 7f)), Speed(8f));
        AudioManager.Play("bzz", 0.7f, 0.65f);
        for (int i = 0; i < count; i++)
        {
            facing = player.position.x > transform.position.x ? 1 : -1;
            AcidBlob.Spawn(MouthPosition(), new Vector2(facing * 4f, 3f + i));
            AudioManager.Play("acide", 0.5f, 1.3f);
            for (float t = 0f; t < 0.45f; t += Time.deltaTime * Mult) yield return null;
        }
        yield return Hover(0.7f);
    }

    // Vague d'acide : il frappe le sol et une onde d'acide court des deux côtés (il faut sauter par-dessus)
    IEnumerator AcidWave()
    {
        float cx = Mathf.Clamp(player.position.x, arenaMin.x + 12f, arenaMax.x - 12f);
        yield return MoveTo(Clamp(new Vector2(cx, floorY + 7f)), Speed(9f));
        pose = Pose.Tell;
        for (float t = 0f; t < 0.6f; t += Time.deltaTime * Mult)
        {
            health.alert = 0.4f + 0.4f * Mathf.Abs(Mathf.Sin(t * 22f));
            transform.position += (Vector3)(Random.insideUnitCircle * 0.03f);
            yield return null;
        }
        health.alert = 0f;
        pose = Pose.Dash;
        yield return MoveTo(new Vector2(transform.position.x, floorY + 1.1f), Speed(18f));
        AudioManager.Play("impact", 0.9f, 0.6f);
        CameraShake(0.1f, 0.4f);
        pose = Pose.Stun;
        float x0 = transform.position.x;
        for (int i = 1; i <= 9; i++)
        {
            float off = i * 2.4f;
            if (x0 - off > arenaMin.x + 0.8f) AcidSplash.Spawn(new Vector3(x0 - off, floorY, 0f), 1.0f, 1.0f, 14, true);
            if (x0 + off < arenaMax.x - 0.8f) AcidSplash.Spawn(new Vector3(x0 + off, floorY, 0f), 1.0f, 1.0f, 14, true);
            for (float t = 0f; t < 0.08f; t += Time.deltaTime * Mult) yield return null;
        }
        yield return Wait(phase == 3 ? 0.5f : 0.9f);
        pose = Pose.Idle;
        yield return MoveTo(Clamp(new Vector2(transform.position.x, floorY + 6f)), Speed(7f));
    }

    // Spirale : il crache des trompes en tournant
    IEnumerator Spiral()
    {
        float cx = (arenaMin.x + arenaMax.x) * 0.5f + Random.Range(-8f, 8f);
        yield return MoveTo(Clamp(new Vector2(cx, floorY + 9f)), Speed(9f));
        pose = Pose.Tell;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime * Mult)
        {
            health.alert = 0.4f + 0.4f * Mathf.Abs(Mathf.Sin(t * 22f));
            transform.position += (Vector3)(Random.insideUnitCircle * 0.03f);
            yield return null;
        }
        health.alert = 0f;
        float a = Random.Range(0f, 360f);
        float turn = Random.value < 0.5f ? 23f : -23f;
        int shots = phase == 3 ? 36 : 28;
        for (int i = 0; i < shots; i++)
        {
            a += turn;
            Vector2 d = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
            BossNeedle.Spawn(transform.position + (Vector3)(d * 1.1f), d, 16, Speed(7.5f));
            if (i % 4 == 0) AudioManager.Play("trompe", 0.4f, 1.1f);
            for (float t = 0f; t < 0.07f; t += Time.deltaTime * Mult) yield return null;
        }
        pose = Pose.Idle;
        yield return Wait(0.3f);
    }

    // Entre deux attaques, il crache de temps en temps une trompe isolée
    IEnumerator Harass()
    {
        while (true)
        {
            float wait = (phase == 1 ? 3.2f : phase == 2 ? 2.2f : 1.5f) * Random.Range(0.8f, 1.2f);
            for (float t = 0f; t < wait; t += Time.deltaTime * Mult) yield return null;
            if (pose != Pose.Idle || hidden || player == null) continue;
            Vector3 mouth = MouthPosition();
            Vector2 d = ((Vector2)player.position + new Vector2(0f, 0.3f)) - (Vector2)mouth;
            if (d.magnitude < 3f) continue;
            AudioManager.Play("trompe", 0.4f, Random.Range(0.95f, 1.15f));
            BossNeedle.Spawn(mouth, d, 14, Speed(10f));
        }
    }

    // ------------------------------------------------------------------ phase 2
    int LiveMinions()
    {
        int n = 0;
        foreach (FlyingMinion m in minions) if (m != null && m.IsAlive) n++;
        return n;
    }

    // À la moitié de sa vie : il plonge dans un bassin d'acide, regagne un peu de vie, puis revient enragé
    IEnumerator Submerge()
    {
        hasSubmerged = true;   // une seule fois par combat
        // il choisit le bassin le plus proche
        Vector2 pit = new Vector2((arenaMin.x + arenaMax.x) * 0.5f, floorY);
        if (pitCenters != null && pitCenters.Length > 0)
        {
            float best = 1e9f;
            foreach (Vector2 c in pitCenters)
            {
                float d = Mathf.Abs(c.x - transform.position.x);
                if (d < best) { best = d; pit = c; }
            }
        }
        health.alert = 0f;
        pose = Pose.Dash;
        AudioManager.Play("bzz", 0.9f, 0.5f);
        if (GameUI.HasInstance) GameUI.Instance.Toast("Séraphel plonge dans l'acide !");
        yield return MoveTo(new Vector2(pit.x, pit.y + 6f), Speed(10f));
        yield return MoveTo(new Vector2(pit.x, pit.y + 0.4f), Speed(14f));
        // plouf : il disparaît sous la surface
        AcidSplash.Spawn(new Vector3(pit.x, pit.y, 0f), 1.6f, 1.5f, 0, true);
        AudioManager.Play("acide", 0.9f, 0.6f);
        CameraShake(0.08f, 0.4f);
        SpurtAcid(16);
        hidden = true;
        SetVisible(false);
        foreach (BossNeedle n in FindObjectsOfType<BossNeedle>()) Destroy(n.gameObject);

        // il se régénère petit à petit
        float duration = Random.Range(2f, 5f);
        float healPerSecond = 2f;
        float acc = 0f;
        Sprite bub = FxUtil.LoadSprite("bulle");
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            acc += healPerSecond * Time.deltaTime;
            while (acc >= 1f) { acc -= 1f; health.Heal(1); }
            if (bub != null && Random.value < 0.6f)
                SimpleParticle.Spawn(bub, new Vector3(pit.x + Random.Range(-1.5f, 1.5f), pit.y, 0f), new Vector2(0f, Random.Range(1f, 2.5f)),
                    0.8f, new Color(0.7f, 1f, 0.4f, 0.9f), Random.Range(0.6f, 1.2f), 9999, null, -0.3f, 0.5f);
            yield return null;
        }

        // il resurgit, enragé
        transform.position = new Vector3(pit.x, pit.y + 0.4f, 0f);
        hidden = false;
        SetVisible(true);
        enraged = true;
        phase = 3;
        facing = player.position.x > transform.position.x ? 1 : -1;
        AcidSplash.Spawn(new Vector3(pit.x, pit.y, 0f), 2.0f, 3f, 14, true);
        AudioManager.Play("bzz", 1f, 0.4f);
        AudioManager.Play("impact", 0.7f, 0.5f);
        CameraShake(0.14f, 0.9f);
        SpurtAcid(20);
        if (GameUI.HasInstance) GameUI.Instance.Toast("Séraphel est enragé !");
        pose = Pose.Idle;
        yield return MoveTo(Clamp(new Vector2(pit.x, floorY + 8f)), Speed(11f));
        yield return Wait(0.3f);
    }

    // Invocation : il se place au centre et fait surgir de petits moustiques
    IEnumerator Summon()
    {
        Vector2 center = Clamp(new Vector2((arenaMin.x + arenaMax.x) * 0.5f, floorY + 9f));
        yield return MoveTo(center, Speed(10f));
        pose = Pose.Tell;
        AudioManager.Play("bzz", 0.9f, 0.45f);
        for (float t = 0f; t < 0.9f; t += Time.deltaTime * Mult)
        {
            health.alert = 0.4f + 0.4f * Mathf.Abs(Mathf.Sin(t * 20f));
            yield return null;
        }
        health.alert = 0f;
        pose = Pose.Idle;
        int count = health.Health < health.maxHealth / 4 ? 4 : 3;
        // Chaque petit se forme d'abord (tourbillon de lueurs) : le joueur voit où il va surgir et ne le prend jamais en pleine figure
        Vector3[] pts = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            bool left = i % 2 == 0;
            pts[i] = new Vector3(left ? arenaMin.x + 3f : arenaMax.x - 3f, floorY + Random.Range(6f, 14f), 0f);
            SpawnFx.Telegraph(pts[i], true, i == 2 ? 0.075f : 0.06f);
            yield return Wait(0.2f);
        }
        yield return Wait(SpawnFx.MinionTime - 0.2f);
        for (int i = 0; i < count; i++)
        {
            FlyingMinion m = SpawnFx.CreateMinion(pts[i], i == 2, null);
            minions.Add(m);
            AudioManager.Play("bzz", 0.5f, Random.Range(1.1f, 1.5f));
        }
        eatTime = Time.time + 10f;
        if (GameUI.HasInstance && !summonHint)
        {
            summonHint = true;
            GameUI.Instance.Toast("Tue les petits moustiques avant qu'il ne les dévore !");
        }
        yield return Wait(0.4f);
    }

    // Les petits moustiques encore vivants sont attirés vers lui et dévorés : il regagne 1/25 de sa vie par moustique
    IEnumerator EatMinions()
    {
        List<FlyingMinion> alive = new List<FlyingMinion>();
        foreach (FlyingMinion m in minions) if (m != null && m.IsAlive) alive.Add(m);
        minions.Clear();
        if (alive.Count == 0) yield break;

        Vector2 center = Clamp(new Vector2((arenaMin.x + arenaMax.x) * 0.5f, floorY + 9f));
        yield return MoveTo(center, Speed(12f));
        pose = Pose.Tell;
        AudioManager.Play("bzz", 0.8f, 0.5f);
        if (GameUI.HasInstance) GameUI.Instance.Toast("Séraphel dévore ses petits !");
        foreach (FlyingMinion m in alive) m.Pull(transform);

        int eaten = 0;
        float timeout = 4f;
        while (eaten < alive.Count && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            for (int i = 0; i < alive.Count; i++)
            {
                FlyingMinion m = alive[i];
                if (m == null) continue;
                if (Vector3.Distance(m.transform.position, transform.position) < 0.7f)
                {
                    Destroy(m.gameObject);
                    alive[i] = null;
                    eaten++;
                    health.Heal(Mathf.Max(1, Mathf.RoundToInt(health.maxHealth / 25f)));
                    hitKick = 0.7f;
                    AudioManager.Play("ecrase", 0.7f, 0.6f);
                    SpurtAcid(6);
                }
            }
            yield return null;
        }
        foreach (FlyingMinion m in alive) if (m != null) Destroy(m.gameObject);
        pose = Pose.Idle;
        yield return Wait(0.5f);
    }

    // Le combat qui dure fait s'effondrer le sol, plaque après plaque
    IEnumerator FloorBreaker()
    {
        if (plates == null || plates.Length == 0) yield break;
        float wait = 16f;
        while (true)
        {
            for (float t = 0f; t < wait; t += Time.deltaTime * Mathf.Max(0.3f, Mult)) yield return null;
            if (hidden) { wait = 1f; continue; }
            List<CrumblePlate> candidates = new List<CrumblePlate>();
            foreach (CrumblePlate pl in plates) if (pl != null && !pl.IsBroken && !pl.IsBreaking) candidates.Add(pl);
            if (candidates.Count == 0) yield break;
            candidates[Random.Range(0, candidates.Count)].Break();
            wait = (enraged ? 9f : 15f) * Random.Range(0.85f, 1.15f);
        }
    }

    // ------------------------------------------------------------------ animation du corps
    void BuildRig()
    {
        if (body == null) return;
        baseScale = Mathf.Abs(body.transform.localScale.x);
        Sprite corps = FxUtil.LoadSprite("boss_corps");
        if (corps == null) return;
        body.sprite = corps;
        Vector2[] roots = { new Vector2(80f, 68f), new Vector2(126f, 82f), new Vector2(141f, 78f) };
        wingPivots = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            Sprite ws = FxUtil.LoadSprite("boss_aile_" + i);
            if (ws == null) continue;
            Vector3 root = PixelToLocal(ws, roots[i]);
            GameObject pivot = new GameObject("Aile " + i);
            pivot.transform.SetParent(body.transform, false);
            pivot.transform.localPosition = root;
            GameObject wing = new GameObject("Sprite");
            wing.transform.SetParent(pivot.transform, false);
            wing.transform.localPosition = -root;
            SpriteRenderer r = wing.AddComponent<SpriteRenderer>();
            r.sprite = ws;
            r.sortingOrder = body.sortingOrder - 1;
            wingPivots[i] = pivot.transform;
        }
        Sprite lg = FxUtil.LoadSprite("lueur");
        if (lg != null)
        {
            abdomenGlow = FxUtil.MakeSprite("Lueur du ventre", lg, Vector3.zero, body.sortingOrder + 1, new Color(0.55f, 1f, 0.2f, 0.3f), FxUtil.Additive, body.transform);
            abdomenGlow.transform.localPosition = PixelToLocal(corps, new Vector2(205f, 150f));
            abdomenGlow.transform.localScale = new Vector3(5f, 5f, 1f);
        }
    }

    static Vector3 PixelToLocal(Sprite s, Vector2 px)
    {
        return new Vector3((px.x - s.pivot.x) / s.pixelsPerUnit, ((s.rect.height - px.y) - s.pivot.y) / s.pixelsPerUnit, 0f);
    }

    void AnimateRig(float dt)
    {
        if (body == null) return;
        dt = Mathf.Max(0.0001f, dt);
        Vector3 vel = (transform.position - lastPos) / dt;
        lastPos = transform.position;
        smoothVel = Vector3.Lerp(smoothVel, vel, Mathf.Clamp01(8f * dt));
        float t = Time.time;
        hitKick = Mathf.MoveTowards(hitKick, 0f, dt * 4f);

        float lean = Mathf.Clamp(-facing * smoothVel.x * 1.5f, -28f, 28f);
        float amp = 16f, freq = 46f, fold = 1f, droop = 0f, stretchX = 1f, glow = 0.3f;
        switch (pose)
        {
            case Pose.Tell:
                lean = facing * 14f;          // il se cabre
                amp = 6f; freq = 85f; stretchX = 0.96f; glow = 0.6f + 0.3f * Mathf.Abs(Mathf.Sin(t * 20f));
                break;
            case Pose.Dash:
                lean = -facing * 24f;         // il pique en avant, ailes repliées
                amp = 3f; freq = 60f; fold = 0.6f; stretchX = 1.06f; glow = 0.5f;
                break;
            case Pose.Stun:
                lean = facing * 22f;          // assommé : ailes qui pendent
                amp = 3f; freq = 5f; droop = 38f; glow = 0.15f;
                break;
        }
        lean += facing * hitKick * 16f;
        stretchX *= 1f + hitKick * 0.08f;
        float breathe = 1f + Mathf.Sin(t * 3f) * 0.015f;
        float bob = Mathf.Sin(bobPhase) * (pose == Pose.Stun ? 0.03f : 0.12f);

        Transform bt = body.transform;
        bt.localPosition = new Vector3(0f, bob, 0f);
        bt.localRotation = Quaternion.Euler(0f, 0f, lean + Mathf.Sin(bobPhase * 0.5f) * 2.5f);
        bt.localScale = new Vector3((facing > 0 ? -1f : 1f) * baseScale * stretchX * breathe, baseScale / breathe, 1f);

        if (wingPivots != null)
        {
            for (int i = 0; i < wingPivots.Length; i++)
            {
                if (wingPivots[i] == null) continue;
                float flap = Mathf.Sin(t * freq + wingPhase[i]) * amp + droop * (i == 0 ? 1f : -1f);
                wingPivots[i].localRotation = Quaternion.Euler(0f, 0f, flap);
                wingPivots[i].localScale = new Vector3(fold, fold, 1f);
            }
        }
        if (abdomenGlow != null)
            abdomenGlow.color = new Color(0.55f, 1f, 0.2f, glow * (0.8f + 0.2f * Mathf.Sin(t * 4f)));
    }

    // ------------------------------------------------------------------ déplacements
    void Step(Vector2 target, float speed, float scaledDt)
    {
        Vector2 p = Vector2.MoveTowards(transform.position, target, speed * scaledDt);
        transform.position = new Vector3(p.x, p.y, 0f);
    }

    IEnumerator MoveTo(Vector2 target, float speed)
    {
        while (Vector2.Distance(transform.position, target) > 0.15f)
        {
            Step(target, speed, Time.deltaTime * Mult);
            yield return null;
        }
    }

    IEnumerator Wait(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime * Mult) yield return null;
    }

    Vector2 Clamp(Vector2 p)
    {
        return new Vector2(Mathf.Clamp(p.x, arenaMin.x + 1.5f, arenaMax.x - 1.5f), Mathf.Clamp(p.y, floorY + 1.1f, arenaMax.y - 1.5f));
    }

    Vector3 MouthPosition()
    {
        return transform.position + new Vector3(facing * 1.2f, -0.55f, 0f);
    }

    void CameraShake(float amount, float duration)
    {
        if (Camera.main == null) return;
        CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
        if (cam != null) cam.Shake(amount, duration);
    }

    // ------------------------------------------------------------------ fin du combat
    void ResetFight()
    {
        StopAllCoroutines();
        routine = null;
        fighting = false;
        phase = 1;
        enraged = false; wantSubmerge = false; hidden = false; hasSubmerged = false;
        foreach (FlyingMinion m in minions) if (m != null) Destroy(m.gameObject);
        minions.Clear();
        if (plates != null) foreach (CrumblePlate pl in plates) if (pl != null) pl.Restore();
        pose = Pose.Idle;
        AudioManager.PlayMusic("musique_donjon", 0.5f, 1.5f);
        health.alert = 0f;
        health.Restore();
        lastHealth = health.maxHealth;
        transform.position = homePosition;
        SetVisible(false);
        if (entranceLock != null) entranceLock.SetLocked(false);
        if (barRoot != null) Destroy(barRoot.gameObject);
        bar = null;
        // Les flaques et gouttes restantes disparaissent d'elles-mêmes ; on enlève les trompes en vol
        foreach (BossNeedle n in FindObjectsOfType<BossNeedle>()) Destroy(n.gameObject);
    }

    void OnDied(bool stomped)
    {
        dead = true;
        StopAllCoroutines();
        pose = Pose.Stun;
        health.alert = 0f;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        foreach (EnemyDamage d in GetComponentsInChildren<EnemyDamage>()) d.enabled = false;
        foreach (FlyingMinion m in minions) if (m != null) Destroy(m.gameObject);
        minions.Clear();
        if (plates != null) foreach (CrumblePlate pl in plates) if (pl != null) pl.Restore();
        SaveSystem.SetFlag(defeatedFlag);
        SaveSystem.SetFlag(Lever.Key(exitFlag));
        if (entranceLock != null) entranceLock.SetLocked(false);
        if (barRoot != null) Destroy(barRoot.gameObject, 0.5f);
        foreach (BossNeedle n in FindObjectsOfType<BossNeedle>()) Destroy(n.gameObject);
        StartCoroutine(DeathScene());
    }

    IEnumerator DeathScene()
    {
        AudioManager.StopMusic(1.2f);
        AudioManager.Play("explosion_livre", 0.9f, 0.6f);
        CameraShake(0.12f, 1.4f);
        for (int i = 0; i < 6; i++)
        {
            SpurtAcid(14);
            yield return new WaitForSeconds(0.15f);
        }
        yield return AbdomenBurst();
        AcidSplash.Spawn(new Vector3(transform.position.x, floorY, 0f), 2.2f, 3f, 0, true);
        if (GameUI.HasInstance)
        {
            GameUI.Instance.PlayTitle("Séraphel s'effondre", "Une issue s'ouvre au fond de la salle");
            GameUI.Instance.Toast("Une grille s'est ouverte au fond de la salle.");
        }
        AudioManager.PlayMusic("musique_donjon", 0.4f, 3f);
        if (collapse != null) collapse.Play();
        Destroy(gameObject, 1.2f);
    }

    /// <summary>L'abdomen, plein d'acide, gonfle puis explose : brûle le héros s'il est trop près, et laisse tomber un abdomen à ramasser.</summary>
    IEnumerator AbdomenBurst()
    {
        Transform bt = body != null ? body.transform : transform;
        Vector3 baseScale = bt.localScale;
        float swell = 1.3f;
        for (float t = 0f; t < swell; t += Time.deltaTime)
        {
            float k = t / swell;
            bt.localScale = baseScale * (1f + 0.3f * k * k + 0.02f * Mathf.Sin(t * 60f));
            if (body != null) body.color = Color.Lerp(Color.white, new Color(0.65f, 1f, 0.4f), k);
            if (Random.value < 0.35f) SpurtAcid(2);
            if (Random.value < Time.deltaTime * (4f + 10f * k)) AudioManager.Play("acide", 0.4f, 0.8f + 0.7f * k);
            yield return null;
        }
        Vector3 at = transform.position;
        // explosion
        AudioManager.Play("explosion_livre", 1f, 0.8f);
        CameraShake(0.3f, 0.9f);
        SpurtAcid(70);
        Sprite e = FxUtil.LoadSprite("eclat");
        if (e != null)
            for (int i = 0; i < 40; i++)
            {
                float a = Random.value * Mathf.PI * 2f, sp = Random.Range(3f, 9f);
                SimpleParticle.Spawn(e, at, new Vector2(Mathf.Cos(a) * sp, Mathf.Sin(a) * sp + 2f), Random.Range(0.6f, 1.1f),
                    new Color(0.6f, 1f, 0.3f), Random.Range(1f, 2f), 10006, FxUtil.Additive, 10f, 0.4f);
            }
        AcidSplash.Spawn(new Vector3(at.x, floorY, 0f), 2.6f, 3.5f, 0, true);
        if (GameUI.HasInstance) StartCoroutine(GameUI.Instance.Flash(new Color(0.6f, 1f, 0.3f, 0.55f), 0.5f));
        if (player != null && playerHealth != null && !AcidImmunity.Has)
        {
            float d = Vector2.Distance(player.position, at);
            if (d < 6f && playerHealth.CanTakeDamage)
            {
                playerHealth.TakeDamage(d < 3.5f ? 30 : 15);
                if (playerMovement != null && playerHealth.CurrentHealth > 0)
                {
                    float side = Mathf.Sign(player.position.x - at.x);
                    if (side == 0f) side = 1f;
                    playerMovement.ApplyKnockback(new Vector2(9f * side, 7f), 0.35f);
                }
            }
        }
        SetVisible(false);
        yield return new WaitForSeconds(0.9f);
        // de l'acide naît un pouvoir : l'abdomen retombe à l'abri des fosses
        Vector3 rest = new Vector3(AvoidPits(Mathf.Clamp(at.x, arenaMin.x + 1.5f, arenaMax.x - 1.5f)), floorY + 0.9f, 0f);
        AbdomenPickup.Spawn(at, rest);
        yield return new WaitForSeconds(1.3f);
    }

    // ------------------------------------------------------------------ interface
    void BuildBar()
    {
        if (!GameUI.HasInstance || bar != null) return;
        GameUI ui = GameUI.Instance;
        barRoot = UIFactory.Rect("Barre du boss", ui.Root);
        barRoot.anchorMin = barRoot.anchorMax = new Vector2(0.5f, 1f);
        barRoot.pivot = new Vector2(0.5f, 1f);
        barRoot.anchoredPosition = new Vector2(0f, -36f);
        barRoot.sizeDelta = new Vector2(900f, 56f);

        Text label = UIFactory.Label("Nom", barRoot, ui.Font, 28, new Color(0.9f, 0.95f, 0.8f), TextAnchor.LowerCenter);
        label.rectTransform.anchorMin = new Vector2(0f, 1f);
        label.rectTransform.anchorMax = new Vector2(1f, 1f);
        label.rectTransform.pivot = new Vector2(0.5f, 0f);
        label.rectTransform.anchoredPosition = new Vector2(0f, 2f);
        label.rectTransform.sizeDelta = new Vector2(0f, 36f);
        label.fontStyle = FontStyle.Bold;
        label.text = bossName;

        Image frame = barRoot.gameObject.AddComponent<Image>();
        frame.color = new Color(0.05f, 0.07f, 0.05f, 0.92f);
        RectTransform inner = UIFactory.Rect("Interieur", barRoot);
        inner.anchorMin = Vector2.zero;
        inner.anchorMax = Vector2.one;
        inner.offsetMin = new Vector2(6f, 6f);
        inner.offsetMax = new Vector2(-6f, -6f);
        inner.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.2f, 0.1f, 1f);
        Image ghost = NewBarImage(inner, new Color(1f, 1f, 1f, 0.8f));
        Image fill = NewBarImage(inner, new Color(0.55f, 0.9f, 0.25f));
        bar = barRoot.gameObject.AddComponent<HealthBar>();
        bar.ghost = ghost;
        bar.fill = fill;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(new Color(0.9f, 0.3f, 0.2f), 0f), new GradientColorKey(new Color(0.6f, 0.85f, 0.2f), 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        bar.gradient = g;
        bar.SetMaxHealth(health.maxHealth);
        bar.SetHealth(health.maxHealth);
    }

    static Image NewBarImage(Transform parent, Color color)
    {
        RectTransform rt = UIFactory.Rect("Barre", parent);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }
}
