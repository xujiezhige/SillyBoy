# Check Parameter Bool

- 类名：`MecanimCheckBool`
- 节点类型：条件节点
- 分类：Animator
- 基类：`ConditionTask<Animator>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Conditions/Animator/MecanimCheckBool.cs`

## 作用

源码未提供 Description。

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `parameter` | `BBParameter<string>` | - | 黑板参数，可绑定变量或直接填写常量。 | RequiredField |
| `value` | `BBParameter<bool>` | - | 要读取、比较或写入的值。 | - |

## 使用备注

条件节点用于判断条件，通常由 Condition Node 或装饰器引用，结果为 true/false。
