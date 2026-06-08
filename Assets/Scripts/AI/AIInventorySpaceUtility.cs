using System.Collections.Generic;

namespace SurvivalEngine
{
    public static class AIInventorySpaceUtility
    {
        public static bool CanTakeOrMakeRoom(PlayerCharacter player, ItemData targetItem, int quantity = 1)
        {
            if (player == null || targetItem == null || quantity <= 0)
                return false;

            if (player.Inventory.CanTakeItem(targetItem, quantity))
                return true;

            return FindDroppableSlot(player, targetItem.id, out _, out _);
        }

        public static bool EnsureCanTake(PlayerCharacter player, ItemData targetItem, int quantity = 1)
        {
            if (player == null || targetItem == null || quantity <= 0)
                return false;

            if (player.Inventory.CanTakeItem(targetItem, quantity))
                return true;

            if (!FindDroppableSlot(player, targetItem.id, out InventoryData inventory, out int slot))
                return false;

            player.Inventory.DropItem(inventory, slot);
            return player.Inventory.CanTakeItem(targetItem, quantity);
        }

        private static bool FindDroppableSlot(PlayerCharacter player, string targetItemId, out InventoryData inventory, out int slot)
        {
            inventory = null;
            slot = -1;

            if (TryFindDroppableSlot(player, player.Inventory.InventoryData, targetItemId, out inventory, out slot))
                return true;

            return TryFindDroppableSlot(player, player.Inventory.BagData, targetItemId, out inventory, out slot);
        }

        private static bool TryFindDroppableSlot(PlayerCharacter player, InventoryData source, string targetItemId, out InventoryData inventory, out int slot)
        {
            inventory = null;
            slot = -1;
            if (player == null || source == null)
                return false;

            foreach (KeyValuePair<int, InventoryItemData> pair in source.items)
            {
                InventoryItemData inventoryItem = pair.Value;
                ItemData item = ItemData.Get(inventoryItem?.item_id);
                if (!CanDropSlot(player, source, inventoryItem, item, targetItemId))
                    continue;

                inventory = source;
                slot = pair.Key;
                return true;
            }

            return false;
        }

        private static bool CanDropSlot(PlayerCharacter player, InventoryData inventory, InventoryItemData inventoryItem, ItemData item, string targetItemId)
        {
            if (player == null || inventory == null || inventoryItem == null || item == null || inventoryItem.quantity <= 0)
                return false;

            if (item.id == targetItemId || item.type == ItemType.Equipment || item.IsBag() || !item.CanBeDropped())
                return false;

            if (item.id.StartsWith("seed_", System.StringComparison.Ordinal))
                return false;

            int remainingNeed = CountRemainingRecipeNeed(player, item);
            int currentCount = player.Inventory.CountItem(item);
            return currentCount - inventoryItem.quantity >= remainingNeed;
        }

        private static int CountRemainingRecipeNeed(PlayerCharacter player, ItemData item)
        {
            if (player == null || item == null)
                return 0;

            int need = 0;
            foreach (CraftData craft in CraftData.GetAll())
            {
                ItemData target = craft != null ? craft.GetItem() : null;
                if (target == null || target.craft_quantity <= 0 || !HasAnyRecipeInput(craft))
                    continue;

                if (player.Crafting.CountTotalCrafted(target) > 0)
                    continue;

                CraftCostData cost = target.GetCraftCost();
                foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
                {
                    if (pair.Key == item && pair.Value > 0)
                        need += pair.Value;
                }

                foreach (KeyValuePair<GroupData, int> pair in cost.craft_fillers)
                {
                    if (pair.Key != null && pair.Value > 0 && item.HasGroup(pair.Key))
                        need += pair.Value;
                }
            }

            return need;
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
    }
}
