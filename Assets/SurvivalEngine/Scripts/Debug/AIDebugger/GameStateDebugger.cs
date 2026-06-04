using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.BehaviourTrees;
using NodeCanvas.Framework;
using UnityEngine;

namespace SurvivalEngine.Debugging
{
    public class GameStateDebugger : MonoBehaviour
    {
        public static GameStateDebugger Instance { get; private set; }

        [Tooltip("Automatically find the first PlayerCharacter when no explicit player is assigned.")]
        public bool autoFindPlayer = true;

        [Tooltip("Player whose state should be sampled and diagnosed.")]
        public PlayerCharacter player;

        [Tooltip("BehaviourTreeOwner to inspect. When empty, the debugger searches the player and scene.")]
        public BehaviourTreeOwner behaviorTreeOwner;

        [Tooltip("Seconds between periodic runtime snapshots.")]
        public float sampleInterval = 0.25f;

        [Tooltip("Maximum number of snapshots retained in memory.")]
        public int maxSamples = 480;

        [Tooltip("Maximum number of events retained in memory.")]
        public int maxEvents = 512;

        [Tooltip("Seconds without position progress before movement is considered blocked.")]
        public float stuckSeconds = 1.5f;

        [Tooltip("Minimum XZ movement that counts as progress during stuck detection.")]
        public float stuckMinProgress = 0.08f;

        [Tooltip("Seconds a MoveToInteract node may self-heal lost auto movement before the debugger escalates it to an error.")]
        public float lostAutoMoveErrorGraceSeconds = 8f;

        [Tooltip("When enabled, automatic diagnostic events are generated while sampling.")]
        public bool enableAutoDiagnostics = true;

        private readonly List<GameDebugSnapshot> samples = new List<GameDebugSnapshot>();
        private readonly List<GameDebugEvent> events = new List<GameDebugEvent>();
        private float nextSampleTime;
        private Vector3 lastProgressPosition;
        private float lastProgressTime;
        private string lastRunningNodeKey;
        private string lastStableCraftItemId;
        private string lastStableMissingMaterialsSignature;
        private float lastStableCraftStateTime;

        public static GameStateDebugger GetOrCreate()
        {
            if (Instance != null)
                return Instance;

            var existing = FindObjectOfType<GameStateDebugger>();
            if (existing != null)
                return existing;

            var go = new GameObject("GameStateDebugger");
            DontDestroyOnLoad(go);
            return go.AddComponent<GameStateDebugger>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            lastProgressTime = Time.time;
        }

        private void Update()
        {
            if (Time.time < nextSampleTime)
                return;

            nextSampleTime = Time.time + Mathf.Max(0.02f, sampleInterval);
            CaptureSample("periodic");
        }

        public GameDebugSnapshot CaptureSample(string reason)
        {
            ResolveTargets();

            var snapshot = GameDebugSnapshot.Capture(player, behaviorTreeOwner, reason);
            AddSample(snapshot);

            if (enableAutoDiagnostics)
                Diagnose(snapshot);

            return snapshot;
        }

        public void RecordEvent(string category, string type, string message, string severity = "info", IDictionary<string, object> data = null)
        {
            var item = new GameDebugEvent
            {
                time = Time.time,
                frame = Time.frameCount,
                category = category,
                type = type,
                severity = severity,
                message = message,
                data = data != null ? SanitizeDictionary(data) : new Dictionary<string, object>()
            };

            events.Add(item);
            Trim(events, maxEvents);
        }

        public Dictionary<string, object> GetReport(int recentEventCount = 80, int recentSampleCount = 40)
        {
            var snapshot = CaptureSample("query");
            var findings = DiagnoseSnapshot(snapshot);

            return new Dictionary<string, object>
            {
                ["generated_at_time"] = Time.time,
                ["generated_at_frame"] = Time.frameCount,
                ["snapshot"] = snapshot.ToDictionary(),
                ["findings"] = findings,
                ["recent_events"] = events.Skip(Math.Max(0, events.Count - recentEventCount)).Select(e => e.ToDictionary()).ToList(),
                ["recent_samples"] = samples.Skip(Math.Max(0, samples.Count - recentSampleCount)).Select(s => s.ToDictionary()).ToList(),
                ["target_failure_memory"] = AITargetFailureMemory.GetSnapshot(),
                ["craft_candidate_failure_memory"] = AICraftCandidateFailureMemory.GetSnapshot(),
                ["recommendations"] = BuildRecommendations(findings)
            };
        }

        public List<Dictionary<string, object>> GetRecentEvents(int count)
        {
            return events.Skip(Math.Max(0, events.Count - count)).Select(e => e.ToDictionary()).ToList();
        }

        public List<Dictionary<string, object>> GetRecentSamples(int count)
        {
            return samples.Skip(Math.Max(0, samples.Count - count)).Select(s => s.ToDictionary()).ToList();
        }

        public void Clear()
        {
            samples.Clear();
            events.Clear();
            AITargetFailureMemory.Clear();
            AICraftCandidateFailureMemory.Clear();
            lastProgressPosition = player != null ? player.transform.position : Vector3.zero;
            lastProgressTime = Time.time;
            lastRunningNodeKey = null;
            lastStableCraftItemId = null;
            lastStableMissingMaterialsSignature = null;
            lastStableCraftStateTime = 0f;
        }

        private void ResolveTargets()
        {
            if (player == null && autoFindPlayer)
                player = AIRuntimeSceneQuery.GetPrimaryPlayer();

            if (behaviorTreeOwner == null && player != null)
                behaviorTreeOwner = player.GetComponent<BehaviourTreeOwner>();

            if (behaviorTreeOwner == null)
                behaviorTreeOwner = FindObjectOfType<BehaviourTreeOwner>();
        }

        private void AddSample(GameDebugSnapshot snapshot)
        {
            samples.Add(snapshot);
            Trim(samples, maxSamples);
        }

        private void Diagnose(GameDebugSnapshot snapshot)
        {
            foreach (var finding in DiagnoseSnapshot(snapshot))
            {
                string key = finding.TryGetValue("key", out var value) ? Convert.ToString(value) : string.Empty;
                string severity = finding.TryGetValue("severity", out var sev) ? Convert.ToString(sev) : "warning";
                string message = finding.TryGetValue("message", out var msg) ? Convert.ToString(msg) : "Diagnostic finding.";

                if (key == lastRunningNodeKey && severity != "error")
                    continue;

                lastRunningNodeKey = key;
                finding["repeat_count"] = CountRecentFindings(key) + 1;
                RecordEvent("diagnostic", key, message, severity, finding);
            }
        }

        private List<Dictionary<string, object>> DiagnoseSnapshot(GameDebugSnapshot snapshot)
        {
            var findings = new List<Dictionary<string, object>>();
            if (snapshot.player == null)
            {
                findings.Add(Finding("missing_player", "error", "No PlayerCharacter was found for AI debugging."));
                return findings;
            }

            if (snapshot.behavior_tree == null)
            {
                findings.Add(Finding("missing_behavior_tree", "error", "No BehaviourTreeOwner was found for AI debugging."));
                return findings;
            }

            var playerState = snapshot.player;
            var treeState = snapshot.behavior_tree;
            var runningActions = treeState.running_nodes
                .Where(n => n.TryGetValue("task_type", out var type) && Convert.ToString(type) == "MoveToInteract")
                .ToList();
            bool hasRunningLeafTask = treeState.running_nodes.Any(n => n.ContainsKey("task_type"));
            string craftItemId = GetBlackboardString(treeState, "_bestCraftItemId");
            string missingMaterialsSignature = GetBlackboardListSignature(treeState, "_missingMaterialItemIds");

            TrackMovementProgress(playerState);
            TrackCraftCandidateState(craftItemId, missingMaterialsSignature);
            float blockedFor = Time.time - lastProgressTime;
            float craftStateStableFor = Time.time - lastStableCraftStateTime;

            if (playerState.is_dead)
            {
                findings.Add(Finding(
                    "player_dead_during_ai_debug",
                    "error",
                    "The sampled PlayerCharacter is dead, so behavior-tree movement and interaction diagnostics are not valid.",
                    new Dictionary<string, object>
                    {
                        ["player_position"] = playerState.position,
                        ["tree_name"] = treeState.graph_name,
                        ["root_status"] = treeState.root_status
                    }));
            }

            if (treeState.is_running && (!playerState.is_controls_enabled || !playerState.is_movement_enabled))
            {
                findings.Add(Finding(
                    "player_movement_disabled_during_ai_debug",
                    "error",
                    "The behavior tree is running while player controls or movement are disabled.",
                    new Dictionary<string, object>
                    {
                        ["is_controls_enabled"] = playerState.is_controls_enabled,
                        ["is_movement_enabled"] = playerState.is_movement_enabled,
                        ["tree_name"] = treeState.graph_name,
                        ["root_status"] = treeState.root_status
                    }));
            }

            if (runningActions.Count > 0 && !playerState.is_auto_move && !playerState.is_busy && !playerState.is_near_auto_target)
            {
                float longestRunningMove = GetLongestRunningNodeElapsed(runningActions);
                bool beyondGrace = longestRunningMove >= lostAutoMoveErrorGraceSeconds;
                findings.Add(Finding(
                    "move_to_interact_lost_auto_move",
                    beyondGrace ? "error" : "warning",
                    beyondGrace
                        ? "MoveToInteract has not self-healed after auto movement stopped before reaching the target."
                        : "MoveToInteract lost auto movement and is still within its retry/fallback grace window.",
                    new Dictionary<string, object>
                    {
                        ["running_nodes"] = runningActions,
                        ["player_position"] = playerState.position,
                        ["auto_move_target"] = playerState.auto_move_target,
                        ["distance_to_auto_target"] = playerState.distance_to_auto_target,
                        ["longest_running_move_seconds"] = longestRunningMove,
                        ["error_grace_seconds"] = lostAutoMoveErrorGraceSeconds
                    }));
            }

            if (runningActions.Count > 0 && playerState.is_swimming && !playerState.is_auto_move)
            {
                findings.Add(Finding(
                    "movement_stopped_while_swimming",
                    "error",
                    "The player is swimming and movement has stopped while MoveToInteract is running. The target path likely crosses water or an unreachable area.",
                    new Dictionary<string, object>
                    {
                        ["running_nodes"] = runningActions,
                        ["player_position"] = playerState.position
                    }));
            }

            if (runningActions.Count > 0 && playerState.is_auto_move && blockedFor >= stuckSeconds)
            {
                findings.Add(Finding(
                    "auto_move_no_progress",
                    "warning",
                    "The player has an active auto-move request but has not made enough XZ progress recently.",
                    new Dictionary<string, object>
                    {
                        ["blocked_for_seconds"] = blockedFor,
                        ["stuck_min_progress"] = stuckMinProgress,
                        ["running_nodes"] = runningActions
                    }));
            }

            if (treeState.is_running && treeState.elapsed_time >= sampleInterval && treeState.running_nodes.Count == 0)
            {
                findings.Add(Finding(
                    "tree_running_without_running_node",
                    "warning",
                    "The behavior tree reports running but no node currently has Running status."));
            }

            if (treeState.is_running &&
                !hasRunningLeafTask &&
                !playerState.is_moving &&
                !playerState.is_busy &&
                !string.IsNullOrEmpty(craftItemId) &&
                craftStateStableFor >= stuckSeconds * 2f)
            {
                findings.Add(Finding(
                    "craft_candidate_stalled",
                    "warning",
                    "The behavior tree is still running, but the same craft candidate has been idle without a running leaf task or player progress for several seconds.",
                    new Dictionary<string, object>
                    {
                        ["craft_item_id"] = craftItemId,
                        ["missing_material_ids"] = missingMaterialsSignature,
                        ["stable_for_seconds"] = craftStateStableFor
                    }));
            }

            foreach (var finding in findings)
            {
                string key = finding.TryGetValue("key", out var value) ? Convert.ToString(value) : string.Empty;
                finding["repeat_count"] = CountRecentFindings(key);
            }

            return findings;
        }

        private int CountRecentFindings(string key, int historyLimit = 12)
        {
            if (string.IsNullOrEmpty(key))
                return 0;

            return events
                .Where(e => e != null && e.category == "diagnostic" && e.type == key)
                .Reverse()
                .Take(historyLimit)
                .Count();
        }

        private void TrackMovementProgress(GameDebugPlayerSnapshot playerState)
        {
            Vector3 position = playerState.raw_position;
            if (lastProgressTime <= 0f || lastProgressPosition == Vector3.zero)
            {
                lastProgressPosition = position;
                lastProgressTime = Time.time;
                return;
            }

            Vector2 last = new Vector2(lastProgressPosition.x, lastProgressPosition.z);
            Vector2 current = new Vector2(position.x, position.z);
            if (Vector2.Distance(last, current) >= stuckMinProgress)
            {
                lastProgressPosition = position;
                lastProgressTime = Time.time;
            }
        }

        private void TrackCraftCandidateState(string craftItemId, string missingMaterialsSignature)
        {
            if (string.IsNullOrEmpty(craftItemId))
            {
                lastStableCraftItemId = null;
                lastStableMissingMaterialsSignature = null;
                lastStableCraftStateTime = Time.time;
                return;
            }

            if (string.Equals(lastStableCraftItemId, craftItemId, StringComparison.Ordinal) &&
                string.Equals(lastStableMissingMaterialsSignature, missingMaterialsSignature, StringComparison.Ordinal))
            {
                if (lastStableCraftStateTime <= 0f)
                    lastStableCraftStateTime = Time.time;
                return;
            }

            lastStableCraftItemId = craftItemId;
            lastStableMissingMaterialsSignature = missingMaterialsSignature;
            lastStableCraftStateTime = Time.time;
        }

        private static string GetBlackboardString(GameDebugBehaviorTreeSnapshot treeState, string suffix)
        {
            if (treeState == null || treeState.blackboard == null)
                return null;

            foreach (var pair in treeState.blackboard)
            {
                if (pair.Key != null && pair.Key.EndsWith(suffix, StringComparison.Ordinal))
                    return Convert.ToString(pair.Value);
            }

            return null;
        }

        private static string GetBlackboardListSignature(GameDebugBehaviorTreeSnapshot treeState, string suffix)
        {
            if (treeState == null || treeState.blackboard == null)
                return string.Empty;

            foreach (var pair in treeState.blackboard)
            {
                if (pair.Key == null || !pair.Key.EndsWith(suffix, StringComparison.Ordinal))
                    continue;

                if (pair.Value is IList list)
                {
                    var values = new List<string>();
                    foreach (var item in list)
                        values.Add(Convert.ToString(item));
                    return string.Join(",", values);
                }

                return Convert.ToString(pair.Value);
            }

            return string.Empty;
        }

        private static Dictionary<string, object> Finding(string key, string severity, string message, Dictionary<string, object> data = null)
        {
            var finding = data != null ? new Dictionary<string, object>(data) : new Dictionary<string, object>();
            finding["key"] = key;
            finding["severity"] = severity;
            finding["message"] = message;
            return finding;
        }

        private static float GetLongestRunningNodeElapsed(List<Dictionary<string, object>> nodes)
        {
            float longest = 0f;
            foreach (var node in nodes)
            {
                if (node == null || !node.TryGetValue("elapsed_time", out var elapsed))
                    continue;

                try
                {
                    longest = Mathf.Max(longest, Convert.ToSingle(elapsed));
                }
                catch
                {
                    continue;
                }
            }

            return longest;
        }

        private static List<string> BuildRecommendations(List<Dictionary<string, object>> findings)
        {
            var recommendations = new List<string>();
            var keys = new HashSet<string>(findings.Select(f => Convert.ToString(f["key"])));

            if (keys.Contains("move_to_interact_lost_auto_move"))
                recommendations.Add("Update MoveToInteract to detect lost PlayerCharacter auto-move, then retry movement or fail the action instead of staying Running forever.");

            if (keys.Contains("movement_stopped_while_swimming") || keys.Contains("auto_move_no_progress"))
                recommendations.Add("Prefer navmesh movement for AI collection targets, or filter FindNearestMaterial targets by reachable path before selecting them.");

            if (keys.Contains("missing_behavior_tree"))
                recommendations.Add("Attach a BehaviourTreeOwner to PlayerCharacter or assign it to GameStateDebugger.behaviorTreeOwner.");

            if (keys.Contains("craft_candidate_stalled"))
                recommendations.Add("Skip craft candidates whose missing materials currently have no reachable world source, or temporarily blacklist the candidate when FindNearestMaterial repeatedly fails.");

            if (keys.Contains("player_dead_during_ai_debug") || keys.Contains("player_movement_disabled_during_ai_debug"))
                recommendations.Add("Reset the Play Mode scene to a living player with movement enabled before accepting the behavior-tree regression result.");

            return recommendations;
        }

        private static Dictionary<string, object> SanitizeDictionary(IDictionary<string, object> source)
        {
            var result = new Dictionary<string, object>();
            if (source == null)
                return result;

            foreach (var pair in source)
                result[pair.Key] = SanitizeValue(pair.Value);

            return result;
        }

        private static object SanitizeValue(object value)
        {
            if (value == null)
                return null;

            if (value is Vector2 vector2)
                return GameDebugSnapshot.FormatVector2(vector2);

            if (value is Vector3 vector3)
                return GameDebugSnapshot.FormatVector3(vector3);

            if (value is GameObject go)
            {
                if (!go)
                    return GameStateDebugger.DestroyedUnityObjectValue("GameObject");

                return new Dictionary<string, object>
                {
                    ["name"] = go.name,
                    ["instance_id"] = go.GetInstanceID(),
                    ["active"] = go.activeInHierarchy,
                    ["position"] = GameDebugSnapshot.FormatVector3(go.transform.position)
                };
            }

            if (value is UnityEngine.Object unityObject)
            {
                if (!unityObject)
                    return GameStateDebugger.DestroyedUnityObjectValue(unityObject != null ? unityObject.GetType().Name : "UnityObject");

                return unityObject.name;
            }

            if (value is IDictionary<string, object> dict)
                return SanitizeDictionary(dict);

            if (value is IDictionary genericDict)
            {
                var result = new Dictionary<string, object>();
                foreach (DictionaryEntry entry in genericDict)
                    result[Convert.ToString(entry.Key)] = SanitizeValue(entry.Value);
                return result;
            }

            if (value is IList list && !(value is string))
            {
                var items = new List<object>();
                foreach (var item in list)
                    items.Add(SanitizeValue(item));
                return items;
            }

            return value;
        }

        internal static Dictionary<string, object> DestroyedUnityObjectValue(string typeName)
        {
            return new Dictionary<string, object>
            {
                ["type"] = typeName,
                ["destroyed"] = true
            };
        }

        private static void Trim<T>(List<T> list, int maxCount)
        {
            int safeMax = Mathf.Max(1, maxCount);
            if (list.Count > safeMax)
                list.RemoveRange(0, list.Count - safeMax);
        }
    }

    [Serializable]
    public class GameDebugEvent
    {
        public float time;
        public int frame;
        public string category;
        public string type;
        public string severity;
        public string message;
        public Dictionary<string, object> data = new Dictionary<string, object>();

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["time"] = time,
                ["frame"] = frame,
                ["category"] = category,
                ["type"] = type,
                ["severity"] = severity,
                ["message"] = message,
                ["data"] = data
            };
        }
    }

    [Serializable]
    public class GameDebugSnapshot
    {
        public float time;
        public int frame;
        public string reason;
        public GameDebugPlayerSnapshot player;
        public GameDebugBehaviorTreeSnapshot behavior_tree;
        public Dictionary<string, object> environment = new Dictionary<string, object>();

        public static GameDebugSnapshot Capture(PlayerCharacter player, BehaviourTreeOwner owner, string reason)
        {
            return new GameDebugSnapshot
            {
                time = Time.time,
                frame = Time.frameCount,
                reason = reason,
                player = GameDebugPlayerSnapshot.Capture(player),
                behavior_tree = GameDebugBehaviorTreeSnapshot.Capture(owner),
                environment = CaptureEnvironment(player)
            };
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["time"] = time,
                ["frame"] = frame,
                ["reason"] = reason,
                ["player"] = player != null ? player.ToDictionary() : null,
                ["behavior_tree"] = behavior_tree != null ? behavior_tree.ToDictionary() : null,
                ["environment"] = environment
            };
        }

        private static Dictionary<string, object> CaptureEnvironment(PlayerCharacter player)
        {
            var data = new Dictionary<string, object>
            {
                ["unity_time_scale"] = Time.timeScale,
                ["active_item_count"] = AIRuntimeSceneQuery.GetItems().Count,
                ["player_count"] = AIRuntimeSceneQuery.GetPlayers().Count
            };

            if (player != null)
            {
                var nearbyItems = AIRuntimeSceneQuery.GetItems()
                    .Where(i => i != null && i.data != null && i.gameObject.activeInHierarchy)
                    .OrderBy(i => (i.transform.position - player.transform.position).sqrMagnitude)
                    .Take(12)
                    .Select(i => new Dictionary<string, object>
                    {
                        ["name"] = i.name,
                        ["item_id"] = i.data.id,
                        ["quantity"] = i.quantity,
                        ["position"] = FormatVector3(i.transform.position),
                        ["distance"] = Vector3.Distance(i.transform.position, player.transform.position)
                    })
                    .ToList();

                data["nearest_items"] = nearbyItems;
            }

            return data;
        }

        internal static Dictionary<string, object> FormatVector3(Vector3 value)
        {
            return new Dictionary<string, object>
            {
                ["x"] = value.x,
                ["y"] = value.y,
                ["z"] = value.z
            };
        }

        internal static Dictionary<string, object> FormatVector2(Vector2 value)
        {
            return new Dictionary<string, object>
            {
                ["x"] = value.x,
                ["y"] = value.y
            };
        }
    }

    [Serializable]
    public class GameDebugPlayerSnapshot
    {
        public string name;
        public Vector3 raw_position;
        public Dictionary<string, object> position;
        public Dictionary<string, object> move;
        public Dictionary<string, object> facing;
        public Dictionary<string, object> auto_move_target;
        public bool is_auto_move;
        public bool is_moving;
        public bool is_busy;
        public bool is_swimming;
        public bool is_grounded;
        public bool is_fronted;
        public bool is_dead;
        public bool is_controls_enabled;
        public bool is_movement_enabled;
        public bool use_navmesh;
        public string busy_action;
        public string auto_target;
        public float move_speed;
        public float distance_to_auto_target;
        public bool is_near_auto_target;

        public static GameDebugPlayerSnapshot Capture(PlayerCharacter player)
        {
            if (player == null)
                return null;

            Vector3 position = player.transform.position;
            Vector3 target = SafeCall(() => player.GetAutoMoveTarget(), position);
            float distance = Vector2.Distance(new Vector2(position.x, position.z), new Vector2(target.x, target.z));
            var autoTarget = SafeCall(() => player.GetAutoTarget(), null as UnityEngine.Object);
            var busyAction = SafeCall(() => player.GetBusyAction(), null as UnityEngine.Object);

            return new GameDebugPlayerSnapshot
            {
                name = player.name,
                raw_position = position,
                position = GameDebugSnapshot.FormatVector3(position),
                move = GameDebugSnapshot.FormatVector3(SafeCall(() => player.GetMove(), Vector3.zero)),
                facing = GameDebugSnapshot.FormatVector3(SafeCall(() => player.GetFacing(), Vector3.forward)),
                auto_move_target = GameDebugSnapshot.FormatVector3(target),
                is_auto_move = SafeCall(() => player.IsAutoMove(), false),
                is_moving = SafeCall(() => player.IsMoving(), false),
                is_busy = SafeCall(() => player.IsBusy(), false),
                is_swimming = SafeCall(() => player.IsSwimming(), false),
                is_grounded = SafeCall(() => player.IsGrounded(), false),
                is_fronted = SafeCall(() => player.IsFronted(), false),
                is_dead = SafeCall(() => player.IsDead(), false),
                is_controls_enabled = SafeCall(() => player.IsControlsEnabled(), false),
                is_movement_enabled = SafeCall(() => player.IsMovementEnabled(), false),
                use_navmesh = player.use_navmesh,
                busy_action = busyAction != null ? busyAction.GetType().Name : null,
                auto_target = autoTarget != null ? autoTarget.name : null,
                move_speed = SafeCall(() => player.GetMoveSpeed(), 0f),
                distance_to_auto_target = distance,
                is_near_auto_target = distance <= 0.25f
            };
        }

        private static T SafeCall<T>(Func<T> getter, T fallback)
        {
            try
            {
                return getter();
            }
            catch
            {
                return fallback;
            }
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["name"] = name,
                ["position"] = position,
                ["move"] = move,
                ["facing"] = facing,
                ["auto_move_target"] = auto_move_target,
                ["is_auto_move"] = is_auto_move,
                ["is_moving"] = is_moving,
                ["is_busy"] = is_busy,
                ["is_swimming"] = is_swimming,
                ["is_grounded"] = is_grounded,
                ["is_fronted"] = is_fronted,
                ["is_dead"] = is_dead,
                ["is_controls_enabled"] = is_controls_enabled,
                ["is_movement_enabled"] = is_movement_enabled,
                ["use_navmesh"] = use_navmesh,
                ["busy_action"] = busy_action,
                ["auto_target"] = auto_target,
                ["move_speed"] = move_speed,
                ["distance_to_auto_target"] = distance_to_auto_target,
                ["is_near_auto_target"] = is_near_auto_target
            };
        }
    }

    [Serializable]
    public class GameDebugBehaviorTreeSnapshot
    {
        public string owner_name;
        public string graph_name;
        public bool is_running;
        public bool is_paused;
        public string root_status;
        public float elapsed_time;
        public int node_count;
        public List<Dictionary<string, object>> running_nodes = new List<Dictionary<string, object>>();
        public List<Dictionary<string, object>> nodes = new List<Dictionary<string, object>>();
        public Dictionary<string, object> blackboard = new Dictionary<string, object>();

        public static GameDebugBehaviorTreeSnapshot Capture(BehaviourTreeOwner owner)
        {
            if (owner == null)
                return null;

            var graph = owner.graph;
            var snapshot = new GameDebugBehaviorTreeSnapshot
            {
                owner_name = owner.name,
                graph_name = graph != null ? graph.name : null,
                is_running = owner.isRunning,
                is_paused = owner.isPaused,
                root_status = owner.rootStatus.ToString(),
                elapsed_time = owner.elapsedTime,
                node_count = graph != null ? graph.allNodes.Count : 0,
                blackboard = CaptureBlackboard(owner, graph)
            };

            if (graph != null)
            {
                foreach (var node in graph.allNodes)
                {
                    var item = CaptureNode(node);
                    snapshot.nodes.Add(item);
                    if (node.status == Status.Running)
                        snapshot.running_nodes.Add(item);
                }
            }

            return snapshot;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["owner_name"] = owner_name,
                ["graph_name"] = graph_name,
                ["is_running"] = is_running,
                ["is_paused"] = is_paused,
                ["root_status"] = root_status,
                ["elapsed_time"] = elapsed_time,
                ["node_count"] = node_count,
                ["running_nodes"] = running_nodes,
                ["nodes"] = nodes,
                ["blackboard"] = blackboard
            };
        }

        private static Dictionary<string, object> CaptureNode(Node node)
        {
            var item = new Dictionary<string, object>
            {
                ["id"] = node.ID,
                ["uid"] = node.UID,
                ["name"] = node.name,
                ["tag"] = node.tag,
                ["type"] = node.GetType().Name,
                ["status"] = node.status.ToString(),
                ["elapsed_time"] = node.elapsedTime,
                ["children"] = node.outConnections.Select(c => c.targetNode != null ? c.targetNode.ID : -1).ToList()
            };

            if (node is ActionNode actionNode && actionNode.action != null)
            {
                item["task_type"] = actionNode.action.GetType().Name;
                item["task_info"] = actionNode.action.summaryInfo;
            }

            if (node is ConditionNode conditionNode && conditionNode.condition != null)
            {
                item["task_type"] = conditionNode.condition.GetType().Name;
                item["task_info"] = conditionNode.condition.summaryInfo;
            }

            return item;
        }

        private static Dictionary<string, object> CaptureBlackboard(BehaviourTreeOwner owner, Graph graph)
        {
            var result = new Dictionary<string, object>();
            AddBlackboard("owner", owner.blackboard, result);

            if (graph != null)
            {
                AddBlackboard("graph", graph.blackboard, result);
                AddBlackboard("parent", graph.parentBlackboard, result);
            }

            return result;
        }

        private static void AddBlackboard(string prefix, IBlackboard blackboard, Dictionary<string, object> result)
        {
            if (blackboard == null || blackboard.variables == null)
                return;

            foreach (var pair in blackboard.variables)
            {
                string key = string.IsNullOrEmpty(prefix) ? pair.Key : prefix + "." + pair.Key;
                result[key] = FormatValue(pair.Value != null ? pair.Value.value : null);
            }
        }

        private static object FormatValue(object value)
        {
            if (value == null)
                return null;

            if (value is Vector2 vector2)
                return GameDebugSnapshot.FormatVector2(vector2);

            if (value is Vector3 vector3)
                return GameDebugSnapshot.FormatVector3(vector3);

            if (value is GameObject go)
            {
                if (!go)
                    return GameStateDebugger.DestroyedUnityObjectValue("GameObject");

                return new Dictionary<string, object>
                {
                    ["name"] = go.name,
                    ["instance_id"] = go.GetInstanceID(),
                    ["active"] = go.activeInHierarchy,
                    ["position"] = GameDebugSnapshot.FormatVector3(go.transform.position)
                };
            }

            if (value is UnityEngine.Object unityObject)
            {
                if (!unityObject)
                    return GameStateDebugger.DestroyedUnityObjectValue(unityObject != null ? unityObject.GetType().Name : "UnityObject");

                return unityObject.name;
            }

            if (value is IList list && !(value is string))
            {
                var items = new List<object>();
                foreach (var item in list)
                    items.Add(FormatValue(item));
                return items;
            }

            return value;
        }
    }

    public static class AITargetFailureMemory
    {
        private sealed class FailureEntry
        {
            public string key;
            public string reason;
            public string itemId;
            public string targetName;
            public int instanceId;
            public int failureCount;
            public float lastFailureTime;
            public float blockedUntilTime;
            public Vector3 targetPosition;
        }

        private static readonly Dictionary<string, FailureEntry> Entries = new Dictionary<string, FailureEntry>();

        public static void RememberFailure(GameObject targetObject, string itemId, Vector3 targetPosition, string reason, float cooldownSeconds = 8f)
        {
            string key = BuildKey(targetObject, itemId, targetPosition);
            if (string.IsNullOrEmpty(key))
                return;

            CleanupExpired();

            if (!Entries.TryGetValue(key, out var entry))
            {
                entry = new FailureEntry
                {
                    key = key
                };
                Entries[key] = entry;
            }

            entry.reason = reason;
            entry.itemId = itemId;
            entry.targetName = targetObject != null ? targetObject.name : string.Empty;
            entry.instanceId = targetObject != null ? targetObject.GetInstanceID() : 0;
            entry.targetPosition = targetObject != null ? targetObject.transform.position : targetPosition;
            entry.failureCount++;
            entry.lastFailureTime = Time.time;
            entry.blockedUntilTime = Mathf.Max(entry.lastFailureTime, entry.blockedUntilTime) + Mathf.Max(0.5f, cooldownSeconds);
        }

        public static bool IsBlocked(GameObject targetObject, string itemId, Vector3 targetPosition, out float remainingSeconds)
        {
            string key = BuildKey(targetObject, itemId, targetPosition);
            remainingSeconds = 0f;
            if (string.IsNullOrEmpty(key))
                return false;

            CleanupExpired();

            if (!Entries.TryGetValue(key, out var entry))
                return false;

            remainingSeconds = Mathf.Max(0f, entry.blockedUntilTime - Time.time);
            if (remainingSeconds <= 0f)
            {
                Entries.Remove(key);
                return false;
            }

            return true;
        }

        public static void Clear()
        {
            Entries.Clear();
        }

        public static List<Dictionary<string, object>> GetSnapshot()
        {
            CleanupExpired();
            return Entries.Values
                .OrderByDescending(e => e.blockedUntilTime)
                .Select(e => new Dictionary<string, object>
                {
                    ["key"] = e.key,
                    ["reason"] = e.reason,
                    ["item_id"] = e.itemId,
                    ["target_name"] = e.targetName,
                    ["instance_id"] = e.instanceId,
                    ["failure_count"] = e.failureCount,
                    ["last_failure_time"] = e.lastFailureTime,
                    ["blocked_until_time"] = e.blockedUntilTime,
                    ["remaining_seconds"] = Mathf.Max(0f, e.blockedUntilTime - Time.time),
                    ["target_position"] = GameDebugSnapshot.FormatVector3(e.targetPosition)
                })
                .ToList();
        }

        private static string BuildKey(GameObject targetObject, string itemId, Vector3 targetPosition)
        {
            if (targetObject != null)
                return "go:" + targetObject.GetInstanceID();

            string normalizedItemId = string.IsNullOrEmpty(itemId) ? "unknown" : itemId;
            Vector2 point = new Vector2(targetPosition.x, targetPosition.z);
            return string.Format("pos:{0}:{1:F1}:{2:F1}", normalizedItemId, point.x, point.y);
        }

        private static void CleanupExpired()
        {
            if (Entries.Count == 0)
                return;

            var expiredKeys = Entries
                .Where(pair => pair.Value == null || pair.Value.blockedUntilTime <= Time.time)
                .Select(pair => pair.Key)
                .ToList();

            foreach (string key in expiredKeys)
                Entries.Remove(key);
        }
    }

    public static class AICraftCandidateFailureMemory
    {
        private sealed class FailureEntry
        {
            public string craftItemId;
            public string reason;
            public int failureCount;
            public float lastFailureTime;
            public float blockedUntilTime;
            public List<string> missingMaterialIds = new List<string>();
        }

        private static readonly Dictionary<string, FailureEntry> Entries = new Dictionary<string, FailureEntry>();

        public static void RememberFailure(string craftItemId, string reason, float cooldownSeconds = 12f, IList<string> missingMaterialIds = null)
        {
            if (string.IsNullOrEmpty(craftItemId))
                return;

            CleanupExpired();

            if (!Entries.TryGetValue(craftItemId, out var entry))
            {
                entry = new FailureEntry
                {
                    craftItemId = craftItemId
                };
                Entries[craftItemId] = entry;
            }

            entry.reason = reason;
            entry.failureCount++;
            entry.lastFailureTime = Time.time;
            entry.blockedUntilTime = Mathf.Max(entry.lastFailureTime, entry.blockedUntilTime) + Mathf.Max(0.5f, cooldownSeconds);
            entry.missingMaterialIds = missingMaterialIds != null ? new List<string>(missingMaterialIds) : new List<string>();
        }

        public static bool IsBlocked(string craftItemId, out float remainingSeconds)
        {
            remainingSeconds = 0f;
            if (string.IsNullOrEmpty(craftItemId))
                return false;

            CleanupExpired();

            if (!Entries.TryGetValue(craftItemId, out var entry))
                return false;

            remainingSeconds = Mathf.Max(0f, entry.blockedUntilTime - Time.time);
            if (remainingSeconds <= 0f)
            {
                Entries.Remove(craftItemId);
                return false;
            }

            return true;
        }

        public static void Clear()
        {
            Entries.Clear();
        }

        public static List<Dictionary<string, object>> GetSnapshot()
        {
            CleanupExpired();
            return Entries.Values
                .OrderByDescending(e => e.blockedUntilTime)
                .Select(e => new Dictionary<string, object>
                {
                    ["craft_item_id"] = e.craftItemId,
                    ["reason"] = e.reason,
                    ["failure_count"] = e.failureCount,
                    ["last_failure_time"] = e.lastFailureTime,
                    ["blocked_until_time"] = e.blockedUntilTime,
                    ["remaining_seconds"] = Mathf.Max(0f, e.blockedUntilTime - Time.time),
                    ["missing_material_ids"] = e.missingMaterialIds
                })
                .ToList();
        }

        private static void CleanupExpired()
        {
            if (Entries.Count == 0)
                return;

            var expiredKeys = Entries
                .Where(pair => pair.Value == null || pair.Value.blockedUntilTime <= Time.time)
                .Select(pair => pair.Key)
                .ToList();

            foreach (string key in expiredKeys)
                Entries.Remove(key);
        }
    }
}
