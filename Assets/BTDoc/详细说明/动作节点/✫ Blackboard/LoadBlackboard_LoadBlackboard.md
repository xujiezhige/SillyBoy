# LoadBlackboard

- 类名：`LoadBlackboard`
- 节点类型：动作节点
- 分类：✫ Blackboard
- 基类：`ActionTask<Blackboard>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Blackboard/LoadBlackboard.cs`

## 作用

Loads the blackboard variables previously saved in the provided PlayerPrefs key if at all. Returns false if no saves found or load was failed

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `saveKey` | `BBParameter<string>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
