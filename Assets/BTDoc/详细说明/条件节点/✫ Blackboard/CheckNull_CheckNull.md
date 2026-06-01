# CheckNull

- 类名：`CheckNull`
- 节点类型：条件节点
- 分类：✫ Blackboard
- 基类：`ConditionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/Blackboard/CheckNull.cs`

## 作用

Check whether or not a variable is null

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `variable` | `BBParameter<System.Object>` | - | 黑板变量引用。 | BlackboardOnly |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
