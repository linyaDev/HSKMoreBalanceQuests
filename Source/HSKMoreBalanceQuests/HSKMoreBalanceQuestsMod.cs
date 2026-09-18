using UnityEngine;
using Verse;

namespace HSKMoreBalanceQuests;

public class HSKMoreBalanceQuestsMod : Mod
{
    public static HSKMoreBalanceQuestsSettings Settings;

    public HSKMoreBalanceQuestsMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<HSKMoreBalanceQuestsSettings>();
    }

    // Модуль квестов включён (до загрузки настроек считаем, что да)
    public static bool QuestsEnabled => Settings == null || Settings.enableQuestPatches;

    // Модуль событий включён
    public static bool EventsEnabled => Settings == null || Settings.enableEventPatches;

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var list = new Listing_Standard();
        list.Begin(inRect);

        list.CheckboxLabeled("QP_EnableQuestPatches".Translate(), ref Settings.enableQuestPatches,
            "QP_EnableQuestPatchesTooltip".Translate());
        list.Gap(6f);
        list.CheckboxLabeled("QP_EnableEventPatches".Translate(), ref Settings.enableEventPatches,
            "QP_EnableEventPatchesTooltip".Translate());

        list.Gap(12f);
        GUI.color = new Color(1f, 1f, 1f, 0.6f);
        Text.Font = GameFont.Tiny;
        list.Label("QP_ThresholdsHint".Translate());
        Text.Font = GameFont.Small;
        GUI.color = Color.white;

        list.End();
    }

    public override string SettingsCategory()
    {
        return "QP_SettingsCategory".Translate();
    }
}
