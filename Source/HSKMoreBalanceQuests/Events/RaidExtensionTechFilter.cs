using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests.Events
{
    [StaticConstructorOnStartup]
    public static class RaidExtensionTechFilter
    {
        private static readonly string[] workerTypeNames =
        {
            "SR.ModRimworld.RaidExtension.IncidentWorkerHostileTraderCaravanPassing",
            "SR.ModRimworld.RaidExtension.IncidentWorkerHostileTraveler",
            "SR.ModRimworld.RaidExtension.IncidentWorkerLogging",
            "SR.ModRimworld.RaidExtension.IncidentWorkerPoaching",
        };

        private static readonly string[] presetFactionResetTypeNames =
        {
            "SR.ModRimworld.RaidExtension.IncidentWorkerHostileTraderCaravanPassing",
            "SR.ModRimworld.RaidExtension.IncidentWorkerHostileTraveler",
        };

        static RaidExtensionTechFilter()
        {
            if (AccessTools.TypeByName("SR.ModRimworld.RaidExtension.IncidentWorkerHostileTraderCaravanPassing") == null)
                return;

            var harmony = new Harmony("linya.hskmorebalancequests.raidextensiontechfilter");
            int patched = 0;
            foreach (var typeName in workerTypeNames)
            {
                var type = AccessTools.TypeByName(typeName);
                var method = type == null ? null : AccessTools.DeclaredMethod(type, "FactionCanBeGroupSource");
                if (method == null)
                {
                    Log.Warning($"[HSKMoreBalanceQuests] RaidExtensionTechFilter: не найден FactionCanBeGroupSource у {typeName}");
                    continue;
                }

                harmony.Patch(method,
                    postfix: new HarmonyMethod(typeof(RaidExtensionTechFilter), nameof(FactionSourcePostfix)));
                patched++;

                if (System.Array.IndexOf(presetFactionResetTypeNames, typeName) < 0)
                    continue;

                var tryExec = AccessTools.DeclaredMethod(type, "TryExecuteWorker");
                if (tryExec != null)
                    harmony.Patch(tryExec,
                        prefix: new HarmonyMethod(typeof(RaidExtensionTechFilter), nameof(TryExecutePrefix)));
                else
                    Log.Warning($"[HSKMoreBalanceQuests] RaidExtensionTechFilter: не найден TryExecuteWorker у {typeName}");
            }

            if (patched == 0)
                Log.Warning("[HSKMoreBalanceQuests] RaidExtensionTechFilter: Raid Extension найден, но методы FactionCanBeGroupSource не пропатчены — API изменилось?");
        }

        public static void FactionSourcePostfix(Faction f, ref bool __result)
        {
            if (!HSKMoreBalanceQuestsMod.EventsEnabled)
                return;

            if (__result && !IgnoranceCompat.FactionIsEligible(f))
                __result = false;
        }

        public static void TryExecutePrefix(IncidentParms parms)
        {
            if (!HSKMoreBalanceQuestsMod.EventsEnabled)
                return;

            var f = parms?.faction;
            if (f == null || IgnoranceCompat.FactionIsEligible(f))
                return;

            parms.faction = null;
        }
    }
}
