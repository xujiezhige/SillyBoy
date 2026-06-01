# InterceptEvent

- 类名：`InterceptEvent`
- 节点类型：条件节点
- 分类：UGUI
- 基类：`ConditionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/UGUI/InterceptEvent.cs`

## 作用

Returns true when the selected event is triggered on the selected agent.\nYou can use this for both GUI and 3D objects.\nPlease make sure that Unity Event Systems are setup correctly

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `eventType` | `EventTriggerType` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
