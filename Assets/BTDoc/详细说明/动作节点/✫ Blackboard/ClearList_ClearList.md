# ClearList

- 类名：`ClearList`
- 节点类型：动作节点
- 分类：✫ Blackboard/Lists
- 基类：`ActionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Blackboard/List Specific/ClearList.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `targetList` | `BBParameter<IList>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | RequiredField, BlackboardOnly |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
