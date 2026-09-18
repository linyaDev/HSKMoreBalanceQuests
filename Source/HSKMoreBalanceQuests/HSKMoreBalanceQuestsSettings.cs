using Verse;

namespace HSKMoreBalanceQuests;

public class HSKMoreBalanceQuestsSettings : ModSettings
{
    public bool limitRefugees = true;
    public int maxRefugees = 4;
    public int maxHelpers = 2;
    public bool nerfWastepacks = true;
    public float wastepackBaseMultiplier = 0.2f;
    public float wastepackNeolithicMult = 0.05f;
    public float wastepackMedievalMult = 0.1f;
    public float wastepackIndustrialMult = 0.5f;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref limitRefugees, "limitRefugees", true);
        Scribe_Values.Look(ref maxRefugees, "maxRefugees", 4);
        Scribe_Values.Look(ref maxHelpers, "maxHelpers", 2);
        Scribe_Values.Look(ref nerfWastepacks, "nerfWastepacks", true);
        Scribe_Values.Look(ref wastepackBaseMultiplier, "wastepackBaseMultiplier", 0.2f);
        Scribe_Values.Look(ref wastepackNeolithicMult, "wastepackNeolithicMult", 0.05f);
        Scribe_Values.Look(ref wastepackMedievalMult, "wastepackMedievalMult", 0.1f);
        Scribe_Values.Look(ref wastepackIndustrialMult, "wastepackIndustrialMult", 0.5f);
        base.ExposeData();
    }
}
