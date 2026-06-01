# PlayAudioAtPosition

- 类名：`PlayAudioAtPosition`
- 节点类型：动作节点
- 分类：Audio
- 基类：`ActionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Audio/PlayAudioAtPosition.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `audioClip` | `BBParameter<AudioClip>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `volume` | `float` | `1` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `waitActionFinish` | `bool` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
