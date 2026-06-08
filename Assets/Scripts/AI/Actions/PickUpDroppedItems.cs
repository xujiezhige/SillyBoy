using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace SurvivalEngine
{
    [Name("PickUpDroppedItems")]
    [Category("SurvivalEngine/Player")]
    [Description("Pick up dropped item GameObjects in order using the current player.")]
    public class PickUpDroppedItems : ActionTask
    {
        [Tooltip("Dropped item GameObjects to pick up in order. Invalid, inactive, or non-interactable items are skipped.")]
        public BBParameter<List<GameObject>> items;

        private PlayerCharacter player;
        private Item currentItem;
        private Selectable currentSelectable;
        private Vector3 lastTargetPosition;
        private int currentIndex;
        private bool hasMoveRequest;
        private bool waitingForPickup;

        protected override string info
        {
            get { return "Pick up " + items; }
        }

        protected override void OnExecute()
        {
            player = PlayerCharacter.GetFirst();
            currentIndex = 0;
            currentItem = null;
            currentSelectable = null;
            hasMoveRequest = false;
            waitingForPickup = false;

            if (player == null)
            {
                EndAction(false);
                return;
            }

            SelectNextItem();
        }

        protected override void OnUpdate()
        {
            if (player == null)
            {
                EndAction(false);
                return;
            }

            if (waitingForPickup)
            {
                if (currentItem == null)
                {
                    SelectNextItem();
                    return;
                }

                if (!player.IsBusy())
                    EndAction(false);

                return;
            }

            if (currentItem == null)
            {
                SelectNextItem();
                return;
            }

            if (!IsExistingDroppedItem(currentItem))
            {
                SelectNextItem();
                return;
            }

            if (!CanStoreItem(currentItem))
            {
                EndAction(false);
                return;
            }

            currentSelectable = currentItem.GetSelectable();
            if (currentSelectable.IsInUseRange(player))
            {
                PickUpCurrentItem();
                return;
            }

            if (!hasMoveRequest || Vector3.Distance(lastTargetPosition, GetTargetPosition()) > 0.2f)
                RequestMove();
        }

        protected override void OnPause()
        {
            OnStop();
        }

        protected override void OnStop()
        {
            if (player != null && !waitingForPickup)
                player.StopMove();

            player = null;
            currentItem = null;
            currentSelectable = null;
            hasMoveRequest = false;
            waitingForPickup = false;
        }

        private void SelectNextItem()
        {
            List<GameObject> itemList = items.value;
            while (itemList != null && currentIndex < itemList.Count)
            {
                GameObject obj = itemList[currentIndex];
                currentIndex++;

                Item item = obj != null ? obj.GetComponent<Item>() : null;
                if (!IsExistingDroppedItem(item))
                    continue;

                if (!CanStoreItem(item))
                {
                    EndAction(false);
                    return;
                }

                currentItem = item;
                currentSelectable = currentItem.GetSelectable();
                hasMoveRequest = false;
                waitingForPickup = false;
                return;
            }

            currentItem = null;
            currentSelectable = null;
            EndAction(true);
        }

        private bool IsExistingDroppedItem(Item item)
        {
            return item != null
                && item.data != null
                && item.quantity > 0
                && item.gameObject.activeInHierarchy
                && item.GetSelectable() != null
                && item.GetSelectable().CanBeInteracted();
        }

        private bool CanStoreItem(Item item)
        {
            return item != null && AIInventorySpaceUtility.CanTakeOrMakeRoom(player, item.data, item.quantity);
        }

        private void PickUpCurrentItem()
        {
            if (!AIInventorySpaceUtility.EnsureCanTake(player, currentItem.data, currentItem.quantity))
            {
                EndAction(false);
                return;
            }

            waitingForPickup = true;
            player.StopMove();
            currentSelectable.Use(player, currentSelectable.GetClosestInteractPoint(player.GetInteractCenter()));

            if (!player.IsBusy() && currentItem == null)
                SelectNextItem();
        }

        private void RequestMove()
        {
            lastTargetPosition = GetTargetPosition();
            hasMoveRequest = true;
            player.MoveTo(lastTargetPosition);
        }

        private Vector3 GetTargetPosition()
        {
            return currentSelectable.GetClosestInteractPoint(player.GetInteractCenter());
        }
    }
}
