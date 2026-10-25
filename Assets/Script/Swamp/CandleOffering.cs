using UnityEngine;

// Offrande de bougies : un sort de feu les allume (la lueur reste).
[RequireComponent(typeof(EnemyHealth))]
public class CandleOffering : MonoBehaviour
{
    public string offeringId = "offrande";
    public Sprite glowSprite;
    public Material glowMaterial;
    public Vector3[] candleOffsets = { new Vector3(-0.45f, 0.4f, 0f), new Vector3(0f, 0.45f, 0f), new Vector3(0.45f, 0.4f, 0f) };

    private bool lit;

    string Key { get { return "bougies_" + offeringId; } }

    void Start()
    {
        EnemyHealth eh = GetComponent<EnemyHealth>();
        if (eh != null) eh.OnHit += Hit;
        if (SaveSystem.HasFlag(Key)) Light(false);
    }

    void Hit(MagicType element)
    {
        if (element == MagicType.Feu && !lit) Light(true);
    }

    void Light(bool fx)
    {
        lit = true;
        SaveSystem.SetFlag(Key);
        GameObject g = new GameObject("Flammes");
        g.transform.SetParent(transform, false);
        g.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = glowSprite;
        sr.sharedMaterial = glowMaterial;
        sr.sortingOrder = 6;
        sr.color = new Color(1f, 0.6f, 0.25f, 0.55f);
        g.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
        g.AddComponent<LightFlicker>().baseAlpha = 0.55f;
        // petites flammes
        Sprite s = FxUtil.LoadSprite("eclat");
        if (s != null)
            foreach (Vector3 o in candleOffsets)
            {
                SpriteRenderer f = FxUtil.MakeSprite("Flamme", s, transform.position + o, 7, new Color(1f, 0.8f, 0.3f), FxUtil.Additive, transform);
                f.transform.localScale = new Vector3(0.7f, 0.9f, 1f);
                f.gameObject.AddComponent<LightFlicker>().baseAlpha = 0.9f;
            }
        if (fx)
        {
            AudioManager.Play("sort_feu", 0.4f, 1.3f);
            if (s != null)
                for (int i = 0; i < 8; i++)
                    SimpleParticle.Spawn(s, transform.position + new Vector3(Random.Range(-0.4f, 0.4f), 0.4f, 0f), new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(1f, 2f)),
                        0.6f, new Color(1f, 0.6f, 0.2f), 1f, 10006, FxUtil.Additive, -0.5f, 1f);
        }
    }
}
