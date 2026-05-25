using System.Collections.Generic;
using UnityEngine;
using System;

namespace SurvivalEngine
{
    /// <summary>
    /// 基于大语言模型的文本指令解析器
    /// 使用 OpenAI API 将自然语言转换为游戏指令序列
    /// </summary>
    public class TextCommandParserLLM : MonoBehaviour
    {
        private static TextCommandParserLLM _instance;
        private Queue<PendingParse> parseQueue = new Queue<PendingParse>();
        private bool isProcessing = false;

        private class PendingParse
        {
            public string text;
            public PlayerCharacter character;
            public System.Action<List<GameCommand>> onSuccess;
            public System.Action<string> onError;
        }

        void Awake()
        {
            _instance = this;
        }

        public static TextCommandParserLLM Get()
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("TextCommandParserLLM");
                _instance = obj.AddComponent<TextCommandParserLLM>();
            }
            return _instance;
        }

        /// <summary>
        /// 异步解析文本为指令序列
        /// </summary>
        public void Parse(string text, PlayerCharacter character, System.Action<List<GameCommand>> onSuccess, System.Action<string> onError)
        {
            if (string.IsNullOrEmpty(text))
            {
                onSuccess?.Invoke(new List<GameCommand>());
                return;
            }

            // 如果 OpenAI 客户端不存在，使用本地备用解析器
            OpenAIClient client = OpenAIClient.Get();
            if (client == null)
            {
                Debug.LogWarning("OpenAIClient 未找到，使用本地备用解析器");
                List<GameCommand> commands = TextCommandParserLocal.Parse(text, character);
                onSuccess?.Invoke(commands);
                return;
            }

            // 调用 OpenAI API
            client.ParseCommand(text, character, onSuccess, onError);
        }

    }

    /// <summary>
    /// 本地备用解析器（当 OpenAI API 不可用时使用）
    /// </summary>
    public static class TextCommandParserLocal
    {
        /// <summary>
        /// 简单的本地解析器作为备用方案
        /// </summary>
        public static List<GameCommand> Parse(string text, PlayerCharacter character)
        {
            List<GameCommand> commands = new List<GameCommand>();
            
            if (string.IsNullOrEmpty(text))
                return commands;

            // 简单的关键词匹配作为备用
            text = text.ToLower().Trim();
            
            // 这里可以保留一些基本的本地解析逻辑作为备用
            // 或者直接返回空列表，强制使用 LLM
            
            return commands;
        }
    }
}

