using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using SurvivalEngine.Debugging;
using UnityEngine;

namespace SillyBoy.Editor.MCPTools
{
    [McpForUnityTool(
        "run_ai_bt_profile_regression",
        Description = "Run a reusable AI behavior-tree profile regression pass across one or more YAML profiles and write a summary report."
    )]
    public static class RunAIBTProfileRegression
    {
        private const string DefaultProfileFolder = "Assets/BTAssets";
        private const string DefaultManifestPath = "Assets/BTAssets/AIBehaviorTreeProfiles.json";

        public static object HandleCommand(JObject @params)
        {
            string manifestPath = @params["manifest_path"]?.ToString() ?? DefaultManifestPath;
            var manifestDefaults = LoadManifestDefaults(manifestPath);

            bool regenerate = @params["regenerate"]?.ToObject<bool?>() ?? true;
            bool bindPlayerTree = @params["bind_player_tree"]?.ToObject<bool?>() ?? true;
            bool clearDebugger = @params["clear_debugger"]?.ToObject<bool?>() ?? true;
            bool writeReports = @params["write_reports"]?.ToObject<bool?>() ?? true;
            bool includeSampleTree = @params["include_sample_tree"]?.ToObject<bool?>() ?? false;
            int eventCount = @params["event_count"]?.ToObject<int?>() ?? manifestDefaults.event_count ?? 120;
            string reportFolder = @params["report_folder"]?.ToString() ?? manifestDefaults.report_folder ?? AIBehaviorTreeReportUtility.DefaultReportFolder;
            string playerName = @params["player_name"]?.ToString() ?? manifestDefaults.player_name ?? "PlayerCharacter";
            int maxErrorFindings = @params["max_error_findings"]?.ToObject<int?>() ?? manifestDefaults.max_error_findings ?? 0;
            int maxRepeatedWarnings = @params["max_repeated_warnings"]?.ToObject<int?>() ?? manifestDefaults.max_repeated_warnings ?? 0;

            try
            {
                var profiles = ResolveProfiles(@params, manifestPath, includeSampleTree);
                var results = new List<ProfileRegressionResult>();

                foreach (var profile in profiles)
                {
                    results.Add(RunProfile(
                        profile,
                        regenerate,
                        bindPlayerTree,
                        clearDebugger,
                        writeReports,
                        reportFolder,
                        eventCount,
                        playerName,
                        maxErrorFindings,
                        maxRepeatedWarnings));
                }

                var summary = new
                {
                    generated_at_utc = DateTime.UtcNow.ToString("o"),
                    application_is_playing = Application.isPlaying,
                    profile_count = results.Count,
                    passed_count = results.Count(r => r.passed),
                    failed_count = results.Count(r => !r.passed),
                    error_count = results.Sum(r => r.error_count),
                    warning_finding_count = results.Sum(r => r.warning_finding_count),
                    repeated_warning_count = results.Sum(r => r.repeated_warning_count),
                    runtime_sampled = Application.isPlaying,
                    thresholds = new
                    {
                        max_error_findings = maxErrorFindings,
                        max_repeated_warnings = maxRepeatedWarnings
                    },
                    profiles = results,
                    acceptance = new
                    {
                        runtime_sampled = Application.isPlaying,
                        no_error_findings = results.All(r => r.error_count <= maxErrorFindings),
                        no_repeated_warnings = results.All(r => r.repeated_warning_count <= maxRepeatedWarnings),
                        all_profiles_passed = results.All(r => r.passed)
                    },
                    next_ai_steps = new[]
                    {
                        "Investigate profiles where passed=false first.",
                        "Treat severity=error findings as blocking defects.",
                        "If repeated_warning_count is greater than zero, compare the referenced reports before changing YAML or node code.",
                        "Run this tool in Play Mode for runtime validation; outside Play Mode it only validates generation and reports the missing runtime sampling warning."
                    }
                };

                string summaryPath = null;
                if (writeReports)
                    summaryPath = AIBehaviorTreeReportUtility.WriteJsonReport(reportFolder, "BTRegressionSummary", summary, includeMilliseconds: false);

                return new SuccessResponse(
                    "AI behavior-tree profile regression completed.",
                    new
                    {
                        summary_path = summaryPath,
                        manifest_path = manifestPath,
                        summary
                    });
            }
            catch (Exception e)
            {
                return new ErrorResponse(
                    "AI behavior-tree profile regression failed.",
                    new
                    {
                        exception_type = e.GetType().Name,
                        message = e.Message
                    });
            }
        }

        private static ProfileRegressionResult RunProfile(
            ProfileDefinition profile,
            bool regenerate,
            bool bindPlayerTree,
            bool clearDebugger,
            bool writeReports,
            string reportFolder,
            int eventCount,
            string playerName,
            int maxErrorFindings,
            int maxRepeatedWarnings)
        {
            int profileEventCount = profile.event_count > 0 ? profile.event_count : eventCount;
            bool commandSucceeded = true;
            string commandMessage = null;

            if (regenerate)
            {
                var regenerationResult = CreateBehaviorTreeFromYaml.HandleCommand(new JObject
                {
                    ["yaml_path"] = profile.yaml_path,
                    ["asset_path"] = profile.asset_path,
                    ["overwrite"] = true,
                    ["strict"] = false
                });

                commandSucceeded = IsCommandSuccess(JObject.FromObject(regenerationResult));
                if (!commandSucceeded)
                    commandMessage = "Behavior tree regeneration failed.";
            }

            if (commandSucceeded && bindPlayerTree)
            {
                var binding = AIBehaviorTreeDebugUtility.BindBehaviorTreeToPlayer(
                    profile.asset_path,
                    playerName,
                    restartIfPlaying: true,
                    saveSceneIfDirty: false);

                commandSucceeded = binding.success;
                if (!commandSucceeded)
                    commandMessage = binding.message;
            }

            if (commandSucceeded && clearDebugger && Application.isPlaying)
                GameStateDebugger.GetOrCreate().Clear();

            object reportObject = Application.isPlaying
                ? GameStateDebugger.GetOrCreate().GetReport(profileEventCount, Math.Max(20, profileEventCount / 2))
                : new
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

            JObject report = JObject.FromObject(reportObject);
            string reportPath = writeReports
                ? AIBehaviorTreeReportUtility.WriteJsonReport(reportFolder, "BTDebugReport", reportObject, includeMilliseconds: true)
                : null;
            JObject history = AIBehaviorTreeReportUtility.BuildHistorySummary(reportFolder, reportPath, report);

            var findings = report?["findings"] as JArray ?? new JArray();
            int errorCount = findings.Count(f => AIBehaviorTreeReportUtility.IsSeverity(f, "error"));
            int warningFindingCount = findings.Count(f => AIBehaviorTreeReportUtility.IsSeverity(f, "warning"));
            int repeatedWarningCount = history?["repeated_warning_count"]?.ToObject<int?>() ?? 0;
            bool runtimeSampled = Application.isPlaying;
            if (commandSucceeded && !runtimeSampled)
                commandMessage = "Unity is not in Play Mode, so runtime sampling was not performed.";

            bool passed = commandSucceeded
                && runtimeSampled
                && errorCount <= maxErrorFindings
                && repeatedWarningCount <= maxRepeatedWarnings;

            return new ProfileRegressionResult
            {
                name = profile.name,
                yaml_path = profile.yaml_path,
                asset_path = profile.asset_path,
                event_count = profileEventCount,
                description = profile.description,
                report_path = reportPath,
                runtime_sampled = runtimeSampled,
                command_success = commandSucceeded,
                command_message = commandMessage,
                passed = passed,
                error_count = errorCount,
                warning_finding_count = warningFindingCount,
                repeated_warning_count = repeatedWarningCount,
                finding_keys = findings
                    .OfType<JObject>()
                    .Select(f => f["key"]?.ToString())
                    .Where(k => !string.IsNullOrEmpty(k))
                    .Distinct()
                    .ToArray(),
                repeated_warning_keys = history?["repeated_warning_keys"]?.Values<string>().ToArray() ?? Array.Empty<string>()
            };
        }

        private static List<ProfileDefinition> ResolveProfiles(JObject @params, string manifestPath, bool includeSampleTree)
        {
            var profiles = new List<ProfileDefinition>();
            var explicitProfiles = @params["profiles"] as JArray;
            if (explicitProfiles != null)
            {
                foreach (var item in explicitProfiles.OfType<JObject>())
                    profiles.Add(ProfileDefinition.FromJson(item));
            }

            if (profiles.Count == 0)
                profiles = LoadManifestProfiles(manifestPath);

            if (profiles.Count == 0)
            {
                profiles = Directory
                    .GetFiles(DefaultProfileFolder, "*.yaml")
                    .Select(ProfileDefinition.FromYamlPath)
                    .Where(p => includeSampleTree || !string.Equals(p.name, "SampleBehaviorTree", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p.name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var profileNames = (@params["profile_names"] as JArray)?.Values<string>()
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (profileNames != null && profileNames.Count > 0)
                profiles = profiles.Where(p => profileNames.Contains(p.name)).ToList();

            return profiles;
        }

        private static ManifestDefaults LoadManifestDefaults(string manifestPath)
        {
            string normalizedPath = NormalizeAssetPath(manifestPath, DefaultManifestPath);
            if (!File.Exists(normalizedPath))
                return new ManifestDefaults();

            try
            {
                var manifest = JObject.Parse(File.ReadAllText(normalizedPath));
                var defaults = manifest["defaults"] as JObject;
                if (defaults == null)
                    return new ManifestDefaults();

                var acceptance = defaults["acceptance"] as JObject;
                return new ManifestDefaults
                {
                    report_folder = defaults["report_folder"]?.ToString(),
                    event_count = defaults["event_count"]?.ToObject<int?>(),
                    player_name = defaults["player_name"]?.ToString(),
                    max_error_findings = acceptance?["max_error_findings"]?.ToObject<int?>(),
                    max_repeated_warnings = acceptance?["max_repeated_warnings"]?.ToObject<int?>()
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to read AI behavior-tree manifest defaults '" + normalizedPath + "': " + e.Message);
                return new ManifestDefaults();
            }
        }

        private static List<ProfileDefinition> LoadManifestProfiles(string manifestPath)
        {
            string normalizedPath = NormalizeAssetPath(manifestPath, DefaultManifestPath);
            if (!File.Exists(normalizedPath))
                return new List<ProfileDefinition>();

            try
            {
                var manifest = JObject.Parse(File.ReadAllText(normalizedPath));
                var items = manifest["profiles"] as JArray;
                if (items == null)
                    return new List<ProfileDefinition>();

                return items
                    .OfType<JObject>()
                    .Select(ProfileDefinition.FromJson)
                    .OrderBy(p => p.name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to read AI behavior-tree profile manifest '" + normalizedPath + "': " + e.Message);
                return new List<ProfileDefinition>();
            }
        }

        private static bool IsCommandSuccess(JObject resultJson)
        {
            string type = resultJson["type"]?.ToString();
            if (!string.IsNullOrEmpty(type))
                return string.Equals(type, "success", StringComparison.OrdinalIgnoreCase);

            bool? success = resultJson["success"]?.ToObject<bool?>();
            return success == null || success.Value;
        }

        private static string NormalizeAssetPath(string path, string fallback)
        {
            if (string.IsNullOrWhiteSpace(path))
                return fallback;

            string normalized = path.Replace('\\', '/').Trim();
            if (!normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                normalized = DefaultProfileFolder + "/" + normalized.TrimStart('/');
            return normalized;
        }

        private sealed class ProfileDefinition
        {
            public string name;
            public string yaml_path;
            public string asset_path;
            public string description;
            public int event_count;

            public static ProfileDefinition FromYamlPath(string yamlPath)
            {
                string normalized = yamlPath.Replace('\\', '/');
                string name = Path.GetFileNameWithoutExtension(normalized);
                return new ProfileDefinition
                {
                    name = name,
                    yaml_path = normalized,
                    asset_path = Path.ChangeExtension(normalized, ".asset").Replace('\\', '/'),
                    description = string.Empty,
                    event_count = 0
                };
            }

            public static ProfileDefinition FromJson(JObject item)
            {
                string yamlPath = item["yaml_path"]?.ToString();
                if (string.IsNullOrWhiteSpace(yamlPath))
                    yamlPath = item["name"]?.ToString() + ".yaml";

                if (!yamlPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    yamlPath = DefaultProfileFolder + "/" + yamlPath.TrimStart('/');

                string assetPath = item["asset_path"]?.ToString();
                if (string.IsNullOrWhiteSpace(assetPath))
                    assetPath = Path.ChangeExtension(yamlPath, ".asset");

                if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    assetPath = DefaultProfileFolder + "/" + assetPath.TrimStart('/');

                return new ProfileDefinition
                {
                    name = item["name"]?.ToString() ?? Path.GetFileNameWithoutExtension(yamlPath),
                    yaml_path = yamlPath.Replace('\\', '/'),
                    asset_path = assetPath.Replace('\\', '/'),
                    description = item["description"]?.ToString() ?? string.Empty,
                    event_count = item["event_count"]?.ToObject<int?>() ?? 0
                };
            }
        }

        private sealed class ProfileRegressionResult
        {
            public string name;
            public string yaml_path;
            public string asset_path;
            public int event_count;
            public string description;
            public string report_path;
            public bool runtime_sampled;
            public bool command_success;
            public string command_message;
            public bool passed;
            public int error_count;
            public int warning_finding_count;
            public int repeated_warning_count;
            public string[] finding_keys;
            public string[] repeated_warning_keys;
        }

        private sealed class ManifestDefaults
        {
            public string report_folder;
            public int? event_count;
            public string player_name;
            public int? max_error_findings;
            public int? max_repeated_warnings;
        }
    }
}
