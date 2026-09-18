using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests.Events
{
    [StaticConstructorOnStartup]
    public static class IncidentTechGate
    {
        static IncidentTechGate()
        {
            var canFireNow = AccessTools.Method(typeof(IncidentWorker), "CanFireNow");
            if (canFireNow == null)
            {
                Log.Warning("[HSKMoreBalanceQuests] IncidentTechGate: IncidentWorker.CanFireNow not found.");
                return;
            }

            new Harmony("linya.hskmorebalancequests.incidenttechgate").Patch(canFireNow,
                postfix: new HarmonyMethod(typeof(IncidentTechGate), nameof(CanFireNowPostfix)));
        }

        public static void CanFireNowPostfix(IncidentWorker __instance, ref bool __result)
        {
            if (!HSKMoreBalanceQuestsMod.EventsEnabled)
                return;

            if (!__result)
                return;

            var gates = EventSettingsDef.IncidentGates;
            string defName = __instance?.def?.defName;
            if (gates == null || defName == null)
                return;

            if (gates.TryGetValue(defName, out TechLevel minTech)
                && IgnoranceCompat.PlayerTechLevel < minTech)
            {
                __result = false;
            }
        }
    }
}
