# DebugLogVariable

- 类名：`DebugLogVariable`
- 节点类型：动作节点
- 分类：✫ Utility
- 基类：`ActionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Utility/DebugLogVariable.cs`

## 作用

Logs the value of a variable in the console

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `log` | `BBParameter<object>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `prefix` | `BBParameter<string>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `secondsToRun` | `float` | `1f` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `finishStatus` | `CompactStatus` | `CompactStatus.Success` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
