# GetOverlapSphereObjects

- 类名：`GetOverlapSphereObjects`
- 节点类型：动作节点
- 分类：Physics
- 基类：`ActionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Physics/GetOverlapSphereObjects.cs`

## 作用

Gets a lists of game objects that are in the physics overlap sphere at the position of the agent, excluding the agent

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `layerMask` | `LayerMask` | `-1` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `radius` | `BBParameter<float>` | `2` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `saveObjectsAs` | `BBParameter<List<GameObject>>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
