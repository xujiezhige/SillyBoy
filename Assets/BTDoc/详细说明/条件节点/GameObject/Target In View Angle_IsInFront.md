# Target In View Angle

- 类名：`IsInFront`
- 节点类型：条件节点
- 分类：GameObject
- 基类：`ConditionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/GameObject/IsInFront.cs`

## 作用

Checks whether the target is in the view angle of the agent

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `checkTarget` | `BBParameter<GameObject>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `viewAngle` | `BBParameter<float>` | `70f` | 黑板参数，可绑定变量或直接填写常量。 | - |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
