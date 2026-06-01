# Curve Tween

- 类名：`CurveTransformTween`
- 节点类型：动作节点
- 分类：Movement/Direct
- 基类：`ActionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Movement/Direct/CurveTransformTween.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `transformMode` | `TransformMode` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `mode` | `TweenMode` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `playMode` | `PlayMode` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `targetPosition` | `BBParameter<Vector3>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `curve` | `BBParameter<AnimationCurve>` | `AnimationCurve.EaseInOut(0, 0, 1, 1)` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `time` | `BBParameter<float>` | `0.5f` | 时间参数。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
