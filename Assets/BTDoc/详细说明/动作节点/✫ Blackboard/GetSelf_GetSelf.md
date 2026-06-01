# GetSelf

- 类名：`GetSelf`
- 节点类型：动作节点
- 分类：✫ Blackboard
- 基类：`ActionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Blackboard/Get Self.cs`

## 作用

Stores the agent gameobject on the blackboard.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `saveAs` | `BBParameter<UnityEngine.GameObject>` | - | 结果写入的黑板变量。 | BlackboardOnly |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
