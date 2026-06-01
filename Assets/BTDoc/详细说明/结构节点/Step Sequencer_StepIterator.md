# Step Sequencer

- 类名：`StepIterator`
- 节点类型：结构节点
- 分类：Composites
- 基类：`BTComposite`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Composites/StepIterator.cs`

## 作用

In comparison to a normal Sequencer which executes all its children until one fails, Step Sequencer executes its children one-by-one per Step Sequencer execution. The executed child status is returned regardless of Success or Failure.

## 参数

无公开配置参数。

## 使用备注

结构节点负责组织和调度子节点，通常拥有多个子连接。
