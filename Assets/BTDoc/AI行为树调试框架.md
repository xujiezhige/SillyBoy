# AI 行为树调试框架

## 目标

把当前项目中的 YAML 行为树、NodeCanvas 资产生成、运行时采样、诊断报告、失败记忆和回归验证整理成可重复执行的调试框架，而不是只服务单一行为树。

## 框架边界

- 行为树源码层：`Assets/BTAssets/*.yaml`
- Profile 清单层：`Assets/BTAssets/AIBehaviorTreeProfiles.json`
- 行为树资产生成层：`create_behavior_tree_from_yaml`
- 运行时绑定与调试采样层：`run_ai_bt_debug_iteration`、`query_ai_debugger`
- 行为诊断层：`GameStateDebugger`、`AITargetFailureMemory`、`AICraftCandidateFailureMemory`
- 运行时世界查询层：`AIRuntimeSceneQuery`
- 运行安全层：`AIMovementReachability`、`MoveToInteract`、`FindNearestMaterial`
- 报告产物层：`Assets/BTDebugReports/*.json`

## 当前可直接复用的调试能力

### 1. 标准迭代入口

- `run_ai_bt_debug_iteration` 支持：
  - 从 YAML 重新生成 `.asset`
  - 自动绑定到 `PlayerCharacter`
  - Play Mode 下重启行为树
  - 清空调试器历史
  - 写入 JSON 报告
  - 对比上一次报告，标记连续重复 warning

### 2. 批量 profile 回归入口

- `run_ai_bt_profile_regression` 支持：
  - 默认读取 `Assets/BTAssets/AIBehaviorTreeProfiles.json`
  - manifest 缺失时扫描 `Assets/BTAssets/*.yaml`，排除 `SampleBehaviorTree`
  - 或通过 `profiles` / `profile_names` 指定子集
  - 读取 manifest 的 `defaults.report_folder`、`defaults.event_count`、`defaults.player_name` 和 `defaults.acceptance`
  - 每个 profile 可独立声明 `yaml_path`、`asset_path`、`description`、`event_count`
  - 对每个 profile 调用标准迭代入口，统一执行重新生成、绑定、采样和报告写入
  - 写入 `Assets/BTDebugReports/BTRegressionSummary_*.json`
  - 汇总每棵树的 `error_count`、`warning_finding_count`、`repeated_warning_count` 和 finding keys
  - 用 `thresholds.max_error_findings`、`thresholds.max_repeated_warnings`、`acceptance.no_error_findings`、`acceptance.no_repeated_warnings`、`acceptance.all_profiles_passed` 作为回归门槛

### 3. 运行时诊断

- `GameStateDebugger` 会持续采样：
  - 玩家移动/游泳/忙碌状态
  - 行为树运行节点
  - 黑板关键变量
  - 近期世界物品
- `AIRuntimeSceneQuery` 会在 `PlayerCharacter.GetAll()` / `Item.GetAll()` 静态列表失真时回退到场景扫描，避免 Play Mode 切树、重编译或域重载后调试节点误判“世界里没有玩家/物品”。
- 默认重点诊断：
  - `move_to_interact_lost_auto_move`
  - `movement_stopped_while_swimming`
  - `auto_move_no_progress`
  - `craft_candidate_stalled`

### 4. 容错与回退

- `MoveToInteract` 在自动移动丢失、游泳卡住时会终止并写入失败记忆。
- `FindNearestMaterial` 会跳过近期失败目标，并过滤不可达或穿水路径。
- `FindBestCraftableItem` 会跳过近期失败的 craft candidate，并要求缺失材料存在可达来源。

## 当前 profile

### `CraftAllUsefulItems`

- 目标：持续挑选最有价值且当前可推进的 craft candidate。
- 场景：回归验证采集 + 合成主循环。

### `GatherWoodAndRockLoop`

- 目标：稳定采集早期最常用的 `wood` / `rock`。
- 场景：只验证寻路、靠近交互、掉落拾取链路。

### `ForageHerbsAndSeedsLoop`

- 目标：循环采集 `herbs` 和种子类资源。
- 场景：验证另一类资源分布下的可达性和移动稳定性。

### `GatherStarterSuppliesLoop`

- 目标：优先采集 `wood` / `rock`，附近没有时自动回退到 `herbs` / 种子。
- 场景：验证单棵树内的多资源优先级切换与失败回退。

### `PotionRedPrepLoop`

- 目标：优先尝试制作 `potion_red` / `potion_vial`，否则采集 `herbs` / `mushroom_red`，再回退采集 `rock`。
- 场景：验证“固定制作目标 + 多材料 fallback”的药水准备路线。

### `ToolProgressionLoop`

- 目标：优先制作 `pickaxe`，材料不足时循环采集 `wood` / `rock`。
- 场景：验证早期工具推进和核心材料循环。

### `WildPantryRotationLoop`

- 目标：用 `FlipSelector` 在野外植物补给和通用制作材料之间轮换。
- 场景：验证非单一路径的补给行为，避免一直重复刚成功的同一资源分支。

## 已验证修复点

- `MoveToInteract` 和 `PickUpDroppedItems` 对 `Selectable` / `GameObject` 目标保留真实交互点高度，只在纯 `Vector2` fallback 目标上使用玩家当前高度，避免高低差地形中“可达性检查到高处，实际移动到高处下方”的错配。
- `MoveToInteract` 不再对每次自动移动重试都写同类 warning；只有放弃当前目标并写入失败记忆时记录一次 warning。
- `GameStateDebugger` 对 `move_to_interact_lost_auto_move` 增加 `lostAutoMoveErrorGraceSeconds` 自愈窗口。短时间丢失自动移动视为 warning，超过窗口仍未恢复才升级为 error。

## 最近验证报告

- `Assets/BTDebugReports/BTDebugReport_20260603_084720.json`：`CraftAllUsefulItems` 长跑报告，`findings=[]`，`history_summary.repeated_warning_count=0`。
- `Assets/BTDebugReports/BTDebugReport_20260603_085146.json`：`PotionRedPrepLoop` 高低差场景短跑报告，暴露过移动目标高度/重试噪声问题，已据此修复节点代码和诊断分级。
- `Assets/BTDebugReports/BTDebugReport_20260603_085356.json`：修复后 `CraftAllUsefulItems` 回归报告，`findings=[]`，`history_summary.repeated_warning_count=0`。该轮在当前库存/场景状态下快速进入 `root_status=Failure`，表示没有可继续推进的 craft candidate；这是可接受终止态，不代表运行时错误。
- `Assets/BTDebugReports/BTRegressionSummary_20260603_110447.json`：Play Mode 全量 profile 回归，7/7 passed，`error_count=0`，`warning_finding_count=0`，`repeated_warning_count=0`。同一轮之前的 `BTRegressionSummary_20260603_110420.json` 是非 Play Mode 生成验证，只包含 `unity_not_in_play_mode` warning，不作为运行时失败依据。

## 推荐回归流程

1. 在 `Assets/BTAssets/AIBehaviorTreeProfiles.json` 中登记 YAML profile，或直接使用 `profiles` 参数传入临时 profile。
2. 单棵树调试时，通过 `run_ai_bt_debug_iteration` 传入对应 `yaml_path` 和 `asset_path`。
3. 多棵树回归时，通过 `run_ai_bt_profile_regression` 传入 `profile_names` 子集，或不传参数跑 manifest 中全部 profile。
4. 开启 `regenerate=true`、`bind_player_tree=true`、`clear_debugger=true`。
5. 在 Play Mode 中至少采两轮报告。
6. 检查单轮报告或 `BTRegressionSummary_*.json`。
7. 满足以下条件才视为通过：
   - `findings` 中没有 `severity=error`
   - `history_summary.repeated_warning_count == 0`
   - 批量回归中 `acceptance.no_error_findings == true`
   - 批量回归中 `acceptance.no_repeated_warnings == true`
   - `recent_events` 不持续重复同一移动失败
   - 失败记忆只用于跳过坏目标，而不是越积越多

## Manifest 配置约定

`Assets/BTAssets/AIBehaviorTreeProfiles.json` 是框架的主要配置入口：

- `defaults.report_folder`：批量回归默认报告目录。
- `defaults.event_count`：profile 未指定 `event_count` 时使用的近期事件数量。
- `defaults.player_name`：默认绑定行为树的玩家对象名称；迁移到新项目时优先修改该字段。
- `defaults.acceptance.max_error_findings`：允许的最大 error finding 数，当前项目固定为 `0`。
- `defaults.acceptance.max_repeated_warnings`：允许连续重复 warning 数，当前项目固定为 `0`。
- `profiles[].event_count`：用于长链路行为树提高采样窗口，短链路可沿用默认值。

命令参数中的 `report_folder`、`event_count`、`player_name`、`max_error_findings`、`max_repeated_warnings` 会覆盖 manifest defaults，适合临时调试；发布或 CI 接入应优先固化在 manifest 中。

## 发布前检查清单

- 保留 `Assets/BTAssets/*.yaml` 作为 profile 源码，`.asset` 只作为 Unity/NodeCanvas 生成产物。
- 保留 `Assets/BTAssets/AIBehaviorTreeProfiles.json` 作为可复用 profile manifest，避免发布后依赖目录扫描推断测试集合。
- 发布或迁移框架时，优先携带 `Assets/Editor/MCPTools/*AI*`、`CreateBehaviorTreeFromYaml.cs`、`GameStateDebugger`、`AIRuntimeSceneQuery`、失败记忆类、移动可达性检查和本框架文档。
- 在新项目接入时，先参数化玩家对象名称、行为树 owner 查找规则、世界物品查询规则和目标交互点解析规则。
- 回归必须在 Play Mode 中执行；非 Play Mode 报告只用于验证 YAML 到 `.asset` 生成链路。

## 后续抽离建议

- `PlayerCharacter` 对象名称已通过 manifest defaults 和命令参数支持配置；更深层的 `PlayerCharacter` / `Item` 类型依赖仍属于 SurvivalEngine 适配层。
- 将 Unity 运行时噪音（如 `AnimalWild` / `Character` 的现有 NullReference）与 AI 行为树诊断分离，避免非 AI 异常污染回归判断。
- 让批量回归支持异步等待窗口，例如每棵树绑定后自动运行 20 到 60 秒再采样。
- 为 manifest 增加更细的场景前置条件、推荐运行时长和按 profile 的通过阈值。

## 2026-06-03 框架化补充

- 新增 `Assets/BTAssets/AIBehaviorTreeProfiles.json`，把 7 个当前回归 profile 固化为显式清单，并为长链路 profile 配置独立 `event_count`。
- 新增 `AIBehaviorTreeReportUtility`，统一单轮与批量回归的 JSON 报告写入、路径规范化和连续 warning 判定。
- `run_ai_bt_profile_regression` 现在优先读取 manifest；仍保留目录扫描 fallback，保证旧流程可继续使用。
- `run_ai_bt_profile_regression` 现在会读取 manifest defaults 中的报告目录、默认采样数量、玩家对象名称和验收阈值，并把实际阈值写入 regression summary 的 `thresholds`。

## 最新运行记录

- `Assets/BTDebugReports/BTRegressionSummary_20260603_130325.json`：Play Mode 全量 profile 回归，7/7 passed，`error_count=0`，`warning_finding_count=0`，`repeated_warning_count=0`。该轮覆盖 `CraftAllUsefulItems`、`ForageHerbsAndSeedsLoop`、`GatherStarterSuppliesLoop`、`GatherWoodAndRockLoop`、`PotionRedPrepLoop`、`ToolProgressionLoop`、`WildPantryRotationLoop`。
- Unity Console 同步检查：error/warning 计数为 0。
- `Assets/BTDebugReports/BTRegressionSummary_20260603_140351.json`：Play Mode 全量 profile 复验，7/7 passed，`error_count=0`，`warning_finding_count=0`，`repeated_warning_count=0`。Unity Console error 计数为 0。
- 当前剩余风险：`MoveToInteract` 静态脚本检查仍提示一个 `Update()` 字符串拼接 GC warning；该项不是行为树诊断 failure，但后续做框架发布前可单独优化。
- `Assets/BTDebugReports/BTRegressionSummary_20260603_150700.json`：manifest 驱动的 Play Mode 全量 profile 回归，7/7 passed，`error_count=0`，`warning_finding_count=0`，`repeated_warning_count=0`。该轮验证 `run_ai_bt_profile_regression` 已优先读取 `Assets/BTAssets/AIBehaviorTreeProfiles.json`，并按 profile 输出 `description` 与独立 `event_count`。
- `Assets/BTDebugReports/BTRegressionSummary_20260603_160228.json`：再次清空 Console 后执行 manifest 驱动的 Play Mode 全量 profile 回归，7/7 passed，`error_count=0`，`warning_finding_count=0`，`repeated_warning_count=0`。本轮确认 7 个 profile 的 YAML 重新生成、玩家绑定、运行时采样和连续 warning 判定仍然稳定。
- 同轮 Console 复查仍出现非 AI 行为树链路的 NullReference 噪音，主要来自 `TheCamera`、`UISlot`、`TheGame`、`ItemSelectedFX`、`PlayerControlsMouse`、`Character`、`KeyControlsUI`、`InventoryData`、`PlayerCharacterData`、`PlayerUI`、`TimeClockUI`。这些异常不影响 `GameStateDebugger` findings 的通过结果，但发布框架前应与 AI 回归结论分开处理。
- `Assets/BTDebugReports/BTRegressionSummary_20260603_170326.json`：本轮 Play Mode 全量 profile 回归，7/7 passed，`error_count=0`，`warning_finding_count=0`，`repeated_warning_count=0`。该轮重新验证 manifest 驱动批量回归、YAML 到 NodeCanvas 资产再生成、玩家行为树绑定、运行时采样和重复 warning 判定均稳定。
- 本轮 Console 复查仍有非 AI 行为树链路 NullReference 噪音，主要来自 `TheCamera`、`UISlot`、`TheGame`、`ItemSelectedFX`、`PlayerControlsMouse`、`Character`、`KeyControlsUI`、`InventoryData`。当前框架验收以 `BTRegressionSummary` 和 `GameStateDebugger` findings 为准，Console 噪音保留为发布前独立清理项。
- `Assets/BTDebugReports/BTRegressionSummary_20260603_180423.json`：框架化 defaults 生效后重新执行 Play Mode 全量 profile 回归，7/7 passed，`error_count=0`，`warning_finding_count=0`，`repeated_warning_count=0`，并在 summary 中输出 `thresholds.max_error_findings=0`、`thresholds.max_repeated_warnings=0`。该轮确认 `AIBehaviorTreeProfiles.json` 的 report folder、event count、player name 和 acceptance defaults 已作为批量回归配置入口生效。
