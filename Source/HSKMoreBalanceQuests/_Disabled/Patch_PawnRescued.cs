using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests;

[HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.Notify_PawnUndowned))]
public static class Patch_PawnRescued
{
    public static void Postfix(Pawn_GuestTracker __instance)
    {
        var pawn = __instance.pawn;
        if (pawn == null || !pawn.RaceProps.Humanlike)
            return;

        if (!__instance.getRescuedThoughtOnUndownedBecauseOfPlayer)
            return;

        // Don't track own colonists
        if (pawn.Faction == Faction.OfPlayer && pawn.IsColonist)
            return;

        var comp = Current.Game?.GetComponent<GameComponent_QuestPressure>();
        comp?.TrackRescuedPawn(pawn);
    }
}
