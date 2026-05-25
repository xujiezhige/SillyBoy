# 文本指令系统使用说明

## 功能概述

文本指令系统允许玩家通过输入自然语言文本来控制游戏角色执行各种动作。系统会自动将文本解析为游戏指令序列并执行。

## 系统组件

1. **TextCommandSystem.cs** - 指令执行系统（单例）
2. **TextCommandParser.cs** - 文本解析器（静态类）
3. **TextCommandPanel.cs** - UI输入面板

## 安装步骤

### 1. 添加 TextCommandSystem 组件

在场景中添加一个空的 GameObject，命名为 "TextCommandSystem"，并添加 `TextCommandSystem` 组件。

### 2. 创建 UI 面板

1. 在 Canvas 下创建一个新的 Panel，命名为 "TextCommandPanel"
2. 添加 `TextCommandPanel` 组件
3. 设置以下 UI 元素：
   - **InputField**: 文本输入框
   - **SubmitButton**: 提交按钮
   - **CloseButton**: 关闭按钮
   - **FeedbackText**: 反馈文本（可选）
   - **CommandHistoryScroll**: 历史记录滚动视图（可选）
   - **HistoryItemPrefab**: 历史记录项预制体（可选）
   - **HistoryContent**: 历史记录内容容器（可选）

### 3. 配置快捷键

在 `TextCommandPanel` 组件中设置 `toggleKey`（默认 T 键）来打开/关闭面板。

## 支持的指令

### 移动指令

- **移动到坐标**: `移动到 (10, 0, 5)` 或 `去 (10, 5)`
- **移动到对象**: `去 箱子` 或 `移动到 树`
- **方向移动**: `向前走 5米` 或 `向左移动`

### 交互指令

- **交互**: `使用 箱子` 或 `打开 门`
- **拾取**: `拾取 苹果` 或 `捡 物品`
- **攻击**: `攻击 敌人` 或 `打 树`

### 动作指令

- **跳跃**: `跳跃` 或 `跳`
- **等待**: `等待 3秒` 或 `wait 2`
- **停止**: `停止`
- **面向**: `面向 敌人` 或 `face (10, 0, 5)`

### 物品指令

- **丢弃**: `丢弃 (5, 0, 3)` - 在指定位置丢弃物品
- **制作**: `制作 木剑` 或 `craft 工具`
- **吃**: `吃 苹果` 或 `eat 食物`
- **喝**: `喝 水` 或 `drink 饮料`

### 其他指令

- **睡觉**: `睡觉` 或 `sleep` - 自动查找最近的床
- **建造**: `建造 房子 (10, 0, 5)` 或 `build 墙`

## 使用示例

### 单个指令

```
移动到 (10, 0, 5)
拾取 苹果
攻击 敌人
```

### 多个指令（用分号或换行分隔）

```
去 箱子; 打开 箱子; 拾取 物品
```

或者：

```
去 箱子
打开 箱子
拾取 物品
```

### 复杂指令序列

```
移动到 (10, 0, 5)
等待 2秒
面向 敌人
攻击 敌人
```

## 编程接口

### 通过代码执行指令

```csharp
// 获取系统实例
TextCommandSystem system = TextCommandSystem.Get();

// 解析并执行指令
PlayerCharacter character = PlayerCharacter.GetFirst();
system.ParseAndExecute("移动到 (10, 0, 5)", character);

// 停止执行
system.StopExecution();

// 检查是否正在执行
bool isExecuting = system.IsExecuting();
```

### 直接解析指令（不执行）

```csharp
PlayerCharacter character = PlayerCharacter.GetFirst();
List<GameCommand> commands = TextCommandParser.Parse("移动到 (10, 0, 5)", character);
```

## 注意事项

1. **对象查找**: 系统会根据对象名称（GameObject.name）进行模糊匹配，找到最近的可交互对象
2. **指令队列**: 多个指令会按顺序执行，前一个指令完成后才会执行下一个
3. **错误处理**: 如果无法解析指令，系统会在控制台输出警告信息
4. **中文和英文**: 系统同时支持中文和英文指令
5. **坐标格式**: 坐标可以使用 `(x, z)` 或 `(x, y, z)` 格式

## 扩展指令

要添加新的指令类型：

1. 在 `CommandType` 枚举中添加新类型
2. 在 `TextCommandParser` 中添加关键词映射
3. 在 `TextCommandSystem` 中添加对应的执行方法
4. 在 `ExecuteCommand` 方法中添加 case 分支

## 故障排除

- **面板无法打开**: 检查是否添加了 `TextCommandSystem` 组件到场景中
- **指令无法执行**: 检查控制台是否有错误信息，确认对象名称是否正确
- **角色不移动**: 确认角色没有被其他系统控制，检查 `IsControlsEnabled()` 状态

