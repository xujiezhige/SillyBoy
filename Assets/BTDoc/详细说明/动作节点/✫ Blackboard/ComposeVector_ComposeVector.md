# ComposeVector

- 类名：`ComposeVector`
- 节点类型：动作节点
- 分类：✫ Blackboard
- 基类：`ActionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Blackboard/ComposeVector.cs`

## 作用

Create a new Vector out of 3 floats and save it to the blackboard

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `x` | `BBParameter<float>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `y` | `BBParameter<float>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `z` | `BBParameter<float>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `saveAs` | `BBParameter<Vector3>` | - | 结果写入的黑板变量。 | BlackboardOnly |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
