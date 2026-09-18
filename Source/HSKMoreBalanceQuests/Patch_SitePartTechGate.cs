using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests;

// Blocks quest site parts below a minimum player tech level (per Ignorance Is Bliss when
// installed, otherwise the player faction tech level). The list lives in
// Defs/Misc/BalanceSettings.xml; the dictionary below is the fallback when that def is missing.
// Site threats/guards are picked by tags through SitePartWorker.IsAvailable (SiteMakerHelper,
// QuestNode_GetSitePartDefsByTagsAndFaction), so a blocked part is simply never chosen and the
// quest uses another guard. Overrides (e.g. SitePartWorker_MechCluster) are patched too: they
// call base.IsAvailable() non-virtually, and the trivial base method may be inlined past the
// base patch.
[HarmonyPatch]
public static class Patch_SitePartTechGate
{
    public static readonly Dictionary<string, TechLevel> defaultMinTechLevel = new Dictionary<string, TechLevel>
    {
        // Condition causers (Royalty): spacer-tech buildings
        { "EMIDynamo", TechLevel.Industrial },
        { "SunBlocker", TechLevel.Industrial },
        { "SmokeSpewer", TechLevel.Industrial },
        { "ToxicSpewer", TechLevel.Industrial },
        { "WeatherController", TechLevel.Industrial },
        { "ClimateAdjuster", TechLevel.Industrial },
        { "PsychicDroner", TechLevel.Industrial },
        { "PsychicSuppressor", TechLevel.Industrial },
        // Mechanoids and turrets
        { "SleepingMechanoids", TechLevel.Industrial },
        { "MechCluster", TechLevel.Industrial },
        { "Turrets", TechLevel.Industrial },
    };

    public static Dictionary<string, TechLevel> MinTechLevel =>
        BalanceSettingsDef.Instance?.sitePartMinTechLevel ?? defaultMinTechLevel;

    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(SitePartWorker), nameof(SitePartWorker.IsAvailable));
        foreach (var type in typeof(SitePartWorker).AllSubclasses())
        {
            var method = AccessTools.DeclaredMethod(type, nameof(SitePartWorker.IsAvailable));
            if (method != null && !method.IsAbstract)
                yield return method;
        }
    }

    public static void Postfix(SitePartWorker __instance, ref bool __result)
    {
        if (!HSKMoreBalanceQuestsMod.QuestsEnabled)
            return;

        if (!__result)
            return;

        string defName = __instance?.def?.defName;
        if (defName == null || !MinTechLevel.TryGetValue(defName, out TechLevel minTech))
            return;

        TechLevel playerTech = IgnoranceCompat.PlayerTechLevel;
        if (playerTech == TechLevel.Undefined || playerTech >= minTech)
            return;

        __result = false;
    }
}
