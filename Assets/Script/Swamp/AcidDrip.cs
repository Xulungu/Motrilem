using System.Collections;
using UnityEngine;

// Goutte d'acide qui tombe du plafond de temps en temps (ambiance, aucun danger).
public class AcidDrip : MonoBehaviour
{
    public float interval = 2.4f;

    private float timer;

    void Start() { timer = Random.Range(0f, interval); }

    void Update()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 v = cam.WorldToViewportPoint(transform.position);
        if (v.x < -0.3f || v.x > 1.3f || v.y < -0.8f || v.y > 1.3f) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = interval * Random.Range(0.7f, 1.5f);
        StartCoroutine(Drop());
    }

    IEnumerator Drop()
    {
        Sprite s = FxUtil.LoadSprite("bulle");
        if (s == null) yield break;
        // distance jusqu'au sol
        float dist = 4f;
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.down, 14f);
        foreach (RaycastHit2D h in hits)
        {
            if (h.collider == null || h.collider.isTrigger) continue;
            dist = h.distance;
            break;
        }
        const float g = 14f;
        float life = Mathf.Sqrt(2f * dist / g);
        Color green = new Color(0.65f, 1f, 0.35f, 0.95f);
        SimpleParticle.Spawn(s, transform.position, Vector2.zero, life, green, 0.5f, 8, null, g, 0f);
        yield return new WaitForSeconds(life);
        Sprite e = FxUtil.LoadSprite("eclat");
        if (e == null) yield break;
        Vector3 at = transform.position + Vector3.down * dist;
        for (int i = 0; i < 4; i++)
            SimpleParticle.Spawn(e, at, new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(1f, 2.2f)), 0.4f, green, 0.5f, 8, FxUtil.Additive, 9f, 0.5f);
    }
}
