# InstantiateGameObject

- 类名：`InstantiateGameObject`
- 节点类型：动作节点
- 分类：GameObject
- 基类：`ActionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/GameObject/InstantiateGameObject.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `parent` | `BBParameter<Transform>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `clonePosition` | `BBParameter<Vector3>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `cloneRotation` | `BBParameter<Vector3>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `saveCloneAs` | `BBParameter<GameObject>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
