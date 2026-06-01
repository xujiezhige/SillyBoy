# CheckBooleanTrigger

- 类名：`CheckBooleanTrigger`
- 节点类型：条件节点
- 分类：✫ Blackboard
- 基类：`ConditionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/Blackboard/CheckBooleanTrigger.cs`

## 作用

Check if a boolean variable is true and if so, it is immediately reset to false.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `trigger` | `BBParameter<bool>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
