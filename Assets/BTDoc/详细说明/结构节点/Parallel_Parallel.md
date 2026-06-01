# Parallel

- 类名：`Parallel`
- 节点类型：结构节点
- 分类：Composites
- 基类：`BTComposite`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Composites/Parallel.cs`

## 作用

Executes all children simultaneously and return Success or Failure depending on the selected Policy.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `policy` | `ParallelPolicy` | `ParallelPolicy.FirstFailure` | The policy determines when the Parallel node will end and return its Status. | - |
| `dynamic` | `bool` | - | 是否每帧重新评估更高优先级子节点。 | - |

## 使用备注

结构节点负责组织和调度子节点，通常拥有多个子连接。
