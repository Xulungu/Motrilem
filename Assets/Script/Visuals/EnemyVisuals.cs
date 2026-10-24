using UnityEngine;

// Animation procédurale du moustique : vol ondulant, inclinaison selon la direction,
// battements d'ailes plus rapides quand il accélère ou attaque, recul quand il est touché.
// Le sprite est déplacé dans un enfant "Visuel" (les hitbox restent sur l'objet d'origine).
[DefaultExecutionOrder(-100)]
public class EnemyVisuals : MonoBehaviour
{
    public float bobAmplitude = 0.06f;
    public float bobSpeed = 5f;
    public float maxTilt = 14f;

    public Transform Visual { get; private set; }
    public SpriteRenderer Renderer { get; private set; }

    private Animator anim;
    private Vector3 lastPos;
    private float tilt;
    private float phase;
    private Vector2 hitOffset;
    private float shake;
    [HideInInspector] public float wingSpeed = 1f;

    void Awake()
    {
        phase = Random.Range(0f, 10f);
        Detach();
        lastPos = transform.position;
    }

    void Detach()
    {
        SpriteRenderer rootSr = GetComponent<SpriteRenderer>();
        if (rootSr == null)
        {
            Renderer = GetComponentInChildren<SpriteRenderer>();
            Visual = Renderer != null ? Renderer.transform : transform;
            anim = GetComponentInChildren<Animator>();
            return;
        }
        Animator rootAnim = GetComponent<Animator>();

        GameObject v = new GameObject("Visuel");
        v.transform.SetParent(transform, false);
        Visual = v.transform;
        SpriteRenderer sr = v.AddComponent<SpriteRenderer>();
        sr.sprite = rootSr.sprite;
        sr.color = rootSr.color;
        sr.flipX = rootSr.flipX;
        sr.sortingLayerID = rootSr.sortingLayerID;
        sr.sortingOrder = rootSr.sortingOrder;
        sr.sharedMaterial = rootSr.sharedMaterial;
        Renderer = sr;
        if (rootAnim != null)
        {
            anim = v.AddComponent<Animator>();
            anim.runtimeAnimatorController = rootAnim.runtimeAnimatorController;
            Destroy(rootAnim);
        }
        Destroy(rootSr);

        EnemyPatrol patrol = GetComponent<EnemyPatrol>();
        if (patrol != null) patrol.graphics = sr;
    }

    /// <summary>Petit recul visuel quand l'ennemi est touché.</summary>
    public void Hit(Vector2 direction)
    {
        hitOffset = direction.normalized * 0.18f;
    }

    /// <summary>Tremblement (avant une attaque).</summary>
    public void Shake(float amount) { shake = amount; }

    void LateUpdate()
    {
        if (Visual == null) return;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 delta = transform.position - lastPos;
        lastPos = transform.position;
        float vx = delta.x / dt;
        float speed = delta.magnitude / dt;

        phase += dt * bobSpeed;
        float bob = Mathf.Sin(phase) * bobAmplitude;

        // Le moustique pique du nez dans la direction de son vol
        float targetTilt = Mathf.Clamp(-vx * 6f, -maxTilt, maxTilt);
        tilt = Mathf.Lerp(tilt, targetTilt, dt * 6f);

        hitOffset = Vector2.Lerp(hitOffset, Vector2.zero, dt * 12f);
        Vector2 shakeOffset = shake > 0f ? Random.insideUnitCircle * shake : Vector2.zero;
        shake = Mathf.MoveTowards(shake, 0f, dt * 0.5f);

        Visual.localPosition = new Vector3(hitOffset.x + shakeOffset.x, bob + hitOffset.y + shakeOffset.y, 0f);
        Visual.localRotation = Quaternion.Euler(0f, 0f, tilt + Mathf.Sin(phase * 0.5f) * 3f);

        // Légère respiration du corps
        float s = 1f + Mathf.Sin(phase * 2f) * 0.03f;
        Visual.localScale = new Vector3(s, 2f - s, 1f);

        if (anim != null) anim.speed = wingSpeed * (1f + Mathf.Clamp01(speed / 4f) * 0.6f);
    }
}
