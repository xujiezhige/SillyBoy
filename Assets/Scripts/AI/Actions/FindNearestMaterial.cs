using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using SurvivalEngine.Debugging;
using UnityEngine;

namespace SurvivalEngine
{
    [Name("FindNearestMaterial")]
    [Category("SurvivalEngine/Player")]
    [Description("Find the nearest collectable item that matches one of the requested material ids.")]
    public class FindNearestMaterial : ActionTask
    {
        [Tooltip("Material item ids to search for in the world. Empty or null entries are ignored.")]
        public BBParameter<List<string>> materialItemIds;

        [Tooltip("Maximum search radius from the current player. Values below zero are treated as zero.")]
        public BBParameter<float> range = 999f;

        [Tooltip("When enabled, ignores matching items that the player's inventory cannot currently take.")]
        public BBParameter<bool> requireInventorySpace = true;

        [Tooltip("When enabled, skips targets that recently failed movement or interaction and are still in short-term failure memory.")]
        public BBParameter<bool> avoidRecentlyFailedTargets = true;

        [Tooltip("Seconds a failed target stays blocked before the tree may consider it again.")]
        public BBParameter<float> failedTargetCooldown = 8f;

        [Tooltip("When enabled, validates that the player can compute a complete NavMesh path to the target interact point.")]
        public BBParameter<bool> requireReachablePath = true;

        [Tooltip("Optional craft item id that requested these materials. Used for short-term failure memory when no gatherable target can be found.")]
        public BBParameter<string> craftItemId;

        [Tooltip("When enabled, temporarily blocks the current craft candidate after this action fails to find any material target.")]
        public BBParameter<bool> rememberFailedCraftCandidate = true;

        [Tooltip("Seconds a failed craft candidate stays blocked before the tree may consider it again.")]
        public BBParameter<float> failedCraftCandidateCooldown = 15f;

        [BlackboardOnly]
        [Tooltip("Output item id of the nearest matching material found. Cleared when no material is found.")]
        public BBParameter<string> materialItemId;

        [BlackboardOnly]
        [Tooltip("Output XZ target point for moving to the found material. Uses the closest interact point when the item is selectable.")]
        public BBParameter<Vector2> target;

        [BlackboardOnly]
        [Tooltip("Output GameObject of the found material item. Set to null when no material is found.")]
        public BBParameter<GameObject> targetObject;

        protected override string info
        {
            get { return "Find nearest material as " + targetObject; }
        }

        protected override void OnExecute()
        {
            PlayerCharacter player = AIRuntimeSceneQuery.GetPrimaryPlayer();
            if (player == null)
            {
                ClearResult();
                EndAction(false);
                return;
            }

            HashSet<string> wantedIds = GetWantedIds();
            if (wantedIds.Count == 0)
            {
                ClearResult();
                EndAction(false);
                return;
            }

            Item item = GetNearestMaterial(player, wantedIds);
            if (item == null)
            {
                HandleNoMaterialFound(wantedIds);
                ClearResult();
                EndAction(false);
                return;
            }

            Vector3 targetPosition = GetTargetPosition(item, player.transform.position);
            materialItemId.value = item.data.id;
            target.value = new Vector2(targetPosition.x, targetPosition.z);
            targetObject.value = item.gameObject;
            EndAction(true);
        }

        private HashSet<string> GetWantedIds()
        {
            HashSet<string> ids = new HashSet<string>();
            List<string> values = materialItemIds.value;
            if (values == null)
                return ids;

            foreach (string id in values)
            {
                if (!string.IsNullOrEmpty(id))
                    ids.Add(id);
            }
            return ids;
        }

        private Item GetNearestMaterial(PlayerCharacter player, HashSet<string> wantedIds)
        {
            Item nearest = null;
            Vector3 playerPosition = player.transform.position;
            float maxDistance = Mathf.Max(0f, range.value);
            float nearestSqrDistance = maxDistance * maxDistance;

            foreach (Item item in AIRuntimeSceneQuery.GetItems())
            {
                if (!IsValidMaterial(item, player, wantedIds, playerPosition))
                    continue;

                Vector3 targetPosition = GetTargetPosition(item, playerPosition);
                float sqrDistance = (targetPosition - playerPosition).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = item;
                }
            }

            return nearest;
        }

        private bool IsValidMaterial(Item item, PlayerCharacter player, HashSet<string> wantedIds, Vector3 playerPosition)
        {
            if (item == null || item.data == null || item.quantity <= 0 || !item.gameObject.activeInHierarchy)
                return false;

            if (!wantedIds.Contains(item.data.id))
                return false;

            Selectable selectable = item.GetSelectable();
            if (selectable != null && !selectable.CanBeInteracted())
                return false;

            if (requireInventorySpace.value && !player.Inventory.CanTakeItem(item.data, item.quantity))
                return false;

            Vector3 targetPosition = GetTargetPosition(item, playerPosition);
            if (avoidRecentlyFailedTargets.value &&
                AITargetFailureMemory.IsBlocked(item.gameObject, item.data.id, targetPosition, out _))
                return false;

            if (requireReachablePath.value && !HasReachablePath(playerPosition, targetPosition))
            {
                AITargetFailureMemory.RememberFailure(
                    item.gameObject,
                    item.data.id,
                    targetPosition,
                    "navmesh_unreachable_material",
                    failedTargetCooldown.value);
                return false;
            }

            return true;
        }

        private Vector3 GetTargetPosition(Item item, Vector3 fromPosition)
        {
            Selectable selectable = item.GetSelectable();
            if (selectable != null)
                return selectable.GetClosestInteractPoint(fromPosition);

            return item.transform.position;
        }

        private bool HasReachablePath(Vector3 fromPosition, Vector3 toPosition)
        {
            return AIMovementReachability.HasReachablePath(AIRuntimeSceneQuery.GetPrimaryPlayer(), fromPosition, toPosition);
        }

        private void ClearResult()
        {
            materialItemId.value = string.Empty;
            target.value = Vector2.zero;
            targetObject.value = null;
        }

        private void HandleNoMaterialFound(HashSet<string> wantedIds)
        {
            string currentCraftItemId = craftItemId.value;
            if (rememberFailedCraftCandidate.value && !string.IsNullOrEmpty(currentCraftItemId))
            {
                AICraftCandidateFailureMemory.RememberFailure(
                    currentCraftItemId,
                    "no_gatherable_material_target",
                    failedCraftCandidateCooldown.value,
                    new List<string>(wantedIds));
            }

            var debugger = GameStateDebugger.Instance;
            if (debugger != null)
            {
                debugger.RecordEvent(
                    "behavior_tree",
                    "find_nearest_material_no_target",
                    "FindNearestMaterial could not find any gatherable target for the requested material ids.",
                    "warning",
                    new Dictionary<string, object>
                    {
                        ["craft_item_id"] = currentCraftItemId,
                        ["requested_material_ids"] = new List<string>(wantedIds)
                    });
            }
        }
    }
}
