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

    // Ограничение числа постояльцев
    public bool limitRefugees = true;
    public int maxRefugees = 4;
    public int maxHelpers = 2;

    // Мешки отходов в квесте на свалку
    public bool nerfWastepacks = true;
    public float wastepackBaseMultiplier = 0.2f;
    public float wastepackNeolithicMult = 0.05f;
    public float wastepackMedievalMult = 0.1f;
    public float wastepackIndustrialMult = 0.5f;

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

    // Запасные значения на случай отсутствия дефа
    private static readonly BalanceSettingsDef fallback = new BalanceSettingsDef();

    public static BalanceSettingsDef Values => Instance ?? fallback;
}
