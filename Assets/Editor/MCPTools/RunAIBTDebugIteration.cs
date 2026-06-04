using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using SurvivalEngine.Debugging;
using UnityEngine;

namespace SillyBoy.Editor.MCPTools
{
    [McpForUnityTool(
        "run_ai_bt_debug_iteration",
        Description = "Run one AI behavior-tree debug iteration: collect runtime report, persist it, and optionally regenerate the BT asset from YAML."
    )]
    public static class RunAIBTDebugIteration
    {
        public static object HandleCommand(JObject @params)
        {
            string yamlPath = @params["yaml_path"]?.ToString() ?? "Assets/BTAssets/CraftAllUsefulItems.yaml";
            string assetPath = @params["asset_path"]?.ToString() ?? "Assets/BTAssets/CraftAllUsefulItems.asset";
            bool regenerate = @params["regenerate"]?.ToObject<bool?>() ?? false;
            bool bindPlayerTree = @params["bind_player_tree"]?.ToObject<bool?>() ?? true;
            bool clearDebugger = @params["clear_debugger"]?.ToObject<bool?>() ?? true;
            bool saveSceneIfDirty = @params["save_scene_if_dirty"]?.ToObject<bool?>() ?? false;
            bool writeReport = @params["write_report"]?.ToObject<bool?>() ?? true;
            int eventCount = @params["event_count"]?.ToObject<int?>() ?? 120;
            string reportFolder = @params["report_folder"]?.ToString() ?? AIBehaviorTreeReportUtility.DefaultReportFolder;
            string playerName = @params["player_name"]?.ToString() ?? "PlayerCharacter";

            try
            {
                object regenerationResult = null;
                if (regenerate)
                {
                    var commandParams = new JObject
                    {
                        ["yaml_path"] = yamlPath,
                        ["asset_path"] = assetPath,
                        ["overwrite"] = true,
                        ["strict"] = false
                    };
                    regenerationResult = CreateBehaviorTreeFromYaml.HandleCommand(commandParams);
                }

                AIBehaviorTreeDebugUtility.BindingResult bindingResult = null;
                if (bindPlayerTree)
                {
                    bindingResult = AIBehaviorTreeDebugUtility.BindBehaviorTreeToPlayer(
                        assetPath,
                        playerName,
                        restartIfPlaying: true,
                        saveSceneIfDirty: saveSceneIfDirty);
                }

                if (clearDebugger && Application.isPlaying)
                    GameStateDebugger.GetOrCreate().Clear();

                object report;
                if (Application.isPlaying)
                {
                    var debugger = GameStateDebugger.GetOrCreate();
                    report = debugger.GetReport(eventCount, Math.Max(20, eventCount / 2));
                }
                else
                {
                    report = new
                    {
                        generated_at_time = 0f,
                        generated_at_frame = 0,
                        findings = new[]
                        {
                            new
                            {
                                key = "unity_not_in_play_mode",
                                severity = "warning",
                                message = "Unity is not in Play Mode, so runtime player and behavior-tree state cannot be sampled."
                            }
                        }
                    };
                }

                string reportPath = null;
                if (writeReport)
                    reportPath = AIBehaviorTreeReportUtility.WriteJsonReport(reportFolder, "BTDebugReport", report, includeMilliseconds: false);

                object historySummary = AIBehaviorTreeReportUtility.BuildHistorySummary(reportFolder, reportPath, report != null ? JObject.FromObject(report) : null);

                return new SuccessResponse(
                    "AI behavior-tree debug iteration completed.",
                    new
                    {
                        yaml_path = yamlPath,
                        asset_path = assetPath,
                        report_path = reportPath,
                        regenerate,
                        bind_player_tree = bindPlayerTree,
                        player_name = playerName,
                        report,
                        history_summary = historySummary,
                        binding_result = bindingResult,
                        regeneration_result = regenerationResult,
                        next_ai_steps = new[]
                        {
                            "Read report.findings and recent_events.",
                            "If findings contain movement or target-selection issues, update the YAML or node code with the smallest targeted change.",
                            "Call create_behavior_tree_from_yaml or rerun this tool with regenerate=true.",
                            "Enter or keep Play Mode running, then rerun this tool until findings are empty or low severity."
                        }
                    });
            }
            catch (Exception e)
            {
                return new ErrorResponse(
                    "AI behavior-tree debug iteration failed.",
                    new
                    {
                        exception_type = e.GetType().Name,
                        message = e.Message,
                        yaml_path = yamlPath,
                        asset_path = assetPath
                    });
            }
        }
    }
}
