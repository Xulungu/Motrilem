using UnityEngine;

// Urne en terre cuite : un sort la fait voler en éclats. Une fois sur sept, elle contenait un peu de soin.
// Elle reste cassée tant que le joueur ne sauvegarde pas (statue) : mourir ou changer de carte ne la répare pas.
[RequireComponent(typeof(EnemyHealth))]
public class BreakableUrn : MonoBehaviour
{
    [Range(0f, 1f)] public float healChance = 1f / 7f;
    public int healAmount = 12;

    private string id;

    void Start()
    {
        id = "urne_" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_" + Mathf.RoundToInt(transform.position.x * 2f) + "_" + Mathf.RoundToInt(transform.position.y * 2f);
        if (SaveSystem.IsBroken(id)) { Destroy(gameObject); return; }
        EnemyHealth eh = GetComponent<EnemyHealth>();
        if (eh != null) eh.OnDeath += Broken;
    }

    void Broken(bool stomped)
    {
        SaveSystem.MarkBroken(id);
        Sprite e = FxUtil.LoadSprite("eclat");
        if (Random.value > healChance) return;
        PlayerHealth ph = FindObjectOfType<PlayerHealth>();
        if (ph != null && ph.CurrentHealth < ph.maxHealth)
        {
            ph.Heal(healAmount);
            AudioManager.Play("fragment", 0.6f, 1.2f);
            if (e != null)
                for (int i = 0; i < 8; i++)
                    SimpleParticle.Spawn(e, transform.position + new Vector3(0f, 0.3f, 0f), new Vector2(Random.Range(-1f, 1f), Random.Range(1.5f, 3f)),
                        0.8f, new Color(1f, 0.5f, 0.6f), 1f, 10006, FxUtil.Additive, -1f, 1f);
            if (GameUI.HasInstance) GameUI.Instance.Toast("+" + healAmount + " PV");
        }
    }
}
