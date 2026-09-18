using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests;

// Soft link to Ignorance Is Bliss (dame.ignorance), copied from HSKMoreHardcore.
// No reference to its DLL: the type is looked up by name once at startup and its
// getters/methods are wrapped in delegates. If IiB is missing or its API changed,
// Active=false and fallbacks are used.
[StaticConstructorOnStartup]
public static class IgnoranceCompat
{
    private static readonly Func<TechLevel> getPlayerTech;
    private static readonly Func<TechLevel, bool> techIsEligible;
    private static readonly Func<Faction, bool> factionIsEligible;

    /// <summary>Ignorance Is Bliss is installed and its API was hooked successfully.</summary>
    public static bool Active { get; }

    static IgnoranceCompat()
    {
        var t = AccessTools.TypeByName("DIgnoranceIsBliss.Core_Patches.IgnoranceBase");
        if (t == null)
            return;

        getPlayerTech = CreateDelegate<Func<TechLevel>>(AccessTools.PropertyGetter(t, "PlayerTechLevel"));
        techIsEligible = CreateDelegate<Func<TechLevel, bool>>(AccessTools.Method(t, "TechIsEligibleForIncident"));
        factionIsEligible = CreateDelegate<Func<Faction, bool>>(AccessTools.Method(t, "FactionInEligibleTechRange"));

        Active = getPlayerTech != null && techIsEligible != null && factionIsEligible != null;
        if (!Active)
            Log.Warning("[HSKMoreBalanceQuests] IgnoranceCompat: Ignorance Is Bliss found but its API changed, link disabled.");
    }

    /// <summary>
    /// Player tech level as calculated by Ignorance Is Bliss. Without IiB it is the
    /// vanilla player faction tech level; outside a game it is Undefined.
    /// </summary>
    public static TechLevel PlayerTechLevel
    {
        get
        {
            if (Current.Game == null)
                return TechLevel.Undefined;
            if (Active)
                return getPlayerTech();
            return Faction.OfPlayer.def.techLevel;
        }
    }

    /// <summary>Would IiB allow an event/faction of this tech level. Always true without IiB.</summary>
    public static bool TechIsEligible(TechLevel tech)
    {
        return !Active || Current.Game == null || techIsEligible(tech);
    }

    /// <summary>
    /// Would IiB allow this faction (respects its Empire/Mechanoid exceptions).
    /// Always true without IiB.
    /// </summary>
    public static bool FactionIsEligible(Faction faction)
    {
        if (faction == null)
            return true;
        return !Active || Current.Game == null || factionIsEligible(faction);
    }

    private static T CreateDelegate<T>(MethodInfo method) where T : Delegate
    {
        if (method == null)
            return null;
        try
        {
            return (T)method.CreateDelegate(typeof(T));
        }
        catch (ArgumentException)
        {
            // signature mismatch: IiB API changed
            return null;
        }
    }
}
