using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests;

[StaticConstructorOnStartup]
public static class Patch_RefugeeChased
{
    static Patch_RefugeeChased()
    {
        var type = AccessTools.TypeByName("SK.IncidentWorker_RefugeeChased");
        if (type == null)
        {
            Log.Message("[HSKMoreBalanceQuests] SK.IncidentWorker_RefugeeChased not found, skipping patch.");
            return;
        }

        var method = AccessTools.Method(type, "TryExecuteWorker");
        if (method == null)
        {
            Log.Message("[HSKMoreBalanceQuests] TryExecuteWorker not found on RefugeeChased, skipping patch.");
            return;
        }

        var harmony = new Harmony("linya.hskmorebalancequests.refugeechased");
        harmony.Patch(method, postfix: new HarmonyMethod(typeof(Patch_RefugeeChased), nameof(Postfix)));
        Log.Message("[HSKMoreBalanceQuests] RefugeeChased patch applied.");
    }

    private static FieldInfo diaNodeField;
    private static FieldInfo incidentParmsField;

    public static void Postfix(object __instance, bool __result)
    {
        Log.Message("[HSKMoreBalanceQuests] Patch_RefugeeChased fired");
        if (!__result) return;

        // Cache reflection
        if (diaNodeField == null)
        {
            var type = __instance.GetType();
            diaNodeField = AccessTools.Field(type, "diaNode");
            incidentParmsField = AccessTools.Field(type, "incidentParms");
        }

        var diaNode = diaNodeField?.GetValue(__instance) as DiaNode;
        var parms = incidentParmsField?.GetValue(__instance) as IncidentParms;

        if (diaNode == null || diaNode.options == null || diaNode.options.Count < 2)
            return;

        // Apply threat multiplier to raid incidentParms
        var settings = HSKMoreBalanceQuestsMod.Settings;
        if (settings != null && parms != null)
        {
            float original = parms.points;
            parms.points *= settings.threatMultiplier;
            Log.Message($"[HSKMoreBalanceQuests] Refugee raid threat reduced: {original:F0} -> {parms.points:F0} (x{settings.threatMultiplier})");
        }
    }
}
