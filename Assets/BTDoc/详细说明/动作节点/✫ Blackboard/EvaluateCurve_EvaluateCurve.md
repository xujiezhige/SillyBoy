# EvaluateCurve

- 类名：`EvaluateCurve`
- 节点类型：动作节点
- 分类：✫ Blackboard
- 基类：`ActionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Blackboard/EvaluateCurve.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `curve` | `BBParameter<AnimationCurve>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `from` | `BBParameter<float>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `to` | `BBParameter<float>` | `1` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `time` | `BBParameter<float>` | `1` | 时间参数。 | - |
| `saveAs` | `BBParameter<float>` | - | 结果写入的黑板变量。 | BlackboardOnly |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
