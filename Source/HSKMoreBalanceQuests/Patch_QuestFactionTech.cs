using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace HSKMoreBalanceQuests;

// Common faction filter for XML quests: every QuestNode_GetFaction pick (quest raid enemies,
// hospitality attackers, site threat owners, peace talks) must pass Ignorance Is Bliss's
// faction tech range. IiB itself doesn't check this node, so quest raids ignored it.
// With IiB defaults (MechanoidsAreAlwaysEligible = false) this also keeps mechanoids away
// from low-tech colonies. Factions dictated by a pawn (ofPawn) are never filtered.
[HarmonyPatch(typeof(QuestNode_GetFaction), "IsGoodFaction")]
public static class Patch_QuestFactionTech
{
    public static void Postfix(QuestNode_GetFaction __instance, Faction faction, Slate slate, ref bool __result)
    {
        if (!HSKMoreBalanceQuestsMod.QuestsEnabled)
            return;

        if (!__result || faction == null || faction.IsPlayer)
            return;

        if (__instance.ofPawn.GetValue(slate) != null)
            return;

        if (IgnoranceCompat.FactionIsEligible(faction))
            return;

        __result = false;
    }
}
