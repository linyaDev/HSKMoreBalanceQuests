using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace HSKMoreBalanceQuests;

[HarmonyPatch(typeof(QuestScriptDef), nameof(QuestScriptDef.CanRun), typeof(Slate), typeof(IIncidentTarget))]
public static class Patch_NoDoubleGuests
{
    private static readonly HashSet<string> lodgerQuests = new HashSet<string>
    {
        "Hospitality_Joiners",
        "Hospitality_Prisoners",
    };

    public static bool Prefix(QuestScriptDef __instance, ref bool __result)
    {
        if (!HSKMoreBalanceQuestsMod.QuestsEnabled)
            return true;

        Log.Message("[HSKMoreBalanceQuests] Patch_NoDoubleGuests fired");
        if (!lodgerQuests.Contains(__instance.defName))
            return true;

        if (!HasLodgersOnAnyMap())
            return true;

        Messages.Message("QP_GuestsAlreadyPresent".Translate(), MessageTypeDefOf.RejectInput, false);
        __result = false;
        return false;
    }

    public static bool HasLodgersOnAnyMap()
    {
        foreach (var map in Find.Maps)
        {
            if (!map.IsPlayerHome) continue;
            if (map.mapPawns.FreeColonistsSpawned.Any(p => p.IsQuestLodger()))
                return true;
        }
        return false;
    }
}

[HarmonyPatch(typeof(QuestNode_Root_Beggars), "TestRunInt")]
public static class Patch_NoDoubleGuests_Beggars
{
    public static bool Prefix(ref bool __result)
    {
        if (!HSKMoreBalanceQuestsMod.QuestsEnabled)
            return true;

        Log.Message("[HSKMoreBalanceQuests] Patch_NoDoubleGuests_Beggars fired");
        if (Patch_NoDoubleGuests.HasLodgersOnAnyMap())
        {
            Messages.Message("QP_GuestsAlreadyPresent".Translate(), MessageTypeDefOf.RejectInput, false);
            __result = false;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(QuestNode_Root_Hospitality_Refugee), "TestRunInt")]
public static class Patch_NoDoubleGuests_Refugee
{
    public static bool Prefix(ref bool __result)
    {
        if (!HSKMoreBalanceQuestsMod.QuestsEnabled)
            return true;

        Log.Message("[HSKMoreBalanceQuests] Patch_NoDoubleGuests_Refugee fired");
        if (Patch_NoDoubleGuests.HasLodgersOnAnyMap())
        {
            Messages.Message("QP_GuestsAlreadyPresent".Translate(), MessageTypeDefOf.RejectInput, false);
            __result = false;
            return false;
        }
        return true;
    }
}
