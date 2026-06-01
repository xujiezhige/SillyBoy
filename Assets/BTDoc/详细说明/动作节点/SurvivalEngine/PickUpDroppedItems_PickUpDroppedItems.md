# PickUpDroppedItems

- 类名：`PickUpDroppedItems`
- 节点类型：动作节点
- 分类：SurvivalEngine/Player
- 基类：`ActionTask`
- 源文件：`Assets/SurvivalEngine/Scripts/AI/Actions/PickUpDroppedItems.cs`

## 作用

Pick up dropped item GameObjects in order using the current player.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `items` | `BBParameter<List<GameObject>>` | - | Dropped item GameObjects to pick up in order. Invalid, inactive, or non-interactable items are skipped. | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
