using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace SillyBoy.Editor.MCPTools
{
    internal static class AIBehaviorTreeReportUtility
    {
        internal const string DefaultReportFolder = "Assets/BTDebugReports";

        internal static string WriteJsonReport(string reportFolder, string prefix, object payload, bool includeMilliseconds)
        {
            reportFolder = NormalizeAssetFolder(reportFolder, DefaultReportFolder);
            string fullFolder = Path.GetFullPath(reportFolder);
            Directory.CreateDirectory(fullFolder);

            string timestamp = includeMilliseconds
                ? DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")
                : DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = prefix + "_" + timestamp + ".json";
            string fullPath = Path.Combine(fullFolder, fileName);
            File.WriteAllText(fullPath, JsonConvert.SerializeObject(payload, Formatting.Indented));

            AssetDatabase.Refresh();
            return Path.Combine(reportFolder, fileName).Replace('\\', '/');
        }

        internal static JObject BuildHistorySummary(string reportFolder, string currentReportPath, JObject currentReport)
        {
            if (string.IsNullOrEmpty(currentReportPath) || currentReport == null)
                return null;

            string fullFolder = Path.GetFullPath(NormalizeAssetFolder(reportFolder, DefaultReportFolder));
            if (!Directory.Exists(fullFolder))
                return null;

            string currentFileName = Path.GetFileName(currentReportPath);
            var previousReport = Directory
                .GetFiles(fullFolder, "BTDebugReport_*.json")
                .Where(path => !string.Equals(Path.GetFileName(path), currentFileName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .Select(path => new { path, json = TryReadJson(path) })
                .FirstOrDefault(item => item.json != null);

            if (previousReport == null)
                return null;

            var currentFindings = (currentReport["findings"] as JArray)?.OfType<JObject>().ToList() ?? new System.Collections.Generic.List<JObject>();
            var previousFindings = (previousReport.json["findings"] as JArray)?.OfType<JObject>().ToList() ?? new System.Collections.Generic.List<JObject>();

            var repeatedWarnings = currentFindings
                .Where(f => IsSeverity(f, "warning"))
                .Select(f => f["key"]?.ToString())
                .Where(key => !string.IsNullOrEmpty(key) && previousFindings.Any(prev =>
                    IsSeverity(prev, "warning") &&
                    string.Equals(prev["key"]?.ToString(), key, StringComparison.Ordinal)))
                .Distinct()
                .ToArray();

            return JObject.FromObject(new
            {
                previous_report_path = ToAssetPath(previousReport.path),
                repeated_warning_keys = repeatedWarnings,
                repeated_warning_count = repeatedWarnings.Length
            });
        }

        internal static bool IsSeverity(JToken token, string severity)
        {
            return string.Equals(token?["severity"]?.ToString(), severity, StringComparison.OrdinalIgnoreCase);
        }

        internal static string NormalizeAssetFolder(string folder, string fallback)
        {
            if (string.IsNullOrWhiteSpace(folder) || !folder.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return fallback;

            return folder.Replace('\\', '/').TrimEnd('/');
        }

        internal static string ToAssetPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return fullPath;

            string normalized = fullPath.Replace('\\', '/');
            string projectPath = Path.GetFullPath(".").Replace('\\', '/');
            if (normalized.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
                return normalized.Substring(projectPath.Length + 1);

            return normalized;
        }

        private static JObject TryReadJson(string path)
        {
            try
            {
                return JObject.Parse(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }
    }
}
