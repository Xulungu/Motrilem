using UnityEngine;

// Urne en terre cuite : un sort la fait voler en éclats. Parfois, elle contenait un peu de soin.
[RequireComponent(typeof(EnemyHealth))]
public class BreakableUrn : MonoBehaviour
{
    [Range(0f, 1f)] public float healChance = 0.4f;
    public int healAmount = 12;

    void Start()
    {
        EnemyHealth eh = GetComponent<EnemyHealth>();
        if (eh != null) eh.OnDeath += Broken;
    }

    void Broken(bool stomped)
    {
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
