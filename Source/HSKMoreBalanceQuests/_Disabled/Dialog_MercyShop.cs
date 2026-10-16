using RimWorld;
using UnityEngine;
using Verse;

namespace HSKMoreBalanceQuests;

public class Dialog_MercyShop : Window
{
    private readonly GameComponent_QuestPressure comp;
    private readonly Rect parentRect;

    private static readonly Color GreenText = new Color(0.4f, 0.95f, 0.4f);
    private static readonly Color DimText = new Color(1f, 1f, 1f, 0.5f);
    private static readonly Color RowBg = new Color(0.2f, 0.2f, 0.2f, 0.3f);

    private struct ShopItem
    {
        public string defName;
        public int count;
        public int cost;
        public string labelKey;
    }

    private static ShopItem[] GetShopItems()
    {
        var techLevel = Faction.OfPlayer?.def?.techLevel ?? TechLevel.Neolithic;
        string medDef;
        if (techLevel >= TechLevel.Industrial)
            medDef = "MedicineIndustrial";
        else if (techLevel >= TechLevel.Medieval)
            medDef = "Herbmedicine";
        else
            medDef = "HerbMedicine";
        return new[]
        {
            new ShopItem { defName = "Pemmican", count = 300, cost = 5, labelKey = "QP_ShopPemmican" },
            new ShopItem { defName = medDef, count = 3, cost = 5, labelKey = "QP_ShopMedicine" },
        };
    }

    public override Vector2 InitialSize => new Vector2(420f, 300f);

    public Dialog_MercyShop(GameComponent_QuestPressure comp, Rect parentRect)
    {
        this.comp = comp;
        this.parentRect = parentRect;
        doCloseButton = true;
        doCloseX = true;
        draggable = true;
        absorbInputAroundWindow = false;
    }

    public override void SetInitialSizeAndPosition()
    {
        base.SetInitialSizeAndPosition();
        float x = parentRect.xMax + 10f;
        float y = parentRect.y;
        if (x + windowRect.width > UI.screenWidth)
            x = parentRect.x - windowRect.width - 10f;
        if (y + windowRect.height > UI.screenHeight)
            y = UI.screenHeight - windowRect.height;
        windowRect.x = x;
        windowRect.y = y;
    }

    public override void DoWindowContents(Rect inRect)
    {
        // Title
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "QP_ShopTitle".Translate());
        Text.Font = GameFont.Small;

        // Current score
        float y = 38f;
        GUI.color = comp.Score >= 0 ? GreenText : new Color(0.95f, 0.4f, 0.4f);
        Widgets.Label(new Rect(0f, y, inRect.width, 22f), "QP_ShopBalance".Translate(comp.Score));
        GUI.color = Color.white;
        y += 28f;

        // Separator
        GUI.color = new Color(1f, 1f, 1f, 0.2f);
        Widgets.DrawLineHorizontal(0f, y, inRect.width);
        GUI.color = Color.white;
        y += 6f;

        // Items list
        var items = GetShopItems();
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            bool canAfford = comp.Score >= item.cost;
            Rect rowRect = new Rect(0f, y, inRect.width, 36f);

            if (i % 2 == 0)
                Widgets.DrawBoxSolid(rowRect, RowBg);

            if (Mouse.IsOver(rowRect))
                Widgets.DrawHighlight(rowRect);

            // Icon
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(item.defName);
            if (def != null)
                Widgets.ThingIcon(new Rect(4f, y + 2f, 32f, 32f), def);

            // Name + count
            Widgets.Label(new Rect(42f, y, inRect.width - 180f, 36f), item.labelKey.Translate(item.count, item.cost));

            // Cost
            GUI.color = canAfford ? GreenText : DimText;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(inRect.width - 130f, y, 50f, 36f), item.cost.ToString() + " " + "QP_ShopPts".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // Buy button
            GUI.color = canAfford ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            if (Widgets.ButtonText(new Rect(inRect.width - 75f, y + 4f, 70f, 28f), "QP_ShopBuy".Translate()) && canAfford)
            {
                SpendMercy(item);
            }
            GUI.color = Color.white;

            y += 38f;
        }

    }

    private void SpendMercy(ShopItem item)
    {
        comp.RecordQuest("QP_ShopPurchase".Translate(), 0, QuestRecordType.ShopPurchase, customWeight: -item.cost);

        var map = Find.CurrentMap;
        if (map == null) return;

        ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(item.defName);
        if (def == null) return;

        Thing thing = ThingMaker.MakeThing(def);
        thing.stackCount = item.count;

        IntVec3 dropCell = DropCellFinder.TradeDropSpot(map);
        DropPodUtility.DropThingsNear(dropCell, map, new[] { thing }, canRoofPunch: false, forbid: false);

        Messages.Message("QP_ShopDelivery".Translate(thing.LabelCap, item.count), new TargetInfo(dropCell, map), MessageTypeDefOf.PositiveEvent);
    }
}
