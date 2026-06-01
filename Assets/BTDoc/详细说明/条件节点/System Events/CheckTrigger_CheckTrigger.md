# CheckTrigger

- 类名：`CheckTrigger`
- 节点类型：条件节点
- 分类：System Events
- 基类：`ConditionTask<Collider>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/SystemEvents/CheckTrigger.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `checkType` | `TriggerTypes` | `TriggerTypes.TriggerEnter` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `specifiedTagOnly` | `bool` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `objectTag` | `string` | `"Untagged"` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `saveGameObjectAs` | `BBParameter<GameObject>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `checkType` | `TriggerTypes` | `TriggerTypes.TriggerEnter` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `specifiedTagOnly` | `bool` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `objectTag` | `string` | `"Untagged"` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `saveGameObjectAs` | `BBParameter<GameObject>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
