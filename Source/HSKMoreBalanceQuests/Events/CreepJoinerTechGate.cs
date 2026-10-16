using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests.Events
{
    [StaticConstructorOnStartup]
    public static class CreepJoinerTechGate
    {
        static CreepJoinerTechGate()
        {
            if (!ModsConfig.AnomalyActive)
                return;

            var specifics = AccessTools.Method(typeof(CreepJoinerUtility), nameof(CreepJoinerUtility.GetCreepjoinerSpecifics));
            var spawn = AccessTools.Method(typeof(CreepJoinerUtility), nameof(CreepJoinerUtility.GenerateAndSpawn),
                new[] { typeof(Map), typeof(float) });
            if (specifics == null || spawn == null)
            {
                Log.Warning("[HSKMoreBalanceQuests] CreepJoinerTechGate: API CreepJoinerUtility изменилось — варианты не ограничиваются.");
                return;
            }

            var harmony = new Harmony("linya.hskmorebalancequests.creepjoinertechgate");
            harmony.Patch(specifics,
                prefix: new HarmonyMethod(typeof(CreepJoinerTechGate), nameof(GetCreepjoinerSpecificsPrefix)));
            harmony.Patch(spawn,
                prefix: new HarmonyMethod(typeof(CreepJoinerTechGate), nameof(GenerateAndSpawnPrefix)));
        }

        public static void GetCreepjoinerSpecificsPrefix(ref CreepJoinerFormKindDef form)
        {
            if (!HSKMoreBalanceQuestsMod.EventsEnabled)
                return;

            if (form == null && TryGetAllowedForm(out var allowed))
                form = allowed;
        }

        public static bool GenerateAndSpawnPrefix(Map map, float combatPoints, ref Pawn __result)
        {
            if (!HSKMoreBalanceQuestsMod.EventsEnabled)
                return true;

            if (!TryGetAllowedForm(out var form))
                return true;

            var requires = new List<CreepJoinerBaseDef>(form.Requires);
            var exclude = new List<CreepJoinerBaseDef>(form.Excludes);
            var benefit = CreepJoinerUtility.GetRandom(DefDatabase<CreepJoinerBenefitDef>.AllDefsListForReading, combatPoints, requires, exclude);
            var downside = CreepJoinerUtility.GetRandom(DefDatabase<CreepJoinerDownsideDef>.AllDefsListForReading, combatPoints, requires, exclude);
            var aggressive = CreepJoinerUtility.GetRandom(DefDatabase<CreepJoinerAggressiveDef>.AllDefsListForReading, combatPoints, requires, exclude);
            var rejection = CreepJoinerUtility.GetRandom(DefDatabase<CreepJoinerRejectionDef>.AllDefsListForReading, combatPoints, requires, exclude);
            __result = CreepJoinerUtility.GenerateAndSpawn(form, benefit, downside, aggressive, rejection, map);
            return false;
        }

        private static bool TryGetAllowedForm(out CreepJoinerFormKindDef form)
        {
            form = null;
            var gates = EventSettingsDef.CreepJoinerGates;
            if (gates == null || gates.Count == 0 || Current.Game == null)
                return false;

            var all = DefDatabase<CreepJoinerFormKindDef>.AllDefsListForReading;
            TechLevel playerTech = IgnoranceCompat.PlayerTechLevel;
            var allowed = all
                .Where(f => !gates.TryGetValue(f.defName, out TechLevel minTech) || playerTech >= minTech)
                .ToList();
            if (allowed.Count == all.Count)
                return false;

            return allowed.TryRandomElementByWeight(f => f.Weight, out form);
        }
    }
}
