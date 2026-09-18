using System.Diagnostics;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace HSKMoreBalanceQuests;

[HarmonyPatch(typeof(QuestNode_Root_Beggars), nameof(QuestNode_Root_Beggars.LodgerCountFromPopulation))]
public static class Patch_RefugeeCount
{
    public static void Postfix(ref int __result, Map map)
    {
        Log.Message("[HSKMoreBalanceQuests] Patch_RefugeeCount fired");
        var settings = HSKMoreBalanceQuestsMod.Settings;
        if (settings == null || !settings.limitRefugees)
            return;

        bool isHospitality = new StackTrace().ToString().Contains("Hospitality_Refugee");
        int maxSetting = isHospitality ? settings.maxHelpers : settings.maxRefugees;

        int colonists = map.mapPawns.FreeColonistsSpawnedCount;
        int randomOffset = Rand.RangeInclusive(-1, 1);
        int limit = Mathf.Min(colonists / 2 + randomOffset, maxSetting);
        limit = Mathf.Max(limit, 1);
        int original = __result;

        __result = Mathf.Min(__result, limit);

        string questType = isHospitality ? "Helpers" : "Refugees";
        Log.Message($"[HSKMoreBalanceQuests] {questType}: colonists={colonists}, original={original}, limit={limit}, max={maxSetting}, result={__result}");
    }
}
