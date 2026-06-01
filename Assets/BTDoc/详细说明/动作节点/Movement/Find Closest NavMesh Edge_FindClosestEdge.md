# Find Closest NavMesh Edge

- 类名：`FindClosestEdge`
- 节点类型：动作节点
- 分类：Movement/Pathfinding
- 基类：`ActionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Movement/Pathfinding/FindClosestEdge.cs`

## 作用

Find the closes Navigation Mesh position to the target position

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `targetPosition` | `BBParameter<Vector3>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `saveFoundPosition` | `BBParameter<Vector3>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
