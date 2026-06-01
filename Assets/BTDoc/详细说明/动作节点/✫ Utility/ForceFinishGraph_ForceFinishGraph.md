# ForceFinishGraph

- 类名：`ForceFinishGraph`
- 节点类型：动作节点
- 分类：✫ Utility
- 基类：`ActionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Utility/ForceFinishGraph.cs`

## 作用

Force Finish the current graph this Task is assigned to.

## 参数

| 参数 | 类型 | 默认值 | 作用 | 约束/标记 |
|---|---|---|---|---|
| `finishStatus` | `CompactStatus` | `CompactStatus.Success` | 源码未提供 Tooltip；请结合节点逻辑配置。 | - |

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
