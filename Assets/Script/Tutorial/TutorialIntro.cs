using System.Collections;
using UnityEngine;

// Ouverture du jeu : fondu depuis le noir et quelques pensées du protagoniste.
public class TutorialIntro : MonoBehaviour
{
    public PlayerMovement player;
    public bool playIntro = true;
    public string playerName = "???";
    [Tooltip("Couleur de départ du fondu (noir dans la grotte, blanc en sortant à la lumière)")]
    public Color fadeFrom = Color.black;
    public float fadeDuration = 2f;
    [Tooltip("Nom de la zone affiché en grand après l'intro (vide = rien)")]
    public string zoneTitle = "";
    public string zoneSubtitle = "";

    [TextArea(2, 4)]
    public string[] lines =
    {
        "Un an. Cela fait un an que je me suis réveillé au fond de cette grotte...",
        "Aucun souvenir. Ni mon nom, ni ce que j'étais avant.",
        "Il n'y a que ce vieux livre, qui flotte au fond de la caverne. Il est temps d'aller voir de plus près."
    };

    IEnumerator Start()
    {
        if (!playIntro || SaveSystem.ConsumeSkipIntro()) yield break;
        if (player == null) player = FindObjectOfType<PlayerMovement>();
        GameUI ui = GameUI.Instance;
        ui.SetFade(fadeFrom);
        yield return null; // laisse les autres scripts s'initialiser

        if (player != null) player.SetCutscene(true);
        StartCoroutine(ui.Letterbox(true, 0.01f));
        yield return new WaitForSeconds(0.6f);
        yield return ui.FadeTo(new Color(fadeFrom.r, fadeFrom.g, fadeFrom.b, 0f), fadeDuration);

        foreach (string line in lines) yield return ui.Say(line, playerName);

        yield return ui.Letterbox(false);
        yield return null;
        if (player != null) player.SetCutscene(false);
        if (!string.IsNullOrEmpty(zoneTitle)) ui.PlayTitle(zoneTitle, zoneSubtitle);
    }
}
