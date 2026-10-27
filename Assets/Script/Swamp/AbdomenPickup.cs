using System.Collections;
using UnityEngine;

// Abdomen de Séraphel : il retombe de l'explosion. Le ramasser donne l'immunité à l'acide
// (la cape du héros se met alors à luire faiblement). Pas de scène : on le prend, c'est tout.
public class AbdomenPickup : MonoBehaviour
{
    private bool settled, taken;
    private Vector3 rest;
    private SpriteRenderer sr, glow;

    /// <summary>Fait naître l'abdomen en 'from' ; il retombe en 'rest' en décrivant un arc.</summary>
    public static AbdomenPickup Spawn(Vector3 from, Vector3 rest, bool instant = false)
    {
        if (AcidImmunity.Has) return null;
        GameObject go = new GameObject("Abdomen de Séraphel");
        go.transform.position = instant ? rest : from;
        AbdomenPickup t = go.AddComponent<AbdomenPickup>();
        t.rest = rest;
        t.sr = FxUtil.MakeSprite("Abdomen", FxUtil.LoadSprite("abdomen_boss"), go.transform.position, 9999, Color.white, null, go.transform);
        Sprite g = FxUtil.LoadSprite("lueur_douce");
        if (g != null)
        {
            t.glow = FxUtil.MakeSprite("Lueur", g, go.transform.position, 9998, new Color(0.5f, 1f, 0.3f, 0.4f), FxUtil.Additive, go.transform);
            t.glow.transform.localScale = Vector3.one * 1.6f;
        }
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        CircleCollider2D c = go.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = 0.9f;
        c.enabled = instant;
        if (instant) t.settled = true;
        else t.StartCoroutine(t.Fly(from, c));
        return t;
    }

    IEnumerator Fly(Vector3 from, CircleCollider2D c)
    {
        float dur = 0.9f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float k = t / dur;
            Vector3 p = Vector3.Lerp(from, rest, k);
            p.y += Mathf.Sin(k * Mathf.PI) * 3f;
            transform.position = p;
            transform.rotation = Quaternion.Euler(0f, 0f, k * 340f);
            yield return null;
        }
        transform.rotation = Quaternion.identity;
        transform.position = rest;
        AudioManager.Play("impact", 0.5f, 0.8f);
        settled = true;
        c.enabled = true;
    }

    void Update()
    {
        if (taken) return;
        if (settled) transform.position = rest + Vector3.up * (0.08f * Mathf.Sin(Time.time * 2.2f));
        if (glow != null) glow.color = new Color(0.5f, 1f, 0.3f, 0.3f + 0.12f * Mathf.Sin(Time.time * 3f));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (taken || !settled || !other.CompareTag("Player")) return;
        taken = true;
        SaveSystem.SetFlag(AcidImmunity.Flag);
        AudioManager.Play("fragment", 0.7f, 0.9f);
        Sprite e = FxUtil.LoadSprite("eclat");
        if (e != null)
            for (int i = 0; i < 14; i++)
                SimpleParticle.Spawn(e, other.transform.position, Random.insideUnitCircle * 3f, 0.6f, new Color(0.6f, 1f, 0.3f), Random.Range(0.6f, 1f), 10006, FxUtil.Additive, 0f, 2f);
        GameUI.Instance.Toast("Abdomen de Séraphel récupéré : l'acide ne te blesse plus");
        Destroy(gameObject);
    }
}
