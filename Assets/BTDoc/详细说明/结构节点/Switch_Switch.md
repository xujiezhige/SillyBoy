# Switch

- 类名：`Switch`
- 节点类型：结构节点
- 分类：Composites
- 基类：`BTComposite`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Composites/Switch.cs`

## 作用

Executes one child based on the provided int or enum case and returns its status.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `dynamic` | `bool` | - | If true and the 'case' change while a child is running, that child will immediately be interrupted and the new child will be executed. | - |
| `selectionMode` | `CaseSelectionMode` | `CaseSelectionMode.IndexBased` | The selection mode used. | - |
| `intCase` | `BBParameter<int>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `outOfRangeMode` | `OutOfRangeMode` | `OutOfRangeMode.LoopIndex` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `enumCase` | `BBObjectParameter` | `new BBObjectParameter(typeof(System.Enum))` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

结构节点负责组织和调度子节点，通常拥有多个子连接。
