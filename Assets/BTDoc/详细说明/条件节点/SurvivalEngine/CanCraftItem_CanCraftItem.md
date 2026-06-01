# CanCraftItem

- 类名：`CanCraftItem`
- 节点类型：条件节点
- 分类：SurvivalEngine/Player
- 基类：`ConditionTask`
- 源文件：`Assets/SurvivalEngine/Scripts/AI/Conditions/CanCraftItem.cs`

## 作用

Check whether the current player can craft the item id.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `itemId` | `BBParameter<string>` | - | Craft item id to check against the current player's known recipes, materials, and crafting requirements. | - |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
