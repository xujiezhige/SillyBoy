# MoveToInteract

- 类名：`MoveToInteract`
- 节点类型：动作节点
- 分类：SurvivalEngine/Player
- 基类：`ActionTask`
- 源文件：`Assets/SurvivalEngine/Scripts/AI/Actions/MoveToInteract.cs`

## 作用

Move to a target point or object, and interact if the target object can be interacted with.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `target` | `BBParameter<Vector2>` | - | Fallback XZ world target used when targetObject is empty or cannot be interacted with. | - |
| `targetObject` | `BBParameter<GameObject>` | - | Optional GameObject target to move toward. If it has a usable Selectable component, the player will interact with it when in range. | - |
| `droppedItems` | `BBParameter<List<GameObject>>` | - | Output list of item GameObjects dropped by the interaction while this action is running. | BlackboardOnly |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
