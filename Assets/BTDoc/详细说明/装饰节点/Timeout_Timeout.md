# Timeout

- 类名：`Timeout`
- 节点类型：装饰节点
- 分类：Decorators
- 基类：`BTDecorator`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Decorators/Timeout.cs`

## 作用

Interupts decorated child node and returns Failure if the child node is still Running after the timeout period.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `timeout` | `BBParameter<float>` | `1` | The timeout period in seconds. | - |

## 使用备注

装饰节点通常包裹一个子节点，用于修改执行条件、结果或生命周期。
