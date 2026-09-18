using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace HSKMoreBalanceQuests;

// ShuttleCrash_Rescue has a C# root that picks the attacking faction itself (any visible faction
// hostile to both the Empire and the player), bypassing QuestNode_GetFaction and therefore
// Patch_QuestFactionTech. Same pick, plus the Ignorance Is Bliss faction tech range.
// TestRunInt calls this method too, so with no eligible enemy the quest is simply not offered.
[HarmonyPatch(typeof(QuestNode_Root_ShuttleCrash_Rescue), "TryFindEnemyFaction")]
public static class Patch_ShuttleCrashEnemyFaction
{
    public static bool Prefix(ref Faction enemyFaction, ref bool __result)
    {
        __result = Find.FactionManager.AllFactionsVisible
            .Where(f => f.HostileTo(Faction.OfEmpire) && f.HostileTo(Faction.OfPlayer) && IgnoranceCompat.FactionIsEligible(f))
            .TryRandomElement(out enemyFaction);
        return false;
    }
}
