using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests;

[HarmonyPatch(typeof(RewardsGenerator), nameof(RewardsGenerator.Generate),
    new[] { typeof(RewardsGeneratorParams), typeof(float) },
    new[] { ArgumentType.Normal, ArgumentType.Out })]
public static class Patch_QuestReward
{
    private static int lastLogTick = -1;

    public static void Prefix(ref RewardsGeneratorParams parms)
    {
        var settings = HSKMoreBalanceQuestsMod.Settings;
        if (settings == null)
            return;

        float original = parms.rewardValue;
        parms.rewardValue *= settings.rewardMultiplier;

        int tick = Find.TickManager?.TicksGame ?? -1;
        if (tick != lastLogTick)
        {
            lastLogTick = tick;
            string msg = $"Reward reduced: {original:F0} -> {parms.rewardValue:F0} (x{settings.rewardMultiplier})";
            Log.Message($"[HSKMoreBalanceQuests] {msg}");
            QuestDebugLog.LogDebug(msg);
        }
    }
}
