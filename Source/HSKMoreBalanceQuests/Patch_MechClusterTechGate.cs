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
