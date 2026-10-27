using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Éboulement : après la mort du gardien, le combat a été trop violent pour la grotte. Le plafond du couloir par lequel
// le joueur est venu cède et un mur de pierres ruinées se construit sous ses yeux. Le joueur est bloqué pendant la scène.
public class CaveCollapse : MonoBehaviour
{
    public string flag = "boss_moustique";
    public Vector2 wallMin, wallMax;   // zone du plafond qui s'effrite (pour les débris)
    public int width = 4, height = 7;  // taille de l'éboulis (cases), posé sur le sol à la position de l'objet
    public Vector3 abdomenRest;           // où réapparaît l'abdomen si le joueur a quitté la salle sans la prendre

    /// <summary>Vrai tant que la scène d'éboulement se joue (la page attend sa fin).</summary>
    public static bool Busy;

    private bool played;
    private GameObject wall;
    private BoxCollider2D wallCollider;
    private readonly List<Transform> cells = new List<Transform>();
    private readonly List<Vector3> targets = new List<Vector3>();
    private readonly List<float> starts = new List<float>();
    private readonly List<int> rowOf = new List<int>();
    private readonly List<bool> landed = new List<bool>();

    void Start()
    {
        Busy = false;
        BuildWall();
        wallCollider.enabled = false;
        foreach (Transform c in cells) c.gameObject.SetActive(false);
        // Le gardien était déjà vaincu à l'arrivée dans la scène : tout est déjà écroulé
        if (SaveSystem.HasFlag(flag))
        {
            played = true;
            for (int i = 0; i < cells.Count; i++) { cells[i].position = targets[i]; cells[i].gameObject.SetActive(true); }
            backing.gameObject.SetActive(true);
            wallCollider.enabled = true;
            if (!AcidImmunity.Has) AbdomenPickup.Spawn(abdomenRest, abdomenRest, true);
        }
    }

    private SpriteRenderer backing;

    void BuildWall()
    {
        wall = new GameObject("Éboulis de roches");
        wall.transform.SetParent(transform, false);
        System.Random rng = new System.Random(4242);
        float x0 = transform.position.x - width * 0.5f;
        float y0 = transform.position.y;

        // fond sombre qui bouche les trous entre les roches (il se remplit au fil de l'éboulement)
        GameObject bk = new GameObject("Fond de l'éboulis");
        bk.transform.SetParent(wall.transform, false);
        backing = bk.AddComponent<SpriteRenderer>();
        backing.sprite = FxUtil.LoadSprite("tuile_ruine_c0");
        backing.drawMode = SpriteDrawMode.Tiled;
        backing.sortingOrder = 9991;
        backing.color = new Color(0.12f, 0.13f, 0.17f, 1f);
        backing.size = new Vector2(width, height);
        bk.transform.position = new Vector3(transform.position.x, y0 + height * 0.5f, 0f);

        // blocs de roche entassés : rangées irrégulières, chaque bloc est une roche à part
        float rowY = 0.45f;
        int row = 0;
        while (rowY < height + 0.3f)
        {
            float cx = x0 + 0.55f + (float)rng.NextDouble() * 0.5f;
            while (cx < x0 + width - 0.35f)
            {
                GameObject go = new GameObject("Roche");
                go.transform.SetParent(wall.transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = FxUtil.LoadSprite("eboulis_roche_" + rng.Next(0, 8));
                sr.sortingOrder = 9992 + (row % 3);
                float k = 0.78f + 0.3f * (float)rng.NextDouble();
                sr.color = new Color(k, k, k * 1.05f, 1f);
                sr.flipX = rng.Next(0, 2) == 0;
                float sc = 1.1f + 0.35f * (float)rng.NextDouble();
                go.transform.localScale = new Vector3(sc, sc, 1f);
                float w = sr.sprite != null ? sr.sprite.bounds.size.x * sc : 1.5f;
                float px = Mathf.Min(cx + w * 0.4f, x0 + width - 0.45f);
                Vector3 target = new Vector3(px, y0 + Mathf.Min(rowY, height - 0.4f), 0f);
                go.transform.position = target;
                cells.Add(go.transform);
                targets.Add(target);
                starts.Add(row * 0.26f + (float)rng.NextDouble() * 0.2f);
                rowOf.Add(row);
                landed.Add(false);
                cx += w * 0.7f;
            }
            rowY += 0.8f;
            row++;
        }
        // gravats qui débordent au pied du mur
        string[] heaps = { "ruine_gravats_a", "ruine_gravats_b", "ruine_gravats_c" };
        for (int i = 0; i < 3; i++)
        {
            GameObject go = new GameObject("Gravats");
            go.transform.SetParent(wall.transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FxUtil.LoadSprite(heaps[i]);
            sr.sortingOrder = 9996;
            sr.color = new Color(0.75f, 0.78f, 0.88f, 1f);
            Vector3 target = new Vector3(x0 + 0.9f + i * 1.1f, y0 + 0.15f, 0f);
            go.transform.position = target;
            cells.Add(go.transform);
            targets.Add(target);
            starts.Add(row * 0.26f + 0.1f * i);
            rowOf.Add(row);
            landed.Add(false);
        }
        wallCollider = wall.AddComponent<BoxCollider2D>();
        wallCollider.size = new Vector2(width, height);
        wallCollider.offset = new Vector2(0f, height * 0.5f);
        backing.gameObject.SetActive(false);
    }

    public void Play()
    {
        if (played) return;
        played = true;
        StartCoroutine(Collapse());
    }

    IEnumerator Collapse()
    {
        Busy = true;
        Sprite stone = FxUtil.LoadSprite("eboulis_roche_3");
        Sprite dust = FxUtil.LoadSprite("poussiere");
        Sprite chip = FxUtil.LoadSprite("eclat");
        CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        GameUI ui = GameUI.Instance;
        if (pm != null) pm.SetCutscene(true);
        StartCoroutine(ui.Letterbox(true));
        // la caméra quitte le héros et va voir l'endroit qui s'effondre
        if (cam != null) cam.SetFocus(new Vector3(transform.position.x, transform.position.y + height * 0.5f, 0f));
        yield return new WaitForSeconds(1.0f);

        // --- la grotte gronde, le plafond s'effrite
        AudioManager.Play("explosion_livre", 0.8f, 0.45f);
        ui.Toast("La grotte tremble...");
        float rumble = 2.2f;
        for (float t = 0f; t < rumble; t += Time.deltaTime)
        {
            if (cam != null && ((int)(t * 10f)) % 3 == 0) cam.Shake(0.05f + 0.07f * (t / rumble), 0.2f);
            if (Random.value < 0.8f)
            {
                Vector3 p = new Vector3(Random.Range(wallMin.x, wallMax.x), Random.Range(wallMin.y, wallMax.y), 0f);
                if (stone != null)
                    SimpleParticle.Spawn(stone, p, new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-2f, 0f)), Random.Range(0.8f, 1.4f),
                        new Color(0.45f, 0.48f, 0.58f), Random.Range(0.15f, 0.35f), 10010, null, 14f, 0.2f);
                if (dust != null)
                    SimpleParticle.Spawn(dust, p, new Vector2(Random.Range(-1f, 1f), Random.Range(-0.5f, 0.5f)), 1.2f,
                        new Color(0.7f, 0.68f, 0.62f, 0.55f), Random.Range(1.2f, 2.4f), 10009, null, 0f, 1f);
            }
            if (Random.value < 0.08f) AudioManager.Play("impact", 0.4f, Random.Range(0.4f, 0.7f));
            yield return null;
        }

        // --- le mur de pierres tombe, rangée après rangée
        foreach (Transform c in cells) c.gameObject.SetActive(false);
        backing.gameObject.SetActive(false);
        float total = (height + 2) * 0.26f + 1.3f;
        float fallTime = 0.8f;
        for (float t = 0f; t < total; t += Time.deltaTime)
        {
            if (cam != null && Random.value < 0.25f) cam.Shake(0.07f, 0.15f);
            if (t > 0.4f)
            {
                if (!backing.gameObject.activeSelf) backing.gameObject.SetActive(true);
                float bh = Mathf.Clamp01((t - 0.4f) / (total - 1.2f)) * height;
                backing.size = new Vector2(width, Mathf.Max(0.1f, bh));
                backing.transform.position = new Vector3(transform.position.x, transform.position.y + bh * 0.5f, 0f);
            }
            for (int i = 0; i < cells.Count; i++)
            {
                float dt = t - starts[i];
                if (dt < 0f) continue;
                if (!cells[i].gameObject.activeSelf) cells[i].gameObject.SetActive(true);
                if (dt < fallTime)
                {
                    float k = dt / fallTime;
                    cells[i].position = targets[i] + new Vector3(0f, 9f * (1f - k * k), 0f);
                }
                else if (!landed[i])
                {
                    landed[i] = true;
                    cells[i].position = targets[i];
                    if (dust != null && Random.value < 0.6f)
                        SimpleParticle.Spawn(dust, targets[i] + new Vector3(Random.Range(-0.6f, 0.6f), -0.2f, 0f), new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(0.2f, 1f)), 0.9f,
                            new Color(0.7f, 0.68f, 0.62f, 0.5f), Random.Range(0.8f, 1.5f), 10011, null, 0f, 1.2f);
                    if (rowOf[i] % 2 == 0 && ((int)(targets[i].x * 2f)) % 2 == 0) AudioManager.Play("impact", 0.55f, Random.Range(0.4f, 0.6f));
                }
            }
            yield return null;
        }
        for (int i = 0; i < cells.Count; i++) { cells[i].gameObject.SetActive(true); cells[i].position = targets[i]; }
        backing.size = new Vector2(width, height);
        backing.transform.position = new Vector3(transform.position.x, transform.position.y + height * 0.5f, 0f);

        // le mur devient solide ; si le héros se tenait contre lui, on le repousse
        if (pm != null)
        {
            float right = transform.position.x + width * 0.5f;
            if (pm.transform.position.x < right + 0.7f && pm.transform.position.x > right - width - 1f && pm.transform.position.y < transform.position.y + height)
                pm.transform.position = new Vector3(right + 0.8f, pm.transform.position.y, pm.transform.position.z);
        }
        wallCollider.enabled = true;
        AudioManager.Play("impact", 0.9f, 0.5f);
        if (cam != null) cam.Shake(0.14f, 0.7f);
        yield return new WaitForSeconds(1.0f);
        yield return ui.Say("Le passage s'est refermé derrière moi... Il ne reste qu'une issue, au fond de la salle.");
        if (cam != null) cam.ClearFocus();
        yield return ui.Letterbox(false);
        yield return null;
        if (pm != null) pm.SetCutscene(false);
        Busy = false;
    }
}
