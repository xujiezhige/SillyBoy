# Set Parameter Float

- 类名：`MecanimSetFloat`
- 节点类型：动作节点
- 分类：Animator
- 基类：`ActionTask<Animator>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Animator/MecanimSetFloat.cs`

## 作用

You can either use a parameter name OR hashID. Leave the parameter name empty or none to use hashID instead.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `parameter` | `BBParameter<string>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `parameterHashID` | `BBParameter<int>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `setTo` | `BBParameter<float>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `transitTime` | `float` | `0.25f` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
