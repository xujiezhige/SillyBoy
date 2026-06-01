# Repeat

- 类名：`Repeater`
- 节点类型：装饰节点
- 分类：Decorators
- 基类：`BTDecorator`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Decorators/Repeater.cs`

## 作用

Repeats the child either x times or until it returns the specified status, or forever.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `repeaterMode` | `RepeaterMode` | `RepeaterMode.RepeatTimes` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `repeatTimes` | `BBParameter<int>` | `1` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `repeatUntilStatus` | `RepeatUntilStatus` | `RepeatUntilStatus.Success` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

装饰节点通常包裹一个子节点，用于修改执行条件、结果或生命周期。
