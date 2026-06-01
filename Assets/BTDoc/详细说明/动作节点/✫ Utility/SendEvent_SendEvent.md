# SendEvent

- 类名：`SendEvent`
- 节点类型：动作节点
- 分类：✫ Utility
- 基类：`ActionTask<GraphOwner>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Utility/SendEvent.cs`

## 作用

Send a graph event. If global is true, all graph owners in scene will receive this event. Use along with the 'Check Event' Condition

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `eventName` | `BBParameter<string>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `delay` | `BBParameter<float>` | - | 延迟时间。 | - |
| `sendGlobal` | `bool` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `eventName` | `BBParameter<string>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `eventValue` | `BBParameter<T>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `delay` | `BBParameter<float>` | - | 延迟时间。 | - |
| `sendGlobal` | `bool` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
