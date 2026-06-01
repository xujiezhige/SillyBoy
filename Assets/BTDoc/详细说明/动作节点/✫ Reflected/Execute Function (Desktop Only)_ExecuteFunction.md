# Execute Function (Desktop Only)

- 类名：`ExecuteFunction`
- 节点类型：动作节点
- 分类：✫ Reflected/Faster Versions (Desktop Platforms Only)
- 基类：`ActionTask`
- 源文件：`Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/ScriptControl/Standalone/ExecuteFunction.cs`

## 作用

This version works in destop/JIT platform only.\n\nExecute a function on a script, of up to 6 parameters and save the return if any. If function is an IEnumerator it will execute as a coroutine.

## 参数

无公开配置参数。

## 使用备注

动作节点通常作为行为树叶子节点执行，返回 Success/Failure，运行中可保持 Running。
