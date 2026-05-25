# OpenAI 文本指令解析系统使用说明

## 概述

本系统使用 OpenAI 等大语言模型来解析玩家的自然语言指令，将其转换为游戏行为指令序列。相比基于关键词匹配的解析器，LLM 解析器能够更好地理解复杂的自然语言表达。

## 安装和配置

### 1. 获取 OpenAI API Key

1. 访问 [OpenAI Platform](https://platform.openai.com/)
2. 注册/登录账号
3. 前往 [API Keys](https://platform.openai.com/api-keys) 页面
4. 创建新的 API Key
5. 复制 API Key（注意：只显示一次，请妥善保存）

### 2. 配置方式一：使用 ScriptableObject 配置（推荐）

1. 在 Unity 编辑器中，右键点击 Project 窗口
2. 选择 `Create > SurvivalEngine > OpenAI Config`
3. 将创建的配置文件放在 `Resources` 文件夹中（命名为 `OpenAIConfig`）
4. 在 Inspector 中配置：
   - **API Key**: 输入你的 OpenAI API Key
   - **Model**: 选择模型（推荐 `gpt-3.5-turbo` 或 `gpt-4`）
   - **Temperature**: 控制随机性（0-2，推荐 0.7）
   - **Max Tokens**: 最大生成 token 数（推荐 500）
   - **Timeout**: 请求超时时间（秒）

### 3. 配置方式二：直接在组件中配置

1. 在场景中找到或创建 `OpenAIClient` GameObject
2. 添加 `OpenAIClient` 组件
3. 在 Inspector 中直接设置 API Key 和其他参数

### 4. 设置场景对象

1. **OpenAIClient**: 在场景中添加一个 GameObject，命名为 "OpenAIClient"，添加 `OpenAIClient` 组件
2. **TextCommandParserLLM**: 系统会自动创建，无需手动添加
3. **TextCommandSystem**: 确保场景中有此组件（如果还没有）

## 支持的模型

### OpenAI 官方模型
- `gpt-3.5-turbo` - 推荐，性价比高
- `gpt-4` - 更准确，但更昂贵
- `gpt-4-turbo` - 最新版本，性能更好

### 自定义端点
系统支持使用自定义 API 端点，可以连接：
- 本地部署的模型（如 Ollama、LocalAI）
- 其他兼容 OpenAI API 格式的服务
- 代理服务器

在 `OpenAIConfig` 中：
1. 勾选 `Use Custom Endpoint`
2. 设置 `Custom API Url` 为你的端点地址

## 使用示例

### 基本指令

```
移动到坐标 (10, 0, 5)
去箱子那里
拾取苹果
攻击最近的敌人
```

### 复杂指令

```
先去箱子那里，打开它，然后拾取里面的物品，最后回到起始位置
找到最近的树，砍倒它，收集木材，然后制作一把木剑
```

### 多步骤指令

```
移动到 (10, 0, 5); 等待 2秒; 面向敌人; 攻击
```

## API 调用流程

1. 玩家输入文本指令
2. `TextCommandPanel` 接收输入
3. `TextCommandSystem` 调用 `TextCommandParserLLM`
4. `TextCommandParserLLM` 调用 `OpenAIClient`
5. `OpenAIClient` 发送 HTTP 请求到 OpenAI API
6. 接收 JSON 响应并解析
7. 转换为 `GameCommand` 对象列表
8. 执行指令序列

## Prompt 设计

系统使用精心设计的 prompt 来指导 LLM 生成正确的指令格式：

- **系统角色**: 定义 LLM 为游戏指令解析器
- **指令类型**: 列出所有可用的指令类型
- **JSON 格式**: 明确指定返回格式
- **示例**: 提供格式示例
- **注意事项**: 说明各字段的用途

## 错误处理

系统包含完善的错误处理：

- **API Key 未设置**: 提示用户配置 API Key
- **网络错误**: 显示友好的错误信息
- **API 错误**: 根据 HTTP 状态码提供具体错误信息
  - 401: API Key 无效
  - 429: 请求频率过高
  - 500+: 服务器错误
- **解析错误**: 记录详细错误信息到控制台

## 成本优化建议

1. **使用 gpt-3.5-turbo**: 对于游戏指令解析，3.5 模型通常足够，成本更低
2. **限制 max_tokens**: 设置为 500 通常足够
3. **缓存常见指令**: 对于频繁使用的指令，可以考虑本地缓存
4. **批量处理**: 如果可能，将多个指令合并为一次 API 调用

## 本地备用方案

如果 OpenAI API 不可用，系统会：
1. 尝试使用本地备用解析器（`TextCommandParserLocal`）
2. 显示警告信息
3. 返回空指令列表

## 安全注意事项

⚠️ **重要**: API Key 是敏感信息，请勿：

- 将 API Key 提交到版本控制系统（Git）
- 在公开场合分享 API Key
- 在客户端代码中硬编码 API Key（如果发布游戏）

**建议**:
- 使用 `.gitignore` 排除配置文件
- 考虑使用服务器端代理来保护 API Key
- 对于发布版本，使用服务器端 API 调用

## 调试

### 查看 API 请求和响应

在 `OpenAIClient.cs` 中可以添加日志：

```csharp
Debug.Log($"发送请求: {jsonData}");
Debug.Log($"收到响应: {responseText}");
```

### 测试 API 连接

可以在 Unity Console 中测试：

```csharp
OpenAIClient client = OpenAIClient.Get();
client.ParseCommand("移动到 (10, 0, 5)", 
    PlayerCharacter.GetFirst(),
    (commands) => Debug.Log($"成功: {commands.Count} 个指令"),
    (error) => Debug.LogError($"失败: {error}"));
```

## 常见问题

**Q: API 调用很慢怎么办？**
A: 可以调整 `timeout` 参数，或使用更快的模型（gpt-3.5-turbo）。

**Q: 如何支持其他 LLM 服务？**
A: 修改 `OpenAIClient` 中的 API URL 和请求格式，或创建新的客户端类。

**Q: 可以离线使用吗？**
A: 需要本地部署兼容 OpenAI API 格式的模型服务，然后使用自定义端点。

**Q: 如何提高解析准确性？**
A: 
- 使用更强大的模型（gpt-4）
- 优化 prompt 设计
- 在 prompt 中添加游戏世界的上下文信息

## 扩展开发

### 添加新的指令类型

1. 在 `CommandType` 枚举中添加新类型
2. 在 `OpenAIClient.BuildSystemPrompt()` 中添加说明
3. 在 `TextCommandSystem` 中添加执行逻辑

### 优化 Prompt

修改 `OpenAIClient.BuildSystemPrompt()` 方法，可以：
- 添加游戏世界上下文
- 提供更多示例
- 调整指令描述

### 支持流式响应

可以修改 `OpenAIClient` 来支持流式响应，实现实时显示解析进度。

