using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// 文本指令解析器 - 将自然语言文本转换为游戏指令序列
    /// </summary>
    public static class TextCommandParser
    {
        // 关键词映射
        private static Dictionary<string, CommandType> actionKeywords = new Dictionary<string, CommandType>
        {
            // 移动相关
            {"移动", CommandType.Move},
            {"去", CommandType.Move},
            {"走到", CommandType.Move},
            {"前往", CommandType.Move},
            {"move", CommandType.Move},
            {"go", CommandType.Move},
            {"walk", CommandType.Move},
            
            // 交互相关
            {"交互", CommandType.Interact},
            {"使用", CommandType.Use},
            {"打开", CommandType.Interact},
            {"操作", CommandType.Interact},
            {"interact", CommandType.Interact},
            {"use", CommandType.Use},
            {"open", CommandType.Interact},
            
            // 攻击相关
            {"攻击", CommandType.Attack},
            {"打", CommandType.Attack},
            {"砍", CommandType.Attack},
            {"attack", CommandType.Attack},
            {"hit", CommandType.Attack},
            
            // 拾取相关
            {"拾取", CommandType.Pickup},
            {"捡", CommandType.Pickup},
            {"拿", CommandType.Pickup},
            {"pickup", CommandType.Pickup},
            {"pick", CommandType.Pickup},
            {"take", CommandType.Pickup},
            
            // 动作相关
            {"跳跃", CommandType.Jump},
            {"跳", CommandType.Jump},
            {"jump", CommandType.Jump},
            
            {"等待", CommandType.Wait},
            {"wait", CommandType.Wait},
            
            {"停止", CommandType.Stop},
            {"stop", CommandType.Stop},
            
            {"面向", CommandType.Face},
            {"face", CommandType.Face},
            
            // 物品相关
            {"丢弃", CommandType.Drop},
            {"drop", CommandType.Drop},
            
            {"制作", CommandType.Craft},
            {"craft", CommandType.Craft},
            
            {"吃", CommandType.Eat},
            {"eat", CommandType.Eat},
            
            {"喝", CommandType.Drink},
            {"drink", CommandType.Drink},
            
            // 其他
            {"睡觉", CommandType.Sleep},
            {"sleep", CommandType.Sleep},
            
            {"建造", CommandType.Build},
            {"build", CommandType.Build}
        };

        /// <summary>
        /// 解析文本为指令序列
        /// </summary>
        public static List<GameCommand> Parse(string text, PlayerCharacter character)
        {
            List<GameCommand> commands = new List<GameCommand>();
            
            if (string.IsNullOrEmpty(text))
                return commands;

            text = text.Trim().ToLower();
            
            // 按句号、分号、换行符分割多个指令
            string[] sentences = Regex.Split(text, @"[。；;\n]");
            
            foreach (string sentence in sentences)
            {
                string trimmed = sentence.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                GameCommand cmd = ParseSentence(trimmed, character);
                if (cmd != null)
                    commands.Add(cmd);
            }

            return commands;
        }

        /// <summary>
        /// 解析单个句子
        /// </summary>
        private static GameCommand ParseSentence(string sentence, PlayerCharacter character)
        {
            sentence = sentence.Trim();
            
            // 查找动作关键词
            CommandType? actionType = null;
            string actionKeyword = "";
            
            foreach (var kvp in actionKeywords)
            {
                if (sentence.Contains(kvp.Key))
                {
                    actionType = kvp.Value;
                    actionKeyword = kvp.Key;
                    break;
                }
            }

            if (!actionType.HasValue)
            {
                // 如果没有找到动作关键词，尝试解析为移动指令（包含坐标或方向）
                if (TryParseMoveCommand(sentence, out GameCommand moveCmd, character))
                {
                    return moveCmd;
                }
                return null;
            }

            GameCommand cmd = new GameCommand(actionType.Value);

            // 解析参数
            switch (actionType.Value)
            {
                case CommandType.Move:
                case CommandType.MoveToObject:
                    ParseMoveCommand(sentence, actionKeyword, cmd, character);
                    break;

                case CommandType.Interact:
                case CommandType.Use:
                case CommandType.Attack:
                case CommandType.Pickup:
                    ParseObjectCommand(sentence, actionKeyword, cmd, character);
                    break;

                case CommandType.Wait:
                    ParseWaitCommand(sentence, actionKeyword, cmd);
                    break;

                case CommandType.Drop:
                    ParseDropCommand(sentence, actionKeyword, cmd, character);
                    break;

                case CommandType.Craft:
                case CommandType.Build:
                    ParseCraftCommand(sentence, actionKeyword, cmd);
                    break;

                case CommandType.Eat:
                case CommandType.Drink:
                    ParseItemCommand(sentence, actionKeyword, cmd);
                    break;

                case CommandType.Sleep:
                    ParseSleepCommand(sentence, actionKeyword, cmd, character);
                    break;

                case CommandType.Face:
                    ParseFaceCommand(sentence, actionKeyword, cmd, character);
                    break;
            }

            return cmd;
        }

        /// <summary>
        /// 解析移动指令
        /// </summary>
        private static void ParseMoveCommand(string sentence, string keyword, GameCommand cmd, PlayerCharacter character)
        {
            // 移除动作关键词
            string remaining = sentence.Replace(keyword, "").Trim();

            // 尝试解析坐标 (x, y, z) 或 (x, z)
            Match coordMatch = Regex.Match(remaining, @"\(?\s*([-\d.]+)\s*[,，]\s*([-\d.]+)\s*(?:[,，]\s*([-\d.]+))?\s*\)?");
            if (coordMatch.Success)
            {
                float x = float.Parse(coordMatch.Groups[1].Value);
                float z = float.Parse(coordMatch.Groups[2].Value);
                float y = coordMatch.Groups[3].Success ? float.Parse(coordMatch.Groups[3].Value) : character.transform.position.y;
                
                cmd.type = CommandType.Move;
                cmd.position = new Vector3(x, y, z);
                return;
            }

            // 尝试解析方向词
            Vector3 direction = Vector3.zero;
            if (remaining.Contains("前") || remaining.Contains("forward") || remaining.Contains("north"))
                direction += Vector3.forward;
            if (remaining.Contains("后") || remaining.Contains("back") || remaining.Contains("south"))
                direction += Vector3.back;
            if (remaining.Contains("左") || remaining.Contains("left") || remaining.Contains("west"))
                direction += Vector3.left;
            if (remaining.Contains("右") || remaining.Contains("right") || remaining.Contains("east"))
                direction += Vector3.right;

            if (direction != Vector3.zero)
            {
                // 解析距离
                float distance = 5f;
                Match distMatch = Regex.Match(remaining, @"(\d+(?:\.\d+)?)\s*(?:米|m|meter)");
                if (distMatch.Success)
                    distance = float.Parse(distMatch.Groups[1].Value);

                cmd.type = CommandType.Move;
                cmd.position = character.transform.position + direction.normalized * distance;
                return;
            }

            // 尝试查找对象
            Selectable target = FindObjectByName(remaining, character);
            if (target != null)
            {
                cmd.type = CommandType.MoveToObject;
                cmd.targetObject = target;
                cmd.objectName = remaining;
            }
            else
            {
                // 默认移动到前方
                cmd.type = CommandType.Move;
                cmd.position = character.transform.position + character.transform.forward * 5f;
            }
        }

        /// <summary>
        /// 解析对象相关指令
        /// </summary>
        private static void ParseObjectCommand(string sentence, string keyword, GameCommand cmd, PlayerCharacter character)
        {
            string remaining = sentence.Replace(keyword, "").Trim();
            
            if (string.IsNullOrEmpty(remaining))
            {
                // 如果没有指定对象，尝试找最近的可交互对象
                Selectable nearest = Selectable.GetNearestAutoInteract(character.transform.position, 10f);
                if (nearest != null)
                {
                    cmd.targetObject = nearest;
                    return;
                }
            }

            Selectable target = FindObjectByName(remaining, character);
            if (target != null)
            {
                cmd.targetObject = target;
                cmd.objectName = remaining;
            }
        }

        /// <summary>
        /// 解析等待指令
        /// </summary>
        private static void ParseWaitCommand(string sentence, string keyword, GameCommand cmd)
        {
            string remaining = sentence.Replace(keyword, "").Trim();
            
            // 解析时间
            float time = 1f;
            Match timeMatch = Regex.Match(remaining, @"(\d+(?:\.\d+)?)\s*(?:秒|s|second)");
            if (timeMatch.Success)
                time = float.Parse(timeMatch.Groups[1].Value);
            else
            {
                // 尝试解析纯数字
                Match numMatch = Regex.Match(remaining, @"(\d+(?:\.\d+)?)");
                if (numMatch.Success)
                    time = float.Parse(numMatch.Groups[1].Value);
            }

            cmd.waitTime = time;
        }

        /// <summary>
        /// 解析丢弃指令
        /// </summary>
        private static void ParseDropCommand(string sentence, string keyword, GameCommand cmd, PlayerCharacter character)
        {
            string remaining = sentence.Replace(keyword, "").Trim();
            
            // 尝试解析位置
            Match coordMatch = Regex.Match(remaining, @"\(?\s*([-\d.]+)\s*[,，]\s*([-\d.]+)\s*(?:[,，]\s*([-\d.]+))?\s*\)?");
            if (coordMatch.Success)
            {
                float x = float.Parse(coordMatch.Groups[1].Value);
                float z = float.Parse(coordMatch.Groups[2].Value);
                float y = coordMatch.Groups[3].Success ? float.Parse(coordMatch.Groups[3].Value) : character.transform.position.y;
                cmd.position = new Vector3(x, y, z);
            }
            else
            {
                // 默认在当前位置
                cmd.position = character.transform.position;
            }
        }

        /// <summary>
        /// 解析制作/建造指令
        /// </summary>
        private static void ParseCraftCommand(string sentence, string keyword, GameCommand cmd)
        {
            string remaining = sentence.Replace(keyword, "").Trim();
            
            // 解析数量
            Match qtyMatch = Regex.Match(remaining, @"(\d+)\s*(?:个|件|piece)");
            if (qtyMatch.Success)
                cmd.quantity = int.Parse(qtyMatch.Groups[1].Value);
            else
                cmd.quantity = 1;

            // 移除数量信息，获取物品名
            remaining = Regex.Replace(remaining, @"\d+\s*(?:个|件|piece)", "").Trim();
            cmd.itemName = remaining;
        }

        /// <summary>
        /// 解析物品指令（吃/喝）
        /// </summary>
        private static void ParseItemCommand(string sentence, string keyword, GameCommand cmd)
        {
            string remaining = sentence.Replace(keyword, "").Trim();
            cmd.itemName = remaining;
        }

        /// <summary>
        /// 解析睡觉指令
        /// </summary>
        private static void ParseSleepCommand(string sentence, string keyword, GameCommand cmd, PlayerCharacter character)
        {
            string remaining = sentence.Replace(keyword, "").Trim();
            
            if (string.IsNullOrEmpty(remaining))
            {
                // 查找最近的床
                Selectable nearest = Selectable.GetNearest(character.transform.position, 20f);
                if (nearest != null && nearest.GetComponent<ActionSleep>() != null)
                {
                    cmd.targetObject = nearest;
                }
            }
            else
            {
                Selectable target = FindObjectByName(remaining, character);
                if (target != null)
                    cmd.targetObject = target;
            }
        }

        /// <summary>
        /// 解析面向指令
        /// </summary>
        private static void ParseFaceCommand(string sentence, string keyword, GameCommand cmd, PlayerCharacter character)
        {
            string remaining = sentence.Replace(keyword, "").Trim();
            
            Selectable target = FindObjectByName(remaining, character);
            if (target != null)
            {
                cmd.targetObject = target;
            }
            else
            {
                // 尝试解析坐标
                Match coordMatch = Regex.Match(remaining, @"\(?\s*([-\d.]+)\s*[,，]\s*([-\d.]+)\s*(?:[,，]\s*([-\d.]+))?\s*\)?");
                if (coordMatch.Success)
                {
                    float x = float.Parse(coordMatch.Groups[1].Value);
                    float z = float.Parse(coordMatch.Groups[2].Value);
                    float y = coordMatch.Groups[3].Success ? float.Parse(coordMatch.Groups[3].Value) : character.transform.position.y;
                    cmd.position = new Vector3(x, y, z);
                }
            }
        }

        /// <summary>
        /// 尝试解析为移动指令（无动作关键词的情况）
        /// </summary>
        private static bool TryParseMoveCommand(string sentence, out GameCommand cmd, PlayerCharacter character)
        {
            cmd = null;

            // 检查是否包含坐标
            Match coordMatch = Regex.Match(sentence, @"\(?\s*([-\d.]+)\s*[,，]\s*([-\d.]+)\s*(?:[,，]\s*([-\d.]+))?\s*\)?");
            if (coordMatch.Success)
            {
                float x = float.Parse(coordMatch.Groups[1].Value);
                float z = float.Parse(coordMatch.Groups[2].Value);
                float y = coordMatch.Groups[3].Success ? float.Parse(coordMatch.Groups[3].Value) : character.transform.position.y;
                
                cmd = new GameCommand(CommandType.Move);
                cmd.position = new Vector3(x, y, z);
                return true;
            }

            // 检查是否包含方向词
            if (sentence.Contains("前") || sentence.Contains("后") || sentence.Contains("左") || sentence.Contains("右") ||
                sentence.Contains("forward") || sentence.Contains("back") || sentence.Contains("left") || sentence.Contains("right") ||
                sentence.Contains("north") || sentence.Contains("south") || sentence.Contains("east") || sentence.Contains("west"))
            {
                cmd = new GameCommand(CommandType.Move);
                ParseMoveCommand(sentence, "", cmd, character);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 根据名称查找对象
        /// </summary>
        private static Selectable FindObjectByName(string name, PlayerCharacter character)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            name = name.Trim().ToLower();
            Selectable nearest = null;
            float nearestDist = float.MaxValue;
            Vector3 playerPos = character.transform.position;

            // 在所有活跃的Selectable中查找
            foreach (Selectable selectable in Selectable.GetAllActive())
            {
                if (selectable == null || !selectable.gameObject.activeSelf)
                    continue;

                string objName = selectable.gameObject.name.ToLower();
                
                // 精确匹配或包含匹配
                if (objName == name || objName.Contains(name) || name.Contains(objName))
                {
                    float dist = Vector3.Distance(selectable.transform.position, playerPos);
                    if (dist < nearestDist)
                    {
                        nearest = selectable;
                        nearestDist = dist;
                    }
                }
            }

            return nearest;
        }
    }
}

