# ShoutEvent

- 类名：`ShoutEvent`
- 节点类型：动作节点
- 分类：✫ Utility
- 基类：`ActionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Utility/ShoutEvent.cs`

## 作用

Sends an event to all GraphOwners within range of the agent and over time like a shockwave.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `eventName` | `BBParameter<string>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `shoutRange` | `BBParameter<float>` | `10` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `completionTime` | `BBParameter<float>` | `1` | 黑板参数，可绑定变量或直接填写常量。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
