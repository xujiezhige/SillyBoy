# AI 行为树自动调试流程

## 目标

建立一个可反复执行的闭环：

1. 生成或修改 `Assets/BTAssets/*.yaml` 行为树逻辑配置。
2. 通过 MCP 工具 `create_behavior_tree_from_yaml` 生成 NodeCanvas 行为树资产。
3. 在 Unity Play Mode 中运行玩家行为树。
4. 通过 MCP 工具 `query_ai_debugger` 或 `run_ai_bt_debug_iteration` 读取运行时状态。
5. 根据诊断结果修改 YAML 或节点代码。
6. 重复执行，直到行为树结构清晰、运行顺畅、诊断报告没有高严重度问题。

当前实现约定：

- `run_ai_bt_debug_iteration` 默认会尝试把目标 `.asset` 绑定到 `PlayerCharacter` 的 `BehaviourTreeOwner`。
- 若处于 Play Mode，绑定后会自动重启该行为树，避免“资产已生成但玩家仍未运行新树”。
- 可选参数 `bind_player_tree=true`、`player_name=PlayerCharacter`、`clear_debugger=true` 可用于标准化每轮调试起点。

## MCP 工具

### create_behavior_tree_from_yaml

从 YAML 生成行为树资产。

关键参数：

- `yaml_path`: 行为树 YAML 路径，建议在 `Assets/BTAssets` 下。
- `asset_path`: 生成的 `.asset` 路径，建议与 YAML 同目录。
- `overwrite`: 迭代时传 `true`。
- `strict`: 调试阶段一般传 `false`，稳定后可传 `true`。

### query_ai_debugger

查询运行时调试器。

常用 action：

- `start`: 创建并启用 `GameStateDebugger`。
- `report`: 获取当前快照、诊断、近期事件和近期采样。
- `snapshot`: 只抓取当前快照。
- `events`: 读取近期事件。
- `samples`: 读取近期采样。
- `clear`: 清空历史。

### run_ai_bt_debug_iteration

执行一次标准化调试迭代。

常用参数：

- `yaml_path`: 当前行为树 YAML。
- `asset_path`: 当前行为树资产。
- `regenerate`: 是否先从 YAML 重新生成资产。
- `write_report`: 是否将报告写入 `Assets/BTDebugReports`。
- `event_count`: 报告中包含多少近期事件。

## 调试判定

优先处理 `severity = error` 的 findings，其次处理持续重复的 warning。

### move_to_interact_lost_auto_move

含义：

`MoveToInteract` 仍在 Running，但 `PlayerCharacter.IsAutoMove()` 已经为 false，且没有到达目标。

处理：

- 优先确认是否是目标不可达、水面、障碍物或 NavMesh 缺失。
- 若是移动节点容错问题，修改 `MoveToInteract` 的重试或失败逻辑。
- 若是目标选择问题，修改 `FindNearestMaterial` 过滤不可达目标。
- 若是树结构问题，确保移动失败后能重新选择材料，而不是卡在同一个目标。

### movement_stopped_while_swimming

含义：

玩家进入水面后移动停止，且行为树仍在执行移动交互。

处理：

- 优先启用或修复 AI 移动使用 NavMesh。
- 对采集目标增加可达性判断。
- 行为树中对移动失败路径回到 `FindNearestMaterial`，避免重复撞同一个目标。

### auto_move_no_progress

含义：

玩家仍有自动移动请求，但一段时间内 XZ 位移不足。

处理：

- 检查目标是否在障碍物内部或交互点不可达。
- 增加目标黑名单或短期失败记忆，避免反复选择同一坏目标。

## 推荐循环

1. 运行 `create_behavior_tree_from_yaml`，确保资产是最新 YAML 生成。
2. 进入 Play Mode，确保玩家挂载目标行为树。
3. 调用 `query_ai_debugger` action=`start`。
4. 让游戏运行 20 到 60 秒。
5. 调用 `run_ai_bt_debug_iteration`，保存报告。
   - 推荐同时传 `bind_player_tree=true`，确保最新资产已经挂到玩家上。
   - 若希望每轮从干净历史开始，传 `clear_debugger=true`。
6. AI 阅读报告：
   - 如果是节点实现问题，修改 C# 节点。
   - 如果是结构问题，修改 YAML。
   - 如果是世界/导航问题，记录为环境配置问题，并尽量在树里做容错。
7. 重新生成资产并继续测试。

## 收敛标准

认为一轮行为树逻辑足够顺畅，需要同时满足：

- 没有 `error` findings。
- 同类 `warning` 不连续重复出现。
- 行为树 Running 节点能随着目标变化推进，而不是长时间固定在同一叶子节点。
- 黑板变量中的目标物体、目标点、材料列表能随采集和合成持续更新。
- 玩家状态不长时间处于 `is_auto_move=false` 且行为树移动节点 Running 的组合。
