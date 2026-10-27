using System.Collections.Generic;
using UnityEngine;

// Bassin d'acide : surface animée, bulles, lueur verte.
// Le joueur qui tombe dedans perd de la vie et revient au dernier point de réapparition.
[RequireComponent(typeof(BoxCollider2D))]
public class AcidPool : MonoBehaviour
{
    public int damage = 25;
    [Tooltip("Vrai : acide purement décoratif (aucun danger)")]
    public bool harmless = false;
    [Tooltip("Largeur et profondeur du bassin (unités)")]
    public Vector2 size = new Vector2(5f, 3.6f);
    [Tooltip("Ordre d'affichage du corps, de la surface et de la lueur (négatifs : l'acide passe derrière les rochers)")]
    public int bodyOrder = 5, surfaceOrder = 6, glowOrder = 7;
    public float bubbleRate = 1.5f; // bulles par seconde et par 10 unités de largeur

    private SpriteRenderer body, surface, glow;
    private Sprite[] surfaceFrames;
    private float frameTimer, bubbleTimer;
    private int frame;

    private static readonly List<AcidPool> all = new List<AcidPool>();
    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    /// <summary>Vrai si la position est dans (ou à moins de 'margin' unités d') un bassin dangereux.</summary>
    public static bool NearAcid(Vector3 p, float margin)
    {
        for (int i = 0; i < all.Count; i++)
        {
            AcidPool a = all[i];
            if (a == null || a.harmless) continue;
            Vector3 c = a.transform.position;
            if (Mathf.Abs(p.x - c.x) <= a.size.x * 0.5f + margin && p.y >= c.y - a.size.y * 0.5f - margin && p.y <= c.y + a.size.y * 0.5f + margin + 2f) return true;
        }
        return false;
    }

    void Awake()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        // Le haut de la zone mortelle est un peu sous la surface : on ne meurt pas en effleurant l'acide
        box.size = new Vector2(size.x, size.y - 0.25f);
        box.offset = new Vector2(0f, -0.125f);
        Build();
    }

    void Build()
    {
        Sprite bodySprite = FxUtil.LoadSprite("acide");
        surfaceFrames = new Sprite[4];
        for (int i = 0; i < 4; i++) surfaceFrames[i] = FxUtil.LoadSprite("acide_surface_" + i);

        // Corps de l'acide (répété)
        body = FxUtil.MakeSprite("Acide", bodySprite, transform.position + new Vector3(0f, -0.15f, 0f), bodyOrder, Color.white, null, transform);
        body.drawMode = SpriteDrawMode.Tiled;
        body.size = new Vector2(size.x, size.y - 0.3f);

        // Surface ondulante
        surface = FxUtil.MakeSprite("Surface", surfaceFrames[0], transform.position + new Vector3(0f, size.y * 0.5f - 0.2f, 0f), surfaceOrder, Color.white, null, transform);
        surface.drawMode = SpriteDrawMode.Tiled;
        surface.size = new Vector2(size.x, 0.375f);

        // Lueur toxique au-dessus
        Sprite g = FxUtil.LoadSprite("lueur_douce");
        if (g != null)
        {
            glow = FxUtil.MakeSprite("Lueur", g, transform.position + new Vector3(0f, size.y * 0.5f, 0f), glowOrder, new Color(0.5f, 1f, 0.3f, 0.18f), FxUtil.Additive, transform);
            glow.transform.localScale = new Vector3(size.x / 2f + 1f, 1.4f, 1f);
        }
    }

    void Update()
    {
        frameTimer -= Time.deltaTime;
        if (frameTimer <= 0f && surfaceFrames[0] != null)
        {
            frameTimer = 0.16f;
            frame = (frame + 1) % surfaceFrames.Length;
            surface.sprite = surfaceFrames[frame];
            surface.size = new Vector2(size.x, 0.375f);
        }
        if (glow != null)
        {
            Color c = glow.color;
            c.a = 0.15f + 0.05f * Mathf.Sin(Time.time * 1.7f + transform.position.x);
            glow.color = c;
        }

        // Bulles qui remontent et éclatent à la surface
        bubbleTimer -= Time.deltaTime * bubbleRate * Mathf.Max(1f, size.x / 10f);
        if (bubbleTimer <= 0f)
        {
            bubbleTimer = Random.Range(0.4f, 1.2f);
            Sprite b = FxUtil.LoadSprite("bulle");
            if (b != null)
            {
                Vector3 p = transform.position + new Vector3(Random.Range(-size.x * 0.45f, size.x * 0.45f), size.y * 0.5f - Random.Range(0.4f, 1.2f), 0f);
                SimpleParticle.Spawn(b, p, new Vector2(0f, Random.Range(0.5f, 1f)), Random.Range(0.6f, 1.1f),
                    new Color(0.8f, 1f, 0.6f, 0.9f), Random.Range(0.8f, 1.3f), 8, null, -0.2f, 0.5f);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 1f, 0.2f, 0.35f);
        Gizmos.DrawCube(transform.position, new Vector3(size.x, size.y, 0.1f));
    }

    /// <summary>Hauteur de la surface (là où flottent les slimes).</summary>
    public float SurfaceY { get { return transform.position.y + size.y * 0.5f - 0.15f; } }

    // Les créatures qui tombent dans l'acide : les slimes flottent (l'acide est leur élément), les moustiques sont dissous.
    void OnTriggerStay2D(Collider2D other)
    {
        if (harmless || other.attachedRigidbody == null) return;
        if (AcidImmunity.Has && other.CompareTag("Player"))
        {
            PlayerMovement pmv = other.GetComponentInParent<PlayerMovement>();
            if (pmv != null) AcidSwim.Touch(pmv, SurfaceY);
            return;
        }
        Slime sl = other.attachedRigidbody.GetComponent<Slime>();
        if (sl != null) { sl.FloatInAcid(SurfaceY); return; }
        FlyingMinion fm = other.attachedRigidbody.GetComponent<FlyingMinion>();
        if (fm != null && fm.IsAlive && !fm.IsPulled)
        {
            AudioManager.Play("acide", 0.5f, Random.Range(1.1f, 1.4f));
            Splash(new Vector3(fm.transform.position.x, SurfaceY, 0f));
            fm.DieInAcid();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (harmless || !other.CompareTag("Player")) return;
        if (AcidImmunity.Has) { Splash(other.transform.position); return; } // l'acide ne brûle plus le héros
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;
        AudioManager.Play("acide", 0.8f, Random.Range(0.9f, 1.1f));
        Splash(other.transform.position);
        health.FallIntoVoid(damage);
    }

    void Splash(Vector3 at)
    {
        Sprite s = FxUtil.LoadSprite("eclat");
        if (s == null) return;
        Vector3 p = new Vector3(at.x, transform.position.y + size.y * 0.5f - 0.1f, 0f);
        for (int i = 0; i < 14; i++)
            SimpleParticle.Spawn(s, p, new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(2f, 5f)), Random.Range(0.4f, 0.7f),
                new Color(0.6f, 1f, 0.3f), Random.Range(1f, 1.6f), 10006, FxUtil.Additive, 12f, 0.5f);
    }
}
