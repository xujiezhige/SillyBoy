# Selector

- 类名：`Selector`
- 节点类型：结构节点
- 分类：Composites
- 基类：`BTComposite`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Composites/Selector.cs`

## 作用

Executes its childrfen in order and returns Failure if all children return Failure. As soon as a child returns Success, the Selector will stop and return Success as well.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `dynamic` | `bool` | - | If true, then higher priority children are re-evaluated per frame and if either returns Success, then the Selector will immediately stop and return Success as well. | - |
| `random` | `bool` | - | If true, the children order of execution is shuffled each time the Selector resets. | - |

## 使用备注

结构节点负责组织和调度子节点，通常拥有多个子连接。
