# CraftItem

- 类名：`CraftItem`
- 节点类型：动作节点
- 分类：SurvivalEngine/Player
- 基类：`ActionTask`
- 源文件：`Assets/SurvivalEngine/Scripts/AI/Actions/CraftItem.cs`

## 作用

Craft the item id with the current player and wait until crafting finishes.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `itemId` | `BBParameter<string>` | - | Craft item id to craft with the current player. The value must match a CraftData item id. | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
