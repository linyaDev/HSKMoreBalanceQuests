using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests;

// Значения патчей квестов, задаются в Defs/Misc/BalanceSettings.xml.
// Если дефа нет, каждый патч берёт свои запасные значения из кода.
public class BalanceSettingsDef : Def
{
    // Квест (defName) -> минимальный техуровень игрока
    public Dictionary<string, TechLevel> questMinTechLevel;
    // Часть площадки (SitePartDef defName) -> минимальный техуровень игрока
    public Dictionary<string, TechLevel> sitePartMinTechLevel;
    // Минимальный техуровень для мех-кластера из квеста (угроза за монумент и т.п.)
    public TechLevel mechClusterMinTechLevel = TechLevel.Industrial;

    private static BalanceSettingsDef cachedInstance;

    public static BalanceSettingsDef Instance
    {
        get
        {
            if (cachedInstance == null)
                cachedInstance = DefDatabase<BalanceSettingsDef>.GetNamedSilentFail("HSKMoreBalanceQuests_Settings");
            return cachedInstance;
        }
    }
}
