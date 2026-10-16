using RimWorld;
using Verse;

namespace HSKMoreBalanceQuests;

public class ThoughtWorker_Mercy : ThoughtWorker
{
    public override ThoughtState CurrentStateInternal(Pawn p)
    {
        var need = p.needs?.TryGetNeed<Need_Mercy>();
        if (need == null || !need.ShowOnNeedList)
            return ThoughtState.Inactive;

        return ThoughtState.ActiveAtStage(Dialog_MercyInfo.GetStageIndex(need.CurLevel));
    }
}
