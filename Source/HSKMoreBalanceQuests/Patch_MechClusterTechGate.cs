using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace HSKMoreBalanceQuests;

// Mech clusters dropped by quests (e.g. the monument "enforcement" threat in
// BuildMonument_TimeProtect, which picks one of several threats through QuestNode_RandomNode)
// fail their test run below the configured tech level (Defs/Misc/BalanceSettings.xml).
// QuestNode_RandomNode only picks nodes whose test run passes, so the quest falls back to
// another threat instead of a mech cluster.
[HarmonyPatch(typeof(QuestNode_SpawnMechCluster), "TestRunInt")]
public static class Patch_MechClusterTechGate
{
    public const TechLevel DefaultMinTechLevel = TechLevel.Industrial;

    public static TechLevel MinTechLevel =>
        BalanceSettingsDef.Instance?.mechClusterMinTechLevel ?? DefaultMinTechLevel;

    public static bool Prefix(ref bool __result)
    {
        if (!HSKMoreBalanceQuestsMod.QuestsEnabled)
            return true;

        TechLevel playerTech = IgnoranceCompat.PlayerTechLevel;
        if (playerTech == TechLevel.Undefined || playerTech >= MinTechLevel)
            return true;

        __result = false;
        return false;
    }
}

// Hospitality_Util_Worker (lodger quests) has mech cluster threat branches built with
// QuestNode_CreateIncidents rather than QuestNode_SpawnMechCluster. Same threshold and the same
// fallback to the raid branches through QuestNode_RandomNode.
[HarmonyPatch(typeof(QuestNode_CreateIncidents), "TestRunInt")]
public static class Patch_MechClusterIncidentTechGate
{
    public static bool Prefix(QuestNode_CreateIncidents __instance, Slate slate, ref bool __result)
    {
        if (!HSKMoreBalanceQuestsMod.QuestsEnabled || __instance.incidentDef.GetValue(slate) != IncidentDefOf.MechCluster)
            return true;

        TechLevel playerTech = IgnoranceCompat.PlayerTechLevel;
        if (playerTech == TechLevel.Undefined || playerTech >= Patch_MechClusterTechGate.MinTechLevel)
            return true;

        __result = false;
        return false;
    }
}
