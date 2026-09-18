using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace HSKMoreBalanceQuests;

// Lodger count of Hospitality_Prisoners and Hospitality_Joiners by player tech level (per Ignorance
// Is Bliss when installed): the vanilla count is clamped into the level range below. A royal asker counts as one
// of the joiners. The count is picked in one of two ways, both patched:
// - usually Hospitality_Util_DecideRandomLodgerCount (QuestNode_GetPawnCountByPointsWeighted);
// - 1-star quests with no lodger conditions use Hospitality_Util_DecideLodgerCountFromPoints
//   (QuestNode_EvaluateSimpleCurve).
// Levels without a range (Spacer and above) keep the vanilla count.
[HarmonyPatch(typeof(QuestNode_GetPawnCountByPointsWeighted), "RunInt")]
public static class Patch_PrisonerCount
{
    public static readonly Dictionary<TechLevel, IntRange> lodgersByTech = new Dictionary<TechLevel, IntRange>
    {
        { TechLevel.Neolithic, new IntRange(1, 2) },
        { TechLevel.Medieval, new IntRange(2, 4) },
        { TechLevel.Industrial, new IntRange(4, 6) },
    };

    public static void Postfix()
    {
        if (!HSKMoreBalanceQuestsMod.QuestsEnabled)
            return;

        LimitLodgers(QuestGen.slate);
    }

    public static void LimitLodgers(Slate slate)
    {
        var settings = HSKMoreBalanceQuestsMod.Settings;
        if (settings == null || !settings.limitRefugees || slate == null)
            return;

        // Only prisoner and joiner hospitality quests
        bool isPrisoners = slate.TryGet<bool>("lodgersArePrisoners", out bool prisoners) && prisoners;
        bool isJoiners = QuestGen.Root?.defName == "Hospitality_Joiners";
        if (!isPrisoners && !isJoiners)
            return;

        TechLevel tech = IgnoranceCompat.PlayerTechLevel;
        // IiB reports Animal until the Neolithic threshold is reached
        if (tech == TechLevel.Animal)
            tech = TechLevel.Neolithic;
        if (!lodgersByTech.TryGetValue(tech, out IntRange range))
            return;

        if (!slate.TryGet<float>("lodgersCount", out float original))
            return;

        int count = Mathf.Clamp(Mathf.RoundToInt(original), range.min, range.max);
        if (count == Mathf.RoundToInt(original))
            return;

        slate.Set("lodgersCount", count);
        string kind = isPrisoners ? "Prisoners" : "Joiners";
        Log.Message($"[HSKMoreBalanceQuests] {kind} count: {original} -> {count} (tech {tech}, range {range.min}-{range.max})");
    }
}

[HarmonyPatch(typeof(QuestNode_EvaluateSimpleCurve), "RunInt")]
public static class Patch_PrisonerCountFromPoints
{
    public static void Postfix(QuestNode_EvaluateSimpleCurve __instance)
    {
        if (!HSKMoreBalanceQuestsMod.QuestsEnabled)
            return;

        var slate = QuestGen.slate;
        if (slate == null || __instance.storeAs.GetValue(slate) != "lodgersCount")
            return;

        Patch_PrisonerCount.LimitLodgers(slate);
    }
}
