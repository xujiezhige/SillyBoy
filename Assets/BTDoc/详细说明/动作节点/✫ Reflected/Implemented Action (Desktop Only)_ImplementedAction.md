# Implemented Action (Desktop Only)

- 类名：`ImplementedAction`
- 节点类型：动作节点
- 分类：✫ Reflected/Faster Versions (Desktop Platforms Only)
- 基类：`ActionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/ScriptControl/Standalone/ImplementedAction.cs`

## 作用

This version works in destop/JIT platform only.\n\nCalls a function that has signature of 'public Status NAME()' or 'public Status NAME(T)'. You should return Status.Success, Failure or Running within that function.

## 参数

无公开配置参数。

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
