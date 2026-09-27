using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests.Events
{
    // Событие Core_SK "вражеский десант" (EnemyShip, SK.Events.IncidentWorker_EnemyShipPart)
    // выбирает фракцию в приватном ChooseFaction: любая враждебная человеческая фракция
    // Industrial+, техуровень игрока не учитывается. Пропускаем выбор через диапазон
    // Ignorance Is Bliss; если в диапазоне никого нет — событие не запускается.
    [StaticConstructorOnStartup]
    public static class EnemyShipFactionTechFilter
    {
        private static readonly System.Type workerType;

        static EnemyShipFactionTechFilter()
        {
            workerType = AccessTools.TypeByName("SK.Events.IncidentWorker_EnemyShipPart");
            if (workerType == null)
                return;

            var chooseFaction = AccessTools.DeclaredMethod(workerType, "ChooseFaction");
            if (chooseFaction == null)
            {
                Log.Warning("[HSKMoreBalanceQuests] EnemyShipFactionTechFilter: не найден ChooseFaction у IncidentWorker_EnemyShipPart — API Core_SK изменилось?");
                return;
            }

            var harmony = new Harmony("linya.hskmorebalancequests.enemyshipfactiontechfilter");
            harmony.Patch(chooseFaction,
                postfix: new HarmonyMethod(typeof(EnemyShipFactionTechFilter), nameof(ChooseFactionPostfix)));
            harmony.Patch(AccessTools.Method(typeof(IncidentWorker), "CanFireNow"),
                postfix: new HarmonyMethod(typeof(EnemyShipFactionTechFilter), nameof(CanFireNowPostfix)));
        }

        // Рассказчик: если ни одна фракция не проходит диапазон, событие не выпадает вовсе.
        public static void CanFireNowPostfix(IncidentWorker __instance, ref bool __result)
        {
            if (!__result || !HSKMoreBalanceQuestsMod.EventsEnabled)
                return;

            if (!workerType.IsInstanceOfType(__instance))
                return;

            if (EligibleFactions(forced: false).Count == 0)
                __result = false;
        }

        // Выбор Core_SK (или его фолбэк на Predators) вне диапазона — перевыбираем.
        public static void ChooseFactionPostfix(ref Faction faction, bool forced)
        {
            if (!HSKMoreBalanceQuestsMod.EventsEnabled)
                return;

            if (faction == null || IgnoranceCompat.FactionIsEligible(faction))
                return;

            var candidates = EligibleFactions(forced);
            if (candidates.Count == 0)
                return; // форс из dev-меню, когда в диапазоне никого: оставляем выбор Core_SK

            var replacement = candidates.RandomElement();
            Log.Message($"[HSKMoreBalanceQuests] EnemyShip: фракция {faction.Name} [{faction.def.techLevel}] вне диапазона техуровня, заменена на {replacement.Name} [{replacement.def.techLevel}]");
            faction = replacement;
        }

        // Критерии ChooseFaction из Core_SK плюс диапазон Ignorance Is Bliss.
        private static List<Faction> EligibleFactions(bool forced)
        {
            var list = new List<Faction>();
            foreach (var f in Find.FactionManager.AllFactions)
            {
                if (f == Faction.OfPlayer || f.defeated || !f.HostileTo(Faction.OfPlayer))
                    continue;
                if ((int)f.def.techLevel < (int)TechLevel.Industrial || !f.def.humanlikeFaction)
                    continue;
                if (f.def.pawnGroupMakers == null
                    || !f.def.pawnGroupMakers.Any(x => x.kindDef == PawnGroupKindDefOf.Combat))
                    continue;
                if (!forced && GenDate.DaysPassed < f.def.earliestRaidDays)
                    continue;
                if (!IgnoranceCompat.FactionIsEligible(f))
                    continue;
                list.Add(f);
            }
            return list;
        }
    }
}
