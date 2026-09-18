using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace HSKMoreBalanceQuests;

public class Dialog_MercyInfo : Window
{
    private readonly Pawn pawn;
    private Vector2 scrollPosition;
    private HashSet<int> selectedIndices = new HashSet<int>();
    private bool editMode;

    private static readonly Dictionary<string, string> questNameOverrides = new Dictionary<string, string>
    {
        { "WandererJoinAbasia", "QP_QuestName_WandererJoinAbasia" },
        { "CreepJoinerArrival", "QP_QuestName_CreepJoinerArrival" },
    };

    private static string PrettyQuestName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unknown";
        // Check exact match first
        if (questNameOverrides.TryGetValue(name, out string key))
            return key.Translate();
        // Check if name contains a known key
        foreach (var kv in questNameOverrides)
        {
            if (name.Contains(kv.Key))
                return kv.Value.Translate();
        }
        return name;
    }

    private static readonly Color GreenBg = new Color(0.2f, 0.5f, 0.2f, 0.3f);
    private static readonly Color RedBg = new Color(0.5f, 0.2f, 0.2f, 0.3f);
    private static readonly Color SelectedBg = new Color(0.3f, 0.3f, 0.8f, 0.4f);
    private static readonly Color GreenText = new Color(0.4f, 0.95f, 0.4f);
    private static readonly Color RedText = new Color(0.95f, 0.4f, 0.4f);
    private static readonly Color DimText = new Color(1f, 1f, 1f, 0.5f);

    public override Vector2 InitialSize => new Vector2(460f, 520f);

    public Dialog_MercyInfo(Pawn pawn)
    {
        this.pawn = pawn;
        doCloseButton = true;
        doCloseX = true;
        draggable = true;
        absorbInputAroundWindow = false;
    }

    public override void DoWindowContents(Rect inRect)
    {
        var comp = Current.Game?.GetComponent<GameComponent_QuestPressure>();
        if (comp == null)
            return;

        // Title
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "QP_MercyHistory".Translate());
        Text.Font = GameFont.Small;

        // Debug button (temporarily always visible)
        if (true) // was: Prefs.DevMode
        {
            Rect debugRect = new Rect(inRect.width - 50f, 4f, 40f, 24f);
            GUI.color = Mouse.IsOver(debugRect) ? Color.yellow : new Color(1f, 1f, 0.5f, 0.6f);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(debugRect, "DBG");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            if (Widgets.ButtonInvisible(debugRect))
            {
                if (!Find.WindowStack.TryRemove(typeof(Dialog_Debug), true))
                    Find.WindowStack.Add(new Dialog_Debug(windowRect));
            }
        }

        float y = 40f;

        // Stats summary
        int gained = 0, lost = 0;
        foreach (var r in comp.Records)
        {
            int w = comp.GetPoints(r);
            if (w > 0) gained += w;
            else lost += w;
        }

        // Score bar area
        Rect statsRect = new Rect(0f, y, inRect.width, 50f);
        Widgets.DrawBoxSolid(statsRect, new Color(0.15f, 0.15f, 0.15f, 0.8f));

        float thirdW = inRect.width / 3f;
        Text.Anchor = TextAnchor.MiddleCenter;

        // Gained
        GUI.color = GreenText;
        Widgets.Label(new Rect(0f, y + 2f, thirdW, 22f), "QP_Gained".Translate());
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, y + 22f, thirdW, 26f), "+" + gained);
        Text.Font = GameFont.Small;

        // Score
        GUI.color = comp.Score >= 0 ? GreenText : RedText;
        Widgets.Label(new Rect(thirdW, y + 2f, thirdW, 22f), "QP_Score".Translate());
        Text.Font = GameFont.Medium;
        string scoreStr = comp.Score >= 0 ? "+" + comp.Score : comp.Score.ToString();
        Widgets.Label(new Rect(thirdW, y + 22f, thirdW, 26f), scoreStr);
        Text.Font = GameFont.Small;

        // Lost
        GUI.color = RedText;
        Widgets.Label(new Rect(thirdW * 2f, y + 2f, thirdW, 22f), "QP_Lost".Translate());
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(thirdW * 2f, y + 22f, thirdW, 26f), lost.ToString());
        Text.Font = GameFont.Small;

        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        y += 56f;

        // === Progress bar with colored stages ===
        y = DrawProgressBar(inRect, y, comp.Score);

        // === Shop ===
        y = DrawShop(inRect, y, comp);

        // Separator
        GUI.color = new Color(1f, 1f, 1f, 0.2f);
        Widgets.DrawLineHorizontal(0f, y, inRect.width);
        GUI.color = Color.white;
        y += 4f;

        // Quest list
        var records = comp.Records;
        float listHeight = records.Count * 30f;
        Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, listHeight);
        Rect outRect = new Rect(0f, y, inRect.width, inRect.height - y - 50f);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

        float rowY = 0f;
        for (int i = records.Count - 1; i >= 0; i--)
        {
            var r = records[i];
            Rect rowRect = new Rect(0f, rowY, viewRect.width, 28f);

            int points = comp.GetPoints(r);
            bool isPositive = points > 0;
            string pointsStr = isPositive ? "+" + points : points.ToString();

            // Row background
            bool isSelected = selectedIndices.Contains(i);
            if (isSelected)
                Widgets.DrawBoxSolid(rowRect, SelectedBg);
            else
                Widgets.DrawBoxSolid(rowRect, isPositive ? GreenBg : RedBg);

            // Edit mode: click to select/deselect
            if (editMode && Widgets.ButtonInvisible(rowRect))
            {
                if (isSelected)
                    selectedIndices.Remove(i);
                else
                    selectedIndices.Add(i);
            }
            if (i % 2 == 0)
                Widgets.DrawLightHighlight(rowRect);

            // Hover highlight + click to open quest
            if (Mouse.IsOver(rowRect))
            {
                Widgets.DrawHighlight(rowRect);
                if (r.questId >= 0 && Widgets.ButtonInvisible(rowRect))
                {
                    Quest quest = Find.QuestManager.QuestsListForReading.Find(q => q.id == r.questId);
                    if (quest != null)
                    {
                        Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Quests);
                        ((MainTabWindow_Quests)MainButtonDefOf.Quests.TabWindow).Select(quest);
                        Close();
                    }
                }
            }

            // Icon
            GUI.color = isPositive ? GreenText : RedText;
            string icon = isPositive ? "▲" : "▼";
            Widgets.Label(new Rect(5f, rowY, 20f, 28f), icon);

            // Quest name (truncate to 1 line)
            GUI.color = Color.white;
            float nameWidth = viewRect.width - 170f;
            string questName = PrettyQuestName(r.questName);
            string displayName = questName.Length > 25 ? questName.Substring(0, 22) + "..." : questName;
            Widgets.Label(new Rect(25f, rowY, nameWidth, 28f), displayName);
            Rect nameRect = new Rect(25f, rowY, nameWidth, 28f);
            if (Mouse.IsOver(nameRect))
            {
                string tip = questName.Length > 25 ? questName : null;
                // Show days remaining for tracked rescued pawns
                if (r.customWeight == 0 && r.type == QuestRecordType.MinorBonus && r.questId > 0)
                {
                    int daysLeft = 30 - (Find.TickManager.TicksGame - r.tick) / 60000;
                    if (daysLeft > 0)
                        tip = (tip != null ? tip + "\n" : "") + "QP_HealDaysLeft".Translate(daysLeft);
                }
                if (tip != null)
                    TooltipHandler.TipRegion(nameRect, tip);
            }

            // Points
            GUI.color = isPositive ? GreenText : RedText;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(viewRect.width - 140f, rowY, 50f, 28f), pointsStr);

            // Time ago
            GUI.color = DimText;
            Text.Anchor = TextAnchor.MiddleRight;
            int daysAgo = (Find.TickManager.TicksGame - r.tick) / 60000;
            string timeStr = daysAgo <= 0 ? "QP_Today".Translate().ToString() : "QP_DaysAgo".Translate(daysAgo);
            Text.WordWrap = false;
            Widgets.Label(new Rect(viewRect.width - 95f, rowY, 90f, 28f), timeStr);
            Text.WordWrap = true;

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            rowY += 30f;
        }

        Widgets.EndScrollView();
    }

    private static int[] _stageMoods;
    private static int[] StageMoods
    {
        get
        {
            if (_stageMoods == null)
            {
                var def = DefDatabase<ThoughtDef>.GetNamedSilentFail("MercyThought");
                if (def?.stages != null)
                    _stageMoods = def.stages.ConvertAll(s => (int)s.baseMoodEffect).ToArray();
                else
                    _stageMoods = new[] { -10, -8, -6, -4, -2, 0, 1, 2, 3, 4 };
            }
            return _stageMoods;
        }
    }
    public static readonly float[] StageThresholds = { 0.10f, 0.20f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f, 1.0f };

    public static int GetStageIndex(float level)
    {
        for (int i = 0; i < StageThresholds.Length - 1; i++)
        {
            // Stage 4 (cold): use < so 0.50 exact maps to neutral (stage 5)
            if (i == 4 ? level < StageThresholds[i] : level <= StageThresholds[i])
                return i;
        }
        return StageThresholds.Length - 1;
    }
    private static readonly Color[] StageColors =
    {
        new Color(0.7f, 0.1f, 0.1f),   // -20 dark red
        new Color(0.85f, 0.2f, 0.2f),  // -15 red
        new Color(0.95f, 0.3f, 0.2f),  // -10 orange-red
        new Color(0.95f, 0.5f, 0.2f),  // -5  orange
        new Color(0.95f, 0.75f, 0.2f), // -2  yellow
        new Color(0.7f, 0.7f, 0.7f),   // 0   gray
        new Color(0.4f, 0.75f, 0.3f),  // +2  light green
        new Color(0.3f, 0.85f, 0.3f),  // +4  green
        new Color(0.2f, 0.9f, 0.4f),   // +6  bright green
        new Color(0.1f, 0.95f, 0.5f),  // +8  vivid green
    };
    private static readonly string[] StageLabels =
    {
        "QP_Stage0", "QP_Stage1", "QP_Stage2", "QP_Stage3", "QP_Stage4",
        "QP_Stage5", "QP_Stage6", "QP_Stage7", "QP_Stage8", "QP_Stage9"
    };

    private float DrawProgressBar(Rect inRect, float y, int score)
    {
        float ScoreMax = GameComponent_QuestPressure.ScoreMax;
        float ScoreMin = -ScoreMax;
        float barHeight = 18f;
        float barX = 20f;
        float barWidth = inRect.width - 40f;

        // Background
        Rect barBg = new Rect(barX, y, barWidth, barHeight);
        Widgets.DrawBoxSolid(barBg, new Color(0.1f, 0.1f, 0.1f, 0.8f));

        // Draw colored segments
        float prevX = 0f;
        for (int i = 0; i < StageThresholds.Length; i++)
        {
            float segEnd = StageThresholds[i] * barWidth;
            Rect segRect = new Rect(barX + prevX, y, segEnd - prevX, barHeight);
            Widgets.DrawBoxSolid(segRect, StageColors[i]);

            // Segment border
            GUI.color = new Color(0f, 0f, 0f, 0.3f);
            Widgets.DrawBox(segRect, 1);
            GUI.color = Color.white;

            prevX = segEnd;
        }

        // Current position marker
        float normalizedScore = Mathf.InverseLerp(ScoreMin, ScoreMax, score);
        float markerX = barX + normalizedScore * barWidth;
        Rect markerRect = new Rect(markerX - 2f, y - 2f, 4f, barHeight + 4f);
        Widgets.DrawBoxSolid(markerRect, Color.white);

        // Tooltips on segments
        prevX = 0f;
        for (int i = 0; i < StageThresholds.Length; i++)
        {
            float segEnd = StageThresholds[i] * barWidth;
            Rect segRect = new Rect(barX + prevX, y, segEnd - prevX, barHeight);
            if (Mouse.IsOver(segRect))
            {
                string moodVal = StageMoods[i] >= 0 ? "+" + StageMoods[i] : StageMoods[i].ToString();
                string stageLabel = StageLabels[i].Translate();
                TooltipHandler.TipRegion(segRect, stageLabel + " (" + moodVal + ")");
            }
            prevX = segEnd;
        }

        y += barHeight + 4f;

        // Current stage info
        int currentStage = GetCurrentStage(normalizedScore);
        string stageName = StageLabels[currentStage].Translate();
        int nextScore = GetScoreForNextStage(currentStage, score);

        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = StageColors[currentStage];
        string infoStr = stageName + " (" + StageMoods[currentStage] + ")";
        if (nextScore > 0 && currentStage < StageMoods.Length - 1)
            infoStr += "  →  " + "QP_NextLevel".Translate(nextScore);
        Widgets.Label(new Rect(0f, y, inRect.width, 20f), infoStr);
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        y += 24f;

        return y;
    }

    private int GetCurrentStage(float normalizedScore)
    {
        return GetStageIndex(normalizedScore);
    }

    private int GetScoreForNextStage(int currentStage, int score)
    {
        if (currentStage >= StageMoods.Length - 1) return 0;
        float nextThreshold = StageThresholds[currentStage];
        float max = GameComponent_QuestPressure.ScoreMax;
        int nextScore = (int)(nextThreshold * max * 2f - max) - score + 1;
        return Mathf.Max(0, nextScore);
    }

    private float DrawShop(Rect inRect, float y, GameComponent_QuestPressure comp)
    {
        if (Widgets.ButtonText(new Rect(0f, y, inRect.width, 30f), "QP_ShopButton".Translate()))
        {
            Find.WindowStack.Add(new Dialog_MercyShop(comp, windowRect));
        }
        y += 36f;
        return y;
    }
}
