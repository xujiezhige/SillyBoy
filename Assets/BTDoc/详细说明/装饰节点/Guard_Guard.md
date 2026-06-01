# Guard

- 类名：`Guard`
- 节点类型：装饰节点
- 分类：Decorators
- 基类：`BTDecorator`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Decorators/Guard.cs`

## 作用

Protects the decorated child from running if another Guard with the same token is already guarding (Running) that token.\nGuarding is global for all of the agent Behaviour Trees.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `token` | `BBParameter<string>` | - | A unique Token to use for guarding. | - |
| `ifGuarded` | `GuardMode` | `GuardMode.ReturnFailure` | What to return in case the token is already guarded by another Guard. | - |

## 使用备注

装饰节点通常包裹一个子节点，用于修改执行条件、结果或生命周期。
