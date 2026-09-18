using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace HSKMoreBalanceQuests;

// Blocks quests below a minimum player tech level (per Ignorance Is Bliss when installed,
// otherwise the player faction tech level). The list lives in Defs/Misc/BalanceSettings.xml;
// the dictionary below is the fallback when that def is missing.
// CanRun(float, target) delegates to CanRun(Slate, target), so patching the Slate overload
// covers storyteller, incidents and dev menu.
[HarmonyPatch(typeof(QuestScriptDef), nameof(QuestScriptDef.CanRun), typeof(Slate), typeof(IIncidentTarget))]
public static class Patch_QuestTechGate
{
    public static readonly Dictionary<string, TechLevel> defaultMinTechLevel = new Dictionary<string, TechLevel>
    {
        { "ThreatReward_MechPods_MiscReward", TechLevel.Industrial },
        { "ProblemCauser", TechLevel.Industrial },
        { "GravshipWreckage", TechLevel.Industrial },
        { "OpportunitySite_AncientComplex", TechLevel.Industrial },
        { "OpportunitySite_AncientComplex_Giver", TechLevel.Industrial },
        { "OpportunitySite_AncientMercenaries", TechLevel.Industrial },
        { "Mission_BanditCamp", TechLevel.Industrial },
        { "ThreatReward_SiteThreat_ItemPod", TechLevel.Industrial },
        { "ThreatReward_SiteThreat_Joiner", TechLevel.Medieval },
        { "ThreatReward_Infestation_ItemPod", TechLevel.Medieval },
    };

    public static Dictionary<string, TechLevel> MinTechLevel =>
        BalanceSettingsDef.Instance?.questMinTechLevel ?? defaultMinTechLevel;

    public static bool Prefix(QuestScriptDef __instance, ref bool __result)
    {
        if (!MinTechLevel.TryGetValue(__instance.defName, out TechLevel minTech))
            return true;

        TechLevel playerTech = IgnoranceCompat.PlayerTechLevel;
        if (playerTech == TechLevel.Undefined || playerTech >= minTech)
            return true;

        __result = false;
        return false;
    }
}
