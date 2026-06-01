# Override Agent

- 类名：`Setter`
- 节点类型：装饰节点
- 分类：Decorators
- 基类：`BTDecorator`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Decorators/Setter.cs`

## 作用

Set another Agent for the rest of the Tree dynamicaly from this point and on. All nodes under this will be executed with the new agent. You can also use this decorator to revert back to the original graph agent.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `revertToOriginal` | `bool` | - | If enabled, will revert back to the original graph agent. | - |
| `newAgent` | `BBParameter<GameObject>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |

## 使用备注

装饰节点通常包裹一个子节点，用于修改执行条件、结果或生命周期。
