using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests.Events
{
    [StaticConstructorOnStartup]
    public static class GuestTechFilter
    {
        private static readonly System.Reflection.MethodInfo planNewVisitMethod;
        private static readonly Func<Faction, Map, float> getTravelDays;
        private static readonly AccessTools.FieldRef<DiaOption, string> diaOptionText;

        private static bool inviting;

        static GuestTechFilter()
        {
            var utility = AccessTools.TypeByName("Hospitality.Utilities.GenericUtility");
            if (utility == null)
                return;

            var planNewVisit = AccessTools.Method(utility, "PlanNewVisit");
            var fillQueue = AccessTools.Method(utility, "FillIncidentQueue");
            var travelDays = AccessTools.Method(utility, "GetTravelDays");
            var dialogFor = AccessTools.Method(typeof(FactionDialogMaker), nameof(FactionDialogMaker.FactionDialogFor));
            if (planNewVisit == null || fillQueue == null || travelDays == null || dialogFor == null)
            {
                Log.Warning("[HSKMoreBalanceQuests] GuestTechFilter: API Hospitality изменилось — фильтр гостей отключён.");
                return;
            }

            planNewVisitMethod = planNewVisit;
            getTravelDays = AccessTools.MethodDelegate<Func<Faction, Map, float>>(travelDays);
            diaOptionText = AccessTools.FieldRefAccess<DiaOption, string>("text");

            var harmony = new Harmony("linya.hskmorebalancequests.guesttechfilter");
            harmony.Patch(planNewVisit,
                prefix: new HarmonyMethod(typeof(GuestTechFilter), nameof(PlanNewVisitPrefix)));
            harmony.Patch(fillQueue,
                prefix: new HarmonyMethod(typeof(GuestTechFilter), nameof(FillIncidentQueuePrefix)));
            harmony.Patch(dialogFor,
                postfix: new HarmonyMethod(typeof(GuestTechFilter), nameof(FactionDialogForPostfix)) { priority = Priority.Last });

            var visitorWorkerType = AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup");
            var factionSource = visitorWorkerType == null ? null : AccessTools.DeclaredMethod(visitorWorkerType, "FactionCanBeGroupSource");
            if (factionSource != null)
                harmony.Patch(factionSource,
                    postfix: new HarmonyMethod(typeof(GuestTechFilter), nameof(FactionCanBeGroupSourcePostfix)));
            else
                Log.Warning("[HSKMoreBalanceQuests] GuestTechFilter: IncidentWorker_VisitorGroup.FactionCanBeGroupSource не найден — визиты от рассказчика не фильтруются.");
        }

        public static bool FactionTechAllowed(Faction faction)
        {
            if (faction?.def == null || Current.Game == null)
                return true;

            TechLevel factionTech = faction.def.techLevel;
            TechLevel playerTech = IgnoranceCompat.PlayerTechLevel;
            if (factionTech == TechLevel.Undefined || playerTech == TechLevel.Undefined)
                return true;
            if (playerTech < TechLevel.Neolithic)
                playerTech = TechLevel.Neolithic;

            int diff = (int)factionTech - (int)playerTech;
            int ahead = EventSettingsDef.GuestMaxTechAhead;
            int behind = EventSettingsDef.GuestMaxTechBehind;
            if (ahead >= 0 && diff > ahead)
                return false;
            if (behind >= 0 && -diff > behind)
                return false;
            return true;
        }

        public static bool PlanNewVisitPrefix(Faction faction)
        {
            if (!HSKMoreBalanceQuestsMod.EventsEnabled)
                return true;

            if (faction == null)
                return true;

            return inviting || FactionTechAllowed(faction);
        }

        public static void FactionCanBeGroupSourcePostfix(Faction f, ref bool __result)
        {
            if (!HSKMoreBalanceQuestsMod.EventsEnabled)
                return;

            if (__result && f != null && !FactionTechAllowed(f))
                __result = false;
        }

        public static bool FillIncidentQueuePrefix(Map map)
        {
            if (!HSKMoreBalanceQuestsMod.EventsEnabled)
                return true;

            float days = Rand.Range(10f, 16f);
            int count = Rand.Range(1, 4);
            var candidates = Find.FactionManager.AllFactionsVisible
                .Where(f => !f.IsPlayer && !f.defeated && !f.HostileTo(Faction.OfPlayer) && FactionTechAllowed(f))
                .OrderBy(f => getTravelDays(f, map));
            foreach (var faction in candidates)
            {
                count--;
                planNewVisitMethod.Invoke(null, new object[] { map, days, faction });
                days += Rand.Range(15f, 25f);
                if (count <= 0)
                    break;
            }
            return false;
        }

        public static void FactionDialogForPostfix(DiaNode __result)
        {
            if (!HSKMoreBalanceQuestsMod.EventsEnabled)
                return;

            if (__result?.options == null)
                return;

            string inviteLabel = "InviteGuests".Translate();
            foreach (var option in __result.options)
            {
                if (option?.action == null || diaOptionText(option) != inviteLabel)
                    continue;

                var original = option.action;
                option.action = () =>
                {
                    inviting = true;
                    try
                    {
                        original();
                    }
                    finally
                    {
                        inviting = false;
                    }
                };
            }
        }
    }
}
