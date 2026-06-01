# TriggerBoolean

- 类名：`TriggerBoolean`
- 节点类型：动作节点
- 分类：✫ Blackboard
- 基类：`ActionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Blackboard/TriggerBoolean.cs`

## 作用

Triggers a boolean variable for 1 frame to True then back to False

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `variable` | `BBParameter<bool>` | - | 黑板变量引用。 | RequiredField, BlackboardOnly |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
