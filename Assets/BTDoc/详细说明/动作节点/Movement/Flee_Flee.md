# Flee

- 类名：`Flee`
- 节点类型：动作节点
- 分类：Movement/Pathfinding
- 基类：`ActionTask<NavMeshAgent>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Movement/Pathfinding/Flee.cs`

## 作用

Flees away from the target

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `target` | `BBParameter<GameObject>` | - | 目标对象、位置或变量。 | - |
| `speed` | `BBParameter<float>` | `4f` | The speed to flee. | - |
| `fledDistance` | `BBParameter<float>` | `10f` | The distance to flee at. | - |
| `lookAhead` | `BBParameter<float>` | `2f` | A distance to look away from the target for valid flee destination. | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
