using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests.Events
{
    // События Core_SK "вражеский десант" (EnemyShip, IncidentWorker_EnemyShipPart) и
    // "портальный рейд" (PortalRaid, IncidentWorker_PortalRaid) выбирают фракцию в
    // одинаковом приватном ChooseFaction: любая враждебная человеческая фракция не ниже
    // порога (Industrial+ у корабля, Spacer+ у портала), техуровень игрока не
    // учитывается. Пропускаем выбор через диапазон Ignorance Is Bliss; если в диапазоне
    // никого нет — событие не запускается.
    [StaticConstructorOnStartup]
    public static class CoreSKRaidFactionTechFilter
    {
        // Тип воркера -> минимальный техуровень фракции из его ChooseFaction.
        private static readonly Dictionary<System.Type, TechLevel> workerMinTech = new();

        static CoreSKRaidFactionTechFilter()
        {
            var harmony = new Harmony("linya.hskmorebalancequests.coreskraidfactiontechfilter");
            TryPatchWorker(harmony, "SK.Events.IncidentWorker_EnemyShipPart", TechLevel.Industrial);
            TryPatchWorker(harmony, "SK.Events.IncidentWorker_PortalRaid", TechLevel.Spacer);
            if (workerMinTech.Count > 0)
                harmony.Patch(AccessTools.Method(typeof(IncidentWorker), "CanFireNow"),
                    postfix: new HarmonyMethod(typeof(CoreSKRaidFactionTechFilter), nameof(CanFireNowPostfix)));
        }

        private static void TryPatchWorker(Harmony harmony, string typeName, TechLevel minTech)
        {
            var type = AccessTools.TypeByName(typeName);
            if (type == null)
                return;

            var chooseFaction = AccessTools.DeclaredMethod(type, "ChooseFaction");
            if (chooseFaction == null)
            {
                Log.Warning($"[HSKMoreBalanceQuests] CoreSKRaidFactionTechFilter: не найден ChooseFaction у {typeName} — API Core_SK изменилось?");
                return;
            }

            harmony.Patch(chooseFaction,
                postfix: new HarmonyMethod(typeof(CoreSKRaidFactionTechFilter), nameof(ChooseFactionPostfix)));
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

        // Выбор Core_SK (или его фолбэк на Predators) вне диапазона — перевыбираем.
        public static void ChooseFactionPostfix(object __instance, ref Faction faction, bool forced)
        {
            if (!HSKMoreBalanceQuestsMod.EventsEnabled)
                return;

            if (faction == null || IgnoranceCompat.FactionIsEligible(faction))
                return;

            if (!workerMinTech.TryGetValue(__instance.GetType(), out TechLevel minTech))
                return;

            var candidates = EligibleFactions(minTech, forced);
            if (candidates.Count == 0)
                return; // форс из dev-меню, когда в диапазоне никого: оставляем выбор Core_SK

            var replacement = candidates.RandomElement();
            Log.Message($"[HSKMoreBalanceQuests] {__instance.GetType().Name}: фракция {faction.Name} [{faction.def.techLevel}] вне диапазона техуровня, заменена на {replacement.Name} [{replacement.def.techLevel}]");
            faction = replacement;
        }

        // Критерии ChooseFaction из Core_SK плюс диапазон Ignorance Is Bliss.
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
