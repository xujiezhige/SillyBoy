# Wander

- 类名：`Wander`
- 节点类型：动作节点
- 分类：Movement/Pathfinding
- 基类：`ActionTask<NavMeshAgent>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Movement/Pathfinding/Wander.cs`

## 作用

Makes the agent wander randomly within the navigation map

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `speed` | `BBParameter<float>` | `4` | The speed to wander with. | - |
| `keepDistance` | `BBParameter<float>` | `0.1f` | The distance to keep from each wander point. | - |
| `minWanderDistance` | `BBParameter<float>` | `5` | A wander point can't be closer than this distance | - |
| `maxWanderDistance` | `BBParameter<float>` | `20` | A wander point can't be further than this distance | - |
| `repeat` | `bool` | `true` | If enabled, will keep wandering forever. If not, only one wander point will be performed. | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
