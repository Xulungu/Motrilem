using UnityEngine;

// Mur fissuré : un sort le fait voler en éclats. Une fois détruit, il le reste (mémorisé dans la sauvegarde).
[RequireComponent(typeof(EnemyHealth))]
public class CrackedWall : MonoBehaviour
{
    public string wallId = "mur";

    string Key { get { return "mur_" + wallId; } }

    void Start()
    {
        if (SaveSystem.HasFlag(Key))
        {
            Destroy(gameObject);
            return;
        }
        EnemyHealth eh = GetComponent<EnemyHealth>();
        if (eh != null) eh.OnDeath += Broken;
    }

    void Broken(bool stomped)
    {
        SaveSystem.SetFlag(Key);
        if (GameUI.HasInstance) GameUI.Instance.Toast("Le mur s'effondre.");
    }
}
