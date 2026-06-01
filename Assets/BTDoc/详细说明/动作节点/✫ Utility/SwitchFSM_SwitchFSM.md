# SwitchFSM

- 类名：`SwitchFSM`
- 节点类型：动作节点
- 分类：✫ Utility
- 基类：`ActionTask<FSMOwner>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Utility/SwitchBehaviour.cs`

## 作用

Switch the entire FSM of FSMTreeOwner

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `behaviourTree` | `BBParameter<BehaviourTree>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `fsm` | `BBParameter<FSM>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
