using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests.Events
{
    // Временная диагностика: почему гость Hospitality не покупает вещь.
    // Логирует по одной строке на пару «гость + предмет» за сессию, только для оружия.
    // Выключается константой DebugLog.
    [StaticConstructorOnStartup]
    public static class GuestBuyDebug
    {
        private const bool DebugLog = true;

        private static MethodInfo getMoney;
        private static MethodInfo getPurchasingCost;
        private static MethodInfo getInventorySpaceFor;
        private static MethodInfo isBuyableNow;

        private static readonly HashSet<long> logged = new HashSet<long>();

        static GuestBuyDebug()
        {
            if (!DebugLog)
                return;

            var itemUtility = AccessTools.TypeByName("Hospitality.Utilities.ItemUtility");
            var joyGiver = AccessTools.TypeByName("Hospitality.JoyGiver_BuyStuff");
            if (itemUtility == null || joyGiver == null)
                return;

            getMoney = AccessTools.Method(AccessTools.TypeByName("Hospitality.Utilities.GuestUtility"), "GetMoney");
            getPurchasingCost = AccessTools.Method(itemUtility, "GetPurchasingCost");
            getInventorySpaceFor = AccessTools.Method(itemUtility, "GetInventorySpaceFor");
            isBuyableNow = AccessTools.Method(itemUtility, "IsBuyableNow");

            var likey = AccessTools.Method(joyGiver, "Likey");
            var buyableAtAll = AccessTools.Method(itemUtility, "IsBuyableAtAll");

            var harmony = new Harmony("linya.hskmorebalancequests.guestbuydebug");
            if (likey != null)
                harmony.Patch(likey, postfix: new HarmonyMethod(typeof(GuestBuyDebug), nameof(LikeyPostfix)));
            else
                Log.Warning("[HSKMoreBalanceQuests] GuestBuyDebug: JoyGiver_BuyStuff.Likey не найден.");

            if (buyableAtAll != null)
                harmony.Patch(buyableAtAll, postfix: new HarmonyMethod(typeof(GuestBuyDebug), nameof(IsBuyableAtAllPostfix)));
            else
                Log.Warning("[HSKMoreBalanceQuests] GuestBuyDebug: ItemUtility.IsBuyableAtAll не найден.");

            Log.Message("[HSKMoreBalanceQuests] GuestBuyDebug applied.");
        }

        // Порог покупки в Hospitality: оценка > 0.5, иначе гость только разглядывает вещь
        public static void LikeyPostfix(Pawn pawn, Thing thing, float __result)
        {
            if (pawn == null || thing?.def == null || !thing.def.IsWeapon)
                return;
            if (!ShouldLog(pawn, thing, "likey"))
                return;

            float hp = thing.def.useHitPoints && thing.MaxHitPoints > 0
                ? (float)thing.HitPoints / thing.MaxHitPoints
                : 1f;
            string quality = thing.TryGetQuality(out var qc) ? qc.ToString() : "нет";
            float qualityFactor = thing.TryGetQuality(out var qc2) ? ((int)qc2 - 2f) / 3f + 1f : 0.7f;
            float techFactor = thing.def.techLevel != TechLevel.Undefined
                ? ((int)thing.def.techLevel - (int)pawn.Faction.def.techLevel) / 5f + 1f
                : 0.8f;

            Log.Message($"[HSKMoreBalanceQuests] BuyDebug ОЦЕНКА {thing.LabelCap} гостем {pawn.LabelShort} " +
                $"({pawn.Faction?.Name}, тех {pawn.Faction?.def?.techLevel}): итог {__result:0.###} " +
                $"(порог 0.5, {(__result > 0.5f ? "КУПИТ" : "только посмотрит")}); " +
                $"целостность {hp:P0} -> x{hp * hp:0.###}, качество {quality} -> x{qualityFactor * qualityFactor:0.###}, " +
                $"тех вещи {thing.def.techLevel} -> x{techFactor:0.###}");
        }

        public static void IsBuyableAtAllPostfix(Pawn pawn, Thing thing, bool __result)
        {
            if (__result || pawn == null || thing?.def == null || !thing.def.IsWeapon)
                return;
            if (!ShouldLog(pawn, thing, "buyable"))
                return;

            Log.Message($"[HSKMoreBalanceQuests] BuyDebug ОТСЕВ {thing.LabelCap} у гостя {pawn.LabelShort}: " +
                $"продаваемость {thing.def.tradeability}, цена {Invoke(getPurchasingCost, thing)}, " +
                $"серебро гостя {Invoke(getMoney, pawn)}, место в инвентаре {Invoke(getInventorySpaceFor, pawn, thing)}, " +
                $"доступна сейчас {Invoke(isBuyableNow, pawn, thing)}, запрещена {thing.IsForbidden(Faction.OfPlayer)}, " +
                $"зарезервирована {pawn.Map != null && !pawn.Map.reservationManager.CanReserve(pawn, thing)}, тег NotForGuests " +
                $"{(thing.def.thingSetMakerTags?.Contains("NotForGuests") ?? false)}");
        }

        private static object Invoke(MethodInfo method, params object[] args)
        {
            if (method == null)
                return "?";
            try
            {
                return method.Invoke(null, args) ?? "null";
            }
            catch (Exception e)
            {
                return "ошибка: " + e.InnerException?.Message;
            }
        }

        private static bool ShouldLog(Pawn pawn, Thing thing, string tag)
        {
            long key = (long)pawn.thingIDNumber * 1000003L + thing.thingIDNumber * 7L + tag.GetHashCode() % 7;
            return logged.Add(key);
        }
    }
}
