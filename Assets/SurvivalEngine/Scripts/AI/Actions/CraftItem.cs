using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace SurvivalEngine
{
    [Name("CraftItem")]
    [Category("SurvivalEngine/Player")]
    [Description("Craft the item id with the current player and wait until crafting finishes.")]
    public class CraftItem : ActionTask
    {
        [Tooltip("Craft item id to craft with the current player. The value must match a CraftData item id.")]
        public BBParameter<string> itemId;

        private PlayerCharacter player;
        private CraftData craft;

        protected override string info
        {
            get { return "Craft " + itemId; }
        }

        protected override void OnExecute()
        {
            player = PlayerCharacter.GetFirst();
            craft = !string.IsNullOrEmpty(itemId.value) ? CraftData.Get(itemId.value) : null;

            if (player == null || craft == null || !player.Crafting.CanCraft(craft))
            {
                EndAction(false);
                return;
            }

            player.Crafting.StartCraftingOrBuilding(craft);

            if (!player.Crafting.IsCrafting() && !player.Crafting.IsBuildMode())
                EndAction(player.Crafting.CountTotalCrafted(craft) > 0 || player.Inventory.HasItem(craft.GetItem(), 1));
        }

        protected override void OnUpdate()
        {
            if (player == null || craft == null)
            {
                EndAction(false);
                return;
            }

            if (!player.Crafting.IsCrafting() && !player.Crafting.IsBuildMode())
                EndAction(player.Crafting.CountTotalCrafted(craft) > 0 || player.Inventory.HasItem(craft.GetItem(), 1));
        }

        protected override void OnStop()
        {
            player = null;
            craft = null;
        }
    }
}
