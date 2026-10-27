using UnityEngine;

// Pouvoir obtenu avec l'abdomen de Séraphel : l'acide ne brûle plus le héros.
public static class AcidImmunity
{
    public const string Flag = "immunite_acide";
    public static bool Has { get { return SaveSystem.HasFlag(Flag); } }
}
