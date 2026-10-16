using HarmonyLib;
using Verse;

namespace HSKMoreBalanceQuests;

[StaticConstructorOnStartup]
public static class HSKMoreBalanceQuestsInit
{
    static HSKMoreBalanceQuestsInit()
    {
        var harmony = new Harmony("linya.hskmorebalancequests");
        harmony.PatchAll();
        // Кривая мешков правится один раз при старте — галка модуля квестов
        // читается здесь, её переключение требует перезапуска игры
        if (HSKMoreBalanceQuestsMod.QuestsEnabled)
            Patch_WastepackCount.PatchCurve();
        Log.Message("[HSKMoreBalanceQuests] Patches applied.");
    }
}
