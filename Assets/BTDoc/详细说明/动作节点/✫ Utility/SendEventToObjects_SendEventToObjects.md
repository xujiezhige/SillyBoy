# SendEventToObjects

- 类名：`SendEventToObjects`
- 节点类型：动作节点
- 分类：✫ Utility
- 基类：`ActionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Utility/SendEventToObjects.cs`

## 作用

Send a Graph Event to multiple gameobjects which should have a GraphOwner component attached.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `targetObjects` | `BBParameter<List<GameObject>>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `eventName` | `BBParameter<string>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `targetObjects` | `BBParameter<List<GameObject>>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `eventName` | `BBParameter<string>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `eventValue` | `BBParameter<T>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
