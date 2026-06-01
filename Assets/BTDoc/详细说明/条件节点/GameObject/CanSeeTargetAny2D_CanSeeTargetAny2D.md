# CanSeeTargetAny2D

- 类名：`CanSeeTargetAny2D`
- 节点类型：条件节点
- 分类：GameObject
- 基类：`ConditionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/GameObject/CanSeeTargetAny2D.cs`

## 作用

A combination of line of sight and view angle check

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `targetObjects` | `BBParameter<List<GameObject>>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `maxDistance` | `BBParameter<float>` | `50` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `layerMask` | `BBParameter<LayerMask>` | `(LayerMask)( -1 )` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `awarnessDistance` | `BBParameter<float>` | `0f` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `viewAngle` | `BBParameter<float>` | `70f` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `offset` | `Vector2` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `allResults` | `BBParameter<List<GameObject>>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `closerResult` | `BBParameter<GameObject>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
