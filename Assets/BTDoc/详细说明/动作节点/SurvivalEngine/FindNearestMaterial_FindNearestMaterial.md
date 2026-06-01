# FindNearestMaterial

- 类名：`FindNearestMaterial`
- 节点类型：动作节点
- 分类：SurvivalEngine/Player
- 基类：`ActionTask`
- 源文件：`Assets/SurvivalEngine/Scripts/AI/Actions/FindNearestMaterial.cs`

## 作用

Find the nearest collectable item that matches one of the requested material ids.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `materialItemIds` | `BBParameter<List<string>>` | - | Material item ids to search for in the world. Empty or null entries are ignored. | - |
| `range` | `BBParameter<float>` | `999f` | Maximum search radius from the current player. Values below zero are treated as zero. | - |
| `requireInventorySpace` | `BBParameter<bool>` | `true` | When enabled, ignores matching items that the player's inventory cannot currently take. | - |
| `materialItemId` | `BBParameter<string>` | - | Output item id of the nearest matching material found. Cleared when no material is found. | BlackboardOnly |
| `target` | `BBParameter<Vector2>` | - | Output XZ target point for moving to the found material. Uses the closest interact point when the item is selectable. | BlackboardOnly |
| `targetObject` | `BBParameter<GameObject>` | - | Output GameObject of the found material item. Set to null when no material is found. | BlackboardOnly |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
