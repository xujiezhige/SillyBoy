# Set Visibility

- 类名：`SetObjectVisibility`
- 节点类型：动作节点
- 分类：GameObject
- 基类：`ActionTask<Renderer>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/GameObject/SetObjectVisibility.cs`

## 作用

Set the Renderer active state, thus making the object visible or invisible.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `setTo` | `SetVisibleMode` | `SetVisibleMode.Toggle` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
