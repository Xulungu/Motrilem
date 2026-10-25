using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Sortie du donjon : retour au temple du marécage, devant la porte par laquelle on est entré.
public class DungeonExitDoor : Interactable
{
    public string sceneToLoad = "EtangAcide";
    public Vector2 returnPosition = new Vector2(322.5f, -43.2f);
    private bool busy;

    protected override void Awake()
    {
        base.Awake();
        promptText = "Sortir du donjon";
        promptOffset = new Vector3(0f, 2.6f, 0f);
    }

    protected override void Interact()
    {
        if (busy) return;
        StartCoroutine(Leave());
    }

    IEnumerator Leave()
    {
        busy = true;
        canInteract = false;
        PlayerMovement pm = player;
        if (pm != null) pm.SetCutscene(true);
        GameUI ui = GameUI.Instance;
        AudioManager.StopMusic(0.8f);
        yield return ui.FadeTo(Color.black, 0.8f);
        if (Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            SaveSystem.SetReturnSpawn(sceneToLoad, returnPosition);
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            yield return ui.FadeTo(new Color(0, 0, 0, 0), 0.4f);
            if (pm != null) pm.SetCutscene(false);
            canInteract = true;
            busy = false;
        }
    }
}
