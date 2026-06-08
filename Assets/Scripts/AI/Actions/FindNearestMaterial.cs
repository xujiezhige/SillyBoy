using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using SurvivalEngine.Debugging;
using UnityEngine;

namespace SurvivalEngine
{
    [Name("FindNearestMaterial")]
    [Category("SurvivalEngine/Player")]
    [Description("Find the nearest world source that can provide one of the requested material ids.")]
    public class FindNearestMaterial : ActionTask
    {
        [Tooltip("Material item ids to search for in the world. Empty or null entries are ignored.")]
        public BBParameter<List<string>> materialItemIds;

        [Tooltip("Maximum search radius from the current player. Values below zero are treated as zero.")]
        public BBParameter<float> range = 999f;

        [Tooltip("When enabled, ignores matching items or drops that the player's inventory cannot currently take.")]
        public BBParameter<bool> requireInventorySpace = true;

        [Tooltip("When enabled, skips targets that recently failed movement or interaction and are still in short-term failure memory.")]
        public BBParameter<bool> avoidRecentlyFailedTargets = true;

        [Tooltip("Seconds a failed target stays blocked before the tree may consider it again.")]
        public BBParameter<float> failedTargetCooldown = 8f;

        [Tooltip("When enabled, validates that the player can compute a complete NavMesh path to the target interact point.")]
        public BBParameter<bool> requireReachablePath = true;

        [Tooltip("When enabled, skips wild animals that can fight back. The AI will prefer non-combat scene sources such as pickups, resource nodes, or fleeing animals.")]
        public BBParameter<bool> avoidDangerousAnimals = true;

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
        [Tooltip("Output GameObject of the found material source. This can be a pickup item or a destructible resource node.")]
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

            MaterialSource source = GetNearestMaterialSource(player, wantedIds);
            if (source == null)
            {
                HandleNoMaterialFound(wantedIds);
                ClearResult();
                EndAction(false);
                return;
            }

            Vector3 targetPosition = source.targetPosition;
            materialItemId.value = source.itemId;
            target.value = new Vector2(targetPosition.x, targetPosition.z);
            targetObject.value = source.gameObject;
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

        private MaterialSource GetNearestMaterialSource(PlayerCharacter player, HashSet<string> wantedIds)
        {
            MaterialSource nearest = null;
            Vector3 playerPosition = player.transform.position;
            float maxDistance = Mathf.Max(0f, range.value);
            float nearestSqrDistance = maxDistance * maxDistance;
            HashSet<GameObject> itemObjects = new HashSet<GameObject>();

            foreach (Item item in AIRuntimeSceneQuery.GetItems())
            {
                if (item == null)
                    continue;
                itemObjects.Add(item.gameObject);

                Vector3 targetPosition = GetTargetPosition(item, playerPosition);
                if (!IsCloserThanCurrent(targetPosition, playerPosition, nearestSqrDistance))
                    continue;

                if (!IsValidMaterialItem(item, player, wantedIds, targetPosition))
                    continue;

                float sqrDistance = (targetPosition - playerPosition).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = new MaterialSource(item.data.id, item.gameObject, targetPosition);
                }
            }

            foreach (Plant plant in Plant.GetAll())
            {
                string itemId;
                if (plant == null)
                    continue;

                Vector3 targetPosition = GetTargetPosition(plant.GetSelectable(), playerPosition);
                if (!IsCloserThanCurrent(targetPosition, playerPosition, nearestSqrDistance))
                    continue;

                if (!IsValidPlantSource(plant, player, wantedIds, targetPosition, out itemId))
                    continue;

                float sqrDistance = (targetPosition - playerPosition).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = new MaterialSource(itemId, plant.gameObject, targetPosition);
                }
            }

            foreach (Selectable selectable in Selectable.GetAll())
            {
                if (selectable == null)
                    continue;

                Vector3 targetPosition = GetTargetPosition(selectable, playerPosition);
                if (!IsCloserThanCurrent(targetPosition, playerPosition, nearestSqrDistance))
                    continue;
                float selectableSqrDistance = (targetPosition - playerPosition).sqrMagnitude;

                if (itemObjects.Contains(selectable.gameObject))
                    continue;

                string itemId;
                Destructible destructible = selectable.Destructible;
                if (IsValidDestructibleSource(destructible, player, wantedIds, targetPosition, out itemId))
                {
                    if (selectableSqrDistance < nearestSqrDistance)
                    {
                        nearestSqrDistance = selectableSqrDistance;
                        nearest = new MaterialSource(itemId, destructible.gameObject, targetPosition);
                    }
                    continue;
                }

                ItemProvider provider = HasProviderAction(selectable) ? selectable.GetComponent<ItemProvider>() : null;
                if (IsValidItemProviderSource(provider, selectable, player, wantedIds, targetPosition, out itemId))
                {
                    if (selectableSqrDistance < nearestSqrDistance)
                    {
                        nearestSqrDistance = selectableSqrDistance;
                        nearest = new MaterialSource(itemId, provider.gameObject, targetPosition);
                    }
                    continue;
                }

                if (!IsValidSelectableActionSource(selectable, player, wantedIds, provider, targetPosition, out itemId))
                    continue;

                if (selectableSqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = selectableSqrDistance;
                    nearest = new MaterialSource(itemId, selectable.gameObject, targetPosition);
                }
            }

            return nearest;
        }

        private bool HasProviderAction(Selectable selectable)
        {
            if (selectable == null || selectable.actions == null)
                return false;

            foreach (SAction action in selectable.actions)
            {
                if (action is ActionFish)
                    return true;
            }

            return false;
        }

        private bool IsCloserThanCurrent(Vector3 targetPosition, Vector3 playerPosition, float nearestSqrDistance)
        {
            return (targetPosition - playerPosition).sqrMagnitude < nearestSqrDistance;
        }

        private bool IsValidMaterialItem(Item item, PlayerCharacter player, HashSet<string> wantedIds, Vector3 targetPosition)
        {
            if (item == null || item.data == null || item.quantity <= 0 || !item.gameObject.activeInHierarchy)
                return false;

            if (!wantedIds.Contains(item.data.id))
                return false;

            Selectable selectable = item.GetSelectable();
            if (selectable != null && !selectable.CanBeInteracted())
                return false;

            if (requireInventorySpace.value && !AIInventorySpaceUtility.CanTakeOrMakeRoom(player, item.data, item.quantity))
                return false;

            if (avoidRecentlyFailedTargets.value &&
                AITargetFailureMemory.IsBlocked(item.gameObject, item.data.id, targetPosition, out _))
                return false;

            if (requireReachablePath.value && !HasReachablePath(player.transform.position, targetPosition))
                return false;

            return true;
        }

        private bool IsValidDestructibleSource(Destructible destructible, PlayerCharacter player, HashSet<string> wantedIds, Vector3 targetPosition, out string itemId)
        {
            itemId = null;
            if (destructible == null || !destructible.gameObject.activeInHierarchy || destructible.IsDead())
                return false;

            if (!CanAttackNowOrAfterEquipping(player, destructible))
                return false;

            if (avoidDangerousAnimals.value && IsDangerousAnimal(destructible))
                return false;

            ItemData lootItem = GetWantedLootItem(destructible, wantedIds);
            if (lootItem == null)
                return false;

            if (requireInventorySpace.value && !AIInventorySpaceUtility.CanTakeOrMakeRoom(player, lootItem, 1))
                return false;

            if (avoidRecentlyFailedTargets.value &&
                AITargetFailureMemory.IsBlocked(destructible.gameObject, lootItem.id, targetPosition, out _))
                return false;

            if (requireReachablePath.value && !HasReachablePath(player.transform.position, targetPosition))
                return false;

            itemId = lootItem.id;
            return true;
        }

        private bool IsValidPlantSource(Plant plant, PlayerCharacter player, HashSet<string> wantedIds, Vector3 targetPosition, out string itemId)
        {
            itemId = null;
            if (plant == null || plant.fruit == null || !plant.gameObject.activeInHierarchy || !plant.IsBuilt() || plant.IsDead() || !plant.HasFruit())
                return false;

            if (!wantedIds.Contains(plant.fruit.id))
                return false;

            Selectable selectable = plant.GetSelectable();
            if (selectable == null || !selectable.CanBeInteracted())
                return false;

            if (requireInventorySpace.value && !AIInventorySpaceUtility.CanTakeOrMakeRoom(player, plant.fruit, 1))
                return false;

            if (avoidRecentlyFailedTargets.value &&
                AITargetFailureMemory.IsBlocked(plant.gameObject, plant.fruit.id, targetPosition, out _))
                return false;

            if (requireReachablePath.value && !HasReachablePath(player.transform.position, targetPosition))
                return false;

            itemId = plant.fruit.id;
            return true;
        }

        private bool IsValidItemProviderSource(ItemProvider provider, Selectable selectable, PlayerCharacter player, HashSet<string> wantedIds, Vector3 targetPosition, out string itemId)
        {
            itemId = null;
            if (provider == null || !provider.gameObject.activeInHierarchy || !provider.HasItem() || provider.items == null)
                return false;

            if (selectable == null || !selectable.CanBeInteracted())
                return false;

            ItemData item = GetWantedProviderItem(provider, wantedIds);
            if (item == null)
                return false;

            if (requireInventorySpace.value && !AIInventorySpaceUtility.CanTakeOrMakeRoom(player, item, 1))
                return false;

            if (avoidRecentlyFailedTargets.value &&
                AITargetFailureMemory.IsBlocked(provider.gameObject, item.id, targetPosition, out _))
                return false;

            if (requireReachablePath.value && !HasReachablePath(player.transform.position, targetPosition))
                return false;

            itemId = item.id;
            return true;
        }

        private bool IsValidSelectableActionSource(Selectable selectable, PlayerCharacter player, HashSet<string> wantedIds, ItemProvider provider, Vector3 targetPosition, out string itemId)
        {
            itemId = null;
            if (selectable == null || !selectable.gameObject.activeInHierarchy || !selectable.CanBeInteracted())
                return false;

            ItemData mergeOutput = GetWantedMergeOutput(selectable, player, wantedIds);
            if (mergeOutput != null)
            {
                if (avoidRecentlyFailedTargets.value &&
                    AITargetFailureMemory.IsBlocked(selectable.gameObject, mergeOutput.id, targetPosition, out _))
                    return false;

                if (requireReachablePath.value && !HasReachablePath(player.transform.position, targetPosition))
                    return false;

                itemId = mergeOutput.id;
                return true;
            }

            ItemData sceneActionOutput = GetWantedSceneActionOutput(selectable, provider, player, wantedIds);
            if (sceneActionOutput != null)
            {
                if (avoidRecentlyFailedTargets.value &&
                    AITargetFailureMemory.IsBlocked(selectable.gameObject, sceneActionOutput.id, targetPosition, out _))
                    return false;

                if (requireReachablePath.value && !HasReachablePath(player.transform.position, targetPosition))
                    return false;

                itemId = sceneActionOutput.id;
                return true;
            }

            Destructible destructible = selectable.Destructible;
            if (destructible == null || destructible.IsDead() || destructible.loots == null)
                return false;

            AAction autoAction = selectable.FindAutoAction(player);
            if (autoAction == null)
                return false;

            ItemData lootItem = GetWantedLootItem(destructible, wantedIds);
            if (lootItem == null)
                return false;

            if (requireInventorySpace.value && !AIInventorySpaceUtility.CanTakeOrMakeRoom(player, lootItem, 1))
                return false;

            if (avoidRecentlyFailedTargets.value &&
                AITargetFailureMemory.IsBlocked(selectable.gameObject, lootItem.id, targetPosition, out _))
                return false;

            if (requireReachablePath.value && !HasReachablePath(player.transform.position, targetPosition))
                return false;

            itemId = lootItem.id;
            return true;
        }

        private bool IsDangerousAnimal(Destructible destructible)
        {
            AnimalWild animal = destructible != null ? destructible.GetComponent<AnimalWild>() : null;
            return animal != null && animal.HasAttackBehavior();
        }

        private bool CanAttackNowOrAfterEquipping(PlayerCharacter player, Destructible destructible)
        {
            if (player == null || destructible == null)
                return false;

            if (player.Combat.CanAttack(destructible))
                return true;

            GroupData requiredGroup = destructible.required_item;
            if (requiredGroup == null)
                return false;

            return HasEquipmentInGroup(player.Inventory.InventoryData, requiredGroup)
                || HasEquipmentInGroup(player.Inventory.BagData, requiredGroup);
        }

        private bool HasEquipmentInGroup(InventoryData inventory, GroupData group)
        {
            if (inventory == null || group == null)
                return false;

            foreach (KeyValuePair<int, InventoryItemData> pair in inventory.items)
            {
                InventoryItemData inventoryItem = pair.Value;
                ItemData item = ItemData.Get(inventoryItem?.item_id);
                if (item != null && inventoryItem != null && inventoryItem.quantity > 0 && item.type == ItemType.Equipment && item.HasGroup(group))
                    return true;
            }

            return false;
        }

        private ItemData GetWantedLootItem(Destructible destructible, HashSet<string> wantedIds)
        {
            if (destructible == null || destructible.loots == null)
                return null;

            foreach (SData loot in destructible.loots)
            {
                ItemData item = GetLootItem(loot);
                if (item != null && wantedIds.Contains(item.id))
                    return item;
            }

            return null;
        }

        private ItemData GetLootItem(SData loot)
        {
            if (loot is ItemData)
                return (ItemData)loot;

            LootData lootData = loot as LootData;
            if (lootData != null && lootData.probability > 0f)
                return lootData.item;

            return null;
        }

        private ItemData GetWantedProviderItem(ItemProvider provider, HashSet<string> wantedIds)
        {
            if (provider == null || provider.items == null)
                return null;

            foreach (ItemData item in provider.items)
            {
                if (item != null && wantedIds.Contains(item.id))
                    return item;
            }

            return null;
        }

        private ItemData GetWantedMergeOutput(Selectable selectable, PlayerCharacter player, HashSet<string> wantedIds)
        {
            if (selectable == null || player == null)
                return null;

            ItemData item = GetWantedMergeOutput(player.Inventory.InventoryData, selectable, player, wantedIds);
            if (item != null)
                return item;

            return GetWantedMergeOutput(player.Inventory.BagData, selectable, player, wantedIds);
        }

        private ItemData GetWantedMergeOutput(InventoryData inventory, Selectable selectable, PlayerCharacter player, HashSet<string> wantedIds)
        {
            if (inventory == null || selectable == null || player == null)
                return null;

            foreach (KeyValuePair<int, InventoryItemData> pair in inventory.items)
            {
                InventoryItemData inventoryItem = pair.Value;
                ItemData item = ItemData.Get(inventoryItem?.item_id);
                if (item == null || inventoryItem == null || inventoryItem.quantity <= 0)
                    continue;

                MAction action = item.FindMergeAction(selectable);
                ItemData output = GetMergeOutputItem(action);
                if (output == null || !wantedIds.Contains(output.id))
                    continue;

                if (requireInventorySpace.value && !AIInventorySpaceUtility.CanTakeOrMakeRoom(player, output, 1))
                    continue;

                return output;
            }

            return null;
        }

        private ItemData GetMergeOutputItem(MAction action)
        {
            ActionFill fill = action as ActionFill;
            if (fill != null)
                return fill.filled_item;

            ActionFillProvider fillProvider = action as ActionFillProvider;
            if (fillProvider != null)
                return fillProvider.filled_item;

            return null;
        }

        private ItemData GetWantedSceneActionOutput(Selectable selectable, ItemProvider provider, PlayerCharacter player, HashSet<string> wantedIds)
        {
            if (selectable == null || selectable.actions == null || provider == null || !provider.HasItem())
                return null;

            foreach (SAction action in selectable.actions)
            {
                ActionFish fish = action as ActionFish;
                if (fish == null)
                    continue;

                if (!CanEquipItemInGroup(player, fish.fishing_rod))
                    continue;

                ItemData output = GetWantedProviderItem(provider, wantedIds);
                if (output == null)
                    continue;

                if (requireInventorySpace.value && !AIInventorySpaceUtility.CanTakeOrMakeRoom(player, output, 1))
                    continue;

                return output;
            }

            return null;
        }

        private bool CanEquipItemInGroup(PlayerCharacter player, GroupData group)
        {
            if (player == null || group == null)
                return false;

            if (player.EquipData.HasItemInGroup(group))
                return true;

            return HasEquipmentInGroup(player.Inventory.InventoryData, group)
                || HasEquipmentInGroup(player.Inventory.BagData, group);
        }

        private Vector3 GetTargetPosition(Item item, Vector3 fromPosition)
        {
            Selectable selectable = item.GetSelectable();
            if (selectable != null)
                return selectable.GetClosestInteractPoint(fromPosition);

            return item.transform.position;
        }

        private Vector3 GetTargetPosition(Selectable selectable, Vector3 fromPosition)
        {
            if (selectable != null)
                return selectable.GetClosestInteractPoint(fromPosition);

            return fromPosition;
        }

        private bool HasReachablePath(Vector3 fromPosition, Vector3 toPosition)
        {
            return AIMovementReachability.HasReachablePath(AIRuntimeSceneQuery.GetPrimaryPlayer(), fromPosition, toPosition);
        }

        private sealed class MaterialSource
        {
            public readonly string itemId;
            public readonly GameObject gameObject;
            public readonly Vector3 targetPosition;

            public MaterialSource(string itemId, GameObject gameObject, Vector3 targetPosition)
            {
                this.itemId = itemId;
                this.gameObject = gameObject;
                this.targetPosition = targetPosition;
            }
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
