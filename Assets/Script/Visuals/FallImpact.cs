using UnityEngine;

// Chute de haut : secousse de l'écran, poussière, puis le joueur est étourdi un instant.
// (Se branche sur l'évènement d'atterrissage de PlayerMovement.)
[RequireComponent(typeof(PlayerMovement))]
public class FallImpact : MonoBehaviour
{
    [Tooltip("Vitesse de chute à partir de laquelle l'écran tremble (≈ 4 cases)")]
    public float shakeSpeed = 13f;
    [Tooltip("Vitesse de chute à partir de laquelle le joueur est étourdi (≈ 6 cases)")]
    public float stunSpeed = 15.5f;
    [Tooltip("Vitesse de chute au-delà de laquelle l'étourdissement est long (chute maximale)")]
    public float heavySpeed = 17.8f;

    private PlayerMovement movement;

    void Awake() { movement = GetComponent<PlayerMovement>(); }
    void OnEnable() { if (movement == null) movement = GetComponent<PlayerMovement>(); movement.OnLanded += Landed; }
    void OnDisable() { if (movement != null) movement.OnLanded -= Landed; }

    void Landed(float verticalSpeed)
    {
        float v = Mathf.Abs(verticalSpeed);
        if (v < shakeSpeed) return;

        bool heavy = v >= heavySpeed;
        bool stun = v >= stunSpeed;
        float power = Mathf.InverseLerp(shakeSpeed, 18f, v);

        if (Camera.main != null)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.Shake(Mathf.Lerp(0.07f, 0.28f, power), Mathf.Lerp(0.2f, 0.6f, power));
        }
        AudioManager.Play("impact", Mathf.Lerp(0.5f, 0.9f, power), Random.Range(0.6f, 0.8f));

        Sprite dust = FxUtil.LoadSprite("poussiere");
        if (dust != null)
        {
            int n = stun ? 14 : 8;
            for (int i = 0; i < n; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                Vector3 p = transform.position + new Vector3(side * 0.2f, -0.4f, 0f);
                SimpleParticle.Spawn(dust, p, new Vector2(side * Random.Range(1.5f, 4.5f), Random.Range(0.3f, 1.6f)), Random.Range(0.35f, 0.7f),
                    new Color(0.85f, 0.85f, 0.8f, 0.8f), Random.Range(0.7f, 1.3f), 9990, null, 0f, 2f);
            }
        }

        // Étourdissement : le joueur ne peut ni bouger ni sauter pendant un court instant
        if (stun) movement.ApplyKnockback(Vector2.zero, heavy ? 0.9f : 0.55f);
    }
}
