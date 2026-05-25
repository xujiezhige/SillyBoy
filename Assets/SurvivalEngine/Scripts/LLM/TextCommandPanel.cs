using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalEngine
{
    /// <summary>
    /// 文本指令输入面板 - 允许玩家输入文本指令控制角色
    /// </summary>
    public class TextCommandPanel : UIPanel
    {
        [Header("UI Elements")]
        public TMP_InputField inputField;
        public Button submitButton;
        public Button closeButton;
        public TMP_Text feedbackText;
        public ScrollRect commandHistoryScroll;
        public GameObject historyItemPrefab;
        public Transform historyContent;
        public int maxHistoryItems = 100;

        private List<string> commandHistory = new List<string>();
        private Coroutine feedbackCoroutine = null;

        private static TextCommandPanel _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        protected override void Start()
        {
            base.Start();

            if (inputField != null)
            {
                inputField.onEndEdit.AddListener(OnInputEndEdit);
            }

            if (submitButton != null)
            {
                submitButton.onClick.AddListener(OnSubmit);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(()=>Hide(true));
            }

            Show(true);
        }

        protected override void Update()
        {
            base.Update();

            // 切换面板显示
            if (Input.GetKeyDown(KeyCode.F1) && !TheGame.Get().IsPaused())
            {
                if (IsVisible())
                    Hide();
                else
                    Show();
            }

            // ESC键关闭面板
            if (IsVisible() && Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);

            if (inputField != null)
            {
                inputField.text = "";
                inputField.ActivateInputField();
                inputField.Select();
            }

            UpdateHistoryDisplay();
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);

            if (inputField != null)
            {
                inputField.DeactivateInputField();
            }
        }

        /// <summary>
        /// 输入框结束编辑事件
        /// </summary>
        private void OnInputEndEdit(string text)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnSubmit();
            }
        }

        /// <summary>
        /// 提交指令
        /// </summary>
        private void OnSubmit()
        {
            if (inputField == null)
                return;

            string command = inputField.text.Trim();
            
            if (string.IsNullOrEmpty(command))
                return;

            // 添加到历史记录
            AddToHistory(command);

            // 执行指令
            TextCommandSystem system = TextCommandSystem.Get();
            if (system != null)
            {
                PlayerCharacter character = PlayerCharacter.GetFirst();
                
                // 显示解析中提示
                ShowFeedback("正在解析指令...", Color.yellow);
                
                system.ParseAndExecute(command, character);
                
                ShowFeedback($"已提交指令: {command}", Color.green);
            }
            else
            {
                ShowFeedback("错误: 找不到TextCommandSystem组件", Color.red);
            }

            // 清空输入框
            inputField.text = "";
            inputField.ActivateInputField();
        }

        /// <summary>
        /// 添加到历史记录
        /// </summary>
        private void AddToHistory(string command)
        {
            commandHistory.Insert(0, command);
            
            // 限制历史记录数量
            if (commandHistory.Count > 100)
            {
                commandHistory.RemoveAt(100);
            }

            UpdateHistoryDisplay();
        }

        /// <summary>
        /// 更新历史记录显示
        /// </summary>
        private void UpdateHistoryDisplay()
        {
            if (historyContent == null || historyItemPrefab == null)
                return;

            // 清除现有历史项
            foreach (Transform child in historyContent)
            {
                Destroy(child.gameObject);
            }

            // 创建历史项
            foreach (string cmd in commandHistory)
            {
                GameObject itemObj = Instantiate(historyItemPrefab, historyContent);
                Text text = itemObj.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.text = cmd;
                }

                // 点击历史项可以重新执行
                Button btn = itemObj.GetComponent<Button>();
                if (btn != null)
                {
                    string cmdCopy = cmd; // 闭包变量
                    btn.onClick.AddListener(() => ExecuteHistoryCommand(cmdCopy));
                }
            }

            // 滚动到底部
            if (commandHistoryScroll != null)
            {
                StartCoroutine(ScrollToTop());
            }
        }

        /// <summary>
        /// 执行历史记录中的指令
        /// </summary>
        private void ExecuteHistoryCommand(string command)
        {
            if (inputField != null)
            {
                inputField.text = command;
                OnSubmit();
            }
        }

        /// <summary>
        /// 滚动到顶部（最新项）
        /// </summary>
        private IEnumerator ScrollToTop()
        {
            yield return null;
            if (commandHistoryScroll != null)
            {
                commandHistoryScroll.verticalNormalizedPosition = 1f;
            }
        }

        /// <summary>
        /// 显示反馈信息
        /// </summary>
        private void ShowFeedback(string message, Color color)
        {
            if (feedbackText == null)
                return;

            feedbackText.text = message;
            feedbackText.color = color;
            feedbackText.gameObject.SetActive(true);

            if (feedbackCoroutine != null)
            {
                StopCoroutine(feedbackCoroutine);
            }

            feedbackCoroutine = StartCoroutine(HideFeedbackAfterDelay());
        }

        /// <summary>
        /// 延迟隐藏反馈信息
        /// </summary>
        private IEnumerator HideFeedbackAfterDelay()
        {
            yield return new WaitForSeconds(1f);
            
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
            }
        }

        public static TextCommandPanel Get()
        {
            return _instance;
        }
    }
}

