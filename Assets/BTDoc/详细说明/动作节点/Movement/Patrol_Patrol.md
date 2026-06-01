# Patrol

- 类名：`Patrol`
- 节点类型：动作节点
- 分类：Movement/Pathfinding
- 基类：`ActionTask<NavMeshAgent>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Movement/Pathfinding/Patrol.cs`

## 作用

Move Randomly or Progressively between various game object positions taken from the list provided

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `targetList` | `BBParameter<List<GameObject>>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `patrolMode` | `BBParameter<PatrolMode>` | `PatrolMode.Random` | The mode to use for patrol (progressive or random) | - |
| `speed` | `BBParameter<float>` | `4` | 移动或变化速度。 | - |
| `keepDistance` | `BBParameter<float>` | `0.1f` | 与目标保持的最小距离。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
