# CheckStateStatus

- 类名：`CheckStateStatus`
- 节点类型：条件节点
- 分类：✫ Utility
- 基类：`ConditionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/Utility/CheckStateStatus.cs`

## 作用

Check the parent state status. This condition is only meant to be used along with an FSM system.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `status` | `CompactStatus` | `CompactStatus.Success` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
