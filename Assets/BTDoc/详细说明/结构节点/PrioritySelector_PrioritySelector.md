# PrioritySelector

- 类名：`PrioritySelector`
- 节点类型：结构节点
- 分类：Composites
- 基类：`BTComposite`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Composites/PrioritySelector.cs`

## 作用

Used for Utility AI, the Priority Selector executes the child with the highest utility weight. If it fails, the Priority Selector will continue with the next highest utility weight child until one Succeeds, or until all Fail (similar to how a normal Selector does).\n\nEach child branch represents a desire, where each desire has one or more consideration which are all averaged.\nConsiderations are a pair of input value and curve, which together produce the consideration utility weight.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `name` | `string` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `foldout` | `bool` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `considerations` | `List<Consideration>` | `new List<Consideration>()` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `input` | `BBParameter<float>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `function` | `BBParameter<AnimationCurve>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `utility` | `float` | `> function.value != null ? function.value.Evaluate(input.value) : input.value` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `desires` | `List<Desire>` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

结构节点负责组织和调度子节点，通常拥有多个子连接。
