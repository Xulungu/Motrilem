using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Porte du donjon : la première fois, le joueur entend le gardien ; puis fondu et chargement de la scène du donjon.
public class DungeonDoor : Interactable
{
    public string sceneToLoad = "Donjon";
    public string[] lines = { "..." };

    private bool busy;

    protected override void Awake()
    {
        base.Awake();
        promptText = "Entrer";
        promptOffset = new Vector3(0f, 3.2f, 0f);
    }

    protected override void Interact()
    {
        if (busy) return;
        StartCoroutine(Enter());
    }

    IEnumerator Enter()
    {
        busy = true;
        canInteract = false;
        PlayerMovement pm = player;
        if (pm != null) pm.SetCutscene(true);
        GameUI ui = GameUI.Instance;
        if (Camera.main != null)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.Shake(0.06f, 1.2f);
        }
        AudioManager.Play("bzz", 0.8f, 0.55f);
        if (!SaveSystem.HasFlag("donjon_ecoute"))
        {
            foreach (string l in lines) yield return ui.Say(l, "");
            SaveSystem.SetFlag("donjon_ecoute");
        }
        yield return ui.FadeTo(Color.black, 0.9f);
        if (Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            // Gardien déjà vaincu : l'entrée est éboulée, on arrive directement dans la Brèche
            if (SaveSystem.HasFlag("boss_moustique")) SaveSystem.SetReturnSpawn(sceneToLoad, new Vector2(229.5f, 1f));
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("La scène '" + sceneToLoad + "' n'est pas dans les Build Settings.");
            yield return ui.FadeTo(new Color(0, 0, 0, 0), 0.5f);
            if (pm != null) pm.SetCutscene(false);
            canInteract = true;
            busy = false;
        }
    }
}
