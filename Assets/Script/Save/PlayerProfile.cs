using System.Text;
using UnityEngine;

// Profil de comportement du joueur, mesuré tout au long de la partie et gardé dans la sauvegarde.
// Il servira de base à l'IA du boss final, qui devra imiter / contrer la façon de jouer du joueur.
[System.Serializable]
public class PlayerProfileData
{
    // Temps
    public float playTime, timeIdle, timeMoving, timeAirborne, timeFullMomentum;
    // Mouvement
    public int jumps, falls;
    // Attaque
    public int castsFire, castsLightning, castsIce, spellHits, stompKills, magicKills;
    public float attackDistanceSum; public int attackDistanceSamples;
    // Défense
    public int hitsTaken, damageTaken, deaths, stingerHits, stingerDodges;
    // Distance moyenne aux ennemis proches (mesurée régulièrement)
    public float enemyDistanceSum; public int enemyDistanceSamples;
    // Divers
    public int saves, interactions;
}

public static class PlayerProfile
{
    public enum Event
    {
        Jump, Fall, CastFire, CastLightning, CastIce, SpellHit, StompKill, MagicKill,
        Death, HitByStinger, DodgedStinger, Save, Interact
    }

    public static PlayerProfileData Data
    {
        get
        {
            if (SaveSystem.Session.profile == null) SaveSystem.Session.profile = new PlayerProfileData();
            return SaveSystem.Session.profile;
        }
    }

    public static void Record(Event e)
    {
        PlayerProfileData d = Data;
        switch (e)
        {
            case Event.Jump: d.jumps++; break;
            case Event.Fall: d.falls++; break;
            case Event.CastFire: d.castsFire++; break;
            case Event.CastLightning: d.castsLightning++; break;
            case Event.CastIce: d.castsIce++; break;
            case Event.SpellHit: d.spellHits++; break;
            case Event.StompKill: d.stompKills++; break;
            case Event.MagicKill: d.magicKills++; break;
            case Event.Death: d.deaths++; break;
            case Event.HitByStinger: d.stingerHits++; break;
            case Event.DodgedStinger: d.stingerDodges++; break;
            case Event.Save: d.saves++; break;
            case Event.Interact: d.interactions++; break;
        }
    }

    public static void RecordDamage(int amount)
    {
        Data.hitsTaken++;
        Data.damageTaken += amount;
    }

    public static void RecordAttackDistance(float distance)
    {
        Data.attackDistanceSum += distance;
        Data.attackDistanceSamples++;
    }

    // ------------------------------------------------------------------ analyse
    public static int Casts { get { return Data.castsFire + Data.castsLightning + Data.castsIce; } }
    /// <summary>Précision des sorts (0..1).</summary>
    public static float Accuracy { get { return Casts == 0 ? 0f : Mathf.Clamp01(Data.spellHits / (float)Casts); } }
    /// <summary>Part des ennemis tués en les écrasant plutôt qu'à la magie (0..1).</summary>
    public static float StompRatio { get { int k = Data.stompKills + Data.magicKills; return k == 0 ? 0.5f : Data.stompKills / (float)k; } }
    /// <summary>Distance moyenne gardée avec les ennemis.</summary>
    public static float AverageEnemyDistance { get { return Data.enemyDistanceSamples == 0 ? 0f : Data.enemyDistanceSum / Data.enemyDistanceSamples; } }
    /// <summary>Taux d'esquive des attaques à distance (0..1).</summary>
    public static float DodgeRate { get { int t = Data.stingerHits + Data.stingerDodges; return t == 0 ? 0.5f : Data.stingerDodges / (float)t; } }
    /// <summary>Agressivité (0 = prudent, 1 = fonce dans le tas).</summary>
    public static float Aggression
    {
        get
        {
            float closeness = AverageEnemyDistance <= 0f ? 0.5f : Mathf.Clamp01(1f - (AverageEnemyDistance - 1.5f) / 5f);
            float moving = Data.playTime <= 0f ? 0.5f : Mathf.Clamp01(Data.timeMoving / Mathf.Max(1f, Data.playTime));
            return Mathf.Clamp01(0.5f * closeness + 0.3f * StompRatio + 0.2f * moving);
        }
    }

    /// <summary>Résumé lisible (affiché dans la Console à chaque sauvegarde).</summary>
    public static string Describe()
    {
        PlayerProfileData d = Data;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("— Profil du joueur —");
        sb.AppendLine("Temps de jeu : " + Mathf.RoundToInt(d.playTime) + " s (en l'air " + Mathf.RoundToInt(d.timeAirborne) + " s, à l'arrêt " + Mathf.RoundToInt(d.timeIdle) + " s)");
        sb.AppendLine("Sauts : " + d.jumps + "  |  chutes : " + d.falls + "  |  morts : " + d.deaths);
        sb.AppendLine("Sorts : " + Casts + " (précision " + Mathf.RoundToInt(Accuracy * 100) + " %)  |  écrasements : " + d.stompKills + "  |  tués à la magie : " + d.magicKills);
        sb.AppendLine("Dégâts reçus : " + d.damageTaken + " en " + d.hitsTaken + " coups  |  esquive des trompes : " + Mathf.RoundToInt(DodgeRate * 100) + " %");
        sb.AppendLine("Distance moyenne aux ennemis : " + AverageEnemyDistance.ToString("0.0") + "  |  agressivité : " + Mathf.RoundToInt(Aggression * 100) + " %");
        return sb.ToString();
    }
}
