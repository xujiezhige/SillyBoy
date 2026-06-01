# Any Target Within Distance

- 类名：`CheckDistanceToGameObjectAny`
- 节点类型：条件节点
- 分类：GameObject
- 基类：`ConditionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/GameObject/CheckDistanceToGameObjectAny.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `targetObjects` | `BBParameter<List<GameObject>>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `checkType` | `CompareMethod` | `CompareMethod.LessThan` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `distance` | `BBParameter<float>` | `10` | 距离阈值或保持距离。 | - |
| `floatingPoint` | `float` | `0.05f` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `allResults` | `BBParameter<List<GameObject>>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `closerResult` | `BBParameter<GameObject>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
