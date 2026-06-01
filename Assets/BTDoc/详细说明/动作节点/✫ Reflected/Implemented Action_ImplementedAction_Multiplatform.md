# Implemented Action

- 类名：`ImplementedAction_Multiplatform`
- 节点类型：动作节点
- 分类：✫ Reflected
- 基类：`ActionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/ScriptControl/ImplementedAction_Multiplatform.cs`

## 作用

Calls a function that has signature of 'public Status NAME()' or 'public Status NAME(T)'. You should return Status.Success, Failure or Running within that function.

## 参数

无公开配置参数。

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
