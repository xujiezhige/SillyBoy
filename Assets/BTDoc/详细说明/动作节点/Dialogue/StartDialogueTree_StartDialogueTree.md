# StartDialogueTree

- 类名：`StartDialogueTree`
- 节点类型：动作节点
- 分类：Dialogue
- 基类：`ActionTask<IDialogueActor>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Dialogue/StartDialogueTree.cs`

## 作用

Starts the Dialogue Tree assigned on a Dialogue Tree Controller object with specified agent used for 'Instigator'.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `dialogueTreeController` | `BBParameter<DialogueTreeController>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `waitActionFinish` | `bool` | `true` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `isPrefab` | `bool` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
