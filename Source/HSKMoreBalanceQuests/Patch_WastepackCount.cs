using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace HSKMoreBalanceQuests;

[HarmonyPatch(typeof(QuestNode_Root_PollutionDump), "RunInt")]
public static class Patch_WastepackCount
{
    private static float originalPoints;

    public static void Prefix()
    {
        if (!HSKMoreBalanceQuestsMod.QuestsEnabled)
            return;

        var settings = BalanceSettingsDef.Values;
        if (!settings.nerfWastepacks)
            return;

        var slate = QuestGen.slate;
        if (!slate.TryGet<float>("points", out float points))
            return;

        originalPoints = points;

        float multiplier = GetMultiplier(settings);
        float newPoints = points * multiplier;
        slate.Set("points", newPoints);

        Log.Message($"[HSKMoreBalanceQuests] Wastepack points: {points:F0} -> {newPoints:F0} (x{multiplier:F3})");
    }

    public static void Postfix()
    {
        if (!HSKMoreBalanceQuestsMod.QuestsEnabled)
            return;

        var settings = BalanceSettingsDef.Values;
        if (!settings.nerfWastepacks)
            return;

        if (originalPoints > 0f)
        {
            QuestGen.slate.Set("points", originalPoints);
            originalPoints = 0f;
        }
    }

    private static float GetMultiplier(BalanceSettingsDef settings)
    {
        float multiplier = settings.wastepackBaseMultiplier;
        var techLevel = Faction.OfPlayer?.def?.techLevel ?? TechLevel.Industrial;
        switch (techLevel)
        {
            case TechLevel.Animal:
            case TechLevel.Neolithic:
                multiplier *= settings.wastepackNeolithicMult;
                break;
            case TechLevel.Medieval:
                multiplier *= settings.wastepackMedievalMult;
                break;
            case TechLevel.Industrial:
                multiplier *= settings.wastepackIndustrialMult;
                break;
        }
        return multiplier;
    }

    /// <summary>
    /// Modifies the private WastepackCountOverPointsCurve to extend below 200 points.
    /// Called once at startup from HSKMoreBalanceQuestsInit.
    /// </summary>
    public static void PatchCurve()
    {
        var field = AccessTools.Field(typeof(QuestNode_Root_PollutionDump), "WastepackCountOverPointsCurve");
        if (field == null)
        {
            Log.Warning("[HSKMoreBalanceQuests] WastepackCountOverPointsCurve field not found");
            return;
        }

        var curve = (SimpleCurve)field.GetValue(null);
        if (curve == null) return;

        // Add a point at 0 -> 0 so the curve scales linearly from 0
        // Original: (200, 90) is the lowest — anything below 200 gives 90
        // With (0, 0): now 26 points -> ~12 wastepacks instead of 90
        bool hasZero = false;
        foreach (var pt in curve)
        {
            if (pt.x <= 0f) { hasZero = true; break; }
        }
        if (!hasZero)
        {
            curve.Add(new CurvePoint(0f, 0f));
            Log.Message("[HSKMoreBalanceQuests] WastepackCountOverPointsCurve patched: added (0, 0) point");
        }
    }
}
