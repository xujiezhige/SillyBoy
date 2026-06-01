# GetLinecastInfo2DAll

- 类名：`GetLinecastInfo2DAll`
- 节点类型：动作节点
- 分类：Physics
- 基类：`ActionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Physics/GetLinecastInfo2DAll.cs`

## 作用

Get hit info for ALL objects in the linecast, in Lists

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `target` | `BBParameter<GameObject>` | - | 目标对象、位置或变量。 | RequiredField |
| `mask` | `LayerMask` | `-1` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `saveHitGameObjectsAs` | `BBParameter<List<GameObject>>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `saveDistancesAs` | `BBParameter<List<float>>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `savePointsAs` | `BBParameter<List<Vector3>>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `saveNormalsAs` | `BBParameter<List<Vector3>>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
