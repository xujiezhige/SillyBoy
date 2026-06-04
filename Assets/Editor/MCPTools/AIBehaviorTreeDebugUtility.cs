using System;
using System.Linq;
using NodeCanvas.BehaviourTrees;
using NodeCanvas.Framework;
using SurvivalEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SillyBoy.Editor.MCPTools
{
    internal static class AIBehaviorTreeDebugUtility
    {
        internal static BindingResult BindBehaviorTreeToPlayer(string assetPath, string playerName, bool restartIfPlaying, bool saveSceneIfDirty)
        {
            string normalizedAssetPath = NormalizeAssetPath(assetPath);
            var tree = AssetDatabase.LoadAssetAtPath<BehaviourTree>(normalizedAssetPath);
            if (tree == null)
            {
                return BindingResult.Fail(
                    $"BehaviourTree asset was not found at '{normalizedAssetPath}'.",
                    normalizedAssetPath);
            }

            var playerObject = FindPlayerObject(playerName);
            if (playerObject == null)
            {
                return BindingResult.Fail(
                    $"Player GameObject '{playerName}' was not found in the loaded scene.",
                    normalizedAssetPath);
            }

            var owner = playerObject.GetComponent<BehaviourTreeOwner>();
            if (owner == null)
                owner = Application.isPlaying
                    ? playerObject.AddComponent<BehaviourTreeOwner>()
                    : Undo.AddComponent<BehaviourTreeOwner>(playerObject);

            var blackboard = playerObject.GetComponent<Blackboard>();
            if (blackboard == null)
                blackboard = Application.isPlaying
                    ? playerObject.AddComponent<Blackboard>()
                    : Undo.AddComponent<Blackboard>(playerObject);

            owner.blackboard = blackboard;
            bool restarted = false;
            if (Application.isPlaying && restartIfPlaying)
            {
                owner.SwitchBehaviour(tree);
                restarted = true;
            }
            else
            {
                owner.behaviour = tree;
                owner.graph = tree;
            }

            EditorUtility.SetDirty(owner);
            EditorUtility.SetDirty(blackboard);

            var scene = playerObject.scene;
            bool sceneDirty = false;
            if (!Application.isPlaying && scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
                sceneDirty = scene.isDirty;
                if (saveSceneIfDirty)
                    EditorSceneManager.SaveScene(scene);
            }

            return BindingResult.Success(
                normalizedAssetPath,
                playerObject.name,
                restarted,
                sceneDirty,
                owner.GetInstanceID(),
                blackboard.GetInstanceID());
        }

        private static GameObject FindPlayerObject(string playerName)
        {
            if (!string.IsNullOrWhiteSpace(playerName))
            {
                var named = GameObject.Find(playerName);
                if (named != null)
                    return named;
            }

            var player = UnityEngine.Object.FindObjectsOfType<PlayerCharacter>(true).FirstOrDefault();
            return player != null ? player.gameObject : null;
        }

        private static string NormalizeAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return "Assets/BTAssets/CraftAllUsefulItems.asset";

            string normalized = assetPath.Replace('\\', '/').Trim();
            if (!normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                normalized = "Assets/BTAssets/" + normalized.TrimStart('/');
            if (!normalized.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                normalized += ".asset";
            return normalized;
        }

        internal sealed class BindingResult
        {
            public bool success;
            public string message;
            public string asset_path;
            public string player_name;
            public bool restarted_in_play_mode;
            public bool scene_dirty;
            public int behavior_tree_owner_instance_id;
            public int blackboard_instance_id;

            public static BindingResult Success(string assetPath, string playerName, bool restarted, bool sceneDirty, int ownerId, int blackboardId)
            {
                return new BindingResult
                {
                    success = true,
                    message = "BehaviourTree asset bound to player.",
                    asset_path = assetPath,
                    player_name = playerName,
                    restarted_in_play_mode = restarted,
                    scene_dirty = sceneDirty,
                    behavior_tree_owner_instance_id = ownerId,
                    blackboard_instance_id = blackboardId
                };
            }

            public static BindingResult Fail(string message, string assetPath)
            {
                return new BindingResult
                {
                    success = false,
                    message = message,
                    asset_path = assetPath,
                    player_name = null,
                    restarted_in_play_mode = false,
                    scene_dirty = false,
                    behavior_tree_owner_instance_id = 0,
                    blackboard_instance_id = 0
                };
            }
        }
    }
}
