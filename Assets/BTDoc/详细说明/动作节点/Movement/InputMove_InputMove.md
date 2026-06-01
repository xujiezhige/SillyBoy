# InputMove

- 类名：`InputMove`
- 节点类型：动作节点
- 分类：Movement/Direct
- 基类：`ActionTask<Transform>`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Movement/Direct/InputMove.cs`

## 作用

Move & turn the agent based on input values provided ranging from -1 to 1, per second (using delta time)

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `strafe` | `BBParameter<float>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `turn` | `BBParameter<float>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `forward` | `BBParameter<float>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `up` | `BBParameter<float>` | - | 仅允许绑定黑板变量，通常用于输出结果。 | BlackboardOnly |
| `moveSpeed` | `BBParameter<float>` | `1` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `rotationSpeed` | `BBParameter<float>` | `1` | 黑板参数，可绑定变量或直接填写常量。 | - |
| `repeat` | `bool` | - | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
