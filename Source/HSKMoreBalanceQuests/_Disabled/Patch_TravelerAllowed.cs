using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests;

// +1 karma when travelers are allowed through (ABE forced=true)
[HarmonyPatch(typeof(IncidentWorker_TravelerGroup), "TryExecuteWorker")]
public static class Patch_TravelerAllowed
{
    public static void Postfix(bool __result, IncidentParms parms)
    {
        Log.Message("[HSKMoreBalanceQuests] Patch_TravelerAllowed fired");
        if (!__result || !parms.forced)
            return;

        var comp = Current.Game?.GetComponent<GameComponent_QuestPressure>();
        comp?.RecordQuest("QP_GuestsAllowed".Translate(), 0, QuestRecordType.TinyBonus);
    }
}

// -1 karma when travelers are sent away (patch ABE dialog)
[StaticConstructorOnStartup]
public static class Patch_TravelerRefused
{
    static Patch_TravelerRefused()
    {
        var abeType = AccessTools.TypeByName("AskBeforeEnter.Main");
        if (abeType == null)
        {
            Log.Message("[HSKMoreBalanceQuests] AskBeforeEnter not found, skipping refusal patch.");
            return;
        }

        var method = AccessTools.Method(abeType, "AskDialog");
        if (method == null)
        {
            Log.Message("[HSKMoreBalanceQuests] AskDialog not found, skipping refusal patch.");
            return;
        }

        var harmony = new Harmony("linya.hskmorebalancequests.travelerrefused");
        harmony.Patch(method, postfix: new HarmonyMethod(typeof(Patch_TravelerRefused), nameof(Postfix)));
        Log.Message("[HSKMoreBalanceQuests] AskBeforeEnter refusal patch applied.");
    }

    public static void Postfix(IncidentParms parms, IncidentWorker incident)
    {
        Log.Message("[HSKMoreBalanceQuests] Patch_TravelerRefused fired");
        if (Find.WindowStack == null) return;

        // Only apply karma penalty for TravelerGroup, not caravans or visitors
        if (incident?.def?.defName != "TravelerGroup") return;

        // Find the Dialog_NodeTree that ABE just added
        for (int i = Find.WindowStack.Count - 1; i >= 0; i--)
        {
            if (Find.WindowStack[i] is Dialog_NodeTree dialog)
            {
                WrapRejectAction(dialog);
                return;
            }
        }
    }

    private static void WrapRejectAction(Dialog_NodeTree dialog)
    {
        // ABE dialog: first option is "Send away" (reject)
        var rootNode = AccessTools.Field(typeof(Dialog_NodeTree), "curNode")?.GetValue(dialog) as DiaNode;
        if (rootNode == null || rootNode.options == null || rootNode.options.Count == 0)
            return;

        var rejectOption = rootNode.options[0];
        var originalAction = rejectOption.action;
        rejectOption.action = delegate
        {
            originalAction?.Invoke();
            var comp = Current.Game?.GetComponent<GameComponent_QuestPressure>();
            comp?.RecordQuest("QP_GuestsRefused".Translate(), 0, QuestRecordType.TinyPenalty);
        };
    }
}
