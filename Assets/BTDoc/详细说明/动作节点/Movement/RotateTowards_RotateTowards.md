# RotateTowards

- 类名：`RotateTowards`
- 节点类型：动作节点
- 分类：Movement/Direct
- 基类：`ActionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Movement/Direct/RotateTowards.cs`

## 作用

Rotate the agent towards the target per frame

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `target` | `BBParameter<GameObject>` | - | 目标对象、位置或变量。 | RequiredField |
| `speed` | `BBParameter<float>` | `2` | 移动或变化速度。 | - |
| `angleDifference` | `BBParameter<float>` | `5` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `upVector` | `BBParameter<Vector3>` | `Vector3.up` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `waitActionFinish` | `bool` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
