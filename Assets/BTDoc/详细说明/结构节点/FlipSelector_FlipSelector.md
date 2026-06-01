# FlipSelector

- 类名：`FlipSelector`
- 节点类型：结构节点
- 分类：Composites
- 基类：`BTComposite`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Composites/FlipSelector.cs`

## 作用

Works like a normal Selector, but when a child returns Success, that child will be moved to the end.\nAs a result, previously Failed children will always be checked first and recently Successful children last.

## 参数

无公开配置参数。

## 使用备注

结构节点负责组织和调度子节点，通常拥有多个子连接。
