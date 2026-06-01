# Target Within Distance

- 类名：`CheckDistanceToGameObject`
- 节点类型：条件节点
- 分类：GameObject
- 基类：`ConditionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/GameObject/CheckDistanceToGameObject.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `checkTarget` | `BBParameter<GameObject>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `checkType` | `CompareMethod` | `CompareMethod.LessThan` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `distance` | `BBParameter<float>` | `10` | 距离阈值或保持距离。 | - |
| `floatingPoint` | `float` | `0.05f` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
