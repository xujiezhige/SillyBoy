using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using NodeCanvas.BehaviourTrees;
using NodeCanvas.Framework;
using UnityEditor;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SillyBoy.Editor.MCPTools
{
    [McpForUnityTool(
        "create_behavior_tree_from_yaml",
        Description = "Create a NodeCanvas BehaviourTree asset from a YAML file path."
    )]
    public static class CreateBehaviorTreeFromYaml
    {
        private const string DefaultBtAssetFolder = "Assets/BTAssets";

        public static object HandleCommand(JObject @params)
        {
            var yamlPath = @params["yaml_path"]?.ToString();
            var assetPath = @params["asset_path"]?.ToString();
            var overwrite = @params["overwrite"]?.ToObject<bool?>() ?? false;
            var strict = @params["strict"]?.ToObject<bool?>() ?? false;

            if (string.IsNullOrWhiteSpace(yamlPath))
            {
                return new ErrorResponse("yaml_path is required.");
            }

            try
            {
                yamlPath = NormalizeFilePath(yamlPath);

                if (!File.Exists(yamlPath))
                {
                    return new ErrorResponse($"YAML file does not exist: {yamlPath}");
                }

                assetPath = string.IsNullOrWhiteSpace(assetPath)
                    ? GetDefaultAssetPathForYaml(yamlPath)
                    : NormalizeAssetPath(assetPath);

                var yamlAssetPath = ToAssetPath(yamlPath);
                var warnings = new List<string>();
                if (!yamlAssetPath.StartsWith(DefaultBtAssetFolder + "/", StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add($"YAML file is outside {DefaultBtAssetFolder}. Future behavior tree YAML/config files should live there.");
                }

                if (!assetPath.StartsWith(DefaultBtAssetFolder + "/", StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add($"BehaviourTree asset is outside {DefaultBtAssetFolder}. Future behavior tree assets should live there.");
                }

                var yamlDirectory = Path.GetDirectoryName(yamlAssetPath)?.Replace('\\', '/');
                var assetDirectory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                if (!string.Equals(yamlDirectory, assetDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add("YAML file and BehaviourTree asset are not in the same directory.");
                }

                var existingAsset = AssetDatabase.LoadAssetAtPath<BehaviourTree>(assetPath);
                if (existingAsset != null && !overwrite)
                {
                    return new ErrorResponse($"Asset already exists: {assetPath}. Pass overwrite=true to replace it.");
                }

                var yamlText = File.ReadAllText(yamlPath);
                var config = ParseYaml(yamlText);
                ValidateConfig(config);

                var builtTree = ScriptableObject.CreateInstance<BehaviourTree>();
                builtTree.name = string.IsNullOrWhiteSpace(config.name)
                    ? Path.GetFileNameWithoutExtension(assetPath)
                    : config.name;
                builtTree.repeat = config.repeat;
                builtTree.updateInterval = config.update_interval;

                var layout = new LayoutState();
                var blackboardVariables = new Dictionary<string, Type>(StringComparer.Ordinal);
                var root = BuildNode(builtTree, config.root, "root", 0, layout, warnings, strict, blackboardVariables);
                builtTree.primeNode = root;
                EnsureGraphBlackboardVariables(builtTree, blackboardVariables, warnings);
                builtTree.SelfSerialize();

                EnsureAssetFolder(assetPath);
                BehaviourTree tree;
                if (existingAsset != null)
                {
                    EditorUtility.CopySerialized(builtTree, existingAsset);
                    UnityEngine.Object.DestroyImmediate(builtTree);
                    tree = existingAsset;
                }
                else
                {
                    tree = builtTree;
                    AssetDatabase.CreateAsset(tree, assetPath);
                }
                
                EditorUtility.SetDirty(tree);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return new SuccessResponse(
                    "BehaviourTree asset created.",
                    new
                    {
                        asset_path = assetPath,
                        yaml_path = yamlPath,
                        node_count = tree.allNodes.Count,
                        warning_count = warnings.Count,
                        warnings
                    }
                );
            }
            catch (YamlException e)
            {
                return new ErrorResponse(
                    "YAML parse error.",
                    new
                    {
                        error_type = "yaml_parse_error",
                        message = e.Message,
                        line = e.Start.Line,
                        column = e.Start.Column,
                        end_line = e.End.Line,
                        end_column = e.End.Column
                    }
                );
            }
            catch (YamlConfigException e)
            {
                return new ErrorResponse(
                    "YAML validation error.",
                    new
                    {
                        error_type = "yaml_validation_error",
                        path = e.Path,
                        message = e.Message
                    }
                );
            }
            catch (Exception e)
            {
                return new ErrorResponse(
                    "BehaviourTree generation error.",
                    new
                    {
                        error_type = "generation_error",
                        exception_type = e.GetType().Name,
                        message = e.Message
                    }
                );
            }
        }

        private static BehaviourTreeYaml ParseYaml(string yamlText)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<BehaviourTreeYaml>(yamlText);
        }

        private static void ValidateConfig(BehaviourTreeYaml config)
        {
            if (config == null)
            {
                throw new YamlConfigException("$", "YAML is empty or invalid.");
            }

            if (config.root == null)
            {
                throw new YamlConfigException("root", "YAML must contain a root node.");
            }

            ValidateNode(config.root, "root");
        }

        private static BTNode BuildNode(
            BehaviourTree tree,
            BehaviourTreeNodeYaml yaml,
            string yamlPath,
            int depth,
            LayoutState layout,
            List<string> warnings,
            bool strict,
            Dictionary<string, Type> blackboardVariables
        )
        {
            if (yaml == null)
            {
                throw new YamlConfigException(yamlPath, "Encountered a null node.");
            }

            var nodeType = ResolveNodeType(yaml.type, yamlPath + ".type");
            var position = yaml.position != null && yaml.position.Count >= 2
                ? new Vector2(Convert.ToSingle(yaml.position[0]), Convert.ToSingle(yaml.position[1]))
                : new Vector2(depth * 260f, layout.NextY());

            var node = tree.AddNode(nodeType, position) as BTNode;
            if (node == null)
            {
                throw new InvalidOperationException($"Failed to create node type '{nodeType.FullName}'.");
            }

            if (!string.IsNullOrWhiteSpace(yaml.name))
            {
                node.tag = yaml.name;
            }

            var assignedTask = AssignTaskIfNeeded(node, yaml, warnings, strict, blackboardVariables);
            if (!assignedTask)
            {
                ApplyParameters(node, yaml.parameters, warnings, strict, $"node '{yaml.type}'", blackboardVariables);
            }

            if (yaml.children != null)
            {
                for (var i = 0; i < yaml.children.Count; i++)
                {
                    var childPath = $"{yamlPath}.children[{i}]";
                    var child = BuildNode(tree, yaml.children[i], childPath, depth + 1, layout, warnings, strict, blackboardVariables);
                    tree.ConnectNodes(node, child, i);
                }
            }

            return node;
        }

        private static bool AssignTaskIfNeeded(
            BTNode node,
            BehaviourTreeNodeYaml yaml,
            List<string> warnings,
            bool strict,
            Dictionary<string, Type> blackboardVariables)
        {
            if (node is ActionNode actionNode)
            {
                if (string.IsNullOrWhiteSpace(yaml.task))
                {
                    warnings.Add($"Action node '{yaml.name ?? yaml.type}' has no task.");
                    return true;
                }

                var taskType = ResolveType(yaml.task, typeof(ActionTask));
                actionNode.action = (ActionTask)Activator.CreateInstance(taskType);
                ApplyParameters(actionNode.action, yaml.parameters, warnings, strict, $"task '{yaml.task}'", blackboardVariables);
                return true;
            }

            if (node is ConditionNode conditionNode)
            {
                if (string.IsNullOrWhiteSpace(yaml.task))
                {
                    warnings.Add($"Condition node '{yaml.name ?? yaml.type}' has no task.");
                    return true;
                }

                var taskType = ResolveType(yaml.task, typeof(ConditionTask));
                conditionNode.condition = (ConditionTask)Activator.CreateInstance(taskType);
                ApplyParameters(conditionNode.condition, yaml.parameters, warnings, strict, $"task '{yaml.task}'", blackboardVariables);
                return true;
            }

            return false;
        }

        private static Type ResolveNodeType(string typeName, string yamlPath)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new YamlConfigException(yamlPath, "Node type is required.");
            }

            if (StringEquals(typeName, "Action") || StringEquals(typeName, "ActionNode"))
            {
                return typeof(ActionNode);
            }

            if (StringEquals(typeName, "Condition") || StringEquals(typeName, "ConditionNode"))
            {
                return typeof(ConditionNode);
            }

            return ResolveType(typeName, typeof(BTNode));
        }

        private static void ValidateNode(BehaviourTreeNodeYaml yaml, string yamlPath)
        {
            if (yaml == null)
            {
                throw new YamlConfigException(yamlPath, "Node must not be empty.");
            }

            if (string.IsNullOrWhiteSpace(yaml.type))
            {
                throw new YamlConfigException($"{yamlPath}.type", "Node type is required.");
            }

            if (yaml.position != null && yaml.position.Count < 2)
            {
                throw new YamlConfigException($"{yamlPath}.position", "Position must contain at least two numbers: [x, y].");
            }

            if (yaml.children == null)
            {
                return;
            }

            for (var i = 0; i < yaml.children.Count; i++)
            {
                ValidateNode(yaml.children[i], $"{yamlPath}.children[{i}]");
            }
        }

        private static Type ResolveType(string typeName, Type requiredBaseType)
        {
            var normalized = typeName.Trim();
            var direct = Type.GetType(normalized);
            if (IsValidType(direct, requiredBaseType))
            {
                return direct;
            }

            var matches = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetLoadableTypes)
                .Where(t => IsValidType(t, requiredBaseType))
                .Where(t => StringEquals(t.FullName, normalized) || StringEquals(t.Name, normalized))
                .ToList();

            if (matches.Count == 1)
            {
                return matches[0];
            }

            if (matches.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Type '{typeName}' is ambiguous: {string.Join(", ", matches.Select(t => t.FullName))}"
                );
            }

            throw new InvalidOperationException($"Could not resolve type '{typeName}' as {requiredBaseType.Name}.");
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        private static bool IsValidType(Type type, Type requiredBaseType)
        {
            return type != null
                && !type.IsAbstract
                && requiredBaseType.IsAssignableFrom(type);
        }

        private static void ApplyParameters(
            object target,
            Dictionary<string, object> parameters,
            List<string> warnings,
            bool strict,
            string context,
            Dictionary<string, Type> blackboardVariables
        )
        {
            if (target == null || parameters == null)
            {
                return;
            }

            foreach (var pair in parameters)
            {
                if (!TrySetMember(target, pair.Key, pair.Value, blackboardVariables, warnings, out var message))
                {
                    var warning = $"{context}: {message}";
                    if (strict)
                    {
                        throw new InvalidOperationException(warning);
                    }

                    warnings.Add(warning);
                }
            }
        }

        private static bool TrySetMember(
            object target,
            string memberName,
            object rawValue,
            Dictionary<string, Type> blackboardVariables,
            List<string> warnings,
            out string message)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var type = target.GetType();
            var field = type.GetField(memberName, flags);
            if (field != null)
            {
                var converted = ConvertValue(rawValue, field.FieldType, blackboardVariables, warnings);
                field.SetValue(target, converted);
                message = null;
                return true;
            }

            var property = type.GetProperty(memberName, flags);
            if (property != null && property.CanWrite)
            {
                var converted = ConvertValue(rawValue, property.PropertyType, blackboardVariables, warnings);
                property.SetValue(target, converted, null);
                message = null;
                return true;
            }

            var caseInsensitiveField = type.GetFields(flags).FirstOrDefault(f => StringEquals(f.Name, memberName));
            if (caseInsensitiveField != null)
            {
                var converted = ConvertValue(rawValue, caseInsensitiveField.FieldType, blackboardVariables, warnings);
                caseInsensitiveField.SetValue(target, converted);
                message = null;
                return true;
            }

            var caseInsensitiveProperty = type.GetProperties(flags).FirstOrDefault(p => StringEquals(p.Name, memberName) && p.CanWrite);
            if (caseInsensitiveProperty != null)
            {
                var converted = ConvertValue(rawValue, caseInsensitiveProperty.PropertyType, blackboardVariables, warnings);
                caseInsensitiveProperty.SetValue(target, converted, null);
                message = null;
                return true;
            }

            message = $"member '{memberName}' was not found.";
            return false;
        }

        private static object ConvertValue(
            object rawValue,
            Type targetType,
            Dictionary<string, Type> blackboardVariables,
            List<string> warnings)
        {
            if (typeof(BBParameter).IsAssignableFrom(targetType))
            {
                var bbParameter = (BBParameter)Activator.CreateInstance(targetType);
                if (TryGetBlackboardVariableName(rawValue, out var variableName))
                {
                    bbParameter.name = variableName;
                    RegisterBlackboardVariable(variableName, bbParameter.varType, blackboardVariables, warnings);
                    return bbParameter;
                }

                bbParameter.value = ConvertValue(rawValue, bbParameter.varType, blackboardVariables, warnings);
                return bbParameter;
            }

            if (rawValue == null)
            {
                return null;
            }

            var nullableType = Nullable.GetUnderlyingType(targetType);
            if (nullableType != null)
            {
                targetType = nullableType;
            }

            if (targetType.IsInstanceOfType(rawValue))
            {
                return rawValue;
            }

            if (targetType == typeof(string))
            {
                return rawValue.ToString();
            }

            if (targetType == typeof(bool))
            {
                return Convert.ToBoolean(rawValue, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(int))
            {
                return Convert.ToInt32(rawValue, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(float))
            {
                return Convert.ToSingle(rawValue, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(double))
            {
                return Convert.ToDouble(rawValue, CultureInfo.InvariantCulture);
            }

            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, rawValue.ToString(), true);
            }

            if (targetType == typeof(Vector2))
            {
                var values = ToFloatList(rawValue);
                return new Vector2(values.ElementAtOrDefault(0), values.ElementAtOrDefault(1));
            }

            if (targetType == typeof(Vector3))
            {
                var values = ToFloatList(rawValue);
                return new Vector3(values.ElementAtOrDefault(0), values.ElementAtOrDefault(1), values.ElementAtOrDefault(2));
            }

            if (targetType.IsArray)
            {
                var elementType = targetType.GetElementType();
                var items = ToObjectList(rawValue);
                var array = Array.CreateInstance(elementType, items.Count);
                for (var i = 0; i < items.Count; i++)
                {
                    array.SetValue(ConvertValue(items[i], elementType, blackboardVariables, warnings), i);
                }

                return array;
            }

            if (targetType.IsGenericType && typeof(IList).IsAssignableFrom(targetType))
            {
                var elementType = targetType.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(targetType);
                foreach (var item in ToObjectList(rawValue))
                {
                    list.Add(ConvertValue(item, elementType, blackboardVariables, warnings));
                }

                return list;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType) && rawValue is string assetPath)
            {
                return AssetDatabase.LoadAssetAtPath(assetPath, targetType);
            }

            return Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);
        }

        private static void RegisterBlackboardVariable(
            string variableName,
            Type variableType,
            Dictionary<string, Type> blackboardVariables,
            List<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(variableName) || variableType == null)
                return;

            if (blackboardVariables.TryGetValue(variableName, out var existingType))
            {
                if (existingType != variableType)
                {
                    warnings.Add(
                        $"Blackboard variable '{variableName}' is bound with conflicting types: {existingType.Name} and {variableType.Name}.");
                }
                return;
            }

            blackboardVariables[variableName] = variableType;
        }

        private static void EnsureGraphBlackboardVariables(
            BehaviourTree tree,
            Dictionary<string, Type> blackboardVariables,
            List<string> warnings)
        {
            if (tree == null || blackboardVariables == null || blackboardVariables.Count == 0)
                return;

            var blackboard = tree.blackboard;
            if (blackboard == null)
            {
                warnings.Add("BehaviourTree graph blackboard is unavailable; variable bindings will be promoted dynamically at runtime.");
                return;
            }

            foreach (var pair in blackboardVariables.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                if (blackboard.variables != null && blackboard.variables.TryGetValue(pair.Key, out var existing))
                {
                    if (existing != null && existing.varType != pair.Value)
                    {
                        warnings.Add(
                            $"Existing graph blackboard variable '{pair.Key}' has type {existing.varType.Name}; YAML binding expects {pair.Value.Name}.");
                    }
                    continue;
                }

                blackboard.AddVariable(pair.Key, pair.Value);
            }
        }

        private static bool TryGetBlackboardVariableName(object rawValue, out string variableName)
        {
            variableName = null;

            if (rawValue is Dictionary<object, object> objectMap)
            {
                foreach (var pair in objectMap)
                {
                    if (StringEquals(pair.Key?.ToString(), "var") || StringEquals(pair.Key?.ToString(), "variable"))
                    {
                        variableName = pair.Value?.ToString();
                        return !string.IsNullOrWhiteSpace(variableName);
                    }
                }
            }

            if (rawValue is Dictionary<string, object> stringMap)
            {
                foreach (var pair in stringMap)
                {
                    if (StringEquals(pair.Key, "var") || StringEquals(pair.Key, "variable"))
                    {
                        variableName = pair.Value?.ToString();
                        return !string.IsNullOrWhiteSpace(variableName);
                    }
                }
            }

            if (rawValue is JObject jsonObject)
            {
                variableName = jsonObject["var"]?.ToString() ?? jsonObject["variable"]?.ToString();
                return !string.IsNullOrWhiteSpace(variableName);
            }

            if (rawValue is string text && text.StartsWith("$", StringComparison.Ordinal))
            {
                variableName = text.Substring(1);
                return !string.IsNullOrWhiteSpace(variableName);
            }

            return false;
        }

        private static List<float> ToFloatList(object rawValue)
        {
            if (rawValue is IEnumerable enumerable && !(rawValue is string))
            {
                return enumerable.Cast<object>()
                    .Select(v => Convert.ToSingle(v, CultureInfo.InvariantCulture))
                    .ToList();
            }

            return rawValue.ToString()
                .Split(',')
                .Select(v => Convert.ToSingle(v.Trim(), CultureInfo.InvariantCulture))
                .ToList();
        }

        private static List<object> ToObjectList(object rawValue)
        {
            if (rawValue is IEnumerable enumerable && !(rawValue is string))
            {
                return enumerable.Cast<object>().ToList();
            }

            return rawValue == null
                ? new List<object>()
                : new List<object> { rawValue };
        }

        private static string NormalizeFilePath(string path)
        {
            path = path.Trim();
            if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || path.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            }

            return Path.GetFullPath(path);
        }

        private static string NormalizeAssetPath(string path)
        {
            path = path.Replace('\\', '/').Trim();
            if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                path = DefaultBtAssetFolder + "/" + path.TrimStart('/');
            }

            if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                path += ".asset";
            }

            return path;
        }

        private static string GetDefaultAssetPathForYaml(string yamlFullPath)
        {
            var yamlAssetPath = ToAssetPath(yamlFullPath);
            var directory = Path.GetDirectoryName(yamlAssetPath)?.Replace('\\', '/');
            var fileName = Path.GetFileNameWithoutExtension(yamlAssetPath) + ".asset";
            return string.IsNullOrEmpty(directory)
                ? DefaultBtAssetFolder + "/" + fileName
                : directory + "/" + fileName;
        }

        private static string ToAssetPath(string fullPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var normalizedFullPath = Path.GetFullPath(fullPath);
            if (!normalizedFullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedFullPath.Replace('\\', '/');
            }

            return normalizedFullPath.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            var directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory) || AssetDatabase.IsValidFolder(directory))
            {
                return;
            }

            var parts = directory.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static bool StringEquals(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        [Serializable]
        private sealed class BehaviourTreeYaml
        {
            public string name { get; set; }
            public bool repeat { get; set; } = true;
            public float update_interval { get; set; }
            public BehaviourTreeNodeYaml root { get; set; }
        }

        [Serializable]
        private sealed class BehaviourTreeNodeYaml
        {
            public string name { get; set; }
            public string type { get; set; }
            public string task { get; set; }

            [YamlMember(Alias = "params")]
            public Dictionary<string, object> parameters { get; set; }

            public List<float> position { get; set; }
            public List<BehaviourTreeNodeYaml> children { get; set; }
        }

        private sealed class LayoutState
        {
            private int row;

            public float NextY()
            {
                return row++ * 140f;
            }
        }

        private sealed class YamlConfigException : Exception
        {
            public string Path { get; }

            public YamlConfigException(string path, string message) : base(message)
            {
                Path = path;
            }
        }
    }
}
