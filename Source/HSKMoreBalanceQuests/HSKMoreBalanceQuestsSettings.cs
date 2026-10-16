using Verse;

namespace HSKMoreBalanceQuests;

public class HSKMoreBalanceQuestsSettings : ModSettings
{
    // Модули целиком: квестовые патчи и патчи событий.
    // Значения самих порогов живут в дефах (BalanceSettings.xml, EventSettings.xml).
    public bool enableQuestPatches = true;
    public bool enableEventPatches = true;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref enableQuestPatches, "enableQuestPatches", true);
        Scribe_Values.Look(ref enableEventPatches, "enableEventPatches", true);
        base.ExposeData();
    }
}
