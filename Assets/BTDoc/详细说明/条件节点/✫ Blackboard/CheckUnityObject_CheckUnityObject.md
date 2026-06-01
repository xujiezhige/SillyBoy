# CheckUnityObject

- 类名：`CheckUnityObject`
- 节点类型：条件节点
- 分类：✫ Blackboard
- 基类：`ConditionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/Blackboard/CheckUnityObject.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `valueA` | `BBParameter<UnityEngine.Object>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `valueB` | `BBParameter<UnityEngine.Object>` | - | 黑板参数，可绑定变量或直接填写常量。 | - |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
