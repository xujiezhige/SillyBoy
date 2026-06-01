# SetEnum

- 类名：`SetEnum`
- 节点类型：动作节点
- 分类：✫ Blackboard
- 基类：`ActionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Blackboard/SetEnum.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `valueA` | `BBObjectParameter` | `new BBObjectParameter(typeof(System.Enum))` | 仅允许绑定黑板变量，通常用于输出结果。 | RequiredField, BlackboardOnly |
| `valueB` | `BBObjectParameter` | `new BBObjectParameter(typeof(System.Enum))` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
