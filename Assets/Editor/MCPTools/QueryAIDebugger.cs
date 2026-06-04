using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using SurvivalEngine.Debugging;
using UnityEditor;
using UnityEngine;

namespace SillyBoy.Editor.MCPTools
{
    [McpForUnityTool(
        "query_ai_debugger",
        Description = "Query or control the runtime GameStateDebugger for AI behavior-tree debugging."
    )]
    public static class QueryAIDebugger
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString() ?? "report";
            int count = @params["count"]?.ToObject<int?>() ?? 80;

            try
            {
                if (!Application.isPlaying)
                {
                    return new ErrorResponse(
                        "Unity is not in Play Mode.",
                        new
                        {
                            action,
                            hint = "Enter Play Mode before querying runtime player, blackboard, or behavior-tree state."
                        });
                }

                var debugger = GameStateDebugger.GetOrCreate();

                switch (action.ToLowerInvariant())
                {
                    case "start":
                    case "enable":
                        debugger.enabled = true;
                        return new SuccessResponse("AI debugger enabled.", debugger.GetReport(20, 10));

                    case "stop":
                    case "disable":
                        debugger.enabled = false;
                        return new SuccessResponse("AI debugger disabled.", new { enabled = debugger.enabled });

                    case "clear":
                        debugger.Clear();
                        return new SuccessResponse("AI debugger history cleared.", new { cleared = true });

                    case "snapshot":
                        return new SuccessResponse(
                            "AI debugger snapshot captured.",
                            debugger.CaptureSample("mcp_snapshot").ToDictionary());

                    case "events":
                        return new SuccessResponse(
                            "AI debugger recent events.",
                            new { events = debugger.GetRecentEvents(count) });

                    case "samples":
                        return new SuccessResponse(
                            "AI debugger recent samples.",
                            new { samples = debugger.GetRecentSamples(count) });

                    case "report":
                    default:
                        return new SuccessResponse(
                            "AI debugger report.",
                            debugger.GetReport(count, Math.Max(10, count / 2)));
                }
            }
            catch (Exception e)
            {
                return new ErrorResponse(
                    "AI debugger query error.",
                    new
                    {
                        action,
                        exception_type = e.GetType().Name,
                        message = e.Message
                    });
            }
        }
    }
}
