# Remap

- 类名：`Remapper`
- 节点类型：装饰节点
- 分类：Decorators
- 基类：`BTDecorator`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Decorators/Remapper.cs`

## 作用

Remaps the child status to another status. Used to either invert the child's return status or to always return a specific status.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `successRemap` | `RemapStatus` | `RemapStatus.Success` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `failureRemap` | `RemapStatus` | `RemapStatus.Failure` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

装饰节点通常包裹一个子节点，用于修改执行条件、结果或生命周期。
