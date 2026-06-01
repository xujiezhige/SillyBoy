# FindAllWithTag

- 类名：`FindAllWithTag`
- 节点类型：动作节点
- 分类：GameObject
- 基类：`ActionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/GameObject/FindAllWithTag.cs`

## 作用

Action will end in Failure if no objects are found

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `searchTag` | `BBParameter<string>` | `"Untagged"` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `saveAs` | `BBParameter<List<GameObject>>` | - | 结果写入的黑板变量。 | BlackboardOnly |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
