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
            "SR.ModRimWorld.RaidExtension.IncidentWorkerHostileTraderCaravanPassing",
            "SR.ModRimWorld.RaidExtension.IncidentWorkerHostileTraveler",
            "SR.ModRimWorld.RaidExtension.IncidentWorkerLogging",
            "SR.ModRimWorld.RaidExtension.IncidentWorkerPoaching",
        };

        private static readonly string[] presetFactionResetTypeNames =
        {
            "SR.ModRimWorld.RaidExtension.IncidentWorkerHostileTraderCaravanPassing",
            "SR.ModRimWorld.RaidExtension.IncidentWorkerHostileTraveler",
        };

        static RaidExtensionTechFilter()
        {
            if (AccessTools.TypeByName("SR.ModRimWorld.RaidExtension.IncidentWorkerHostileTraderCaravanPassing") == null)
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

                bool resetsPresetFaction = System.Array.IndexOf(presetFactionResetTypeNames, typeName) >= 0;
                var tryExec = AccessTools.DeclaredMethod(type, "TryExecuteWorker");
                if (tryExec != null)
                    harmony.Patch(tryExec,
                        prefix: resetsPresetFaction ? new HarmonyMethod(typeof(RaidExtensionTechFilter), nameof(TryExecutePrefix)) : null,
                        postfix: new HarmonyMethod(typeof(RaidExtensionTechFilter), nameof(TryExecutePostfix)));
                else if (resetsPresetFaction)
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

            Log.Message($"[HSKMoreBalanceQuests] RaidExtension: preset faction {Describe(f)} is outside the tech range, cleared");
            parms.faction = null;
        }

        public static void TryExecutePostfix(IncidentWorker __instance, IncidentParms parms, bool __result)
        {
            if (!HSKMoreBalanceQuestsMod.EventsEnabled)
                return;

            var f = parms?.faction;
            Log.Message($"[HSKMoreBalanceQuests] RaidExtension {__instance.def?.defName} ({__instance.GetType().Name}) fired={__result} " +
                        $"playerTech={IgnoranceCompat.PlayerTechLevel} faction={Describe(f)} iibEligible={IgnoranceCompat.FactionIsEligible(f)}");
        }

        private static string Describe(Faction f)
        {
            return f == null ? "none" : $"{f.Name} [{f.def.defName}, {f.def.techLevel}, {f.PlayerRelationKind}]";
        }
    }
}
