# PathExists

- 类名：`PathExists`
- 节点类型：条件节点
- 分类：Movement
- 基类：`ConditionTask<NavMeshAgent>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/Movement/PathExists.cs`

## 作用

Check if a path exists for the agent and optionaly save the resulting path positions

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `targetPosition` | `BBParameter<Vector3>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `savePathAs` | `BBParameter<List<Vector3>>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
