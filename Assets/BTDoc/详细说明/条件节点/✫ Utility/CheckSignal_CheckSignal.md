# CheckSignal

- 类名：`CheckSignal`
- 节点类型：条件节点
- 分类：✫ Utility
- 基类：`ConditionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/Utility/CheckSignal.cs`

## 作用

Check for an invoked Signal with agent as the target. If Signal was invoked as global, then the target does not matter.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `signalDefinition` | `BBParameter<SignalDefinition>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
