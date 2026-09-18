using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests;

// Quest item rewards (Reward_Items -> Reward_ItemsStandard) only offer weapons a fixed number of
// tech level above the player. Player tech comes from Ignorance Is Bliss when installed.
// The validator is chained onto the params passed to the root thing set maker: ApplyFixedParams
// only fills null fields, and XML can't set a validator, so it reaches every nested maker
// (including HSK reward sets). Options with no allowed things fail CanGenerate and another
// option is picked. Non-weapon items and weapons without a tech level are not filtered.
[HarmonyPatch(typeof(ThingSetMaker), nameof(ThingSetMaker.Generate), typeof(ThingSetMakerParams))]
public static class Patch_RewardWeaponTech
{
    public const int TechOffset = 1;

    public static void Prefix(ThingSetMaker __instance, ref ThingSetMakerParams parms)
    {
        if (__instance != ThingSetMakerDefOf.Reward_ItemsStandard?.root)
            return;

        TechLevel playerTech = IgnoranceCompat.PlayerTechLevel;
        if (playerTech == TechLevel.Undefined)
            return;

        TechLevel weaponTech = playerTech + TechOffset;
        Predicate<ThingDef> previous = parms.validator;
        parms.validator = def => (previous == null || previous(def)) && WeaponAllowed(def, weaponTech);
    }

    private static bool WeaponAllowed(ThingDef def, TechLevel weaponTech)
    {
        if (def == null || !def.IsWeapon || def.techLevel == TechLevel.Undefined)
            return true;
        return def.techLevel == weaponTech;
    }
}
