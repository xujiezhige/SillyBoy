# GetCloserGameObjectInList

- 类名：`GetCloserGameObjectInList`
- 节点类型：动作节点
- 分类：✫ Blackboard/Lists
- 基类：`ActionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Blackboard/List Specific/GetCloserGameObjectInList.cs`

## 作用

Get the closer game object to the agent from within a list of game objects and save it in the blackboard.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `list` | `BBParameter<List<GameObject>>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `saveAs` | `BBParameter<GameObject>` | - | 结果写入的黑板变量。 | BlackboardOnly |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
