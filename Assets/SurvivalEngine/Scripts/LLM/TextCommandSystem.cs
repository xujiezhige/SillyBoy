using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// 指令类型枚举
    /// </summary>
    public enum CommandType
    {
        Move,           // 移动到位置
        MoveToObject,   // 移动到对象
        Interact,       // 交互
        Attack,         // 攻击
        Pickup,         // 拾取
        Use,            // 使用
        Jump,           // 跳跃
        Wait,           // 等待
        Stop,           // 停止
        Face,           // 面向
        Drop,           // 丢弃
        Craft,          // 制作
        Eat,            // 吃
        Drink,          // 喝
        Sleep,          // 睡觉
        Build           // 建造
    }

    /// <summary>
    /// 游戏指令数据结构
    /// </summary>
    [System.Serializable]
    public class GameCommand
    {
        public CommandType type;
        public Vector3 position;           // 位置参数
        public string objectName;         // 对象名称
        public Selectable targetObject;    // 目标对象
        public float waitTime;            // 等待时间
        public string itemName;           // 物品名称
        public int quantity;              // 数量

        public GameCommand(CommandType cmdType)
        {
            type = cmdType;
            position = Vector3.zero;
            objectName = "";
            targetObject = null;
            waitTime = 0f;
            itemName = "";
            quantity = 1;
        }
    }

    /// <summary>
    /// 文本指令系统 - 将自然语言转换为游戏指令并执行
    /// </summary>
    public class TextCommandSystem : MonoBehaviour
    {
        private static TextCommandSystem _instance;
        private Queue<GameCommand> commandQueue = new Queue<GameCommand>();
        private bool isExecuting = false;
        private Coroutine executionCoroutine = null;

        void Awake()
        {
            _instance = this;
        }

        public static TextCommandSystem Get()
        {
            if (_instance == null)
                _instance = FindObjectOfType<TextCommandSystem>();
            return _instance;
        }

        /// <summary>
        /// 解析文本并执行指令序列（异步）
        /// </summary>
        public void ParseAndExecute(string text, PlayerCharacter character = null)
        {
            if (character == null)
                character = PlayerCharacter.GetFirst();

            if (character == null)
            {
                Debug.LogWarning("TextCommandSystem: 找不到玩家角色");
                return;
            }

            // 使用 LLM 解析器
            TextCommandParserLLM parser = TextCommandParserLLM.Get();
            parser.Parse(text, character,
                (commands) => {
                    // 解析成功
                    if (commands != null && commands.Count > 0)
                    {
                        foreach (var cmd in commands)
                        {
                            commandQueue.Enqueue(cmd);
                        }

                        if (!isExecuting)
                        {
                            executionCoroutine = StartCoroutine(ExecuteCommands(character));
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"TextCommandSystem: 无法解析指令: {text}");
                    }
                },
                (error) => {
                    // 解析失败
                    Debug.LogError($"TextCommandSystem: 解析指令失败: {error}");
                });
        }

        /// <summary>
        /// 执行指令队列
        /// </summary>
        private IEnumerator ExecuteCommands(PlayerCharacter character)
        {
            isExecuting = true;

            while (commandQueue.Count > 0)
            {
                GameCommand cmd = commandQueue.Dequeue();
                yield return StartCoroutine(ExecuteCommand(cmd, character));
            }

            isExecuting = false;
        }

        /// <summary>
        /// 执行单个指令
        /// </summary>
        private IEnumerator ExecuteCommand(GameCommand cmd, PlayerCharacter character)
        {
            if (character == null || character.IsDead())
                yield break;

            switch (cmd.type)
            {
                case CommandType.Move:
                    yield return StartCoroutine(CommandMove(cmd, character));
                    break;

                case CommandType.MoveToObject:
                    yield return StartCoroutine(CommandMoveToObject(cmd, character));
                    break;

                case CommandType.Interact:
                    yield return StartCoroutine(CommandInteract(cmd, character));
                    break;

                case CommandType.Attack:
                    yield return StartCoroutine(CommandAttack(cmd, character));
                    break;

                case CommandType.Pickup:
                    yield return StartCoroutine(CommandPickup(cmd, character));
                    break;

                case CommandType.Use:
                    yield return StartCoroutine(CommandUse(cmd, character));
                    break;

                case CommandType.Jump:
                    CommandJump(character);
                    yield return new WaitForSeconds(0.5f);
                    break;

                case CommandType.Wait:
                    yield return new WaitForSeconds(cmd.waitTime);
                    break;

                case CommandType.Stop:
                    CommandStop(character);
                    yield break;

                case CommandType.Face:
                    CommandFace(cmd, character);
                    yield return new WaitForSeconds(0.2f);
                    break;

                case CommandType.Drop:
                    yield return StartCoroutine(CommandDrop(cmd, character));
                    break;

                case CommandType.Craft:
                    yield return StartCoroutine(CommandCraft(cmd, character));
                    break;

                case CommandType.Eat:
                    yield return StartCoroutine(CommandEat(cmd, character));
                    break;

                case CommandType.Drink:
                    yield return StartCoroutine(CommandDrink(cmd, character));
                    break;

                case CommandType.Sleep:
                    yield return StartCoroutine(CommandSleep(cmd, character));
                    break;

                case CommandType.Build:
                    yield return StartCoroutine(CommandBuild(cmd, character));
                    break;
            }
        }

        // 指令实现方法
        private IEnumerator CommandMove(GameCommand cmd, PlayerCharacter character)
        {
            character.MoveTo(cmd.position);
            yield return new WaitUntil(() => !character.IsMoving() || Vector3.Distance(character.transform.position, cmd.position) < 1f);
        }

        private IEnumerator CommandMoveToObject(GameCommand cmd, PlayerCharacter character)
        {
            if (cmd.targetObject != null)
            {
                character.InteractMove(cmd.targetObject, cmd.targetObject.GetPosition());
                yield return new WaitUntil(() => !character.IsMoving() || 
                    Vector3.Distance(character.transform.position, cmd.targetObject.GetPosition()) < 2f);
            }
        }

        private IEnumerator CommandInteract(GameCommand cmd, PlayerCharacter character)
        {
            if (cmd.targetObject != null)
            {
                character.Interact(cmd.targetObject);
                yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator CommandAttack(GameCommand cmd, PlayerCharacter character)
        {
            if (cmd.targetObject != null)
            {
                Destructible destruct = cmd.targetObject.GetComponent<Destructible>();
                if (destruct != null)
                {
                    character.Attack(destruct);
                    yield return new WaitForSeconds(1f);
                }
            }
            else
            {
                character.Attack();
                yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator CommandPickup(GameCommand cmd, PlayerCharacter character)
        {
            if (cmd.targetObject != null)
            {
                Item item = cmd.targetObject.GetComponent<Item>();
                if (item != null)
                {
                    character.InteractMove(cmd.targetObject, cmd.targetObject.GetPosition());
                    yield return new WaitUntil(() => !character.IsMoving() || 
                        Vector3.Distance(character.transform.position, cmd.targetObject.GetPosition()) < 2f);
                    character.Interact(cmd.targetObject);
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        private IEnumerator CommandUse(GameCommand cmd, PlayerCharacter character)
        {
            yield return StartCoroutine(CommandInteract(cmd, character));
        }

        private void CommandJump(PlayerCharacter character)
        {
            PlayerCharacterJump jump = character.GetComponent<PlayerCharacterJump>();
            if (jump != null)
                jump.Jump();
        }

        private void CommandStop(PlayerCharacter character)
        {
            character.StopAutoMove();
        }

        private void CommandFace(GameCommand cmd, PlayerCharacter character)
        {
            if (cmd.targetObject != null)
            {
                character.FaceTorward(cmd.targetObject.GetPosition());
            }
            else
            {
                character.FaceTorward(cmd.position);
            }
        }

        private IEnumerator CommandDrop(GameCommand cmd, PlayerCharacter character)
        {
            character.MoveTo(cmd.position);
            yield return new WaitUntil(() => !character.IsMoving() || Vector3.Distance(character.transform.position, cmd.position) < 1f);
            PlayerUI ui = PlayerUI.Get(character.player_id);
            if (ui != null)
            {
                character.AutoDropItem();
                yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator CommandCraft(GameCommand cmd, PlayerCharacter character)
        {
            PlayerCharacterCraft craft = character.GetComponent<PlayerCharacterCraft>();
            if (craft != null && !string.IsNullOrEmpty(cmd.itemName))
            {
                // 尝试查找 CraftData
                CraftData craftData = CraftData.Get(cmd.itemName);
                if (craftData != null && craft.CanCraft(craftData))
                {
                    craft.StartCraftingOrBuilding(craftData);
                    yield return new WaitForSeconds(craftData.craft_duration + 0.5f);
                }
            }
        }

        private IEnumerator CommandEat(GameCommand cmd, PlayerCharacter character)
        {
            if (!string.IsNullOrEmpty(cmd.itemName))
            {
                ItemData itemData = ItemData.Get(cmd.itemName);
                if (itemData != null)
                {
                    PlayerCharacterInventory inventory = character.GetComponent<PlayerCharacterInventory>();
                    var slot = inventory.InventoryData.GetFirstItemSlot(itemData.id, itemData.inventory_max, true);
                    if (slot >= 0)
                    {
                        inventory.EatItem(slot);
                        yield return new WaitForSeconds(1f);
                    }
                }
            }
        }

        private IEnumerator CommandDrink(GameCommand cmd, PlayerCharacter character)
        {
            yield return StartCoroutine(CommandEat(cmd, character)); // 喝和吃使用相同的逻辑
        }

        private IEnumerator CommandSleep(GameCommand cmd, PlayerCharacter character)
        {
            if (cmd.targetObject != null)
            {
                ActionSleep sleepAction = cmd.targetObject.GetComponent<ActionSleep>();
                if (sleepAction != null)
                {
                    character.InteractMove(cmd.targetObject, cmd.targetObject.GetPosition());
                    yield return new WaitUntil(() => !character.IsMoving() || 
                        Vector3.Distance(character.transform.position, cmd.targetObject.GetPosition()) < 2f);
                    character.Interact(cmd.targetObject);
                    yield return new WaitForSeconds(1f);
                }
            }
        }

        private IEnumerator CommandBuild(GameCommand cmd, PlayerCharacter character)
        {
            PlayerCharacterCraft craft = character.GetComponent<PlayerCharacterCraft>();
            if (craft != null && !string.IsNullOrEmpty(cmd.itemName))
            {
                // 尝试查找 ConstructionData
                ConstructionData buildData = ConstructionData.Get(cmd.itemName);
                if (buildData != null && craft.CanCraft(buildData))
                {
                    craft.CraftConstructionBuildMode(buildData);
                    yield return new WaitForSeconds(0.5f);
                    
                    if (cmd.position != Vector3.zero)
                    {
                        character.MoveTo(cmd.position);
                        yield return new WaitUntil(() => !character.IsMoving() || 
                            Vector3.Distance(character.transform.position, cmd.position) < 2f);
                        craft.BuildMoveAt(cmd.position);
                    }
                    yield return new WaitForSeconds(1f);
                }
            }
        }

        /// <summary>
        /// 停止执行所有指令
        /// </summary>
        public void StopExecution()
        {
            if (executionCoroutine != null)
            {
                StopCoroutine(executionCoroutine);
                executionCoroutine = null;
            }
            commandQueue.Clear();
            isExecuting = false;
        }

        /// <summary>
        /// 检查是否正在执行指令
        /// </summary>
        public bool IsExecuting()
        {
            return isExecuting;
        }
    }
}

