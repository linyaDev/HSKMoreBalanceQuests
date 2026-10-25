using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests.Events
{
    // События Core_SK "вражеский десант" (EnemyShip, IncidentWorker_EnemyShipPart) и
    // "портальный рейд" (PortalRaid, IncidentWorker_PortalRaid) выбирают фракцию прямо
    // в TryExecuteWorker: берут CandidateFactions и отбирают из них любую с техуровнем
    // не ниже порога (Industrial+ у корабля, Spacer+ у портала) и с боевым pawnGroupMaker;
    // техуровень игрока не учитывается. Раньше тот же выбор жил в приватном ChooseFaction,
    // но в Core_SK 1.6.1023 его убрали, поэтому режем список кандидатов там, откуда он
    // берётся — в FactionCanBeGroupSource у IncidentWorker_RaidEnemy (своего
    // переопределения у этих двух воркеров нет). Пропускаем кандидатов через диапазон
    // Ignorance Is Bliss; если в диапазоне никого нет — событие не запускается.
    [StaticConstructorOnStartup]
    public static class CoreSKRaidFactionTechFilter
    {
        // Тип воркера -> минимальный техуровень фракции из его TryExecuteWorker.
        private static readonly Dictionary<System.Type, TechLevel> workerMinTech = new();

        static CoreSKRaidFactionTechFilter()
        {
            TryRegisterWorker("SK.Events.IncidentWorker_EnemyShipPart", TechLevel.Industrial);
            TryRegisterWorker("SK.Events.IncidentWorker_PortalRaid", TechLevel.Spacer);
            if (workerMinTech.Count == 0)
                return;

            var harmony = new Harmony("linya.hskmorebalancequests.coreskraidfactiontechfilter");

            var groupSource = AccessTools.DeclaredMethod(typeof(IncidentWorker_RaidEnemy),
                nameof(IncidentWorker_RaidEnemy.FactionCanBeGroupSource));
            if (groupSource == null)
            {
                Log.Warning("[HSKMoreBalanceQuests] CoreSKRaidFactionTechFilter: не найден "
                    + "IncidentWorker_RaidEnemy.FactionCanBeGroupSource — API RimWorld изменилось?");
                return;
            }

            harmony.Patch(groupSource,
                postfix: new HarmonyMethod(typeof(CoreSKRaidFactionTechFilter), nameof(FactionSourcePostfix)));
            harmony.Patch(AccessTools.Method(typeof(IncidentWorker), "CanFireNow"),
                postfix: new HarmonyMethod(typeof(CoreSKRaidFactionTechFilter), nameof(CanFireNowPostfix)));
        }

        private static void TryRegisterWorker(string typeName, TechLevel minTech)
        {
            var type = AccessTools.TypeByName(typeName);
            if (type != null)
                workerMinTech[type] = minTech;
        }

        // Рассказчик: если ни одна фракция не проходит диапазон, событие не выпадает вовсе.
        public static void CanFireNowPostfix(IncidentWorker __instance, ref bool __result)
        {
            if (!__result || !HSKMoreBalanceQuestsMod.EventsEnabled)
                return;

            if (!workerMinTech.TryGetValue(__instance.GetType(), out TechLevel minTech))
                return;

            if (EligibleFactions(minTech, forced: false).Count == 0)
                __result = false;
        }

        // Кандидат вне диапазона Ignorance Is Bliss — выбрасываем из списка Core_SK.
        public static void FactionSourcePostfix(IncidentWorker __instance, Faction f, bool desperate, ref bool __result)
        {
            if (!__result || !HSKMoreBalanceQuestsMod.EventsEnabled)
                return;

            if (!workerMinTech.TryGetValue(__instance.GetType(), out TechLevel minTech))
                return;

            if (IgnoranceCompat.FactionIsEligible(f))
                return;

            // Форс из dev-меню, когда в диапазоне никого: не вычищаем список совсем,
            // иначе Core_SK выберет случайный элемент из пустого и останется без фракции.
            if (EligibleFactions(minTech, desperate).Count == 0)
                return;

            __result = false;
        }

        // Критерии выбора из Core_SK плюс диапазон Ignorance Is Bliss.
        private static List<Faction> EligibleFactions(TechLevel minTech, bool forced)
        {
            var list = new List<Faction>();
            foreach (var f in Find.FactionManager.AllFactions)
            {
                if (f == Faction.OfPlayer || f.defeated || !f.HostileTo(Faction.OfPlayer))
                    continue;
                if (f.def.techLevel < minTech || !f.def.humanlikeFaction)
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
