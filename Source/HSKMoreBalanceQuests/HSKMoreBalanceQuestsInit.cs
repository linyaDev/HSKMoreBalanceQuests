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
        Patch_WastepackCount.PatchCurve();
        Log.Message("[HSKMoreBalanceQuests] Patches applied.");
    }
}
