using System.Collections;
using UnityEngine;

// Mesure en continu le comportement du joueur (PlayerProfile) et, au chargement d'une sauvegarde,
// replace le joueur à l'endroit où il a sauvegardé. Ajouté automatiquement par PlayerMovement.
public class PlayerBehaviorTracker : MonoBehaviour
{
    private PlayerMovement pm;
    private Rigidbody2D rb;
    private float sampleTimer;

    void Awake()
    {
        pm = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable() { if (pm != null) pm.OnJumped += OnJump; }
    void OnDisable() { if (pm != null) pm.OnJumped -= OnJump; }

    void OnJump() { PlayerProfile.Record(PlayerProfile.Event.Jump); }

    IEnumerator Start()
    {
        Vector2 ret;
        if (SaveSystem.ConsumeReturnSpawn(out ret))
        {
            Vector3 rpos = new Vector3(ret.x, ret.y, transform.position.z);
            transform.position = rpos;
            // Passage entre deux cartes : on garde l'élan (limité) ; le sol sûr sert de point de retour en cas de chute
            Vector2 carryV = SaveSystem.ConsumeReturnVelocity();
            carryV = new Vector2(Mathf.Clamp(carryV.x, -9f, 9f), Mathf.Clamp(carryV.y, -14f, 12f));
            if (rb != null) { rb.position = rpos; rb.velocity = carryV; }
            PlayerHealth h2 = GetComponent<PlayerHealth>();
            Vector2 safeGround;
            bool transit = SaveSystem.ConsumeReturnSafe(out safeGround);
            if (h2 != null)
            {
                if (transit) h2.SetArrivalGround(new Vector3(safeGround.x, safeGround.y, rpos.z));
                else h2.SetArrivalGround(rpos);
            }
            if (Camera.main != null)
            {
                CameraFollow cam2 = Camera.main.GetComponent<CameraFollow>();
                if (cam2 != null) cam2.SnapToTarget();
            }
            GameUI.Instance.SetFade(Color.black);
            int carried = SaveSystem.ConsumeReturnHealth();
            yield return null;
            if (carried > 0 && h2 != null) h2.SetHealth(carried);
            yield return GameUI.Instance.FadeTo(new Color(0, 0, 0, 0), transit ? 0.3f : 0.8f);
            yield break;
        }
        if (!SaveSystem.ConsumePendingApply()) yield break;

        // Reprise d'une sauvegarde : on se place devant la statue
        SaveData s = SaveSystem.Session;
        Vector3 pos = new Vector3(s.x, s.y, transform.position.z);
        transform.position = pos;
        if (rb != null) { rb.position = pos; rb.velocity = Vector2.zero; }
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null) health.SetSpawnPoint(pos);
        if (Camera.main != null)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.SnapToTarget();
        }
        GameUI.Instance.SetFade(Color.black);

        // La vie est appliquée une image plus tard (après l'initialisation de PlayerHealth)
        yield return null;
        if (health != null && s.health > 0) health.SetHealth(s.health);
        yield return GameUI.Instance.FadeTo(new Color(0, 0, 0, 0), 1f);
    }

    void Update()
    {
        if (pm == null || !pm.controlsEnabled || pm.externalControl) return;
        PlayerProfileData d = PlayerProfile.Data;
        float dt = Time.deltaTime;
        d.playTime += dt;
        if (!pm.isGrounded) d.timeAirborne += dt;
        if (rb != null && Mathf.Abs(rb.velocity.x) > 0.3f) d.timeMoving += dt; else d.timeIdle += dt;
        if (pm.Momentum > 0.9f) d.timeFullMomentum += dt;

        // Toutes les demi-secondes : distance à l'ennemi le plus proche (s'il y en a un pas trop loin)
        sampleTimer -= dt;
        if (sampleTimer <= 0f)
        {
            sampleTimer = 0.5f;
            float best = NearestEnemyDistance(10f);
            if (best < 10f)
            {
                d.enemyDistanceSum += best;
                d.enemyDistanceSamples++;
            }
        }
    }

    public float NearestEnemyDistance(float maxRange)
    {
        float best = maxRange;
        foreach (EnemyHealth e in FindObjectsOfType<EnemyHealth>())
        {
            if (e.IsDead || !e.countsAsEnemy) continue; // on ignore les cristaux
            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist < best) best = dist;
        }
        return best;
    }
}
