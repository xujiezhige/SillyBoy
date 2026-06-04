using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SurvivalEngine.Debugging
{
    public static class AIRuntimeSceneQuery
    {
        public static PlayerCharacter GetPrimaryPlayer()
        {
            var player = PlayerCharacter.GetFirst();
            if (IsUsable(player))
                return player;

            return FindPlayersInScene()
                .OrderBy(p => p.player_id)
                .ThenBy(p => p.GetInstanceID())
                .FirstOrDefault();
        }

        public static List<PlayerCharacter> GetPlayers()
        {
            var players = PlayerCharacter.GetAll();
            var validPlayers = players != null
                ? players.Where(IsUsable).Distinct().ToList()
                : new List<PlayerCharacter>();

            return validPlayers.Count > 0 ? validPlayers : FindPlayersInScene();
        }

        public static List<Item> GetItems()
        {
            var items = Item.GetAll();
            var validItems = items != null
                ? items.Where(IsUsable).Distinct().ToList()
                : new List<Item>();

            return validItems.Count > 0 ? validItems : FindItemsInScene();
        }

        private static List<PlayerCharacter> FindPlayersInScene()
        {
            return Object.FindObjectsOfType<PlayerCharacter>(true)
                .Where(IsUsable)
                .ToList();
        }

        private static List<Item> FindItemsInScene()
        {
            return Object.FindObjectsOfType<Item>(true)
                .Where(IsUsable)
                .ToList();
        }

        private static bool IsUsable(PlayerCharacter player)
        {
            return player != null && player.gameObject.scene.IsValid();
        }

        private static bool IsUsable(Item item)
        {
            return item != null && item.gameObject.scene.IsValid();
        }
    }
}
