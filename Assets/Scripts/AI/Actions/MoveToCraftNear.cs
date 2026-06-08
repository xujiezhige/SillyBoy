using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using SurvivalEngine.Debugging;
using UnityEngine;

namespace SurvivalEngine
{
    [Name("MoveToCraftNear")]
    [Category("SurvivalEngine/Player")]
    [Description("Move the player near the craft_near group required by the current craft item.")]
    public class MoveToCraftNear : ActionTask
    {
        public BBParameter<string> itemId;
        public BBParameter<float> range = 999f;
        public BBParameter<bool> requireReachablePath = true;

        private const float AutoMoveLostRetryDelay = 0.5f;
        private const int MaxAutoMoveRetries = 3;
        private const float MaxMoveSeconds = 45f;
        private const float TargetChangeDistance = 0.2f;
        private const float FailureMemoryCooldown = 20f;

        private PlayerCharacter player;
        private CraftData craft;
        private GroupData requiredGroup;
        private Selectable targetSelectable;
        private Vector3 lastTargetPosition;
        private bool hasMoveRequest;
        private bool completed;
        private float startedAt;
        private float autoMoveLostTimer;
        private int autoMoveRetryCount;

        protected override string info
        {
            get { return "Move near craft requirement for " + itemId; }
        }

        protected override void OnExecute()
        {
            player = AIRuntimeSceneQuery.GetPrimaryPlayer();
            craft = !string.IsNullOrEmpty(itemId.value) ? CraftData.Get(itemId.value) : null;
            requiredGroup = craft != null ? craft.GetCraftCost().craft_near : null;
            targetSelectable = null;
            hasMoveRequest = false;
            completed = false;
            startedAt = Time.time;
            autoMoveLostTimer = 0f;
            autoMoveRetryCount = 0;

            if (player == null || craft == null)
            {
                EndAction(false);
                return;
            }

            if (requiredGroup == null || player.Crafting.HasCraftNear(craft) || player.EquipData.HasItemInGroup(requiredGroup))
            {
                CompleteSuccess();
                return;
            }

            targetSelectable = FindNearestCraftNearTarget();
            if (targetSelectable == null)
            {
                Fail("craft_near_target_not_found");
                return;
            }

            RequestMove();
        }

        protected override void OnUpdate()
        {
            if (completed)
                return;

            if (player == null || craft == null || requiredGroup == null || targetSelectable == null)
            {
                Fail("craft_near_target_lost");
                return;
            }

            if (player.IsDead())
            {
                Fail("craft_near_player_dead");
                return;
            }

            if (player.Crafting.HasCraftNear(craft) || player.EquipData.HasItemInGroup(requiredGroup))
            {
                CompleteSuccess();
                return;
            }

            if (Time.time - startedAt > MaxMoveSeconds)
            {
                Fail("craft_near_move_timeout");
                return;
            }

            if (!hasMoveRequest || HasTargetChanged())
                RequestMove();

            DetectLostAutoMove();
        }

        protected override void OnStop()
        {
            if (player != null && !completed)
                player.StopMove();

            player = null;
            craft = null;
            requiredGroup = null;
            targetSelectable = null;
            completed = false;
            hasMoveRequest = false;
        }

        private Selectable FindNearestCraftNearTarget()
        {
            if (player == null || requiredGroup == null)
                return null;

            Vector3 playerPosition = player.transform.position;
            float maxDistance = Mathf.Max(0f, range.value);
            float bestSqrDistance = maxDistance * maxDistance;
            Selectable best = null;

            foreach (Selectable selectable in Selectable.GetAllActive())
            {
                if (selectable == null || !selectable.enabled || !selectable.gameObject.activeInHierarchy)
                    continue;

                if (!selectable.HasGroup(requiredGroup))
                    continue;

                Vector3 targetPosition = selectable.GetClosestInteractPoint(player.GetInteractCenter());
                if (requireReachablePath.value && !AIMovementReachability.HasReachablePath(player, playerPosition, targetPosition))
                    continue;

                float sqrDistance = (targetPosition - playerPosition).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = selectable;
                }
            }

            return best;
        }

        private void RequestMove()
        {
            if (targetSelectable == null || player == null)
                return;

            lastTargetPosition = targetSelectable.GetClosestInteractPoint(player.GetInteractCenter());
            hasMoveRequest = true;
            autoMoveLostTimer = 0f;
            player.MoveTo(lastTargetPosition);
        }

        private bool HasTargetChanged()
        {
            return targetSelectable != null &&
                Vector3.Distance(lastTargetPosition, targetSelectable.GetClosestInteractPoint(player.GetInteractCenter())) > TargetChangeDistance;
        }

        private bool DetectLostAutoMove()
        {
            if (!hasMoveRequest || targetSelectable == null || player.Crafting.HasCraftNear(craft))
                return false;

            if (player.IsAutoMove())
            {
                autoMoveLostTimer = 0f;
                return false;
            }

            autoMoveLostTimer += Time.deltaTime;
            if (autoMoveLostTimer < AutoMoveLostRetryDelay)
                return true;

            if (autoMoveRetryCount < MaxAutoMoveRetries)
            {
                autoMoveRetryCount++;
                RequestMove();
                return true;
            }

            Fail("craft_near_lost_auto_move");
            return true;
        }

        private void Fail(string reason)
        {
            string currentItemId = itemId.value;
            if (!string.IsNullOrEmpty(currentItemId))
                AICraftCandidateFailureMemory.RememberFailure(currentItemId, reason, FailureMemoryCooldown, new List<string>());

            var debugger = GameStateDebugger.Instance;
            if (debugger != null)
            {
                debugger.RecordEvent(
                    "behavior_tree",
                    reason,
                    "MoveToCraftNear could not find or reach the required nearby craft source.",
                    "warning",
                    new Dictionary<string, object>
                    {
                        ["craft_item_id"] = currentItemId,
                        ["required_group"] = requiredGroup != null ? requiredGroup.group_id : null,
                        ["player_position"] = player != null ? player.transform.position : Vector3.zero
                    });
            }

            if (player != null)
                player.StopMove();

            EndAction(false);
        }

        private void CompleteSuccess()
        {
            completed = true;
            if (player != null)
                player.StopMove();
            EndAction(true);
        }
    }
}
