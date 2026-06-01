# ProbabilitySelector

- 类名：`ProbabilitySelector`
- 节点类型：结构节点
- 分类：Composites
- 基类：`BTComposite`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Composites/ProbabilitySelector.cs`

## 作用

Selects a child to execute based on its chance to be selected and returns Success if the child returns Success, otherwise picks another child.\nReturns Failure if all children return Failure, or a direct 'Failure Chance' is introduced.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `childWeights` | `List<BBParameter<float>>` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `failChance` | `BBParameter<float>` | - | A chance for the node to fail immediately. | - |

## 使用备注

结构节点负责组织和调度子节点，通常拥有多个子连接。
