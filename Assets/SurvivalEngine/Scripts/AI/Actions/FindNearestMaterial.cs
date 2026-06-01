using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
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
            PlayerCharacter player = PlayerCharacter.GetFirst();
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

            foreach (Item item in Item.GetAll())
            {
                if (!IsValidMaterial(item, player, wantedIds))
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

        private bool IsValidMaterial(Item item, PlayerCharacter player, HashSet<string> wantedIds)
        {
            if (item == null || item.data == null || item.quantity <= 0 || !item.gameObject.activeInHierarchy)
                return false;

            if (!wantedIds.Contains(item.data.id))
                return false;

            Selectable selectable = item.GetSelectable();
            if (selectable != null && !selectable.CanBeInteracted())
                return false;

            return !requireInventorySpace.value || player.Inventory.CanTakeItem(item.data, item.quantity);
        }

        private Vector3 GetTargetPosition(Item item, Vector3 fromPosition)
        {
            Selectable selectable = item.GetSelectable();
            if (selectable != null)
                return selectable.GetClosestInteractPoint(fromPosition);

            return item.transform.position;
        }

        private void ClearResult()
        {
            materialItemId.value = string.Empty;
            target.value = Vector2.zero;
            targetObject.value = null;
        }
    }
}
