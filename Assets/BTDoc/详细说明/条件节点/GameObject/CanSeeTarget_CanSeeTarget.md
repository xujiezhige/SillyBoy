# CanSeeTarget

- 类名：`CanSeeTarget`
- 节点类型：条件节点
- 分类：GameObject
- 基类：`ConditionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/GameObject/CanSeeTarget.cs`

## 作用

A combination of line of sight and view angle check

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `target` | `BBParameter<GameObject>` | - | 目标对象、位置或变量。 | RequiredField |
| `maxDistance` | `BBParameter<float>` | `50` | Distance within which to look out for. | - |
| `layerMask` | `BBParameter<LayerMask>` | `(LayerMask)( -1 )` | A layer mask to use for line of sight check. | - |
| `awarnessDistance` | `BBParameter<float>` | `0f` | Distance within which the target can be seen (or rather sensed) regardless of view angle. | - |
| `viewAngle` | `BBParameter<float>` | `70f` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `offset` | `Vector3` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
