using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using SurvivalEngine.Debugging;
using UnityEngine;

namespace SurvivalEngine
{
    [Name("FindBestCraftableItem")]
    [Category("SurvivalEngine/Player")]
    [Description("Find the easiest useful item for the current player to craft and save its item id and missing material ids.")]
    public class FindBestCraftableItem : ActionTask
    {
        [Tooltip("When enabled, excludes recipes for items the current player already has in inventory.")]
        public bool skipOwnedItems = true;

        [Tooltip("When enabled, excludes recipes for items the current player has already crafted before.")]
        public bool skipAlreadyCraftedItems = true;

        [Tooltip("When enabled, only recipes already visible to the player are considered. Disable for full item-system completion passes.")]
        public bool requireLearnedRecipes = true;

        [Tooltip("When enabled, an already-crafted item can be crafted again if an unfinished recipe currently needs it as a material or requirement.")]
        public bool allowCraftedDependencyItems = false;

        [Tooltip("When enabled, outputs only missing material ids. When disabled, outputs all material ids required by the selected recipe.")]
        public bool outputOnlyMissingMaterials = true;

        [Tooltip("When enabled, skips craft candidates that recently failed because the tree could not find a gatherable source for their missing materials.")]
        public bool skipRecentlyFailedCraftCandidates = true;

        [Tooltip("When enabled, requires every distinct missing material id to have at least one reachable world item source before the candidate is considered.")]
        public bool requireGatherableMissingMaterials = true;

        [BlackboardOnly]
        [Tooltip("Output item id of the selected craftable item. Cleared when no suitable recipe is found.")]
        public BBParameter<string> itemId;

        [BlackboardOnly]
        [Tooltip("Output material item ids for the selected recipe. Contains missing materials only or all materials based on outputOnlyMissingMaterials.")]
        public BBParameter<List<string>> materialItemIds;

        private readonly Dictionary<string, bool> gatherableSourceCache = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> itemCountCache = new Dictionary<string, int>();
        private readonly Dictionary<GroupData, int> groupCountCache = new Dictionary<GroupData, int>();
        private List<Destructible> destructibleCache;
        private List<ItemProvider> itemProviderCache;

        protected override string info
        {
            get { return "Find best craftable item as " + itemId; }
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

            ResetExecutionCaches();
            CraftCandidate best = null;
            foreach (CraftData craft in CraftData.GetAll())
            {
                CraftCandidate candidate = GetCandidate(player, craft);
                if (candidate != null && IsBetterCandidate(candidate, best))
                    best = candidate;
            }

            if (best == null)
            {
                RecordNoCandidate();
                ClearResult();
                EndAction(false);
                return;
            }

            itemId.value = best.item.id;
            materialItemIds.value = outputOnlyMissingMaterials ? best.missingMaterialIds : best.allMaterialIds;
            EndAction(true);
        }

        private CraftCandidate GetCandidate(PlayerCharacter player, CraftData craft)
        {
            ItemData item = craft != null ? craft.GetItem() : null;
            if (item == null || item.craft_quantity <= 0)
                return null;

            if (!HasAnyRecipeInput(item))
                return null;

            bool learnt = item.craftable || player.SaveData.IsIDUnlocked(item.id);
            if (requireLearnedRecipes && !learnt)
                return null;

            if (skipOwnedItems && player.Inventory.HasItem(item, 1))
                return null;

            int craftedCount = player.Crafting.CountTotalCrafted(item);
            bool alreadyCrafted = craftedCount > 0;
            bool craftedDependency = alreadyCrafted && allowCraftedDependencyItems && IsNeededByUncraftedRecipe(player, item);
            if (skipAlreadyCraftedItems && alreadyCrafted && !craftedDependency)
                return null;

            if (skipRecentlyFailedCraftCandidates && AICraftCandidateFailureMemory.IsBlocked(item.id, out _))
                return null;

            CraftCostData cost = item.GetCraftCost();
            CraftCandidate candidate = new CraftCandidate(item);
            candidate.craftedCount = craftedCount;
            candidate.isCraftedDependency = craftedDependency;
            candidate.isLearned = learnt;
            Dictionary<GroupData, int> exactItemGroups = new Dictionary<GroupData, int>();

            foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
            {
                ItemData material = pair.Key;
                int required = pair.Value;
                if (material == null || required <= 0)
                    continue;

                AddRepeated(candidate.allMaterialIds, material.id, required);
                AddItemGroups(exactItemGroups, material, required);

                int missing = required - CountInventoryItem(player, material);
                if (missing > 0)
                {
                    candidate.missingItemCount += missing;
                    AddRepeated(candidate.missingMaterialIds, material.id, missing);
                }
            }

            foreach (KeyValuePair<GroupData, int> pair in cost.craft_fillers)
            {
                GroupData group = pair.Key;
                int required = pair.Value + CountGroup(exactItemGroups, group);
                if (group == null || required <= 0)
                    continue;

                string fillerId = GetFillerItemId(player, group);
                AddRepeated(candidate.allMaterialIds, fillerId, pair.Value);

                int missing = required - CountInventoryGroup(player, group);
                if (missing > 0)
                {
                    string missingFillerId = GetGatherableFillerItemId(player, group);
                    if (!string.IsNullOrEmpty(missingFillerId))
                        fillerId = missingFillerId;

                    candidate.missingItemCount += missing;
                    AddRepeated(candidate.missingMaterialIds, fillerId, missing);
                }
            }

            foreach (KeyValuePair<CraftData, int> pair in cost.craft_requirements)
            {
                CraftData requirement = pair.Key;
                int required = pair.Value;
                if (requirement == null || required <= 0)
                    continue;

                ItemData requirementItem = requirement.GetItem();
                if (requirementItem != null)
                    AddRepeated(candidate.allMaterialIds, requirementItem.id, required);

                int missing = required - player.Crafting.CountRequirements(requirement);
                if (missing > 0)
                {
                    candidate.missingRequirementCount += missing;
                    if (requirementItem != null)
                        AddRepeated(candidate.missingMaterialIds, requirementItem.id, missing);
                }
            }

            if (cost.craft_near != null && !player.Crafting.HasCraftNear(item))
                candidate.missingNearCount = 1;

            candidate.totalMaterialCount = candidate.allMaterialIds.Count;
            candidate.sortOrder = item.craft_sort_order;

            if (requireGatherableMissingMaterials && !HasGatherableSourcesForMissingMaterials(player, candidate))
                return null;

            return candidate;
        }

        private bool IsNeededByUncraftedRecipe(PlayerCharacter player, ItemData dependency)
        {
            if (player == null || dependency == null)
                return false;

            foreach (CraftData craft in CraftData.GetAll())
            {
                ItemData target = craft != null ? craft.GetItem() : null;
                if (target == null || target.craft_quantity <= 0)
                    continue;

                if (!HasAnyRecipeInput(target))
                    continue;

                if (target == dependency)
                    continue;

                if (player.Crafting.CountTotalCrafted(target) > 0)
                    continue;

                if (skipRecentlyFailedCraftCandidates && AICraftCandidateFailureMemory.IsBlocked(target.id, out _))
                    continue;

                CraftCostData cost = target.GetCraftCost();
                Dictionary<GroupData, int> exactItemGroups = new Dictionary<GroupData, int>();
                foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
                {
                    ItemData material = pair.Key;
                    int required = pair.Value;
                    if (material == null || required <= 0)
                        continue;

                    AddItemGroups(exactItemGroups, material, required);
                    if (material == dependency && CountInventoryItem(player, material) < required)
                        return true;
                }

                foreach (KeyValuePair<GroupData, int> pair in cost.craft_fillers)
                {
                    GroupData group = pair.Key;
                    if (group == null || !dependency.HasGroup(group))
                        continue;

                    int required = pair.Value + CountGroup(exactItemGroups, group);
                    if (required > 0 && CountInventoryGroup(player, group) < required)
                        return true;
                }

                foreach (KeyValuePair<CraftData, int> pair in cost.craft_requirements)
                {
                    if (pair.Key == dependency && pair.Value > 0 && player.Crafting.CountRequirements(pair.Key) < pair.Value)
                        return true;
                }
            }

            return false;
        }

        private bool HasAnyRecipeInput(CraftData craft)
        {
            if (craft == null)
                return false;

            return (craft.craft_items != null && craft.craft_items.Length > 0)
                || (craft.craft_fillers != null && craft.craft_fillers.Length > 0)
                || (craft.craft_requirements != null && craft.craft_requirements.Length > 0)
                || craft.craft_near != null;
        }

        private bool HasGatherableSourcesForMissingMaterials(PlayerCharacter player, CraftCandidate candidate)
        {
            if (candidate == null || candidate.missingMaterialIds.Count == 0)
                return true;

            Vector3 playerPosition = player.transform.position;
            HashSet<string> checkedIds = new HashSet<string>();
            foreach (string missingId in candidate.missingMaterialIds)
            {
                if (string.IsNullOrEmpty(missingId) || !checkedIds.Add(missingId))
                    continue;

                if (!HasGatherableWorldSource(player, playerPosition, missingId))
                    return false;
            }

            return true;
        }

        private bool HasGatherableWorldSource(PlayerCharacter player, Vector3 playerPosition, string itemId)
        {
            bool cached;
            if (gatherableSourceCache.TryGetValue(itemId, out cached))
                return cached;

            bool hasSource = HasGatherableWorldSourceUncached(player, playerPosition, itemId);
            gatherableSourceCache[itemId] = hasSource;
            return hasSource;
        }

        private bool HasGatherableWorldSourceUncached(PlayerCharacter player, Vector3 playerPosition, string itemId)
        {
            foreach (Item item in AIRuntimeSceneQuery.GetItems())
            {
                if (item == null || item.data == null || item.quantity <= 0 || !item.gameObject.activeInHierarchy)
                    continue;

                if (!string.Equals(item.data.id, itemId, System.StringComparison.Ordinal))
                    continue;

                Selectable selectable = item.GetSelectable();
                if (selectable != null && !selectable.CanBeInteracted())
                    continue;

                if (!AIInventorySpaceUtility.CanTakeOrMakeRoom(player, item.data, item.quantity))
                    continue;

                if (!HasReachablePath(player, playerPosition, GetTargetPosition(item, playerPosition)))
                    continue;

                return true;
            }

            foreach (Destructible destructible in GetDestructibles())
            {
                if (!CanHarvestLootFromDestructible(player, destructible, itemId))
                    continue;

                if (!HasReachablePath(player, playerPosition, destructible.transform.position))
                    continue;

                return true;
            }

            foreach (Plant plant in Plant.GetAll())
            {
                if (!CanHarvestFruitFromPlant(player, plant, itemId))
                    continue;

                Selectable selectable = plant.GetSelectable();
                Vector3 targetPosition = GetTargetPosition(selectable, playerPosition);
                if (!HasReachablePath(player, playerPosition, targetPosition))
                    continue;

                return true;
            }

            foreach (ItemProvider provider in GetItemProviders())
            {
                if (!CanTakeFromItemProvider(player, provider, itemId))
                    continue;

                Selectable selectable = provider.GetComponent<Selectable>();
                Vector3 targetPosition = GetTargetPosition(selectable, playerPosition);
                if (!HasReachablePath(player, playerPosition, targetPosition))
                    continue;

                return true;
            }

            foreach (Selectable selectable in Selectable.GetAll())
            {
                if (!CanUseSelectableActionSource(player, selectable, itemId))
                    continue;

                Vector3 targetPosition = GetTargetPosition(selectable, playerPosition);
                if (!HasReachablePath(player, playerPosition, targetPosition))
                    continue;

                return true;
            }

            return false;
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

        private bool HasReachablePath(PlayerCharacter player, Vector3 fromPosition, Vector3 toPosition)
        {
            return AIMovementReachability.HasReachablePath(player, fromPosition, toPosition);
        }

        private bool IsBetterCandidate(CraftCandidate candidate, CraftCandidate best)
        {
            if (best == null)
                return true;

            if (candidate.isLearned != best.isLearned)
                return candidate.isLearned;

            int score = candidate.GetScore();
            int bestScore = best.GetScore();
            if (score != bestScore)
                return score < bestScore;

            if (candidate.isCraftedDependency != best.isCraftedDependency)
                return !candidate.isCraftedDependency;

            if (candidate.totalMaterialCount != best.totalMaterialCount)
                return candidate.totalMaterialCount < best.totalMaterialCount;

            if (candidate.sortOrder != best.sortOrder)
                return candidate.sortOrder < best.sortOrder;

            return string.Compare(candidate.item.title, best.item.title, System.StringComparison.Ordinal) < 0;
        }

        private string GetFillerItemId(PlayerCharacter player, GroupData group)
        {
            InventoryItemData owned = player.Inventory.GetFirstItemInGroup(group);
            if (owned != null && !string.IsNullOrEmpty(owned.item_id))
                return owned.item_id;

            ItemData best = null;
            foreach (ItemData item in ItemData.GetAll())
            {
                if (item != null && item.HasGroup(group) && (best == null || string.Compare(item.id, best.id, System.StringComparison.Ordinal) < 0))
                    best = item;
            }

            if (best != null)
                return best.id;

            return !string.IsNullOrEmpty(group.group_id) ? group.group_id : group.name;
        }

        private string GetGatherableFillerItemId(PlayerCharacter player, GroupData group)
        {
            if (player == null || group == null)
                return null;

            Vector3 playerPosition = player.transform.position;
            Item bestItem = null;
            float bestSqrDistance = float.MaxValue;

            foreach (Item item in AIRuntimeSceneQuery.GetItems())
            {
                if (item == null || item.data == null || item.quantity <= 0 || !item.gameObject.activeInHierarchy)
                    continue;

                if (!item.data.HasGroup(group))
                    continue;

                Selectable selectable = item.GetSelectable();
                if (selectable != null && !selectable.CanBeInteracted())
                    continue;

                if (!AIInventorySpaceUtility.CanTakeOrMakeRoom(player, item.data, item.quantity))
                    continue;

                Vector3 targetPosition = GetTargetPosition(item, playerPosition);
                if (!HasReachablePath(player, playerPosition, targetPosition))
                    continue;

                float sqrDistance = (targetPosition - playerPosition).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestItem = item;
                }
            }

            string bestDestructibleItemId = null;
            foreach (Destructible destructible in GetDestructibles())
            {
                ItemData lootItem = GetLootItemInGroup(destructible, group);
                if (lootItem == null || !CanHarvestLootFromDestructible(player, destructible, lootItem.id))
                    continue;

                Vector3 targetPosition = destructible.transform.position;
                if (!HasReachablePath(player, playerPosition, targetPosition))
                    continue;

                float sqrDistance = (targetPosition - playerPosition).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestItem = null;
                    bestDestructibleItemId = lootItem.id;
                }
            }

            string bestPlantItemId = null;
            foreach (Plant plant in Plant.GetAll())
            {
                if (plant == null || plant.fruit == null || !plant.fruit.HasGroup(group) || !CanHarvestFruitFromPlant(player, plant, plant.fruit.id))
                    continue;

                Selectable selectable = plant.GetSelectable();
                Vector3 targetPosition = GetTargetPosition(selectable, playerPosition);
                if (!HasReachablePath(player, playerPosition, targetPosition))
                    continue;

                float sqrDistance = (targetPosition - playerPosition).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestItem = null;
                    bestDestructibleItemId = null;
                    bestPlantItemId = plant.fruit.id;
                }
            }

            string bestProviderItemId = null;
            foreach (ItemProvider provider in GetItemProviders())
            {
                ItemData item = GetProviderItemInGroup(provider, group);
                if (item == null || !CanTakeFromItemProvider(player, provider, item.id))
                    continue;

                Selectable selectable = provider.GetComponent<Selectable>();
                Vector3 targetPosition = GetTargetPosition(selectable, playerPosition);
                if (!HasReachablePath(player, playerPosition, targetPosition))
                    continue;

                float sqrDistance = (targetPosition - playerPosition).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestItem = null;
                    bestDestructibleItemId = null;
                    bestPlantItemId = null;
                    bestProviderItemId = item.id;
                }
            }

            if (bestItem != null && bestItem.data != null)
                return bestItem.data.id;

            if (!string.IsNullOrEmpty(bestPlantItemId))
                return bestPlantItemId;

            if (!string.IsNullOrEmpty(bestProviderItemId))
                return bestProviderItemId;

            string bestSelectableItemId = null;
            foreach (Selectable selectable in Selectable.GetAll())
            {
                ItemData item = GetSelectableActionItemInGroup(player, selectable, group);
                if (item == null || !CanUseSelectableActionSource(player, selectable, item.id))
                    continue;

                Vector3 targetPosition = GetTargetPosition(selectable, playerPosition);
                if (!HasReachablePath(player, playerPosition, targetPosition))
                    continue;

                float sqrDistance = (targetPosition - playerPosition).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestItem = null;
                    bestDestructibleItemId = null;
                    bestPlantItemId = null;
                    bestProviderItemId = null;
                    bestSelectableItemId = item.id;
                }
            }

            if (!string.IsNullOrEmpty(bestSelectableItemId))
                return bestSelectableItemId;

            return bestDestructibleItemId;
        }

        private bool CanHarvestLootFromDestructible(PlayerCharacter player, Destructible destructible, string itemId)
        {
            if (player == null || destructible == null || string.IsNullOrEmpty(itemId))
                return false;

            if (!destructible.gameObject.activeInHierarchy || destructible.IsDead() || IsDangerousAnimal(destructible) || !CanAttackNowOrAfterEquipping(player, destructible))
                return false;

            ItemData lootItem = GetLootItem(destructible, itemId);
            return lootItem != null && AIInventorySpaceUtility.CanTakeOrMakeRoom(player, lootItem, 1);
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

        private ItemData GetLootItem(Destructible destructible, string itemId)
        {
            if (destructible == null || destructible.loots == null)
                return null;

            foreach (SData loot in destructible.loots)
            {
                ItemData item = GetLootItem(loot);
                if (item != null && string.Equals(item.id, itemId, System.StringComparison.Ordinal))
                    return item;
            }

            return null;
        }

        private ItemData GetLootItemInGroup(Destructible destructible, GroupData group)
        {
            if (destructible == null || destructible.loots == null || group == null)
                return null;

            foreach (SData loot in destructible.loots)
            {
                ItemData item = GetLootItem(loot);
                if (item != null && item.HasGroup(group))
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

        private bool CanHarvestFruitFromPlant(PlayerCharacter player, Plant plant, string itemId)
        {
            if (player == null || plant == null || plant.fruit == null || string.IsNullOrEmpty(itemId))
                return false;

            if (!plant.gameObject.activeInHierarchy || !plant.IsBuilt() || plant.IsDead() || !plant.HasFruit())
                return false;

            if (!string.Equals(plant.fruit.id, itemId, System.StringComparison.Ordinal))
                return false;

            Selectable selectable = plant.GetSelectable();
            return selectable != null && selectable.CanBeInteracted() && AIInventorySpaceUtility.CanTakeOrMakeRoom(player, plant.fruit, 1);
        }

        private bool CanTakeFromItemProvider(PlayerCharacter player, ItemProvider provider, string itemId)
        {
            if (player == null || provider == null || string.IsNullOrEmpty(itemId))
                return false;

            if (!provider.gameObject.activeInHierarchy || !provider.HasItem())
                return false;

            Selectable selectable = provider.GetComponent<Selectable>();
            if (selectable == null || !selectable.CanBeInteracted())
                return false;

            ItemData item = GetProviderItem(provider, itemId);
            return item != null && AIInventorySpaceUtility.CanTakeOrMakeRoom(player, item, 1);
        }

        private bool CanUseSelectableActionSource(PlayerCharacter player, Selectable selectable, string itemId)
        {
            if (player == null || selectable == null || string.IsNullOrEmpty(itemId))
                return false;

            if (!selectable.gameObject.activeInHierarchy || !selectable.CanBeInteracted())
                return false;

            ItemData mergeItem = GetSelectableMergeOutput(player, selectable, itemId);
            if (mergeItem != null)
                return AIInventorySpaceUtility.CanTakeOrMakeRoom(player, mergeItem, 1);

            ItemData sceneActionItem = GetSelectableSceneActionOutput(player, selectable, itemId);
            if (sceneActionItem != null)
                return AIInventorySpaceUtility.CanTakeOrMakeRoom(player, sceneActionItem, 1);

            Destructible destructible = selectable.Destructible;
            if (destructible == null || destructible.IsDead())
                return false;

            AAction autoAction = selectable.FindAutoAction(player);
            if (autoAction == null)
                return false;

            ItemData lootItem = GetLootItem(destructible, itemId);
            return lootItem != null && AIInventorySpaceUtility.CanTakeOrMakeRoom(player, lootItem, 1);
        }

        private ItemData GetSelectableActionItemInGroup(PlayerCharacter player, Selectable selectable, GroupData group)
        {
            if (player == null || selectable == null || group == null)
                return null;

            ItemData mergeItem = GetSelectableMergeOutputInGroup(player, selectable, group);
            if (mergeItem != null)
                return mergeItem;

            ItemData sceneActionItem = GetSelectableSceneActionOutputInGroup(player, selectable, group);
            if (sceneActionItem != null)
                return sceneActionItem;

            Destructible destructible = selectable.Destructible;
            if (destructible == null || selectable.FindAutoAction(player) == null)
                return null;

            return GetLootItemInGroup(destructible, group);
        }

        private ItemData GetSelectableMergeOutput(PlayerCharacter player, Selectable selectable, string itemId)
        {
            ItemData item = GetSelectableMergeOutput(player.Inventory.InventoryData, selectable, itemId);
            if (item != null)
                return item;

            return GetSelectableMergeOutput(player.Inventory.BagData, selectable, itemId);
        }

        private ItemData GetSelectableMergeOutput(InventoryData inventory, Selectable selectable, string itemId)
        {
            if (inventory == null || selectable == null || string.IsNullOrEmpty(itemId))
                return null;

            foreach (KeyValuePair<int, InventoryItemData> pair in inventory.items)
            {
                InventoryItemData inventoryItem = pair.Value;
                ItemData item = ItemData.Get(inventoryItem?.item_id);
                if (item == null || inventoryItem == null || inventoryItem.quantity <= 0)
                    continue;

                ItemData output = GetMergeOutputItem(item.FindMergeAction(selectable));
                if (output != null && string.Equals(output.id, itemId, System.StringComparison.Ordinal))
                    return output;
            }

            return null;
        }

        private ItemData GetSelectableMergeOutputInGroup(PlayerCharacter player, Selectable selectable, GroupData group)
        {
            ItemData item = GetSelectableMergeOutputInGroup(player.Inventory.InventoryData, selectable, group);
            if (item != null)
                return item;

            return GetSelectableMergeOutputInGroup(player.Inventory.BagData, selectable, group);
        }

        private ItemData GetSelectableMergeOutputInGroup(InventoryData inventory, Selectable selectable, GroupData group)
        {
            if (inventory == null || selectable == null || group == null)
                return null;

            foreach (KeyValuePair<int, InventoryItemData> pair in inventory.items)
            {
                InventoryItemData inventoryItem = pair.Value;
                ItemData item = ItemData.Get(inventoryItem?.item_id);
                if (item == null || inventoryItem == null || inventoryItem.quantity <= 0)
                    continue;

                ItemData output = GetMergeOutputItem(item.FindMergeAction(selectable));
                if (output != null && output.HasGroup(group))
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

        private ItemData GetSelectableSceneActionOutput(PlayerCharacter player, Selectable selectable, string itemId)
        {
            ItemProvider provider = selectable.GetComponent<ItemProvider>();
            if (provider == null || !provider.HasItem() || selectable.actions == null)
                return null;

            foreach (SAction action in selectable.actions)
            {
                ActionFish fish = action as ActionFish;
                if (fish == null || !CanEquipItemInGroup(player, fish.fishing_rod))
                    continue;

                ItemData providerItem = GetProviderItem(provider, itemId);
                if (providerItem != null)
                    return providerItem;
            }

            return null;
        }

        private ItemData GetSelectableSceneActionOutputInGroup(PlayerCharacter player, Selectable selectable, GroupData group)
        {
            ItemProvider provider = selectable.GetComponent<ItemProvider>();
            if (provider == null || !provider.HasItem() || selectable.actions == null)
                return null;

            foreach (SAction action in selectable.actions)
            {
                ActionFish fish = action as ActionFish;
                if (fish == null || !CanEquipItemInGroup(player, fish.fishing_rod))
                    continue;

                ItemData providerItem = GetProviderItemInGroup(provider, group);
                if (providerItem != null)
                    return providerItem;
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

        private ItemData GetProviderItem(ItemProvider provider, string itemId)
        {
            if (provider == null || provider.items == null)
                return null;

            foreach (ItemData item in provider.items)
            {
                if (item != null && string.Equals(item.id, itemId, System.StringComparison.Ordinal))
                    return item;
            }

            return null;
        }

        private ItemData GetProviderItemInGroup(ItemProvider provider, GroupData group)
        {
            if (provider == null || provider.items == null || group == null)
                return null;

            foreach (ItemData item in provider.items)
            {
                if (item != null && item.HasGroup(group))
                    return item;
            }

            return null;
        }

        private void AddItemGroups(Dictionary<GroupData, int> groups, ItemData item, int quantity)
        {
            foreach (GroupData group in item.groups)
            {
                if (group == null)
                    continue;

                if (groups.ContainsKey(group))
                    groups[group] += quantity;
                else
                    groups[group] = quantity;
            }
        }

        private int CountGroup(Dictionary<GroupData, int> groups, GroupData group)
        {
            if (group != null && groups.ContainsKey(group))
                return groups[group];
            return 0;
        }

        private void ResetExecutionCaches()
        {
            gatherableSourceCache.Clear();
            itemCountCache.Clear();
            groupCountCache.Clear();
            destructibleCache = null;
            itemProviderCache = null;
        }

        private List<Destructible> GetDestructibles()
        {
            if (destructibleCache == null)
                destructibleCache = new List<Destructible>(Object.FindObjectsOfType<Destructible>(true));
            return destructibleCache;
        }

        private List<ItemProvider> GetItemProviders()
        {
            if (itemProviderCache == null)
                itemProviderCache = new List<ItemProvider>(Object.FindObjectsOfType<ItemProvider>(true));
            return itemProviderCache;
        }

        private int CountInventoryItem(PlayerCharacter player, ItemData item)
        {
            if (player == null || item == null)
                return 0;

            int count;
            if (!itemCountCache.TryGetValue(item.id, out count))
            {
                count = player.Inventory.CountItem(item);
                itemCountCache[item.id] = count;
            }
            return count;
        }

        private int CountInventoryGroup(PlayerCharacter player, GroupData group)
        {
            if (player == null || group == null)
                return 0;

            int count;
            if (!groupCountCache.TryGetValue(group, out count))
            {
                count = player.Inventory.CountItemInGroup(group);
                groupCountCache[group] = count;
            }
            return count;
        }

        private void AddRepeated(List<string> list, string id, int quantity)
        {
            if (string.IsNullOrEmpty(id) || quantity <= 0)
                return;

            for (int i = 0; i < quantity; i++)
                list.Add(id);
        }

        private void ClearResult()
        {
            itemId.value = string.Empty;
            materialItemIds.value = new List<string>();
        }

        private class CraftCandidate
        {
            [Tooltip("Item data produced by this craft candidate.")]
            public ItemData item;

            [Tooltip("Total missing exact item material count for this candidate.")]
            public int missingItemCount;

            [Tooltip("Total missing prerequisite craft requirement count for this candidate.")]
            public int missingRequirementCount;

            [Tooltip("Set when this candidate requires a nearby crafting station or object that the player is not currently near.")]
            public int missingNearCount;

            [Tooltip("Total number of material ids required by this candidate, including duplicates for required quantities.")]
            public int totalMaterialCount;

            [Tooltip("Craft sorting order copied from the item data and used as a tie breaker.")]
            public int sortOrder;

            [Tooltip("Number of times this recipe has already been crafted.")]
            public int craftedCount;

            [Tooltip("True when this already-crafted item is being made again to satisfy an unfinished recipe.")]
            public bool isCraftedDependency;

            [Tooltip("True when this recipe is already visible to the player.")]
            public bool isLearned;

            [Tooltip("All material item ids required by this candidate, including duplicates for required quantities.")]
            public List<string> allMaterialIds = new List<string>();

            [Tooltip("Only the material item ids still missing for this candidate, including duplicates for missing quantities.")]
            public List<string> missingMaterialIds = new List<string>();

            public CraftCandidate(ItemData item)
            {
                this.item = item;
            }

            public int GetScore()
            {
                return missingItemCount + missingRequirementCount * 10 + missingNearCount * 20;
            }
        }
    

private void RecordNoCandidate()
        {
            var debugger = GameStateDebugger.Instance;
            if (debugger == null)
                return;

            debugger.RecordEvent(
                "behavior_tree",
                "find_best_craftable_no_candidate",
                "FindBestCraftableItem did not find a learned, unowned craft candidate with currently gatherable missing materials.",
                "info",
                new Dictionary<string, object>
                {
                    ["skip_owned_items"] = skipOwnedItems,
                    ["skip_already_crafted_items"] = skipAlreadyCraftedItems,
                    ["require_learned_recipes"] = requireLearnedRecipes,
                    ["allow_crafted_dependency_items"] = allowCraftedDependencyItems,
                    ["require_gatherable_missing_materials"] = requireGatherableMissingMaterials,
                    ["skip_recently_failed_craft_candidates"] = skipRecentlyFailedCraftCandidates
                });
        }
}
}
