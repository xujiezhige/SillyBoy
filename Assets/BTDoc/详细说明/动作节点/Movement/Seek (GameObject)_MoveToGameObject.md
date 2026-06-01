# Seek (GameObject)

- 类名：`MoveToGameObject`
- 节点类型：动作节点
- 分类：Movement/Pathfinding
- 基类：`ActionTask<NavMeshAgent>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Movement/Pathfinding/MoveToGameObject.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `target` | `BBParameter<GameObject>` | - | 目标对象、位置或变量。 | RequiredField |
| `speed` | `BBParameter<float>` | `4` | 移动或变化速度。 | - |
| `keepDistance` | `BBParameter<float>` | `0.1f` | 与目标保持的最小距离。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
