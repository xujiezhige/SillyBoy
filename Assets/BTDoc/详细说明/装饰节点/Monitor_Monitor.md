# Monitor

- 类名：`Monitor`
- 节点类型：装饰节点
- 分类：Decorators
- 基类：`BTDecorator`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Decorators/Monitor.cs`

## 作用

Monitors the decorated child for a returned Status and executes an Action when that is the case.\nThe final Status returned to the parent can either be the original decorated child Status, or the new decorator Action Status.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `monitorMode` | `MonitorMode` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `returnMode` | `ReturnStatusMode` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

装饰节点通常包裹一个子节点，用于修改执行条件、结果或生命周期。
