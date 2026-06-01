# SendMessage

- 类名：`SendMessage`
- 节点类型：动作节点
- 分类：✫ Reflected
- 基类：`ActionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/ScriptControl/SendMessage.cs`

## 作用

SendMessage to the agent, optionaly with an argument

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `methodName` | `BBParameter<string>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `methodName` | `BBParameter<string>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `argument` | `BBParameter<T>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
