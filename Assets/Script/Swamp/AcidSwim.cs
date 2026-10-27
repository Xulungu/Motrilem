using UnityEngine;

// Nage dans l'acide : une fois immunisé, le héros peut se déplacer librement dans les bassins d'acide
// (ZQSD / WASD / flèches dans toutes les directions). Espace près de la surface : bond hors de l'acide.
// Le composant s'ajoute tout seul au joueur ; le bassin l'actualise tant que le héros est dedans.
[DefaultExecutionOrder(200)]
public class AcidSwim : MonoBehaviour
{
    public float swimSpeed = 4.2f;
    public float sinkSpeed = 0.9f;
    private PlayerMovement pm;
    private Rigidbody2D rb;
    private float lastTouch = -10f;
    private float surfaceY;
    private float bubbleTimer;

    public bool Swimming { get { return Time.time - lastTouch < 0.12f; } }

    public static void Touch(PlayerMovement pm, float surfaceY)
    {
        AcidSwim s = pm.GetComponent<AcidSwim>();
        if (s == null) { s = pm.gameObject.AddComponent<AcidSwim>(); s.pm = pm; s.rb = pm.GetComponent<Rigidbody2D>(); }
        s.lastTouch = Time.time;
        s.surfaceY = surfaceY;
    }

    void FixedUpdate()
    {
        if (!Swimming || pm == null || rb == null || pm.externalControl || !pm.controlsEnabled) return;
        float h = GameInput.Horizontal(), v = GameInput.Vertical();
        rb.gravityScale = 0f;
        float ty = v != 0f ? v * swimSpeed : -sinkSpeed;
        Vector2 target = new Vector2(h * swimSpeed, ty);
        // près de la surface on ne peut pas monter plus haut : on flotte
        if (transform.position.y > surfaceY - 0.3f && ty > 0f) target.y = 0f;
        rb.velocity = Vector2.Lerp(rb.velocity, target, 0.25f);
        // bond hors de l'acide
        if (GameInput.JumpHeld() && transform.position.y > surfaceY - 1.6f)
            rb.velocity = new Vector2(rb.velocity.x, pm.jumpVelocity);
    }

    void Update()
    {
        if (!Swimming) return;
        bubbleTimer -= Time.deltaTime;
        if (bubbleTimer <= 0f)
        {
            bubbleTimer = Random.Range(0.25f, 0.6f);
            Sprite b = FxUtil.LoadSprite("bulle");
            if (b != null)
                SimpleParticle.Spawn(b, transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 0.4f, 0f), new Vector2(0f, Random.Range(0.8f, 1.4f)), 0.8f,
                    new Color(0.8f, 1f, 0.6f, 0.9f), Random.Range(0.6f, 1f), 12, null, -0.2f, 0.5f);
        }
    }
}
