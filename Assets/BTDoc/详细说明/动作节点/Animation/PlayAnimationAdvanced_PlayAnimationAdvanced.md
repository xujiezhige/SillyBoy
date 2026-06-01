# PlayAnimationAdvanced

- 类名：`PlayAnimationAdvanced`
- 节点类型：动作节点
- 分类：Animation
- 基类：`ActionTask<Animation>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Animation (Legacy)/PlayAnimationAdvanced.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `animationClip` | `BBParameter<AnimationClip>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `animationWrap` | `WrapMode` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `blendMode` | `AnimationBlendMode` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `playbackSpeed` | `float` | `1` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `crossFadeTime` | `float` | `0.25f` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `playDirection` | `PlayDirections` | `PlayDirections.Forward` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `mixTransformName` | `BBParameter<string>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `animationLayer` | `BBParameter<int>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |
| `queueAnimation` | `bool` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `waitActionFinish` | `bool` | `true` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
