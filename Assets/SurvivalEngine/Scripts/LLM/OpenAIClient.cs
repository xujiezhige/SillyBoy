using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;
using System.Linq;

namespace SurvivalEngine
{
    /// <summary>
    /// OpenAI API 客户端 - 用于调用 OpenAI API
    /// </summary>
    public class OpenAIClient : MonoBehaviour
    {
        [Header("API Configuration")]
        [Tooltip("如果设置了 OpenAIConfig，将优先使用配置文件中的设置")]
        public OpenAIConfig config;
        
        [Header("Direct Settings (如果未设置 config)")]
        public string apiKey = ""; // 在 Inspector 中设置，或通过代码设置
        public string apiUrl = "https://api.openai.com/v1/chat/completions";
        public string model = "gpt-3.5-turbo"; // 或 "gpt-4", "gpt-4-turbo" 等
        
        [Header("Settings")]
        public float temperature = 0.7f;
        public int maxTokens = 500;
        public float timeout = 10f;

        private static OpenAIClient _instance;

        void Awake()
        {
            _instance = this;
        }

        public static OpenAIClient Get()
        {
            if (_instance == null)
                _instance = FindObjectOfType<OpenAIClient>();
            return _instance;
        }

        /// <summary>
        /// 调用 OpenAI API 解析文本指令
        /// </summary>
        public void ParseCommand(string userText, PlayerCharacter character, System.Action<List<GameCommand>> onSuccess, System.Action<string> onError)
        {
            StartCoroutine(CallOpenAIAPI(userText, character, onSuccess, onError));
        }

        private IEnumerator CallOpenAIAPI(string userText, PlayerCharacter character, System.Action<List<GameCommand>> onSuccess, System.Action<string> onError)
        {
            // 优先使用配置文件
            string finalApiKey = apiKey;
            string finalApiUrl = apiUrl;
            string finalModel = model;
            float finalTemperature = temperature;
            int finalMaxTokens = maxTokens;
            float finalTimeout = timeout;

            if (config != null)
            {
                if (!string.IsNullOrEmpty(config.apiKey))
                    finalApiKey = config.apiKey;
                finalApiUrl = config.GetApiUrl();
                finalModel = config.model;
                finalTemperature = config.temperature;
                finalMaxTokens = config.maxTokens;
                finalTimeout = config.timeout;
            }

            if (string.IsNullOrEmpty(finalApiKey))
            {
                onError?.Invoke("API Key 未设置，请在 OpenAIClient 组件或 OpenAIConfig 中配置");
                yield break;
            }

            // 构建 prompt
            string systemPrompt = BuildSystemPrompt(character);
            string userPrompt = userText;

            // 构建请求数据
            ChatRequest requestData = new ChatRequest
            {
                model = finalModel,
                messages = new List<ChatMessage>
                {
                    new ChatMessage { role = "system", content = systemPrompt },
                    new ChatMessage { role = "user", content = userPrompt }
                },
                temperature = finalTemperature,
                max_tokens = finalMaxTokens,
                response_format = new ResponseFormat { type = "json_object" }
            };

            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            // 创建请求
            UnityWebRequest request = new UnityWebRequest(finalApiUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + finalApiKey);
            request.timeout = (int)finalTimeout;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                try
                {
                    ChatResponse response = JsonUtility.FromJson<ChatResponse>(responseText);
                    if (response != null && response.choices != null && response.choices.Count > 0)
                    {
                        string content = response.choices[0].message.content;
                        List<GameCommand> commands = ParseJSONResponse(content, character);
                        onSuccess?.Invoke(commands);
                    }
                    else
                    {
                        onError?.Invoke("API 响应格式错误");
                    }
                }
                catch (Exception e)
                {
                    onError?.Invoke($"解析响应失败: {e.Message}");
                }
            }
            else
            {
                string errorMsg = $"API 请求失败: {request.error}";
                if (request.responseCode == 401)
                    errorMsg = "API Key 无效或未授权";
                else if (request.responseCode == 429)
                    errorMsg = "API 请求频率过高，请稍后重试";
                else if (request.responseCode >= 500)
                    errorMsg = "OpenAI 服务器错误，请稍后重试";
                
                onError?.Invoke(errorMsg);
            }

            request.Dispose();
        }

        /// <summary>
        /// 构建系统提示词
        /// </summary>
        private string BuildSystemPrompt(PlayerCharacter character)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("你是一个游戏指令解析器，负责将玩家的自然语言指令转换为游戏行为指令。");
            sb.AppendLine();
            sb.AppendLine("可用的指令类型：");
            sb.AppendLine("- Move: 移动到指定坐标 (x, y, z)");
            sb.AppendLine("- MoveToObject: 移动到指定对象");
            sb.AppendLine("- Interact: 与对象交互");
            sb.AppendLine("- Attack: 攻击目标");
            sb.AppendLine("- Pickup: 拾取物品");
            sb.AppendLine("- Use: 使用物品或对象");
            sb.AppendLine("- Jump: 跳跃");
            sb.AppendLine("- Wait: 等待指定秒数");
            sb.AppendLine("- Stop: 停止当前动作");
            sb.AppendLine("- Face: 面向目标或位置");
            sb.AppendLine("- Drop: 在指定位置丢弃物品");
            sb.AppendLine("- Craft: 制作物品");
            sb.AppendLine("- Eat: 吃物品");
            sb.AppendLine("- Drink: 喝物品");
            sb.AppendLine("- Sleep: 睡觉");
            sb.AppendLine("- Build: 建造建筑");
            sb.AppendLine();
            sb.AppendLine("请以 JSON 格式返回指令数组，格式如下：");
            sb.AppendLine("{");
            sb.AppendLine("  \"commands\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"type\": \"Move\",");
            sb.AppendLine("      \"position\": {\"x\": 10, \"y\": 0, \"z\": 5},");
            sb.AppendLine("      \"objectName\": \"\",");
            sb.AppendLine("      \"waitTime\": 0,");
            sb.AppendLine("      \"itemName\": \"\",");
            sb.AppendLine("      \"quantity\": 1");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("注意：");
            sb.AppendLine("1. position 字段只在需要坐标时使用，格式为 {\"x\": 数字, \"y\": 数字, \"z\": 数字}");
            sb.AppendLine("2. objectName 用于指定对象名称（如\"箱子\"、\"树\"等）");
            sb.AppendLine("3. itemName 用于指定物品名称（如\"苹果\"、\"木剑\"等）");
            sb.AppendLine("4. waitTime 用于 Wait 指令，单位为秒");
            sb.AppendLine("5. quantity 用于 Craft 指令，表示制作数量");
            sb.AppendLine("6. 如果玩家指令包含多个动作，请拆分为多个指令");
            sb.AppendLine("7. 如果无法理解指令，返回空数组");
            
            return sb.ToString();
        }

        /// <summary>
        /// 解析 JSON 响应
        /// </summary>
        private List<GameCommand> ParseJSONResponse(string jsonContent, PlayerCharacter character)
        {
            List<GameCommand> commands = new List<GameCommand>();

            try
            {
                // 清理 JSON 字符串（移除可能的 markdown 代码块标记）
                jsonContent = jsonContent.Trim();
                if (jsonContent.StartsWith("```json"))
                {
                    jsonContent = jsonContent.Substring(7);
                }
                if (jsonContent.StartsWith("```"))
                {
                    jsonContent = jsonContent.Substring(3);
                }
                if (jsonContent.EndsWith("```"))
                {
                    jsonContent = jsonContent.Substring(0, jsonContent.Length - 3);
                }
                jsonContent = jsonContent.Trim();

                // 解析 JSON
                CommandResponse response = JsonUtility.FromJson<CommandResponse>(jsonContent);
                
                if (response != null && response.commands != null)
                {
                    foreach (var cmdData in response.commands)
                    {
                        GameCommand cmd = ConvertToGameCommand(cmdData, character);
                        if (cmd != null)
                            commands.Add(cmd);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"解析 JSON 响应失败: {e.Message}\n内容: {jsonContent}");
            }

            return commands;
        }

        /// <summary>
        /// 转换 JSON 数据为 GameCommand
        /// </summary>
        private GameCommand ConvertToGameCommand(CommandData cmdData, PlayerCharacter character)
        {
            GameCommand cmd = new GameCommand(ParseCommandType(cmdData.type));
            
            // 设置位置
            if (cmdData.position != null)
            {
                cmd.position = new Vector3(cmdData.position.x, cmdData.position.y, cmdData.position.z);
            }

            // 设置对象名称并查找对象
            if (!string.IsNullOrEmpty(cmdData.objectName))
            {
                cmd.objectName = cmdData.objectName;
                cmd.targetObject = FindObjectByName(cmdData.objectName, character);
            }

            // 设置其他参数
            cmd.waitTime = cmdData.waitTime;
            cmd.itemName = cmdData.itemName;
            cmd.quantity = cmdData.quantity > 0 ? cmdData.quantity : 1;

            return cmd;
        }

        /// <summary>
        /// 解析指令类型字符串
        /// </summary>
        private CommandType ParseCommandType(string typeStr)
        {
            if (Enum.TryParse<CommandType>(typeStr, true, out CommandType result))
            {
                return result;
            }
            return CommandType.Stop; // 默认值
        }

        /// <summary>
        /// 根据名称查找对象
        /// </summary>
        private Selectable FindObjectByName(string name, PlayerCharacter character)
        {
            if (string.IsNullOrEmpty(name) || character == null)
                return null;

            name = name.Trim().ToLower();
            Selectable nearest = null;
            float nearestDist = float.MaxValue;
            Vector3 playerPos = character.transform.position;

            foreach (Selectable selectable in Selectable.GetAllActive())
            {
                if (selectable == null || !selectable.gameObject.activeSelf)
                    continue;

                string objName = selectable.gameObject.name.ToLower();
                
                if (objName == name || objName.Contains(name) || name.Contains(objName))
                {
                    float dist = Vector3.Distance(selectable.transform.position, playerPos);
                    if (dist < nearestDist)
                    {
                        nearest = selectable;
                        nearestDist = dist;
                    }
                }
            }

            return nearest;
        }

        // JSON 数据结构
        [System.Serializable]
        private class ChatRequest
        {
            public string model;
            public List<ChatMessage> messages;
            public float temperature;
            public int max_tokens;
            public ResponseFormat response_format;
        }

        [System.Serializable]
        private class ChatMessage
        {
            public string role;
            public string content;
        }

        [System.Serializable]
        private class ResponseFormat
        {
            public string type;
        }

        [System.Serializable]
        private class ChatResponse
        {
            public List<Choice> choices;
        }

        [System.Serializable]
        private class Choice
        {
            public ChatMessage message;
        }

        [System.Serializable]
        private class CommandResponse
        {
            public List<CommandData> commands;
        }

        [System.Serializable]
        private class CommandData
        {
            public string type;
            public PositionData position;
            public string objectName;
            public float waitTime;
            public string itemName;
            public int quantity;
        }

        [System.Serializable]
        private class PositionData
        {
            public float x;
            public float y;
            public float z;
        }
    }
}

