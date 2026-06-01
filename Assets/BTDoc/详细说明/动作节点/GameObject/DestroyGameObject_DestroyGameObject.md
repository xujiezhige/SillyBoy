# DestroyGameObject

- 类名：`DestroyGameObject`
- 节点类型：动作节点
- 分类：GameObject
- 基类：`ActionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/GameObject/DestroyGameObject.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `immediately` | `bool` | - | DestroyImmediately is recomended if you are destroying objects in use of the framework. | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
