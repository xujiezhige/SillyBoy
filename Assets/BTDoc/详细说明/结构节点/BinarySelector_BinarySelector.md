# BinarySelector

- 类名：`BinarySelector`
- 节点类型：结构节点
- 分类：Composites
- 基类：`BTNode`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Composites/BinarySelector.cs`

## 作用

Quick way to execute the left or the right child, based on a Condition Task.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `dynamic` | `bool` | - | If true, the condition will be re-evaluated per frame. | - |

## 使用备注

结构节点负责组织和调度子节点，通常拥有多个子连接。
