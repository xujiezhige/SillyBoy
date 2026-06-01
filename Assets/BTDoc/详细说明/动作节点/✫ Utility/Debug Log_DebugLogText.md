# Debug Log

- 类名：`DebugLogText`
- 节点类型：动作节点
- 分类：✫ Utility
- 基类：`ActionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Utility/DebugLogText.cs`

## 作用

Display a UI label on the agent's position if seconds to run is not 0 and also logs the message, which can also be mapped to any variable.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `log` | `BBParameter<string>` | `"Hello World"` | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `labelYOffset` | `float` | `0` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `secondsToRun` | `float` | `1f` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `verboseMode` | `VerboseMode` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `logMode` | `LogMode` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `finishStatus` | `CompactStatus` | `CompactStatus.Success` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
