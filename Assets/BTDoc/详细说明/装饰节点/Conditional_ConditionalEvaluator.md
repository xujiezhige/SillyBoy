# Conditional

- 类名：`ConditionalEvaluator`
- 节点类型：装饰节点
- 分类：Decorators
- 基类：`BTDecorator`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Decorators/ConditionalEvaluator.cs`

## 作用

Executes and returns the child status only if the condition is true. Returns Failure if the condition is false.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `isDynamic` | `bool` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `conditionFailReturn` | `CompactStatus` | `CompactStatus.Failure` | The status that will be returned if the assigned condition is or becomes false. | - |

## 使用备注

装饰节点通常包裹一个子节点，用于修改执行条件、结果或生命周期。
