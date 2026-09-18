# HSK More Balance: Quests

Quest tuning for the Hardcore SK modlist: tech-level gates, faction filters and lodger caps.
Harmony patches only, plus one settings def — safe to add to or remove from a save.

## Settings def

`Defs/Misc/BalanceSettings.xml` (`BalanceSettingsDef`, defName `HSKMoreBalanceQuests_Settings`).
Every patch falls back to its in-code defaults when the def is missing.

- `questMinTechLevel` — quest defName -> minimum player tech level
- `sitePartMinTechLevel` — SitePartDef defName -> minimum player tech level
- `mechClusterMinTechLevel` — quest-spawned mech clusters

Player tech level comes from Ignorance Is Bliss (`IgnoranceCompat`, reflection, no DLL reference);
without it, the player faction's tech level is used.

## Patches

| File | What it does |
|---|---|
| `Patch_QuestTechGate.cs` | Blocks gated quests in `QuestScriptDef.CanRun` |
| `Patch_SitePartTechGate.cs` | Blocks gated site parts in `SitePartWorker.IsAvailable` (base + overrides) |
| `Patch_MechClusterTechGate.cs` | Fails `QuestNode_SpawnMechCluster.TestRunInt` below the threshold |
| `Patch_QuestFactionTech.cs` | `QuestNode_GetFaction` picks must pass the IiB tech range |
| `Patch_ShuttleCrashEnemyFaction.cs` | Same for `ShuttleCrash_Rescue`, which picks its enemy in C# |
| `Patch_RewardWeaponTech.cs` | Quest item rewards only offer weapons one tech level above the player |
| `Patch_PrisonerCount.cs` | Lodger count by tech level: Neolithic 1-2, Medieval 2-4, Industrial 4-6 |
| `Patch_RefugeeCount.cs` | Refugees/beggars: colonists / 2, capped by mod settings |
| `Patch_NoDoubleGuests.cs` | No guest quests while quest lodgers are present |
| `Patch_NoDoubleMonument.cs` | No second monument quest while one is active |
| `Patch_WastepackCount.cs` | Fewer wastepacks in `PollutionDump`, by colony tech level |
| `Patches/Patch_QuestBalance.xml` | Smaller monuments |

`HSKMoreBalanceQuestsMod.cs` / `HSKMoreBalanceQuestsSettings.cs` hold the mod settings window
(lodger caps, wastepack multipliers). `HSKMoreBalanceQuestsInit.cs` runs `PatchAll`.

## Disabled

`Source/HSKMoreBalanceQuests/_Disabled/` and `_Disabled/` hold the old karma system (component, need,
thought, UI, karma patches) — excluded from the build via `<Compile Remove="_Disabled\**" />`
and not loaded by the game. Move files back to re-enable.

## Build

```bash
dotnet build Source/HSKMoreBalanceQuests/HSKMoreBalanceQuests.v16.csproj -c Release
```

1.6 only. Output: `1.6/Assemblies/HSKMoreBalanceQuests.dll`. The csproj defaults `RimWorldPath`
to the Steam install; override with `-p:RimWorldPath=...`.
