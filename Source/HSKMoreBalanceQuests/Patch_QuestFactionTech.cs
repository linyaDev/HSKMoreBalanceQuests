using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace HSKMoreBalanceQuests;

// Common faction filter for XML quests: every QuestNode_GetFaction pick (quest raid enemies,
// hospitality attackers, site threat owners, peace talks) must pass Ignorance Is Bliss's
// faction tech range. IiB itself doesn't check this node, so quest raids ignored it.
// With IiB defaults (MechanoidsAreAlwaysEligible = false) this also keeps mechanoids away
// from low-tech colonies. Factions dictated by a pawn (ofPawn) are never filtered.
[HarmonyPatch(typeof(QuestNode_GetFaction), "IsGoodFaction")]
public static class Patch_QuestFactionTech
{
    public static bool bypassGate;

    public static void Postfix(QuestNode_GetFaction __instance, Faction faction, Slate slate, ref bool __result)
    {
        if (bypassGate || !HSKMoreBalanceQuestsMod.QuestsEnabled)
            return;

        if (!__result || faction == null || faction.IsPlayer)
            return;

        if (__instance.ofPawn.GetValue(slate) != null)
            return;

        if (IgnoranceCompat.FactionIsEligible(faction))
            return;

        __result = false;
    }
}

// TEMPORARY diagnostics: on every real QuestNode_GetFaction run, log which factions passed the
// vanilla filter, which of those our tech gate cut, and which one was picked.
[HarmonyPatch(typeof(QuestNode_GetFaction), "RunInt")]
public static class Patch_QuestFactionPickLog
{
    private static readonly System.Reflection.MethodInfo isGoodFaction =
        AccessTools.Method(typeof(QuestNode_GetFaction), "IsGoodFaction");

    public static void Postfix(QuestNode_GetFaction __instance)
    {
        if (!HSKMoreBalanceQuestsMod.QuestsEnabled || isGoodFaction == null)
            return;

        var slate = QuestGen.slate;
        if (slate == null || __instance.ofPawn.GetValue(slate) != null)
            return;

        slate.TryGet<Faction>(__instance.storeAs.GetValue(slate), out Faction picked);

        var passed = new System.Collections.Generic.List<string>();
        var cutByTech = new System.Collections.Generic.List<string>();
        var rejected = new System.Collections.Generic.List<string>();
        foreach (var f in Find.FactionManager.GetFactions(allowHidden: true))
        {
            Patch_QuestFactionTech.bypassGate = true;
            bool vanillaOk;
            try { vanillaOk = (bool)isGoodFaction.Invoke(__instance, new object[] { f, slate }); }
            finally { Patch_QuestFactionTech.bypassGate = false; }

            string label = $"{f.Name} [{f.def.defName}, {f.def.techLevel}, {f.PlayerRelationKind}, permEnemy={f.def.permanentEnemy}]";
            if (!vanillaOk)
            {
                rejected.Add(label);
                continue;
            }

            if (IgnoranceCompat.FactionIsEligible(f))
                passed.Add(label);
            else
                cutByTech.Add(label);
        }

        Log.Message($"[HSKMoreBalanceQuests] FactionPick root={QuestGen.Root?.defName} node={__instance.storeAs.GetValue(slate)} " +
                    $"playerTech={IgnoranceCompat.PlayerTechLevel} picked={(picked == null ? "none" : picked.Name)}\n" +
                    $"  pool ({passed.Count}): {string.Join("; ", passed)}\n" +
                    $"  cut by tech gate ({cutByTech.Count}): {string.Join("; ", cutByTech)}\n" +
                    $"  rejected by quest conditions ({rejected.Count}): {string.Join("; ", rejected)}");
    }
}
