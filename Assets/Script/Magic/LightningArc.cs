using UnityEngine;

// Arc électrique en zigzag entre deux points, visible un court instant.
[RequireComponent(typeof(LineRenderer))]
public class LightningArc : MonoBehaviour
{
    private LineRenderer line;
    private Vector3 a, b;
    private float life = 0.18f, age;

    public static void Draw(Vector3 from, Vector3 to, Color color)
    {
        GameObject go = new GameObject("Arc électrique");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = FxUtil.Additive;
        lr.widthMultiplier = 0.08f;
        lr.positionCount = 8;
        lr.startColor = Color.white;
        lr.endColor = color;
        lr.sortingOrder = 10007;
        LightningArc arc = go.AddComponent<LightningArc>();
        arc.line = lr;
        arc.a = from;
        arc.b = to;
        arc.Jitter();
    }

    void Jitter()
    {
        Vector3 dir = b - a;
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f).normalized;
        for (int i = 0; i < line.positionCount; i++)
        {
            float t = i / (float)(line.positionCount - 1);
            float off = (i == 0 || i == line.positionCount - 1) ? 0f : Random.Range(-0.25f, 0.25f);
            line.SetPosition(i, a + dir * t + perp * off);
        }
    }

    void Update()
    {
        age += Time.deltaTime;
        Jitter();
        Color c = line.endColor;
        c.a = 1f - age / life;
        line.endColor = c;
        line.startColor = new Color(1f, 1f, 1f, c.a);
        if (age >= life) Destroy(gameObject);
    }
}
