using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
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

        [Tooltip("When enabled, outputs only missing material ids. When disabled, outputs all material ids required by the selected recipe.")]
        public bool outputOnlyMissingMaterials = true;

        [BlackboardOnly]
        [Tooltip("Output item id of the selected craftable item. Cleared when no suitable recipe is found.")]
        public BBParameter<string> itemId;

        [BlackboardOnly]
        [Tooltip("Output material item ids for the selected recipe. Contains missing materials only or all materials based on outputOnlyMissingMaterials.")]
        public BBParameter<List<string>> materialItemIds;

        protected override string info
        {
            get { return "Find best craftable item as " + itemId; }
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

            CraftCandidate best = null;
            foreach (CraftData craft in CraftData.GetAll())
            {
                CraftCandidate candidate = GetCandidate(player, craft);
                if (candidate != null && IsBetterCandidate(candidate, best))
                    best = candidate;
            }

            if (best == null)
            {
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

            bool learnt = item.craftable || player.SaveData.IsIDUnlocked(item.id);
            if (!learnt)
                return null;

            if (skipOwnedItems && player.Inventory.HasItem(item, 1))
                return null;

            if (skipAlreadyCraftedItems && player.Crafting.CountTotalCrafted(item) > 0)
                return null;

            CraftCostData cost = item.GetCraftCost();
            CraftCandidate candidate = new CraftCandidate(item);
            Dictionary<GroupData, int> exactItemGroups = new Dictionary<GroupData, int>();

            foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
            {
                ItemData material = pair.Key;
                int required = pair.Value;
                if (material == null || required <= 0)
                    continue;

                AddRepeated(candidate.allMaterialIds, material.id, required);
                AddItemGroups(exactItemGroups, material, required);

                int missing = required - player.Inventory.CountItem(material);
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

                int missing = required - player.Inventory.CountItemInGroup(group);
                if (missing > 0)
                {
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
            return candidate;
        }

        private bool IsBetterCandidate(CraftCandidate candidate, CraftCandidate best)
        {
            if (best == null)
                return true;

            int score = candidate.GetScore();
            int bestScore = best.GetScore();
            if (score != bestScore)
                return score < bestScore;

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
    }
}
