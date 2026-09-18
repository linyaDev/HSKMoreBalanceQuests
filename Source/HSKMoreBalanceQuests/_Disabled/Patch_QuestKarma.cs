using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests;

[StaticConstructorOnStartup]
public static class HSKMoreBalanceQuestsInit
{
    static HSKMoreBalanceQuestsInit()
    {
        var harmony = new Harmony("linya.hskmorebalancequests");
        harmony.PatchAll();
        Patch_WastepackCount.PatchCurve();
        Log.Message("[QuestPressure] Patches applied.");
    }
}

// Quests excluded from karma penalty
static class QuestKarmaExclusions
{
    public static readonly HashSet<string> excludedQuests = new HashSet<string>
    {
        "OpportunitySite_WorkSite",
        "OpportunitySite_ItemStash",
        "OpportunitySite_DownedRefugee",
        "OpportunitySite_PrisonerWillingToJoin",
        "OpportunitySite_PeaceTalks",
        "OpportunitySite_AncientComplex",
        "OpportunitySite_AncientComplex_Mechanitor",
        "OpportunitySite_DistressCall",
        "RefugeeBetrayal",
        "SanguophageShip",
        "Intro_Deserter",
        "SightstealerArrival",
        "BestowingCeremony",
        "CreepJoinerArrival",
        "CreepJoinerArrival_Metalhorror",
        "TradeRequest"
    };

    public static bool IsExcluded(Quest quest)
    {
        return quest.root != null && excludedQuests.Contains(quest.root.defName);
    }

    public static bool IsBanditCamp(Quest quest)
    {
        return quest.root != null && quest.root.defName == "OpportunitySite_BanditCamp";
    }

    public static bool IsBugged(Quest quest)
    {
        return quest.PartsListForReading == null || quest.PartsListForReading.Count == 0
            || (quest.name != null && quest.name.StartsWith("ERR:"));
    }
}

[HarmonyPatch(typeof(Quest), nameof(Quest.QuestTick))]
public static class Patch_QuestExpired
{
    public static void Prefix(Quest __instance, out bool __state)
    {
#if V15
        __state = __instance.ticksUntilAcceptanceExpiry == 1
                  && __instance.State == QuestState.NotYetAccepted
                  && !__instance.cleanedUp;
#else
        __state = __instance.TicksUntilExpiry == 0
                  && __instance.State == QuestState.NotYetAccepted
                  && !__instance.cleanedUp;
#endif
    }

    public static void Postfix(Quest __instance, bool __state)
    {
        if (!__state) return;
        Log.Message("[HSKMoreBalanceQuests] Patch_QuestExpired: quest expiring: " + (__instance.name ?? "?"));
        if (QuestKarmaExclusions.IsExcluded(__instance)) return;
        if (QuestKarmaExclusions.IsBugged(__instance))
        {
            Messages.Message("QP_BuggedQuestSkipped".Translate(__instance.name ?? "Unknown"), MessageTypeDefOf.NeutralEvent, false);
            return;
        }

        var comp = Current.Game?.GetComponent<GameComponent_QuestPressure>();
        if (comp == null) return;

        if (__instance.charity)
            comp.RecordQuest(__instance.name ?? "Unknown", __instance.id, QuestRecordType.CharityExpired);
        else
            comp.RecordQuest(__instance.name ?? "Unknown", __instance.id, QuestRecordType.Expired);
    }
}

[HarmonyPatch(typeof(Quest), nameof(Quest.End))]
public static class Patch_QuestCompleted
{
    public static void Postfix(Quest __instance, QuestEndOutcome outcome)
    {
        Log.Message("[HSKMoreBalanceQuests] Patch_QuestCompleted fired");
        var comp = Current.Game?.GetComponent<GameComponent_QuestPressure>();
        if (comp == null) return;

        string name = __instance.name ?? "Unknown";

        if (outcome == QuestEndOutcome.Success)
        {
            if (QuestKarmaExclusions.IsExcluded(__instance)
                && !QuestKarmaExclusions.IsBanditCamp(__instance))
                return;

            if (__instance.charity)
                comp.RecordQuest(name, __instance.id, QuestRecordType.CharityCompleted);
            else
                comp.RecordQuest(name, __instance.id, QuestRecordType.Completed);

            // Bonus karma for higher difficulty (1 star = 0, 2 stars = +1, 3 stars = +2)
            int bonus = __instance.challengeRating - 1;
            if (bonus > 0)
            {
                comp.RecordQuest("QP_DifficultyBonus".Translate(), __instance.id, QuestRecordType.TinyBonus, customWeight: bonus);
                GameComponent_QuestPressure.LogDebug($"Quest difficulty bonus: +{bonus} for {name} (rating {__instance.challengeRating})");
            }
        }
        else if (outcome == QuestEndOutcome.Fail)
        {
            if (QuestKarmaExclusions.IsExcluded(__instance)) return;
            if (QuestKarmaExclusions.IsBugged(__instance))
            {
                Messages.Message("QP_BuggedQuestSkipped".Translate(__instance.name ?? "Unknown"), MessageTypeDefOf.NeutralEvent, false);
                return;
            }

            if (__instance.charity)
                comp.RecordQuest(name, __instance.id, QuestRecordType.CharityFailed);
            else
                comp.RecordQuest(name, __instance.id, QuestRecordType.Failed);
        }
    }
}
