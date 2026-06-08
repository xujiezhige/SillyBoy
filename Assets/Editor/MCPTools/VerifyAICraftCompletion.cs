using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using SurvivalEngine;
using SurvivalEngine.Debugging;
using UnityEngine;

namespace SillyBoy.Editor.MCPTools
{
    [McpForUnityTool(
        "verify_ai_craft_completion",
        Description = "Verify whether every item recipe in the item system has been crafted at least once, with current-candidate diagnostics."
    )]
    public static class VerifyAICraftCompletion
    {
        private const int MaxObtainabilityDepth = 24;
        private static readonly Dictionary<string, bool> reachableSourceCache = new Dictionary<string, bool>();
        private static readonly Dictionary<string, string> reachableGroupSourceCache = new Dictionary<string, string>();
        private static readonly Dictionary<string, bool> obtainableSourceCache = new Dictionary<string, bool>();
        private static readonly Dictionary<string, string> obtainableGroupSourceCache = new Dictionary<string, string>();
        private static readonly Dictionary<string, bool> generatedSourceCache = new Dictionary<string, bool>();
        private static System.Diagnostics.Stopwatch verificationStopwatch;
        private static int verificationTimeoutMs;

        public static object HandleCommand(JObject @params)
        {
            try
            {
                obtainableSourceCache.Clear();
                obtainableGroupSourceCache.Clear();
                reachableSourceCache.Clear();
                reachableGroupSourceCache.Clear();
                generatedSourceCache.Clear();

                int maxCandidates = @params["max_candidates"]?.ToObject<int?>() ?? 40;
                verificationTimeoutMs = Mathf.Max(1000, @params["timeout_ms"]?.ToObject<int?>() ?? 8000);
                verificationStopwatch = System.Diagnostics.Stopwatch.StartNew();
                bool requireLearnedRecipes = @params["require_learned_recipes"]?.ToObject<bool?>() ?? false;
                var player = AIRuntimeSceneQuery.GetPrimaryPlayer();
                if (player == null)
                    return new ErrorResponse("No PlayerCharacter was found.");

                EnsureDataLoaded();

                var uncraftedRecipes = new List<Dictionary<string, object>>();
                var currentCandidates = new List<Dictionary<string, object>>();
                int includedRecipeCount = 0;
                int totalRecipeCount = 0;
                int itemRecipeCount = 0;
                int gatherOnlyItemCount = 0;
                int craftedRecipeCount = 0;

                foreach (CraftData craft in CraftData.GetAll())
                {
                    CheckDeadline();

                    totalRecipeCount++;
                    CraftData data = craft;
                    ItemData item = data != null ? data.GetItem() : null;
                    if (item == null || item.craft_quantity <= 0)
                        continue;

                    itemRecipeCount++;
                    if (!HasAnyRecipeInput(item))
                    {
                        gatherOnlyItemCount++;
                        continue;
                    }

                    bool learned = item.craftable || player.SaveData.IsIDUnlocked(item.id);
                    if (requireLearnedRecipes && !learned)
                        continue;

                    includedRecipeCount++;
                    CandidateState state = GetCandidateState(player, item);
                    int craftedCount = player.Crafting.CountTotalCrafted(item);
                    if (craftedCount > 0)
                    {
                        craftedRecipeCount++;
                        continue;
                    }

                    uncraftedRecipes.Add(ToDictionary(player, item, state));
                    if (state.remaining)
                        currentCandidates.Add(ToDictionary(player, item, state));
                }

                uncraftedRecipes = uncraftedRecipes
                    .OrderBy(c => Convert.ToInt32(c["score"]))
                    .ThenBy(c => Convert.ToString(c["item_id"]))
                    .ToList();

                currentCandidates = currentCandidates
                    .OrderBy(c => Convert.ToInt32(c["score"]))
                    .ThenBy(c => Convert.ToString(c["item_id"]))
                    .ToList();

                var report = new Dictionary<string, object>
                {
                    ["is_complete"] = uncraftedRecipes.Count == 0,
                    ["uncrafted_item_recipe_count"] = uncraftedRecipes.Count,
                    ["uncrafted_item_recipes"] = uncraftedRecipes.Take(Mathf.Max(1, maxCandidates)).ToList(),
                    ["current_candidate_count"] = currentCandidates.Count,
                    ["current_candidates"] = currentCandidates.Take(Mathf.Max(1, maxCandidates)).ToList(),
                    ["total_recipe_count"] = totalRecipeCount,
                    ["item_recipe_count"] = itemRecipeCount,
                    ["gather_only_item_count"] = gatherOnlyItemCount,
                    ["included_item_recipe_count"] = includedRecipeCount,
                    ["crafted_item_recipe_count"] = craftedRecipeCount,
                    ["completion_scope"] = "ItemData recipes with at least one recipe input. No-input base items are gather-only scene materials, not craft targets.",
                    ["require_learned_recipes"] = requireLearnedRecipes,
                    ["active_world_item_count"] = AIRuntimeSceneQuery.GetItems().Count(i => i != null && i.data != null && i.gameObject.activeInHierarchy),
                    ["player_name"] = player.name,
                    ["player_position"] = FormatVector3(player.transform.position),
                    ["craft_candidate_failure_memory"] = AICraftCandidateFailureMemory.GetSnapshot(),
                    ["target_failure_memory"] = AITargetFailureMemory.GetSnapshot()
                };

                return new SuccessResponse(
                    uncraftedRecipes.Count == 0
                        ? "AI craft completion verified: every included item recipe has been crafted at least once."
                        : "AI craft completion is not finished: uncrafted item recipes remain.",
                    report);
            }
            catch (Exception e)
            {
                return new ErrorResponse(
                    "AI craft completion verification failed.",
                    new
                    {
                        exception_type = e.GetType().Name,
                        message = e.Message,
                        stack_trace = e.StackTrace
                    });
            }
        }

        private static CandidateState GetCandidateState(PlayerCharacter player, ItemData item)
        {
            CheckDeadline();

            var state = new CandidateState();
            if (player.Crafting.CanCraft(item))
            {
                state.remaining = true;
                state.immediate = true;
                return state;
            }

            CraftCostData cost = item.GetCraftCost();
            var exactItemGroups = new Dictionary<GroupData, int>();

            foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
            {
                ItemData material = pair.Key;
                int required = pair.Value;
                if (material == null || required <= 0)
                    continue;

                AddItemGroups(exactItemGroups, material, required);
                int owned = player.Inventory.CountItem(material);
                int missing = Mathf.Max(0, required - owned);
                if (missing > 0)
                {
                    state.missingMaterialCount += missing;
                    state.missing.Add(MaterialEntry(material.id, material.title, missing, owned, required, HasObtainableSource(player, material.id)));
                }
            }

            foreach (KeyValuePair<GroupData, int> pair in cost.craft_fillers)
            {
                GroupData group = pair.Key;
                if (group == null)
                    continue;

                int required = pair.Value + CountGroup(exactItemGroups, group);
                int owned = player.Inventory.CountItemInGroup(group);
                int missing = Mathf.Max(0, required - owned);
                if (missing > 0)
                {
                    string sourceId = FindObtainableSourceInGroup(player, group);
                    bool hasSource = !string.IsNullOrEmpty(sourceId);
                    state.missingMaterialCount += missing;
                    state.missing.Add(new Dictionary<string, object>
                    {
                        ["item_id"] = sourceId ?? GroupId(group),
                        ["title"] = group.title,
                        ["missing"] = missing,
                        ["owned"] = owned,
                        ["required"] = required,
                        ["has_reachable_source"] = hasSource,
                        ["is_group"] = true
                    });
                }
            }

            foreach (KeyValuePair<CraftData, int> pair in cost.craft_requirements)
            {
                CraftData requirement = pair.Key;
                int required = pair.Value;
                if (requirement == null || required <= 0)
                    continue;

                int owned = player.Crafting.CountRequirements(requirement);
                int missing = Mathf.Max(0, required - owned);
                if (missing > 0)
                {
                    state.missingRequirementCount += missing;
                    state.missing.Add(new Dictionary<string, object>
                    {
                        ["item_id"] = requirement.id,
                        ["title"] = requirement.title,
                        ["missing"] = missing,
                        ["owned"] = owned,
                        ["required"] = required,
                        ["has_reachable_source"] = false,
                        ["is_requirement"] = true
                    });
                }
            }

            if (cost.craft_near != null && !player.Crafting.HasCraftNear(item))
            {
                state.missingNear = true;
                state.missing.Add(new Dictionary<string, object>
                {
                    ["item_id"] = GroupId(cost.craft_near),
                    ["title"] = cost.craft_near.title,
                    ["missing"] = 1,
                    ["owned"] = 0,
                    ["required"] = 1,
                    ["has_reachable_source"] = false,
                    ["is_near_requirement"] = true
                });
            }

            state.remaining = state.missingMaterialCount > 0
                && state.missing
                    .Where(m => Convert.ToInt32(m["missing"]) > 0)
                    .Where(m => !m.ContainsKey("is_requirement") && !m.ContainsKey("is_near_requirement"))
                    .All(m => Convert.ToBoolean(m["has_reachable_source"]));

            return state;
        }

        private static Dictionary<string, object> ToDictionary(PlayerCharacter player, ItemData item, CandidateState state)
        {
            return new Dictionary<string, object>
            {
                ["item_id"] = item.id,
                ["title"] = item.title,
                ["craft_quantity"] = item.craft_quantity,
                ["inventory_count"] = player.Inventory.CountItem(item),
                ["crafted_count"] = player.Crafting.CountTotalCrafted(item),
                ["learned"] = item.craftable || player.SaveData.IsIDUnlocked(item.id),
                ["immediate"] = state.immediate,
                ["currently_actionable"] = state.remaining,
                ["missing_material_count"] = state.missingMaterialCount,
                ["missing_requirement_count"] = state.missingRequirementCount,
                ["missing_near"] = state.missingNear,
                ["score"] = state.Score,
                ["missing"] = state.missing
            };
        }

        private static Dictionary<string, object> MaterialEntry(string itemId, string title, int missing, int owned, int required, bool hasSource)
        {
            return new Dictionary<string, object>
            {
                ["item_id"] = itemId,
                ["title"] = title,
                ["missing"] = missing,
                ["owned"] = owned,
                ["required"] = required,
                ["has_reachable_source"] = hasSource
            };
        }

        private static Dictionary<string, object> FormatVector3(Vector3 value)
        {
            return new Dictionary<string, object>
            {
                ["x"] = value.x,
                ["y"] = value.y,
                ["z"] = value.z
            };
        }

        private static bool HasReachableSource(PlayerCharacter player, string itemId)
        {
            CheckDeadline();

            if (reachableSourceCache.TryGetValue(itemId, out bool cached))
                return cached;

            bool reachable = AIRuntimeSceneQuery.GetItems().Any(item =>
                item != null &&
                item.data != null &&
                string.Equals(item.data.id, itemId, StringComparison.Ordinal) &&
                IsReachableTakeableItem(player, item));

            if (!reachable)
            {
                reachable = UnityEngine.Object.FindObjectsOfType<Destructible>(true)
                    .Any(destructible => IsReachableHarvestableDestructible(player, destructible, itemId))
                    || Plant.GetAll().Any(plant => IsReachableHarvestablePlant(player, plant, itemId))
                    || UnityEngine.Object.FindObjectsOfType<ItemProvider>(true)
                        .Any(provider => IsReachableItemProvider(player, provider, itemId))
                    || Selectable.GetAll().Any(selectable => IsReachableSelectableActionSource(player, selectable, itemId));
            }

            reachableSourceCache[itemId] = reachable;
            return reachable;
        }

        private static bool HasObtainableSource(PlayerCharacter player, string itemId)
        {
            return HasObtainableSource(player, itemId, new HashSet<string>());
        }

        private static bool HasObtainableSource(PlayerCharacter player, string itemId, HashSet<string> visiting)
        {
            CheckDeadline();

            if (player == null || string.IsNullOrEmpty(itemId))
                return false;

            if (obtainableSourceCache.TryGetValue(itemId, out bool cached))
                return cached;

            ItemData directItem = ItemData.Get(itemId);
            if (directItem != null && player.Inventory.CountItem(directItem) > 0)
            {
                obtainableSourceCache[itemId] = true;
                return true;
            }

            if (HasReachableSource(player, itemId))
            {
                obtainableSourceCache[itemId] = true;
                return true;
            }

            if (visiting.Contains(itemId) || visiting.Count >= MaxObtainabilityDepth)
                return false;

            visiting.Add(itemId);

            if (HasReachableGeneratedSource(player, itemId, visiting))
            {
                visiting.Remove(itemId);
                obtainableSourceCache[itemId] = true;
                return true;
            }

            ItemData item = ItemData.Get(itemId);
            bool obtainable = item != null && HasAnyRecipeInput(item) && HasObtainableCraftInputs(player, item, visiting);
            visiting.Remove(itemId);
            obtainableSourceCache[itemId] = obtainable;
            return obtainable;
        }

        private static bool HasObtainableCraftInputs(PlayerCharacter player, ItemData item, HashSet<string> visiting)
        {
            CheckDeadline();

            if (player == null || item == null)
                return false;

            CraftCostData cost = item.GetCraftCost();
            if (cost == null)
                return false;

            foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
            {
                ItemData material = pair.Key;
                int required = pair.Value;
                if (material == null || required <= 0)
                    continue;

                if (player.Inventory.CountItem(material) >= required)
                    continue;

                if (!HasObtainableSource(player, material.id, visiting))
                    return false;
            }

            foreach (KeyValuePair<GroupData, int> pair in cost.craft_fillers)
            {
                GroupData group = pair.Key;
                int required = pair.Value;
                if (group == null || required <= 0)
                    continue;

                if (player.Inventory.CountItemInGroup(group) >= required)
                    continue;

                if (string.IsNullOrEmpty(FindObtainableSourceInGroup(player, group, visiting)))
                    return false;
            }

            foreach (KeyValuePair<CraftData, int> pair in cost.craft_requirements)
            {
                CraftData requirement = pair.Key;
                int required = pair.Value;
                if (requirement == null || required <= 0 || player.Crafting.CountRequirements(requirement) >= required)
                    continue;

                ItemData requirementItem = requirement.GetItem();
                if (requirementItem == null || !HasObtainableSource(player, requirementItem.id, visiting))
                    return false;
            }

            return true;
        }

        private static bool HasReachableGeneratedSource(PlayerCharacter player, string itemId, HashSet<string> visiting)
        {
            CheckDeadline();

            if (generatedSourceCache.TryGetValue(itemId, out bool cached))
                return cached;

            foreach (Selectable selectable in Selectable.GetAll())
            {
                if (selectable == null || !selectable.gameObject.activeInHierarchy || !selectable.CanBeInteracted())
                    continue;

                ItemData output = GetSelectableActionOutput(player, selectable, itemId, visiting);
                if (output == null || !AIInventorySpaceUtility.CanTakeOrMakeRoom(player, output, 1))
                    continue;

                Vector3 playerPosition = player.transform.position;
                if (AIMovementReachability.HasReachablePath(player, playerPosition, GetTargetPosition(selectable, playerPosition)))
                {
                    generatedSourceCache[itemId] = true;
                    return true;
                }
            }

            generatedSourceCache[itemId] = false;
            return false;
        }

        private static string FindObtainableSourceInGroup(PlayerCharacter player, GroupData group)
        {
            return FindObtainableSourceInGroup(player, group, new HashSet<string>());
        }

        private static string FindObtainableSourceInGroup(PlayerCharacter player, GroupData group, HashSet<string> visiting)
        {
            CheckDeadline();

            string groupKey = GroupId(group);
            if (obtainableGroupSourceCache.TryGetValue(groupKey, out string cached))
                return cached;

            string sceneSource = FindReachableSourceInGroup(player, group);
            if (!string.IsNullOrEmpty(sceneSource))
            {
                obtainableGroupSourceCache[groupKey] = sceneSource;
                return sceneSource;
            }

            foreach (ItemData item in ItemData.GetAll())
            {
                if (item != null && item.HasGroup(group) && HasObtainableSource(player, item.id, visiting))
                {
                    obtainableGroupSourceCache[groupKey] = item.id;
                    return item.id;
                }
            }

            obtainableGroupSourceCache[groupKey] = null;
            return null;
        }

        private static string FindReachableSourceInGroup(PlayerCharacter player, GroupData group)
        {
            CheckDeadline();

            string groupKey = GroupId(group);
            if (reachableGroupSourceCache.TryGetValue(groupKey, out string cached))
                return cached;

            Item best = null;
            float bestSqrDistance = float.MaxValue;
            Vector3 playerPosition = player.transform.position;

            foreach (Item item in AIRuntimeSceneQuery.GetItems())
            {
                if (item == null || item.data == null || !item.data.HasGroup(group) || !IsReachableTakeableItem(player, item))
                    continue;

                Vector3 targetPosition = GetTargetPosition(item, playerPosition);
                float sqrDistance = (targetPosition - playerPosition).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    best = item;
                    bestSqrDistance = sqrDistance;
                }
            }

            string bestDestructibleItemId = null;
            foreach (Destructible destructible in UnityEngine.Object.FindObjectsOfType<Destructible>(true))
            {
                ItemData lootItem = GetLootItemInGroup(destructible, group);
                if (lootItem == null || !IsReachableHarvestableDestructible(player, destructible, lootItem.id))
                    continue;

                Vector3 targetPosition = destructible.transform.position;
                float sqrDistance = (targetPosition - playerPosition).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    best = null;
                    bestDestructibleItemId = lootItem.id;
                    bestSqrDistance = sqrDistance;
                }
            }

            string bestPlantItemId = null;
            foreach (Plant plant in Plant.GetAll())
            {
                if (plant == null || plant.fruit == null || !plant.fruit.HasGroup(group) || !IsReachableHarvestablePlant(player, plant, plant.fruit.id))
                    continue;

                Selectable selectable = plant.GetSelectable();
                Vector3 targetPosition = GetTargetPosition(selectable, playerPosition);
                float sqrDistance = (targetPosition - playerPosition).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    best = null;
                    bestDestructibleItemId = null;
                    bestPlantItemId = plant.fruit.id;
                    bestSqrDistance = sqrDistance;
                }
            }

            string bestProviderItemId = null;
            foreach (ItemProvider provider in UnityEngine.Object.FindObjectsOfType<ItemProvider>(true))
            {
                ItemData item = GetProviderItemInGroup(provider, group);
                if (item == null || !IsReachableItemProvider(player, provider, item.id))
                    continue;

                Selectable selectable = provider.GetComponent<Selectable>();
                Vector3 targetPosition = GetTargetPosition(selectable, playerPosition);
                float sqrDistance = (targetPosition - playerPosition).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    best = null;
                    bestDestructibleItemId = null;
                    bestPlantItemId = null;
                    bestProviderItemId = item.id;
                    bestSqrDistance = sqrDistance;
                }
            }

            string bestSelectableItemId = null;
            foreach (Selectable selectable in Selectable.GetAll())
            {
                ItemData item = GetSelectableActionOutputInGroup(player, selectable, group);
                if (item == null || !IsReachableSelectableActionSource(player, selectable, item.id))
                    continue;

                Vector3 targetPosition = GetTargetPosition(selectable, playerPosition);
                float sqrDistance = (targetPosition - playerPosition).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    best = null;
                    bestDestructibleItemId = null;
                    bestPlantItemId = null;
                    bestProviderItemId = null;
                    bestSelectableItemId = item.id;
                    bestSqrDistance = sqrDistance;
                }
            }

            if (best != null && best.data != null)
            {
                reachableGroupSourceCache[groupKey] = best.data.id;
                return best.data.id;
            }

            if (!string.IsNullOrEmpty(bestSelectableItemId))
            {
                reachableGroupSourceCache[groupKey] = bestSelectableItemId;
                return bestSelectableItemId;
            }

            if (!string.IsNullOrEmpty(bestPlantItemId))
            {
                reachableGroupSourceCache[groupKey] = bestPlantItemId;
                return bestPlantItemId;
            }

            if (!string.IsNullOrEmpty(bestProviderItemId))
            {
                reachableGroupSourceCache[groupKey] = bestProviderItemId;
                return bestProviderItemId;
            }

            if (!string.IsNullOrEmpty(bestDestructibleItemId))
            {
                reachableGroupSourceCache[groupKey] = bestDestructibleItemId;
                return bestDestructibleItemId;
            }

            reachableGroupSourceCache[groupKey] = null;
            return null;
        }

        private static bool IsReachableHarvestableDestructible(PlayerCharacter player, Destructible destructible, string itemId)
        {
            CheckDeadline();

            if (player == null || destructible == null || string.IsNullOrEmpty(itemId))
                return false;

            if (!destructible.gameObject.activeInHierarchy || destructible.IsDead() || IsDangerousAnimal(destructible) || !CanAttackNowOrAfterEquipping(player, destructible))
                return false;

            ItemData lootItem = GetLootItem(destructible, itemId);
            if (lootItem == null || !AIInventorySpaceUtility.CanTakeOrMakeRoom(player, lootItem, 1))
                return false;

            return AIMovementReachability.HasReachablePath(player, player.transform.position, destructible.transform.position);
        }

        private static bool IsReachableHarvestablePlant(PlayerCharacter player, Plant plant, string itemId)
        {
            CheckDeadline();

            if (player == null || plant == null || plant.fruit == null || string.IsNullOrEmpty(itemId))
                return false;

            if (!plant.gameObject.activeInHierarchy || !plant.IsBuilt() || plant.IsDead() || !plant.HasFruit())
                return false;

            if (!string.Equals(plant.fruit.id, itemId, StringComparison.Ordinal))
                return false;

            Selectable selectable = plant.GetSelectable();
            if (selectable == null || !selectable.CanBeInteracted())
                return false;

            if (!AIInventorySpaceUtility.CanTakeOrMakeRoom(player, plant.fruit, 1))
                return false;

            Vector3 playerPosition = player.transform.position;
            return AIMovementReachability.HasReachablePath(player, playerPosition, GetTargetPosition(selectable, playerPosition));
        }

        private static bool IsReachableSelectableActionSource(PlayerCharacter player, Selectable selectable, string itemId)
        {
            CheckDeadline();

            if (player == null || selectable == null || string.IsNullOrEmpty(itemId))
                return false;

            if (!selectable.gameObject.activeInHierarchy || !selectable.CanBeInteracted())
                return false;

            if (selectable.GetComponent<Item>() != null)
                return false;

            ItemData output = GetSelectableActionOutput(player, selectable, itemId);
            if (output == null || !AIInventorySpaceUtility.CanTakeOrMakeRoom(player, output, 1))
                return false;

            Vector3 playerPosition = player.transform.position;
            return AIMovementReachability.HasReachablePath(player, playerPosition, GetTargetPosition(selectable, playerPosition));
        }

        private static bool IsReachableItemProvider(PlayerCharacter player, ItemProvider provider, string itemId)
        {
            CheckDeadline();

            if (player == null || provider == null || string.IsNullOrEmpty(itemId))
                return false;

            if (!provider.gameObject.activeInHierarchy || !provider.HasItem())
                return false;

            Selectable selectable = provider.GetComponent<Selectable>();
            if (selectable == null || !selectable.CanBeInteracted())
                return false;

            ItemData item = GetProviderItem(provider, itemId);
            if (item == null || !AIInventorySpaceUtility.CanTakeOrMakeRoom(player, item, 1))
                return false;

            Vector3 playerPosition = player.transform.position;
            return AIMovementReachability.HasReachablePath(player, playerPosition, GetTargetPosition(selectable, playerPosition));
        }

        private static bool IsDangerousAnimal(Destructible destructible)
        {
            AnimalWild animal = destructible != null ? destructible.GetComponent<AnimalWild>() : null;
            return animal != null && animal.HasAttackBehavior();
        }

        private static bool CanAttackNowOrAfterEquipping(PlayerCharacter player, Destructible destructible)
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

        private static bool HasEquipmentInGroup(InventoryData inventory, GroupData group)
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

        private static ItemData GetLootItem(Destructible destructible, string itemId)
        {
            if (destructible == null || destructible.loots == null)
                return null;

            foreach (SData loot in destructible.loots)
            {
                ItemData item = GetLootItem(loot);
                if (item != null && string.Equals(item.id, itemId, StringComparison.Ordinal))
                    return item;
            }

            return null;
        }

        private static ItemData GetLootItem(SData loot)
        {
            if (loot is ItemData)
                return (ItemData)loot;

            LootData lootData = loot as LootData;
            if (lootData != null && lootData.probability > 0f)
                return lootData.item;

            return null;
        }

        private static ItemData GetLootItemInGroup(Destructible destructible, GroupData group)
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

        private static ItemData GetProviderItem(ItemProvider provider, string itemId)
        {
            if (provider == null || provider.items == null)
                return null;

            foreach (ItemData item in provider.items)
            {
                if (item != null && string.Equals(item.id, itemId, StringComparison.Ordinal))
                    return item;
            }

            return null;
        }

        private static ItemData GetProviderItemInGroup(ItemProvider provider, GroupData group)
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

        private static ItemData GetSelectableActionOutput(PlayerCharacter player, Selectable selectable, string itemId)
        {
            if (player == null || selectable == null || string.IsNullOrEmpty(itemId))
                return null;

            ItemData mergeOutput = GetMergeOutput(player.Inventory.InventoryData, selectable, itemId);
            if (mergeOutput != null)
                return mergeOutput;

            mergeOutput = GetMergeOutput(player.Inventory.BagData, selectable, itemId);
            if (mergeOutput != null)
                return mergeOutput;

            ItemProvider provider = selectable.GetComponent<ItemProvider>();
            ItemData sceneActionOutput = GetSceneActionOutput(selectable, provider, player, itemId);
            if (sceneActionOutput != null)
                return sceneActionOutput;

            Destructible destructible = selectable.Destructible;
            if (destructible == null || destructible.IsDead() || destructible.loots == null)
                return null;

            if (selectable.FindAutoAction(player) == null)
                return null;

            return GetLootItem(destructible, itemId);
        }

        private static ItemData GetSelectableActionOutput(PlayerCharacter player, Selectable selectable, string itemId, HashSet<string> visiting)
        {
            ItemData output = GetSelectableActionOutput(player, selectable, itemId);
            if (output != null)
                return output;

            output = GetMergeOutputFromObtainableItems(player, selectable, itemId, visiting);
            if (output != null)
                return output;

            ItemProvider provider = selectable.GetComponent<ItemProvider>();
            return GetSceneActionOutput(selectable, provider, player, itemId, visiting);
        }

        private static ItemData GetSelectableActionOutputInGroup(PlayerCharacter player, Selectable selectable, GroupData group)
        {
            if (player == null || selectable == null || group == null)
                return null;

            ItemData mergeOutput = GetMergeOutputInGroup(player.Inventory.InventoryData, selectable, group);
            if (mergeOutput != null)
                return mergeOutput;

            mergeOutput = GetMergeOutputInGroup(player.Inventory.BagData, selectable, group);
            if (mergeOutput != null)
                return mergeOutput;

            ItemProvider provider = selectable.GetComponent<ItemProvider>();
            ItemData sceneActionOutput = GetSceneActionOutputInGroup(selectable, provider, player, group);
            if (sceneActionOutput != null)
                return sceneActionOutput;

            Destructible destructible = selectable.Destructible;
            if (destructible == null || destructible.IsDead() || destructible.loots == null)
                return null;

            if (selectable.FindAutoAction(player) == null)
                return null;

            return GetLootItemInGroup(destructible, group);
        }

        private static ItemData GetMergeOutput(InventoryData inventory, Selectable selectable, string itemId)
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
                if (output != null && string.Equals(output.id, itemId, StringComparison.Ordinal))
                    return output;
            }

            return null;
        }

        private static ItemData GetMergeOutputInGroup(InventoryData inventory, Selectable selectable, GroupData group)
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

        private static ItemData GetMergeOutputItem(MAction action)
        {
            ActionFill fill = action as ActionFill;
            if (fill != null)
                return fill.filled_item;

            ActionFillProvider fillProvider = action as ActionFillProvider;
            if (fillProvider != null)
                return fillProvider.filled_item;

            return null;
        }

        private static ItemData GetMergeOutputFromObtainableItems(PlayerCharacter player, Selectable selectable, string itemId, HashSet<string> visiting)
        {
            if (player == null || selectable == null || string.IsNullOrEmpty(itemId))
                return null;

            foreach (ItemData item in ItemData.GetAll())
            {
                if (item == null)
                    continue;

                ItemData output = GetMergeOutputItem(item.FindMergeAction(selectable));
                if (output == null || !string.Equals(output.id, itemId, StringComparison.Ordinal))
                    continue;

                if (!HasObtainableSource(player, item.id, visiting))
                    continue;

                return output;
            }

            return null;
        }

        private static ItemData GetSceneActionOutput(Selectable selectable, ItemProvider provider, PlayerCharacter player, string itemId)
        {
            if (selectable == null || selectable.actions == null || provider == null || !provider.HasItem() || string.IsNullOrEmpty(itemId))
                return null;

            foreach (SAction action in selectable.actions)
            {
                ActionFish fish = action as ActionFish;
                if (fish == null || !CanEquipItemInGroup(player, fish.fishing_rod))
                    continue;

                ItemData output = GetProviderItem(provider, itemId);
                if (output != null)
                    return output;
            }

            return null;
        }

        private static ItemData GetSceneActionOutput(Selectable selectable, ItemProvider provider, PlayerCharacter player, string itemId, HashSet<string> visiting)
        {
            if (selectable == null || selectable.actions == null || provider == null || !provider.HasItem() || string.IsNullOrEmpty(itemId))
                return null;

            foreach (SAction action in selectable.actions)
            {
                ActionFish fish = action as ActionFish;
                if (fish == null || !CanObtainItemInGroup(player, fish.fishing_rod, visiting))
                    continue;

                ItemData output = GetProviderItem(provider, itemId);
                if (output != null)
                    return output;
            }

            return null;
        }

        private static ItemData GetSceneActionOutputInGroup(Selectable selectable, ItemProvider provider, PlayerCharacter player, GroupData group)
        {
            if (selectable == null || selectable.actions == null || provider == null || !provider.HasItem() || group == null)
                return null;

            foreach (SAction action in selectable.actions)
            {
                ActionFish fish = action as ActionFish;
                if (fish == null || !CanEquipItemInGroup(player, fish.fishing_rod))
                    continue;

                ItemData output = GetProviderItemInGroup(provider, group);
                if (output != null)
                    return output;
            }

            return null;
        }

        private static bool CanEquipItemInGroup(PlayerCharacter player, GroupData group)
        {
            if (player == null || group == null)
                return false;

            if (player.EquipData.HasItemInGroup(group))
                return true;

            return HasEquipmentInGroup(player.Inventory.InventoryData, group)
                || HasEquipmentInGroup(player.Inventory.BagData, group);
        }

        private static bool CanObtainItemInGroup(PlayerCharacter player, GroupData group, HashSet<string> visiting)
        {
            if (CanEquipItemInGroup(player, group))
                return true;

            return !string.IsNullOrEmpty(FindObtainableSourceInGroup(player, group, visiting));
        }

        private static bool IsReachableTakeableItem(PlayerCharacter player, Item item)
        {
            if (item == null || item.data == null || item.quantity <= 0 || !item.gameObject.activeInHierarchy)
                return false;

            Selectable selectable = item.GetSelectable();
            if (selectable != null && !selectable.CanBeInteracted())
                return false;

            if (!AIInventorySpaceUtility.CanTakeOrMakeRoom(player, item.data, item.quantity))
                return false;

            Vector3 playerPosition = player.transform.position;
            return AIMovementReachability.HasReachablePath(player, playerPosition, GetTargetPosition(item, playerPosition));
        }

        private static Vector3 GetTargetPosition(Item item, Vector3 fromPosition)
        {
            Selectable selectable = item.GetSelectable();
            return selectable != null ? selectable.GetClosestInteractPoint(fromPosition) : item.transform.position;
        }

        private static Vector3 GetTargetPosition(Selectable selectable, Vector3 fromPosition)
        {
            return selectable != null ? selectable.GetClosestInteractPoint(fromPosition) : fromPosition;
        }

        private static void EnsureDataLoaded()
        {
            if (CraftData.GetAll().Count > 0 && ItemData.GetAll().Count > 0)
                return;

            CraftData.Load();
            ItemData.Load();
            PlantData.Load();
            ConstructionData.Load();
            CharacterData.Load();
        }

        private static void CheckDeadline()
        {
            if (verificationStopwatch == null || verificationTimeoutMs <= 0)
                return;

            if (verificationStopwatch.ElapsedMilliseconds <= verificationTimeoutMs)
                return;

            throw new TimeoutException("AI craft completion verification exceeded its internal timeout. Increase timeout_ms after reducing the candidate/source search space or adding more caches.");
        }

        private static void AddItemGroups(Dictionary<GroupData, int> groups, ItemData item, int quantity)
        {
            if (item == null || item.groups == null)
                return;

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

        private static int CountGroup(Dictionary<GroupData, int> groups, GroupData group)
        {
            if (group != null && groups.ContainsKey(group))
                return groups[group];
            return 0;
        }

        private static string GroupId(GroupData group)
        {
            if (group == null)
                return string.Empty;
            return !string.IsNullOrEmpty(group.group_id) ? group.group_id : group.name;
        }

        private static bool HasAnyRecipeInput(CraftData craft)
        {
            if (craft == null)
                return false;

            return (craft.craft_items != null && craft.craft_items.Length > 0)
                || (craft.craft_fillers != null && craft.craft_fillers.Length > 0)
                || (craft.craft_requirements != null && craft.craft_requirements.Length > 0)
                || craft.craft_near != null;
        }

        private sealed class CandidateState
        {
            public bool remaining;
            public bool immediate;
            public bool missingNear;
            public int missingMaterialCount;
            public int missingRequirementCount;
            public readonly List<Dictionary<string, object>> missing = new List<Dictionary<string, object>>();

            public int Score
            {
                get { return missingMaterialCount + missingRequirementCount * 10 + (missingNear ? 20 : 0); }
            }
        }
    }
}
