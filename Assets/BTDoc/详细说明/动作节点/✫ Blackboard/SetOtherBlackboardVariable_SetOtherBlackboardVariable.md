# SetOtherBlackboardVariable

- 类名：`SetOtherBlackboardVariable`
- 节点类型：动作节点
- 分类：✫ Blackboard
- 基类：`ActionTask<Blackboard>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Blackboard/SetOtherBlackboardVariable.cs`

## 作用

Use this to set a variable on any blackboard by overriding the agent

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `targetVariableName` | `BBParameter<string>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `newValue` | `BBObjectParameter` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
