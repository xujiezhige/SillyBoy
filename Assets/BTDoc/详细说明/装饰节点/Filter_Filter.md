# Filter

- 类名：`Filter`
- 节点类型：装饰节点
- 分类：Decorators
- 基类：`BTDecorator`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Decorators/Filter.cs`

## 作用

Filters the access of its child either a specific number of times, or every specific amount of time.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `filterMode` | `FilterMode` | `FilterMode.CoolDown` | The mode to use. | - |
| `maxCount` | `BBParameter<int>` | `1` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `policy` | `Policy` | `Policy.SuccessOrFailure` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `coolDownTime` | `BBParameter<float>` | `5f` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `inactiveWhenLimited` | `bool` | `true` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

装饰节点通常包裹一个子节点，用于修改执行条件、结果或生命周期。
