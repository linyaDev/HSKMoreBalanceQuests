using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HSKMoreBalanceQuests;

[HarmonyPatch(typeof(StorytellerComp), nameof(StorytellerComp.GenerateParms))]
public static class Patch_QuestThreat
{
    public static void Postfix(ref IncidentParms __result, IncidentCategoryDef incCat, StorytellerComp __instance)
    {
        if (incCat != IncidentCategoryDefOf.GiveQuest)
            return;

        var settings = HSKMoreBalanceQuestsMod.Settings;
        if (settings == null)
            return;

        bool isSite = __instance is StorytellerComp_WorkSite;
        float multiplier = isSite ? settings.siteThreatMultiplier : settings.threatMultiplier;

        const float MinPoints = 350f;
        float original = __result.points;
        float scaled = original * multiplier;
        __result.points = Mathf.Min(Mathf.Max(scaled, MinPoints), original);
        string questInfo = __result.questScriptDef?.defName ?? __result.quest?.name ?? "?";
        string label = isSite ? "Site" : "Quest";
        string clamped = scaled < MinPoints ? ", clamped to min" : "";
        string msg = $"{label} points: {original:F0} -> {__result.points:F0} (x{multiplier}{clamped}), quest: {questInfo}";
        Log.Message($"[HSKMoreBalanceQuests] {msg}");
        QuestDebugLog.LogDebug(msg);
    }
}
