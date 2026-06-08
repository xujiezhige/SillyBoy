using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using SurvivalEngine.Debugging;
using UnityEngine;

namespace SurvivalEngine
{
    [Name("GatherMaterialTarget")]
    [Category("SurvivalEngine/Player")]
    [Description("Move to a material source, then pick it up or attack the resource node until it drops items.")]
    public class GatherMaterialTarget : ActionTask
    {
        public BBParameter<Vector2> target;
        public BBParameter<GameObject> targetObject;

        public BBParameter<string> materialItemId;

        public BBParameter<string> craftItemId;

        [BlackboardOnly]
        public BBParameter<List<GameObject>> droppedItems;

        private const float StopDistance = 0.2f;
        private const float AutoMoveLostRetryDelay = 0.5f;
        private const int MaxAutoMoveRetries = 3;
        private const float FailureMemoryCooldown = 10f;
        private const float FinishAfterDropDelay = 0.35f;
        private const float MaxGatherSeconds = 75f;

        private PlayerCharacter player;
        private GameObject gameObjectTarget;
        private Item itemTarget;
        private Selectable selectableTarget;
        private Destructible destructibleTarget;
        private Plant plantTarget;
        private ItemProvider itemProviderTarget;
        private MAction mergeActionTarget;
        private ItemSlot mergeActionSlot;
        private SAction sceneActionTarget;
        private List<GameObject> droppedItemObjects;
        private Vector3 lastTargetPosition;
        private bool useNativeSelectableInteraction;
        private bool hasMoveRequest;
        private bool interactionStarted;
        private bool completed;
        private float autoMoveLostTimer;
        private int autoMoveRetryCount;
        private float startedAt;
        private float firstDropAt = -1f;

        protected override string info
        {
            get { return "GatherMaterialTarget " + targetObject; }
        }

        protected override void OnExecute()
        {
            player = AIRuntimeSceneQuery.GetPrimaryPlayer();
            gameObjectTarget = targetObject.value;
            droppedItemObjects = new List<GameObject>();
            SaveDroppedItems(droppedItemObjects);
            hasMoveRequest = false;
            interactionStarted = false;
            completed = false;
            autoMoveLostTimer = 0f;
            autoMoveRetryCount = 0;
            startedAt = Time.time;
            firstDropAt = -1f;

            if (player == null || gameObjectTarget == null)
            {
                EndAction(false);
                return;
            }

            itemTarget = gameObjectTarget.GetComponent<Item>();
            selectableTarget = gameObjectTarget.GetComponent<Selectable>();
            destructibleTarget = gameObjectTarget.GetComponent<Destructible>();
            plantTarget = gameObjectTarget.GetComponent<Plant>();
            itemProviderTarget = gameObjectTarget.GetComponent<ItemProvider>();
            mergeActionTarget = FindSceneMergeAction(materialItemId.value, selectableTarget, out mergeActionSlot);
            sceneActionTarget = FindSceneAction(materialItemId.value, selectableTarget);
            useNativeSelectableInteraction = selectableTarget != null
                && mergeActionTarget == null
                && sceneActionTarget == null
                && IsSelectableGatherSource();
            TryEquipRequiredTool(destructibleTarget);
            SubscribeDropEvent(selectableTarget);

            if (TryUseSelectableSource())
                return;

            RequestMove();
        }

        protected override void OnUpdate()
        {
            if (player == null || gameObjectTarget == null)
            {
                if (gameObjectTarget == null && (useNativeSelectableInteraction || firstDropAt > 0f))
                    CompleteSuccess();
                else
                    EndAction(false);
                return;
            }

            if (player.IsDead())
            {
                HandleFailure("gather_material_player_dead");
                return;
            }

            if (completed)
                return;

            if (Time.time - startedAt > MaxGatherSeconds)
            {
                HandleFailure("gather_material_timeout");
                return;
            }

            if (interactionStarted)
            {
                if (!player.IsBusy())
                    CompleteSuccess();
                return;
            }

            if (firstDropAt > 0f && Time.time - firstDropAt >= FinishAfterDropDelay)
            {
                CompleteSuccess();
                return;
            }

            if (TryUseSelectableSource())
                return;

            if (destructibleTarget != null && !IsSelectableGatherSource())
            {
                if (IsDangerousAnimal(destructibleTarget))
                {
                    HandleFailure("gather_material_dangerous_animal");
                    return;
                }

                TryEquipRequiredTool(destructibleTarget);
                EquipBestCombatGear(destructibleTarget);
                if (destructibleTarget.IsDead() || !destructibleTarget.gameObject.activeInHierarchy)
                {
                    CompleteSuccess();
                    return;
                }

                if (player.Combat.IsAttackTargetInRange(destructibleTarget))
                {
                    if (!player.IsBusy())
                        player.Attack(destructibleTarget);
                    return;
                }
            }

            if (!hasMoveRequest || HasTargetChanged())
                RequestMove();

            if (DetectLostAutoMove())
                return;

            if (destructibleTarget == null && HasReachedTarget())
                CompleteSuccess();
        }

        protected override void OnPause()
        {
            OnStop();
        }

        protected override void OnStop()
        {
            UnsubscribeDropEvent(selectableTarget);
            if (player != null && !completed)
                player.StopMove();

            player = null;
            gameObjectTarget = null;
            itemTarget = null;
            selectableTarget = null;
            destructibleTarget = null;
            plantTarget = null;
            itemProviderTarget = null;
            mergeActionTarget = null;
            mergeActionSlot = null;
            sceneActionTarget = null;
            droppedItemObjects = null;
            useNativeSelectableInteraction = false;
            hasMoveRequest = false;
            interactionStarted = false;
            completed = false;
            firstDropAt = -1f;
        }

        private bool TryUseSelectableSource()
        {
            if (selectableTarget == null || !selectableTarget.gameObject.activeInHierarchy)
                return false;

            if (!selectableTarget.CanBeInteracted() || !selectableTarget.IsInUseRange(player))
                return false;

            if (!IsSelectableGatherSource())
                return false;

            if (!EnsureInventorySpaceForSelectableSource())
                return false;

            interactionStarted = true;
            player.StopMove();
            if (sceneActionTarget != null)
                sceneActionTarget.DoAction(player, selectableTarget);
            else if (mergeActionTarget != null && mergeActionSlot != null)
                mergeActionTarget.DoAction(player, mergeActionSlot, selectableTarget);
            else
                selectableTarget.Use(player, selectableTarget.GetClosestInteractPoint(player.GetInteractCenter()));
            if (!player.IsBusy())
                CompleteSuccess();
            return true;
        }

        private bool IsSelectableGatherSource()
        {
            if (itemTarget != null)
                return itemTarget.data != null && itemTarget.quantity > 0;

            if (plantTarget != null)
            {
                if (plantTarget.fruit != null && plantTarget.HasFruit() && plantTarget.IsBuilt() && !plantTarget.IsDead())
                    return true;

                return plantTarget.IsBuilt() && !plantTarget.IsDead() && GetSelectableAutoActionLootItem(destructibleTarget) != null;
            }

            if (itemProviderTarget != null)
                return itemProviderTarget.HasItem();

            if (mergeActionTarget != null && mergeActionSlot != null)
                return true;

            if (sceneActionTarget != null)
                return true;

            if (selectableTarget != null && selectableTarget.FindAutoAction(player) != null && GetSelectableAutoActionLootItem(destructibleTarget) != null)
                return true;

            return false;
        }

        private bool EnsureInventorySpaceForSelectableSource()
        {
            if (itemTarget != null)
                return itemTarget.data != null && AIInventorySpaceUtility.EnsureCanTake(player, itemTarget.data, itemTarget.quantity);

            if (plantTarget != null)
                return plantTarget.fruit != null && AIInventorySpaceUtility.EnsureCanTake(player, plantTarget.fruit, 1);

            if (itemProviderTarget != null && itemProviderTarget.items != null)
            {
                foreach (ItemData item in itemProviderTarget.items)
                {
                    if (item != null)
                        return AIInventorySpaceUtility.EnsureCanTake(player, item, 1);
                }
            }

            ItemData mergeOutput = GetMergeOutputItem(mergeActionTarget);
            if (mergeOutput != null)
                return AIInventorySpaceUtility.EnsureCanTake(player, mergeOutput, 1);

            ItemData sceneActionOutput = GetSceneActionOutputItem(sceneActionTarget, itemProviderTarget);
            if (sceneActionOutput != null)
                return AIInventorySpaceUtility.EnsureCanTake(player, sceneActionOutput, 1);

            ItemData lootItem = GetSelectableAutoActionLootItem(destructibleTarget);
            if (lootItem != null)
                return AIInventorySpaceUtility.EnsureCanTake(player, lootItem, 1);

            return false;
        }

        private MAction FindSceneMergeAction(string wantedItemId, Selectable selectable, out ItemSlot slot)
        {
            slot = null;
            if (player == null || selectable == null || string.IsNullOrEmpty(wantedItemId))
                return null;

            foreach (ItemSlotPanel panel in ItemSlotPanel.GetAll())
            {
                if (panel == null || panel.GetPlayerID() != player.player_id)
                    continue;

                InventoryData inventory = panel.GetInventory();
                if (inventory == null)
                    continue;

                foreach (KeyValuePair<int, InventoryItemData> pair in inventory.items)
                {
                    InventoryItemData inventoryItem = pair.Value;
                    ItemData item = ItemData.Get(inventoryItem?.item_id);
                    if (item == null || inventoryItem == null || inventoryItem.quantity <= 0)
                        continue;

                    MAction action = item.FindMergeAction(selectable);
                    ItemData output = GetMergeOutputItem(action);
                    if (output == null || !string.Equals(output.id, wantedItemId, System.StringComparison.Ordinal))
                        continue;

                    ItemSlot candidateSlot = panel.GetSlotByIndex(pair.Key);
                    if (candidateSlot != null && action.CanDoAction(player, candidateSlot, selectable))
                    {
                        slot = candidateSlot;
                        return action;
                    }
                }
            }

            return null;
        }

        private SAction FindSceneAction(string wantedItemId, Selectable selectable)
        {
            if (player == null || selectable == null || selectable.actions == null || string.IsNullOrEmpty(wantedItemId))
                return null;

            foreach (SAction action in selectable.actions)
            {
                ActionFish fish = action as ActionFish;
                if (fish == null)
                    continue;

                ItemData output = GetSceneActionOutputItem(fish, selectable.GetComponent<ItemProvider>());
                if (output == null || !string.Equals(output.id, wantedItemId, System.StringComparison.Ordinal))
                    continue;

                TryEquipItemInGroup(fish.fishing_rod);
                if (fish.CanDoAction(player, selectable))
                    return fish;
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

        private ItemData GetSceneActionOutputItem(SAction action, ItemProvider provider)
        {
            if (action is ActionFish && provider != null && provider.items != null)
            {
                foreach (ItemData item in provider.items)
                {
                    if (item != null)
                        return item;
                }
            }

            return null;
        }

        private bool TryEquipItemInGroup(GroupData group)
        {
            if (player == null || group == null)
                return false;

            if (player.EquipData.HasItemInGroup(group))
                return true;

            if (TryEquipFromInventory(player.Inventory.InventoryData, group))
                return true;

            if (TryEquipFromInventory(player.Inventory.BagData, group))
                return true;

            return false;
        }

        private ItemData GetWantedLootItem(Destructible destructible)
        {
            if (destructible == null || destructible.loots == null || string.IsNullOrEmpty(materialItemId.value))
                return null;

            foreach (SData loot in destructible.loots)
            {
                ItemData item = GetLootItem(loot);
                if (item != null && string.Equals(item.id, materialItemId.value, System.StringComparison.Ordinal))
                    return item;
            }

            return null;
        }

        private ItemData GetSelectableAutoActionLootItem(Destructible destructible)
        {
            ItemData wantedLoot = GetWantedLootItem(destructible);
            if (wantedLoot != null)
                return wantedLoot;

            if (destructible == null || destructible.loots == null)
                return null;

            foreach (SData loot in destructible.loots)
            {
                ItemData item = GetLootItem(loot);
                if (item != null)
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

        private void RequestMove()
        {
            lastTargetPosition = GetTargetPosition();
            hasMoveRequest = true;
            autoMoveLostTimer = 0f;

            if (destructibleTarget != null && !IsSelectableGatherSource())
            {
                TryEquipRequiredTool(destructibleTarget);
                EquipBestCombatGear(destructibleTarget);
                player.Attack(destructibleTarget);
            }
            else if (useNativeSelectableInteraction && selectableTarget != null)
            {
                player.Interact(selectableTarget, lastTargetPosition);
            }
            else
            {
                player.MoveTo(lastTargetPosition);
            }
        }

        private bool TryEquipRequiredTool(Destructible destructible)
        {
            if (player == null || destructible == null || destructible.required_item == null)
                return true;

            if (player.EquipData.HasItemInGroup(destructible.required_item))
                return true;

            if (TryEquipFromInventory(player.Inventory.InventoryData, destructible.required_item))
                return true;

            if (TryEquipFromInventory(player.Inventory.BagData, destructible.required_item))
                return true;

            return false;
        }

        private bool IsDangerousAnimal(Destructible destructible)
        {
            AnimalWild animal = destructible != null ? destructible.GetComponent<AnimalWild>() : null;
            return animal != null && animal.HasAttackBehavior();
        }

        private void EquipBestCombatGear(Destructible target)
        {
            if (player == null || target == null)
                return;

            EquipBestArmorFromInventory(player.Inventory.InventoryData);
            EquipBestArmorFromInventory(player.Inventory.BagData);

            if (target.required_item != null)
                return;

            EquipBestWeaponFromInventory(player.Inventory.InventoryData, target);
            EquipBestWeaponFromInventory(player.Inventory.BagData, target);
        }

        private void EquipBestWeaponFromInventory(InventoryData inventory, Destructible target)
        {
            if (inventory == null || target == null)
                return;

            InventoryChoice best = default(InventoryChoice);
            best.score = GetEquippedWeaponScore(target);
            foreach (KeyValuePair<int, InventoryItemData> pair in inventory.items)
            {
                InventoryItemData inventoryItem = pair.Value;
                ItemData item = ItemData.Get(inventoryItem?.item_id);
                if (item == null || inventoryItem == null || inventoryItem.quantity <= 0 || !item.IsWeapon())
                    continue;

                int score = GetWeaponScore(item, target);
                if (score > best.score)
                {
                    best.inventory = inventory;
                    best.slot = pair.Key;
                    best.score = score;
                }
            }

            if (best.inventory != null)
                player.Inventory.EquipItem(best.inventory, best.slot);
        }

        private void EquipBestArmorFromInventory(InventoryData inventory)
        {
            if (inventory == null)
                return;

            Dictionary<EquipSlot, InventoryChoice> bestChoices = new Dictionary<EquipSlot, InventoryChoice>();
            foreach (KeyValuePair<int, InventoryItemData> pair in inventory.items)
            {
                InventoryItemData inventoryItem = pair.Value;
                ItemData item = ItemData.Get(inventoryItem?.item_id);
                if (item == null || inventoryItem == null || inventoryItem.quantity <= 0 || item.type != ItemType.Equipment)
                    continue;

                if (item.equip_slot == EquipSlot.None || item.equip_slot == EquipSlot.Hand || item.IsWeapon())
                    continue;

                int equippedArmor = GetEquippedArmor(item.equip_slot);
                int score = item.armor;
                InventoryChoice current;
                bool hasChoice = bestChoices.TryGetValue(item.equip_slot, out current);
                if (score > equippedArmor && (!hasChoice || score > current.score))
                {
                    bestChoices[item.equip_slot] = new InventoryChoice
                    {
                        inventory = inventory,
                        slot = pair.Key,
                        score = score
                    };
                }
            }

            foreach (KeyValuePair<EquipSlot, InventoryChoice> pair in bestChoices)
            {
                InventoryChoice choice = pair.Value;
                if (choice.inventory != null)
                    player.Inventory.EquipItem(choice.inventory, choice.slot);
            }
        }

        private int GetEquippedWeaponScore(Destructible target)
        {
            ItemData equipped = player.EquipData.GetEquippedWeaponData();
            return equipped != null ? GetWeaponScore(equipped, target) : player.Combat.hand_damage;
        }

        private int GetWeaponScore(ItemData item, Destructible target)
        {
            if (item == null || !item.IsWeapon())
                return 0;

            if (target != null && item.IsRangedWeapon() && target.attack_melee_only)
                return 0;

            int strikes = Mathf.Max(item.strike_per_attack, 1);
            int score = item.damage * strikes;
            score += Mathf.RoundToInt(item.range);
            return score;
        }

        private int GetEquippedArmor(EquipSlot slot)
        {
            ItemData equipped = player.Inventory.GetEquippedItemData(slot);
            return equipped != null ? equipped.armor : 0;
        }

        private struct InventoryChoice
        {
            public InventoryData inventory;
            public int slot;
            public int score;
        }

        private bool TryEquipFromInventory(InventoryData inventory, GroupData requiredGroup)
        {
            if (inventory == null || requiredGroup == null)
                return false;

            foreach (KeyValuePair<int, InventoryItemData> pair in inventory.items)
            {
                InventoryItemData inventoryItem = pair.Value;
                ItemData item = ItemData.Get(inventoryItem?.item_id);
                if (item == null || inventoryItem == null || inventoryItem.quantity <= 0)
                    continue;

                if (item.type == ItemType.Equipment && item.HasGroup(requiredGroup))
                {
                    player.Inventory.EquipItem(inventory, pair.Key);
                    return true;
                }
            }

            return false;
        }

        private Vector3 GetTargetPosition()
        {
            if (destructibleTarget != null)
                return destructibleTarget.transform.position;

            if (selectableTarget != null)
                return selectableTarget.GetClosestInteractPoint(player.GetInteractCenter());

            Vector2 targetPoint = target.value;
            return new Vector3(targetPoint.x, player.transform.position.y, targetPoint.y);
        }

        private bool HasTargetChanged()
        {
            return Vector3.Distance(lastTargetPosition, GetTargetPosition()) > StopDistance;
        }

        private bool HasReachedTarget()
        {
            Vector3 playerPosition = player.transform.position;
            Vector2 playerPoint = new Vector2(playerPosition.x, playerPosition.z);
            Vector2 targetPoint = new Vector2(GetTargetPosition().x, GetTargetPosition().z);
            return Vector2.Distance(playerPoint, targetPoint) <= StopDistance;
        }

        private bool DetectLostAutoMove()
        {
            if (!hasMoveRequest || HasReachedTarget())
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

            HandleFailure("gather_material_lost_auto_move");
            return true;
        }

        private void HandleFailure(string reason)
        {
            RememberFailure(reason);
            RecordFailureEvent(reason);
            player.StopMove();
            EndAction(false);
        }

        private void RememberFailure(string reason)
        {
            if (gameObjectTarget == null)
                return;

            string itemId = null;
            Item item = gameObjectTarget.GetComponent<Item>();
            if (item != null && item.data != null)
                itemId = item.data.id;

            AITargetFailureMemory.RememberFailure(gameObjectTarget, itemId, lastTargetPosition, reason, FailureMemoryCooldown);

            string currentCraftItemId = craftItemId.value;
            if (!string.IsNullOrEmpty(currentCraftItemId))
            {
                AICraftCandidateFailureMemory.RememberFailure(
                    currentCraftItemId,
                    reason,
                    FailureMemoryCooldown,
                    new List<string>());
            }
        }

        private void RecordFailureEvent(string reason)
        {
            var debugger = GameStateDebugger.Instance;
            if (debugger == null)
                return;

            debugger.RecordEvent(
                "behavior_tree",
                reason,
                "GatherMaterialTarget could not finish gathering from the selected scene source.",
                "warning",
                new Dictionary<string, object>
                {
                    ["target_position"] = lastTargetPosition,
                    ["player_position"] = player != null ? player.transform.position : Vector3.zero,
                    ["target_object"] = gameObjectTarget != null ? gameObjectTarget.name : null,
                    ["retry_count"] = autoMoveRetryCount
                });
        }

        private void CompleteSuccess()
        {
            completed = true;
            SaveDroppedItems(droppedItemObjects ?? new List<GameObject>());
            if (player != null)
                player.StopMove();
            EndAction(true);
        }

        private void SaveDroppedItems(List<GameObject> items)
        {
            droppedItems.value = items;
        }

        private void OnDropItem(Item item)
        {
            if (item == null || droppedItemObjects == null)
                return;

            droppedItemObjects.Add(item.gameObject);
            if (firstDropAt < 0f)
                firstDropAt = Time.time;
        }

        private void SubscribeDropEvent(Selectable targetSelectable)
        {
            if (targetSelectable != null)
                targetSelectable.onDropItem += OnDropItem;
        }

        private void UnsubscribeDropEvent(Selectable targetSelectable)
        {
            if (targetSelectable != null)
                targetSelectable.onDropItem -= OnDropItem;
        }
    }
}
