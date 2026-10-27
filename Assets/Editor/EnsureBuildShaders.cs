using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Avant chaque build : s'assure que les shaders utilisés par le code (Shader.Find) sont bien inclus dans le jeu.
// Sans ça, Unity vide parfois la liste « Always Included Shaders » et tout s'affiche en violet dans le .exe.
public class EnsureBuildShaders : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report) { Ensure(); }

    [MenuItem("Motrilem/Réparer les shaders du build")]
    public static void Ensure()
    {
        Object gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
        if (gs == null) { Debug.LogWarning("GraphicsSettings introuvable."); return; }
        SerializedObject so = new SerializedObject(gs);
        SerializedProperty arr = so.FindProperty("m_AlwaysIncludedShaders");
        if (arr == null) return;
        for (int i = arr.arraySize - 1; i >= 0; i--)
            if (arr.GetArrayElementAtIndex(i).objectReferenceValue == null) arr.DeleteArrayElementAtIndex(i);
        foreach (string name in new[] { "Sprites/Default", "Sprites/Mask", "UI/Default", "Legacy Shaders/Particles/Additive", "Particles/Additive" })
        {
            Shader sh = Shader.Find(name);
            if (sh == null) continue;
            bool found = false;
            for (int i = 0; i < arr.arraySize; i++)
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) { found = true; break; }
            if (found) continue;
            arr.InsertArrayElementAtIndex(arr.arraySize);
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
        }
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }
}
