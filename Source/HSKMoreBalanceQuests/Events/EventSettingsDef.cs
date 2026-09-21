using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests.Events
{
    public class EventSettingsDef : Def
    {
        public Dictionary<string, TechLevel> incidentMinTechLevel;
        public Dictionary<string, TechLevel> creepJoinerFormMinTechLevel;
        public int guestMaxTechAhead = -1;
        public int guestMaxTechBehind = -1;

        private static EventSettingsDef cachedInstance;

        public static EventSettingsDef Instance
        {
            get
            {
                if (cachedInstance == null)
                    cachedInstance = DefDatabase<EventSettingsDef>.GetNamedSilentFail("HSKMoreBalanceQuests_EventSettings");
                return cachedInstance;
            }
        }

        public static readonly Dictionary<string, TechLevel> defaultIncidentMinTechLevel = new Dictionary<string, TechLevel>
        {
            { "LongNight", TechLevel.Industrial },
            { "IceAge", TechLevel.Industrial },
            { "MechanoidTerraformerIncident", TechLevel.Spacer },
            { "DefoliatorShipPartCrash", TechLevel.Spacer },
            { "PsychicEmanatorShipPartCrash", TechLevel.Spacer },
        };

        public static readonly Dictionary<string, TechLevel> defaultCreepJoinerFormMinTechLevel = new Dictionary<string, TechLevel>
        {
            { "LoneGenius", TechLevel.Industrial },
            { "LeatheryStranger", TechLevel.Medieval },
        };

        public const int defaultGuestMaxTechAhead = 0;
        public const int defaultGuestMaxTechBehind = 2;

        public static Dictionary<string, TechLevel> IncidentGates =>
            Instance?.incidentMinTechLevel ?? defaultIncidentMinTechLevel;

        public static Dictionary<string, TechLevel> CreepJoinerGates =>
            Instance?.creepJoinerFormMinTechLevel ?? defaultCreepJoinerFormMinTechLevel;

        public static int GuestMaxTechAhead =>
            Instance?.guestMaxTechAhead ?? defaultGuestMaxTechAhead;

        public static int GuestMaxTechBehind =>
            Instance?.guestMaxTechBehind ?? defaultGuestMaxTechBehind;
    }
}
