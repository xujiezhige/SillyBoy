# Target In Line Of Sight 2D

- 类名：`CheckLOS2D`
- 节点类型：条件节点
- 分类：GameObject
- 基类：`ConditionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/GameObject/CheckLOS2D.cs`

## 作用

Check of agent is in line of sight with target by doing a linecast and optionaly save the distance

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `LOSTarget` | `BBParameter<GameObject>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `layerMask` | `BBParameter<LayerMask>` | `(LayerMask)( -1 )` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `saveDistanceAs` | `BBParameter<float>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
