# InvokeSignal

- 类名：`InvokeSignal`
- 节点类型：动作节点
- 分类：✫ Utility
- 基类：`ActionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Utility/InvokeSignal.cs`

## 作用

Invoke a defined Signal with agent as the target and optionally global.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `signalDefinition` | `BBParameter<SignalDefinition>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `global` | `bool` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
