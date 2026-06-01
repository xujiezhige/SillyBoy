# Say

- 类名：`Say`
- 节点类型：动作节点
- 分类：Dialogue
- 基类：`ActionTask<IDialogueActor>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Dialogue/Say.cs`

## 作用

You can use a variable inline with the text by using brackets likeso: [myVarName] or [Global/myVarName].\nThe bracket will be replaced with the variable value ToString

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `statement` | `Statement` | `new Statement("This is a dialogue text...")` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
