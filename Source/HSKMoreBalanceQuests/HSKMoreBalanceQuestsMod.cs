using UnityEngine;
using Verse;

namespace HSKMoreBalanceQuests;

public class HSKMoreBalanceQuestsMod : Mod
{
    public static HSKMoreBalanceQuestsSettings Settings;
    private Vector2 scrollPos;

    public HSKMoreBalanceQuestsMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<HSKMoreBalanceQuestsSettings>();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Rect scrollArea = new Rect(0f, 0f, inRect.width, inRect.height);
        Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 800f);
        Widgets.BeginScrollView(scrollArea, ref scrollPos, viewRect);

        var list = new Listing_Standard();
        list.ColumnWidth = viewRect.width;
        list.Begin(viewRect);

        // === Quests ===
        SectionHeader(list, "QP_QuestSection".Translate());

        list.CheckboxLabeled("QP_LimitRefugees".Translate(), ref Settings.limitRefugees, "QP_LimitRefugeesTooltip".Translate());
        if (Settings.limitRefugees)
        {
            InputLabeled(list, "QP_MaxRefugees".Translate(), ref Settings.maxRefugees, 1, 10);
            InputLabeled(list, "QP_MaxHelpers".Translate(), ref Settings.maxHelpers, 1, 6);
        }

        // === Wastepacks ===
        list.Gap(4f);
        list.CheckboxLabeled("QP_NerfWastepacks".Translate(), ref Settings.nerfWastepacks, "QP_NerfWastepacksTooltip".Translate());
        if (Settings.nerfWastepacks)
        {
            InputLabeled(list, "QP_WastepackBase".Translate(), ref Settings.wastepackBaseMultiplier, 0.1f, 1.0f, pct: true);
            InputLabeled(list, "QP_WastepackNeolithic".Translate(), ref Settings.wastepackNeolithicMult, 0.1f, 1.0f, pct: true);
            InputLabeled(list, "QP_WastepackMedieval".Translate(), ref Settings.wastepackMedievalMult, 0.1f, 1.0f, pct: true);
        }


        list.End();
        Widgets.EndScrollView();
    }

    private static void SectionHeader(Listing_Standard list, string label)
    {
        list.GapLine();
        GUI.color = new Color(1f, 1f, 0.7f);
        list.Label(label, -1f, (TipSignal?)null);
        GUI.color = Color.white;
        list.Gap(4f);
    }

    private static void InputLabeled(Listing_Standard list, string label, ref float value, float min, float max, bool pct = false)
    {
        Rect row = list.GetRect(24f);
        float labelW = row.width - 60f;
        string suffix = pct ? "%" : "";
        Widgets.Label(new Rect(row.x, row.y, labelW, row.height), label + suffix);

        float displayVal = pct ? value * 100f : value;
        string buf = displayVal.ToString(pct ? "F0" : "F2");
        buf = Widgets.TextField(new Rect(row.xMax - 55f, row.y, 55f, row.height), buf);
        if (float.TryParse(buf, out float parsed))
        {
            float newVal = pct ? parsed / 100f : parsed;
            value = Mathf.Clamp(newVal, min, max);
        }
        list.Gap(2f);
    }

    private static void InputLabeled(Listing_Standard list, string label, ref int value, int min, int max)
    {
        Rect row = list.GetRect(24f);
        float labelW = row.width - 60f;
        Widgets.Label(new Rect(row.x, row.y, labelW, row.height), label);

        string buf = value.ToString();
        buf = Widgets.TextField(new Rect(row.xMax - 55f, row.y, 55f, row.height), buf);
        if (int.TryParse(buf, out int parsed))
            value = Mathf.Clamp(parsed, min, max);
        list.Gap(2f);
    }

    public override string SettingsCategory()
    {
        return "QP_SettingsCategory".Translate();
    }
}
