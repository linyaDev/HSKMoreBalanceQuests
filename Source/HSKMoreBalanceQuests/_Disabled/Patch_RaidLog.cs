using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests;

[HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.TryExecute))]
public static class Patch_RaidLog
{
    public static void Prefix(IncidentWorker __instance, IncidentParms parms)
    {
        if (__instance.def != IncidentDefOf.RaidEnemy)
            return;

        string questInfo = parms.quest?.name ?? parms.questScriptDef?.defName ?? "no quest";
        string msg = $"Raid incoming: {parms.points:F0} pts, faction: {parms.faction?.Name ?? "?"}, quest: {questInfo}";
        Log.Message($"[HSKMoreBalanceQuests] {msg}");
        QuestDebugLog.LogDebug(msg);
    }
}
