# Sub FSM

- 类名：`NestedFSM`
- 节点类型：叶子节点
- 分类：Leafs
- 基类：`BTNodeNested<FSM>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Leafs/NestedFSM.cs`

## 作用

Executes a sub FSM. Returns Running while the sub FSM is active. If a Success or Failure State is selected, then it will return Success or Failure as soon as the Nested FSM enters that state at which point the sub FSM will also be stoped. If the sub FSM ends otherwise, this node will return Success.

## 参数

无公开配置参数。

## 使用备注

该节点属于行为树基础节点，用于连接任务或子图。
