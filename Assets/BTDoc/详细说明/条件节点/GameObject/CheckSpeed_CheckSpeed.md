# CheckSpeed

- 类名：`CheckSpeed`
- 节点类型：条件节点
- 分类：GameObject
- 基类：`ConditionTask<Rigidbody>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/GameObject/CheckSpeed.cs`

## 作用

Checks the current speed of the agent against a value based on it's Rigidbody velocity

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `checkType` | `CompareMethod` | `CompareMethod.EqualTo` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `value` | `BBParameter<float>` | - | 要读取、比较或写入的值。 | - |
| `differenceThreshold` | `float` | `0.05f` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
