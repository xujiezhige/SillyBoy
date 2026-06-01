using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace SurvivalEngine
{
    [Name("CanCraftItem")]
    [Category("SurvivalEngine/Player")]
    [Description("Check whether the current player can craft the item id.")]
    public class CanCraftItem : ConditionTask
    {
        [Tooltip("Craft item id to check against the current player's known recipes, materials, and crafting requirements.")]
        public BBParameter<string> itemId;

        protected override string info
        {
            get { return "Can craft " + itemId; }
        }

        protected override bool OnCheck()
        {
            PlayerCharacter player = PlayerCharacter.GetFirst();
            if (player == null || string.IsNullOrEmpty(itemId.value))
                return false;

            CraftData craft = CraftData.Get(itemId.value);
            return craft != null && player.Crafting.CanCraft(craft);
        }
    }
}
