using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace SurvivalEngine
{
    [Name("MoveToInteract")]
    [Category("SurvivalEngine/Player")]
    [Description("Move to a target point or object, and interact if the target object can be interacted with.")]
    public class MoveToInteract : ActionTask
    {
        [Tooltip("Fallback XZ world target used when targetObject is empty or cannot be interacted with.")]
        public BBParameter<Vector2> target;

        [Tooltip("Optional GameObject target to move toward. If it has a usable Selectable component, the player will interact with it when in range.")]
        public BBParameter<GameObject> targetObject;

        [BlackboardOnly]
        [Tooltip("Output list of item GameObjects dropped by the interaction while this action is running.")]
        public BBParameter<List<GameObject>> droppedItems;

        private const float StopDistance = 0.2f;

        private PlayerCharacter player;
        private Selectable selectable;
        private GameObject gameObjectTarget;
        private List<GameObject> droppedItemObjects;
        private Vector2 lastTarget;
        private Vector3 lastTargetPosition;
        private bool hasMoveRequest;
        private bool interactionStarted;
        private bool completed;

        protected override string info
        {
            get { return "MoveToInteract " + targetObject; }
        }

        protected override void OnExecute()
        {
            player = PlayerCharacter.GetFirst();
            droppedItemObjects = new List<GameObject>();
            gameObjectTarget = targetObject.value;
            selectable = GetInteractableTarget();
            interactionStarted = false;
            completed = false;
            SaveDroppedItems(droppedItemObjects);

            if (player == null)
            {
                EndAction(false);
                return;
            }

            if (CanInteract())
                SubscribeDropEvent(selectable);

            if (CanInteract() && selectable.IsInUseRange(player))
            {
                UseSelectable();
                return;
            }

            RequestMove();
        }

        protected override void OnUpdate()
        {
            if (player == null)
            {
                EndAction(false);
                return;
            }

            if (interactionStarted)
            {
                if (!player.IsBusy())
                    CompleteSuccess();
                return;
            }

            gameObjectTarget = targetObject.value;
            Selectable nextSelectable = GetInteractableTarget();
            if (nextSelectable != selectable)
            {
                UnsubscribeDropEvent(selectable);
                selectable = nextSelectable;
                if (CanInteract())
                    SubscribeDropEvent(selectable);
            }

            if (CanInteract() && selectable.IsInUseRange(player))
            {
                UseSelectable();
                return;
            }

            if (!hasMoveRequest || HasTargetChanged())
                RequestMove();

            if (!CanInteract() && HasReachedTarget())
            {
                completed = true;
                player.StopMove();
                EndAction(true);
            }
        }

        protected override void OnPause()
        {
            OnStop();
        }

        protected override void OnStop()
        {
            if (player != null && !interactionStarted && !completed)
                player.StopMove();

            player = null;
            UnsubscribeDropEvent(selectable);
            selectable = null;
            gameObjectTarget = null;
            droppedItemObjects = null;
            hasMoveRequest = false;
            interactionStarted = false;
            completed = false;
        }

        private bool CanInteract()
        {
            return player != null
                && selectable != null
                && selectable.CanBeInteracted()
                && HasInteraction(selectable);
        }

        private bool HasInteraction(Selectable target)
        {
            return target.FindAutoAction(player) != null
                || target.actions.Length > 0
                || target.onUse != null;
        }

        private void UseSelectable()
        {
            interactionStarted = true;
            player.StopMove();
            selectable.Use(player, selectable.GetClosestInteractPoint(player.GetInteractCenter()));

            if (!player.IsBusy())
                CompleteSuccess();
        }

        private void RequestMove()
        {
            lastTarget = target.value;
            lastTargetPosition = GetTargetPosition();
            hasMoveRequest = true;
            player.MoveTo(lastTargetPosition);
        }

        private Vector3 GetTargetPosition()
        {
            Vector3 position;
            if (selectable != null)
                position = selectable.GetClosestInteractPoint(player.GetInteractCenter());
            else if (gameObjectTarget != null)
                position = gameObjectTarget.transform.position;
            else
            {
                Vector2 targetPoint = target.value;
                position = new Vector3(targetPoint.x, player.transform.position.y, targetPoint.y);
            }

            position.y = player.transform.position.y;
            return position;
        }

        private bool HasReachedTarget()
        {
            if (CanInteract())
                return selectable.IsInUseRange(player);

            Vector3 playerPosition = player.transform.position;
            Vector2 playerPoint = new Vector2(playerPosition.x, playerPosition.z);
            return Vector2.Distance(playerPoint, GetTargetPointXZ()) <= StopDistance;
        }

        private bool HasTargetChanged()
        {
            if (selectable != null || gameObjectTarget != null)
            {
                Vector2 lastPoint = new Vector2(lastTargetPosition.x, lastTargetPosition.z);
                return Vector2.Distance(lastPoint, GetTargetPointXZ()) > StopDistance;
            }

            return lastTarget != target.value;
        }

        private Vector2 GetTargetPointXZ()
        {
            if (selectable != null)
            {
                Vector3 position = selectable.GetClosestInteractPoint(player.GetInteractCenter());
                return new Vector2(position.x, position.z);
            }

            if (gameObjectTarget != null)
                return new Vector2(gameObjectTarget.transform.position.x, gameObjectTarget.transform.position.z);

            return target.value;
        }

        private Selectable GetInteractableTarget()
        {
            GameObject obj = targetObject.value;
            Selectable targetSelectable = obj != null ? obj.GetComponent<Selectable>() : null;
            return targetSelectable != null && CanInteract(targetSelectable) ? targetSelectable : null;
        }

        private void CompleteSuccess()
        {
            completed = true;
            SaveDroppedItems(droppedItemObjects ?? new List<GameObject>());
            EndAction(true);
        }

        private void SaveDroppedItems(List<GameObject> items)
        {
            droppedItems.value = items;
        }

        private void OnDropItem(Item item)
        {
            if (item != null && droppedItemObjects != null)
                droppedItemObjects.Add(item.gameObject);
        }

        private void SubscribeDropEvent(Selectable target)
        {
            if (target != null)
                target.onDropItem += OnDropItem;
        }

        private void UnsubscribeDropEvent(Selectable target)
        {
            if (target != null)
                target.onDropItem -= OnDropItem;
        }

        private bool CanInteract(Selectable target)
        {
            return player != null
                && target != null
                && target.CanBeInteracted()
                && HasInteraction(target);
        }
    }
}
