# AI Behavior Tree Debug Framework

This document describes the reusable AI behavior-tree debug loop currently implemented in this project. It is intentionally written as a portable framework note so the workflow can later be moved into a standalone GitHub repository or copied into another Unity project.

## Scope

The framework covers four layers:

- YAML behavior-tree sources in `Assets/BTAssets/*.yaml`.
- NodeCanvas asset generation through `create_behavior_tree_from_yaml`.
- Runtime sampling and diagnostics through `GameStateDebugger`, `query_ai_debugger`, and `run_ai_bt_debug_iteration`.
- Batch profile regression through `Assets/BTAssets/AIBehaviorTreeProfiles.json` and `run_ai_bt_profile_regression`.

Project-specific adapters are isolated in these areas:

- `AIRuntimeSceneQuery` resolves the current `PlayerCharacter` and world `Item` instances.
- `AIBehaviorTreeDebugUtility` binds a generated `BehaviourTree` asset to the player `BehaviourTreeOwner`.
- `AIMovementReachability` validates NavMesh paths and rejects paths that cross water.
- `MoveToInteract`, `FindNearestMaterial`, and `FindBestCraftableItem` implement SurvivalEngine-specific movement, gathering, crafting, target failure memory, and craft candidate failure memory.

## Standard Single-Tree Loop

1. Edit a YAML tree under `Assets/BTAssets`.
2. Regenerate the NodeCanvas asset:

```json
{
  "yaml_path": "Assets/BTAssets/CraftAllUsefulItems.yaml",
  "asset_path": "Assets/BTAssets/CraftAllUsefulItems.asset",
  "overwrite": true,
  "strict": false
}
```

3. Enter Unity Play Mode.
4. Run one debug iteration with regeneration and player binding enabled:

```json
{
  "yaml_path": "Assets/BTAssets/CraftAllUsefulItems.yaml",
  "asset_path": "Assets/BTAssets/CraftAllUsefulItems.asset",
  "regenerate": true,
  "bind_player_tree": true,
  "clear_debugger": true,
  "write_report": true,
  "event_count": 160
}
```

5. Read the generated `Assets/BTDebugReports/BTDebugReport_*.json`.
6. Fix the smallest responsible surface:

- YAML structure when the tree cannot recover from a normal failure.
- Node code when an action stays running after movement or interaction can no longer progress.
- Scene or NavMesh setup when no reasonable code fallback can make a target reachable.

7. Repeat until the report has no `severity=error` findings and no repeated warning keys.

## Batch Regression Loop

The batch entry point is `Assets/BTAssets/AIBehaviorTreeProfiles.json`.

Each profile declares:

- `name`: stable regression name.
- `description`: behavior being validated.
- `yaml_path`: YAML source.
- `asset_path`: generated NodeCanvas asset.
- `event_count`: sampling window for reports.

Run the full manifest-driven regression in Play Mode:

```json
{}
```

The tool writes `Assets/BTDebugReports/BTRegressionSummary_*.json` with:

- `passed_count` and `failed_count`.
- `runtime_sampled`, which must be `true` for runtime acceptance.
- total `error_count`.
- total `warning_finding_count`.
- total `repeated_warning_count`.
- per-profile report paths and finding keys.
- acceptance flags for zero errors, zero repeated warnings, and all profiles passing.

The current acceptance thresholds are:

```json
{
  "max_error_findings": 0,
  "max_repeated_warnings": 0
}
```

## Diagnostics Contract

Blocking findings:

- `move_to_interact_lost_auto_move`: `MoveToInteract` is still running after auto movement stopped and the target was not reached.
- `movement_stopped_while_swimming`: the player entered water or a swimming state while a movement interaction was active.
- `auto_move_no_progress`: auto movement exists but XZ displacement is too small over the sampling window.
- `craft_candidate_stalled`: crafting repeatedly selects a candidate that cannot progress.
- `player_dead_during_ai_debug`: the sampled player is dead, so movement and interaction diagnostics are not a valid acceptance signal.
- `player_movement_disabled_during_ai_debug`: the tree is running while player controls or movement are disabled.

Expected recovery behavior:

- `MoveToInteract` retries short auto-move interruptions, then fails the action and records short-term target failure memory.
- `FindNearestMaterial` skips recently failed targets and unreachable NavMesh or water-crossing paths.
- `FindBestCraftableItem` skips recently failed craft candidates when their missing materials cannot be gathered.
- YAML trees route movement or gathering failure back to target selection instead of retrying the same blocked object forever.

## Current Profiles

- `CraftAllUsefulItems`: craft the best reachable useful item, gathering missing materials as needed.
- `ForageHerbsAndSeedsLoop`: repeatedly gather herbs and seed resources.
- `GatherStarterSuppliesLoop`: prefer wood and rock, then fall back to forage resources.
- `GatherWoodAndRockLoop`: validate a minimal early-game material collection loop.
- `PotionRedPrepLoop`: prepare red potion crafting with vial, herb, mushroom, and rock fallback paths.
- `ToolProgressionLoop`: progress toward pickaxe crafting through wood and rock collection.
- `WildPantryRotationLoop`: rotate between forage plants and general crafting materials.

## Latest Validation

Latest Play Mode batch regression:

- Summary: `Assets/BTDebugReports/BTRegressionSummary_20260603_230209.json`
- Profiles: 7
- Passed: 7
- Failed: 0
- Error findings: 0
- Warning findings: 0
- Repeated warnings: 0

Important follow-up: the latest report snapshot shows `PlayerCharacter.is_dead=true` and movement disabled while the tree was bound. `GameStateDebugger` now reports this as `player_dead_during_ai_debug` / `player_movement_disabled_during_ai_debug` errors. Re-run the profile regression from a reset Play Mode state with a living, movable player before treating that latest summary as final acceptance.

Unity Console still contains non-AI `NullReferenceException` noise from UI, camera, inventory, character, and control scripts. These exceptions are outside the behavior-tree diagnostic chain and should be tracked separately before publishing the framework.

## Porting Checklist

When extracting this framework to another project:

1. Keep the YAML-to-NodeCanvas importer independent from SurvivalEngine-specific actions.
2. Replace `AIRuntimeSceneQuery` with project-specific player and world-object lookup.
3. Replace `AIBehaviorTreeDebugUtility.BindBehaviorTreeToPlayer` with the target project's behavior-tree owner binding rules.
4. Replace `AIMovementReachability` if the project does not use Unity NavMesh or has different hazard layers.
5. Keep the report schema stable so regression summaries remain comparable across runs.
6. Keep profile manifests explicit; directory scanning is useful as a fallback, but release workflows should use a committed manifest.
7. Treat Play Mode regression as the source of truth. Non-Play Mode runs only validate YAML-to-asset generation and should not pass runtime acceptance gates.

## Release Notes For Extraction

Recommended package boundary:

- Editor tools: `CreateBehaviorTreeFromYaml`, `RunAIBTDebugIteration`, `RunAIBTProfileRegression`, `AIBehaviorTreeReportUtility`, and binding utilities.
- Runtime diagnostics: `GameStateDebugger`, runtime scene query adapter, failure-memory helpers, and movement reachability adapter.
- Content examples: profile manifest, sample YAML trees, and sample debug reports.
- Documentation: this file, a quickstart, profile schema, report schema, and a troubleshooting page.

Before publishing, separate the framework from SurvivalEngine types behind interfaces or small adapter classes. The current implementation is stable for this project, but still directly references `PlayerCharacter`, `Item`, `Selectable`, and project-specific craft data.
