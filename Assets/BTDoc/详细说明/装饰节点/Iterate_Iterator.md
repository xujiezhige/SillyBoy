# Iterate

- 类名：`Iterator`
- 节点类型：装饰节点
- 分类：Decorators
- 基类：`BTDecorator`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Decorators/Iterator.cs`

## 作用

Iterates a list and executes its child once for each element in that list. Keeps iterating until the Termination Policy is met or until the whole list is iterated, in which case the last iteration child status is returned.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `targetList` | `BBParameter<IList>` | - | The list to iterate. | RequiredField, BlackboardOnly |
| `current` | `BBObjectParameter` | - | Store the currently iterated list element in a variable. | BlackboardOnly |
| `storeIndex` | `BBParameter<int>` | - | Store the currently iterated list index in a variable. | BlackboardOnly |
| `terminationCondition` | `TerminationConditions` | `TerminationConditions.None` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |
| `maxIteration` | `BBParameter<int>` | `-1` | The maximum allowed iterations. Leave at -1 to iterate the whole list. | - |
| `resetIndex` | `bool` | `true` | Should the iteration start from the begining after the Iterator node resets? | - |

## 使用备注

装饰节点通常包裹一个子节点，用于修改执行条件、结果或生命周期。
