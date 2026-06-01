# CheckEvent

- 类名：`CheckEvent`
- 节点类型：条件节点
- 分类：✫ Utility
- 基类：`ConditionTask<GraphOwner>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/Utility/CheckEvent.cs`

## 作用

Check if an event is received and return true for one frame

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `eventName` | `BBParameter<string>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `eventName` | `BBParameter<string>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `saveEventValue` | `BBParameter<T>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
