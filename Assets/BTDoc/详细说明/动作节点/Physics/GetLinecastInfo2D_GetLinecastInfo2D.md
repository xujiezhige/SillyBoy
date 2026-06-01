# GetLinecastInfo2D

- 类名：`GetLinecastInfo2D`
- 节点类型：动作节点
- 分类：Physics
- 基类：`ActionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Physics/GetLinecastInfo2D.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `target` | `BBParameter<GameObject>` | - | 目标对象、位置或变量。 | RequiredField |
| `mask` | `LayerMask` | `-1` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `saveHitGameObjectAs` | `BBParameter<GameObject>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `saveDistanceAs` | `BBParameter<float>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `savePointAs` | `BBParameter<Vector3>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `saveNormalAs` | `BBParameter<Vector3>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
