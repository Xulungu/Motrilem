using System.Collections;
using UnityEngine;

// Moustique piqueur : quand le joueur est proche, il s'arrête, tremble en rougissant,
// et sa trompe se "duplique" : une copie fantôme se forme puis file vers le joueur (StingerNeedle).
// La trompe lancée inflige plus de dégâts que le simple contact.
[RequireComponent(typeof(EnemyPatrol))]
public class MosquitoStinger : MonoBehaviour
{
    public float detectRange = 6f;
    [Tooltip("Durée pendant laquelle la copie de la trompe se forme (alerte)")]
    public float appearTime = 0.55f;
    [Tooltip("Petit recul avant le tir")]
    public float aimTime = 0.18f;
    public float cooldown = 2.4f;
    public int damage = 30;
    public float needleSpeed = 13f;

    private EnemyPatrol patrol;
    private EnemyVisuals visuals;
    private EnemyHealth health;
    private Transform player;
    private Transform mouth;
    private float cd;
    private Coroutine attack;
    private StingerNeedle needle;

    void Start()
    {
        patrol = GetComponent<EnemyPatrol>();
        visuals = GetComponent<EnemyVisuals>();
        health = GetComponent<EnemyHealth>();
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) player = pm.transform;
        cd = Random.Range(0.5f, 1.5f);

        // Repère qui suit la bouche du moustique (le visuel ondule et s'incline)
        mouth = new GameObject("Bouche").transform;
        mouth.SetParent(visuals != null && visuals.Visual != null ? visuals.Visual : transform, false);
    }

    void Update()
    {
        cd -= Time.deltaTime;
        if (mouth != null) UpdateMouth();
        if (attack != null || player == null || health == null || health.IsDead || health.IsStunned) return;
        if (cd > 0f) return;
        if (Vector2.Distance(player.position, transform.position) <= detectRange)
            attack = StartCoroutine(Attack());
    }

    void UpdateMouth()
    {
        // La trompe dessinée sur le sprite part vers l'avant et vers le bas
        bool facingRight = visuals != null && visuals.Renderer != null && visuals.Renderer.flipX;
        mouth.localPosition = new Vector3(facingRight ? 0.22f : -0.22f, -0.13f, 0f);
    }

    IEnumerator Attack()
    {
        patrol.paused = true;
        AudioManager.Play("bzz", 0.55f, Random.Range(0.95f, 1.1f));

        // Il se tourne vers le joueur
        if (visuals != null && visuals.Renderer != null) visuals.Renderer.flipX = player.position.x > transform.position.x;
        UpdateMouth();

        needle = StingerNeedle.Spawn(mouth, Vector3.zero, player, appearTime, aimTime, damage);
        needle.speed = needleSpeed;

        // Alerte : tremblement et rougeur pendant que la copie se forme
        float total = appearTime + aimTime;
        for (float t = 0; t < total; t += Time.deltaTime)
        {
            if (health.IsDead || health.IsStunned)
            {
                if (needle != null) needle.Dissipate();
                EndAttack();
                yield break;
            }
            if (visuals != null && visuals.Renderer != null) visuals.Renderer.flipX = player.position.x > transform.position.x;
            if (visuals != null) { visuals.Shake(0.03f); visuals.wingSpeed = 2.2f; }
            health.alert = 0.35f + 0.35f * Mathf.Abs(Mathf.Sin(t * 20f));
            yield return null;
        }
        health.alert = 0f;

        // Tir : le moustique a un petit mouvement de recul
        if (visuals != null && player != null) visuals.Hit((Vector2)(transform.position - player.position));
        yield return new WaitForSeconds(0.35f);
        EndAttack();
    }

    void EndAttack()
    {
        if (health != null) health.alert = 0f;
        if (visuals != null) visuals.wingSpeed = 1f;
        if (patrol != null) patrol.paused = false;
        cd = cooldown;
        attack = null;
        needle = null;
    }

    /// <summary>Annule l'attaque en cours (mort de l'ennemi).</summary>
    public void Cancel()
    {
        if (attack != null) StopCoroutine(attack);
        if (needle != null) needle.Dissipate();
        EndAttack();
        enabled = false;
    }
}
