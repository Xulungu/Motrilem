// Fichier généré par l'outil de conception du marécage (world.py / portals.py) : ne pas modifier à la main.
using UnityEngine;

/// <summary>Découpage du marécage en cartes (une scène par groupe de zones), façon Hollow Knight.</summary>
public static class SwampZones
{
    public static readonly string[] Names = { "EtangAcide", "EtangRuines", "EtangHauteurs", "EtangCoeur", "EtangGluant" };
    // Règles dans l'ordre (la première qui correspond gagne) : x0, x1, y0, y1, groupe
    static readonly int[,] Rules =
    {
        { 335, 500, -5, 40, 4 },
        { 151, 154, -30, -2, 0 },
        { 99, 102, -35, -31, 0 },
        { 171, 174, -35, -31, 0 },
        { 286, 334, -72, 52, 3 },
        { 252, 334, -72, -20, 3 },
        { -12, 251, -72, -31, 1 },
        { -12, 143, -30, 52, 0 },
        { 144, 175, -30, -15, 0 },
        { 144, 334, -14, 52, 2 },
    };

    public static int GroupOf(float x, float y)
    {
        int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
        for (int i = 0; i < Rules.GetLength(0); i++)
            if (cx >= Rules[i, 0] && cx <= Rules[i, 1] && cy >= Rules[i, 2] && cy <= Rules[i, 3]) return Rules[i, 4];
        return 0;
    }

    public static string SceneAt(float x, float y) { return Names[GroupOf(x, y)]; }

    // Noms de zones affichés au joueur : régions (rectangle, titre, sous-titre), puis un nom par carte à défaut
    static readonly object[][] Titles =
    {
        new object[] { 0, 100, -1, 30, "Les berges contaminées", "L'acide ronge la rive" },
        new object[] { 86, 252, -50, -1, "Les racines noyées", "Sous le marécage" },
        new object[] { 90, 252, -66, -38, "Les ruines englouties", "Ce qui fut une cité" },
        new object[] { 144, 290, 19, 50, "Les hauteurs infestées", "Au-dessus du ravin" },
        new object[] { 252, 334, -52, -20, "Le cœur du marécage", "Là où l'acide se rassemble" },
        new object[] { 286, 334, -19, 29, "Le cœur du marécage", "Sous la falaise" },
        new object[] { 380, 500, 0, 35, "Le terrier gluant", "Ça sent la vase et le sucre" },
    };
    static readonly string[] GroupTitles = { "Les berges contaminées", "Les ruines englouties", "Les hauteurs infestées", "Le cœur du marécage", "Le terrier gluant" };
    static readonly string[] GroupSubtitles = { "L'acide ronge la rive", "Ce qui fut une cité", "Au-dessus du ravin", "Là où l'acide se rassemble", "Ça sent la vase et le sucre" };

    /// <summary>Nom de la zone à cet endroit (région si elle est connue, sinon nom de la carte).</summary>
    public static string TitleAt(float x, float y, out string subtitle)
    {
        int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
        foreach (object[] t in Titles)
            if (cx >= (int)t[0] && cx <= (int)t[1] && cy >= (int)t[2] && cy <= (int)t[3]) { subtitle = (string)t[5]; return (string)t[4]; }
        int g = GroupOf(x, y);
        subtitle = GroupSubtitles[g];
        return GroupTitles[g];
    }

    public static bool IsSwampScene(string scene)
    {
        foreach (string n in Names) if (n == scene) return true;
        return scene == "EtangAcide2" || scene == "EtangAcide3";   // anciennes sauvegardes
    }
}
