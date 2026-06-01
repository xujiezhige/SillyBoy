# CheckVectorDistance

- 类名：`CheckVectorDistance`
- 节点类型：条件节点
- 分类：✫ Blackboard
- 基类：`ConditionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/Blackboard/CheckVectorDistance.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `vectorA` | `BBParameter<Vector3>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `vectorB` | `BBParameter<Vector3>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `comparison` | `CompareMethod` | `CompareMethod.EqualTo` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `distance` | `BBParameter<float>` | - | 距离阈值或保持距离。 | - |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
