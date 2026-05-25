using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// OpenAI 配置数据 - 用于在 Inspector 中配置 API 设置
    /// </summary>
    [CreateAssetMenu(fileName = "OpenAIConfig", menuName = "SurvivalEngine/OpenAI Config", order = 100)]
    public class OpenAIConfig : ScriptableObject
    {
        [Header("API Settings")]
        [Tooltip("OpenAI API Key - 从 https://platform.openai.com/api-keys 获取")]
        public string apiKey = "";
        
        [Tooltip("API 端点 URL")]
        public string apiUrl = "https://api.openai.com/v1/chat/completions";
        
        [Tooltip("使用的模型名称 (gpt-3.5-turbo, gpt-4, gpt-4-turbo 等)")]
        public string model = "gpt-3.5-turbo";
        
        [Header("Generation Settings")]
        [Tooltip("温度参数 (0-2)，值越高越随机")]
        [Range(0f, 2f)]
        public float temperature = 0.7f;
        
        [Tooltip("最大生成 token 数")]
        public int maxTokens = 500;
        
        [Tooltip("请求超时时间（秒）")]
        public float timeout = 10f;

        [Header("Alternative API")]
        [Tooltip("是否使用自定义 API 端点（如本地部署的模型）")]
        public bool useCustomEndpoint = false;
        
        [Tooltip("自定义 API 端点 URL（如果 useCustomEndpoint 为 true）")]
        public string customApiUrl = "";

        private static OpenAIConfig _instance;

        public static OpenAIConfig Get()
        {
            if (_instance == null)
            {
                _instance = Resources.Load<OpenAIConfig>("OpenAIConfig");
            }
            return _instance;
        }

        public string GetApiUrl()
        {
            return useCustomEndpoint && !string.IsNullOrEmpty(customApiUrl) ? customApiUrl : apiUrl;
        }
    }
}

