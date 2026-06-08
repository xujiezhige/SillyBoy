using System.Collections.Generic;
using NodeCanvas.BehaviourTrees;
using NodeCanvas.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalEngine.Debugging
{
    public class AICraftDebugPanel : MonoBehaviour
    {
        private const string CraftItemVariableSuffix = "_bestCraftItemId";

        public Vector2 anchoredPosition = new Vector2(-18f, -18f);
        public Vector2 panelSize = new Vector2(360f, 420f);
        public float refreshInterval = 0.5f;

        private Canvas canvas;
        private RectTransform panel;
        private Image targetIcon;
        private Text titleText;
        private Text statusText;
        private RectTransform materialRoot;
        private Font font;
        private BehaviourTreeOwner cachedOwner;
        private readonly List<MaterialRow> rows = new List<MaterialRow>();
        private readonly List<DebugMaterialEntry> materialEntries = new List<DebugMaterialEntry>();
        private float nextRefreshTime;
        private float nextOwnerSearchTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimePanel()
        {
            if (FindObjectsByType<AICraftDebugPanel>(FindObjectsSortMode.None).Length > 0)
                return;

            GameObject go = new GameObject("AICraftDebugPanel");
            DontDestroyOnLoad(go);
            go.AddComponent<AICraftDebugPanel>();
        }

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime)
                return;

            nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshInterval);
            Refresh();
        }

        private void BuildUI()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            panel = CreateRect("Panel", transform);
            panel.anchorMin = new Vector2(1f, 1f);
            panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(1f, 1f);
            panel.anchoredPosition = anchoredPosition;
            panel.sizeDelta = panelSize;

            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.04f, 0.05f, 0.06f, 0.78f);

            VerticalLayoutGroup panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(12, 12, 10, 12);
            panelLayout.spacing = 8f;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;

            Text header = CreateText("Header", panel, "AI Craft Debug", 14, FontStyle.Bold);
            header.color = new Color(0.78f, 0.86f, 1f, 1f);
            SetLayout(header.gameObject, -1f, 20f);

            RectTransform targetRow = CreateRect("Target", panel);
            HorizontalLayoutGroup targetLayout = targetRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            targetLayout.spacing = 8f;
            targetLayout.childForceExpandWidth = false;
            targetLayout.childForceExpandHeight = false;
            targetLayout.childControlWidth = true;
            targetLayout.childControlHeight = true;
            SetLayout(targetRow.gameObject, -1f, 54f);

            targetIcon = CreateImage("TargetIcon", targetRow);
            SetLayout(targetIcon.gameObject, 48f, 48f);

            RectTransform targetTextRoot = CreateRect("TargetText", targetRow);
            VerticalLayoutGroup targetTextLayout = targetTextRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            targetTextLayout.spacing = 2f;
            targetTextLayout.childForceExpandWidth = true;
            targetTextLayout.childForceExpandHeight = false;
            targetTextLayout.childControlWidth = true;
            targetTextLayout.childControlHeight = true;
            SetLayout(targetTextRoot.gameObject, 280f, 50f);

            titleText = CreateText("TargetTitle", targetTextRoot, "No craft target", 15, FontStyle.Bold);
            titleText.color = Color.white;
            SetLayout(titleText.gameObject, -1f, 24f);

            statusText = CreateText("TargetStatus", targetTextRoot, "", 12, FontStyle.Normal);
            statusText.color = new Color(0.8f, 0.84f, 0.88f, 1f);
            SetLayout(statusText.gameObject, -1f, 20f);

            Text materialHeader = CreateText("MaterialHeader", panel, "Materials", 13, FontStyle.Bold);
            materialHeader.color = new Color(0.78f, 0.86f, 1f, 1f);
            SetLayout(materialHeader.gameObject, -1f, 18f);

            ScrollRect scroll = CreateScroll(panel);
            materialRoot = scroll.content;
            SetLayout(scroll.gameObject, -1f, 300f);
        }

        private ScrollRect CreateScroll(Transform parent)
        {
            RectTransform viewport = CreateRect("MaterialsViewport", parent);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.18f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 6f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        private void Refresh()
        {
            string craftItemId = FindCraftItemId();
            CraftData craft = !string.IsNullOrEmpty(craftItemId) ? CraftData.Get(craftItemId) : null;

            if (craft == null)
            {
                targetIcon.enabled = false;
                titleText.text = "No craft target";
                statusText.text = string.IsNullOrEmpty(craftItemId) ? "Waiting for behavior tree target" : craftItemId;
                materialEntries.Clear();
                SetRows(materialEntries);
                return;
            }

            PlayerCharacter player = AIRuntimeSceneQuery.GetPrimaryPlayer();
            targetIcon.sprite = craft.icon;
            targetIcon.enabled = craft.icon != null;
            titleText.text = string.IsNullOrEmpty(craft.title) ? craft.id : craft.title;
            statusText.text = string.Format("{0}  x{1}", craft.id, Mathf.Max(1, craft.craft_quantity));
            materialEntries.Clear();
            BuildMaterialEntries(player, craft, materialEntries);
            SetRows(materialEntries);
        }

        private string FindCraftItemId()
        {
            BehaviourTreeOwner owner = FindCraftTreeOwner();
            if (owner == null)
                return null;

            string value = TryGetBlackboardString(owner.blackboard, CraftItemVariableSuffix);
            if (!string.IsNullOrEmpty(value))
                return value;

            Graph graph = owner.graph;
            value = TryGetBlackboardString(graph != null ? graph.blackboard : null, CraftItemVariableSuffix);
            if (!string.IsNullOrEmpty(value))
                return value;

            return TryGetBlackboardString(graph != null ? graph.parentBlackboard : null, CraftItemVariableSuffix);
        }

        private BehaviourTreeOwner FindCraftTreeOwner()
        {
            if (cachedOwner != null && cachedOwner.graph != null)
                return cachedOwner;

            if (Time.unscaledTime < nextOwnerSearchTime)
                return null;

            nextOwnerSearchTime = Time.unscaledTime + 1f;
            BehaviourTreeOwner[] owners = FindObjectsByType<BehaviourTreeOwner>(FindObjectsSortMode.None);
            foreach (BehaviourTreeOwner owner in owners)
            {
                if (owner != null && owner.graph != null && owner.graph.name.Contains("CraftAllUsefulItems"))
                {
                    cachedOwner = owner;
                    return owner;
                }
            }

            foreach (BehaviourTreeOwner owner in owners)
            {
                if (owner != null && owner.graph != null)
                {
                    cachedOwner = owner;
                    return cachedOwner;
                }
            }

            cachedOwner = null;
            return cachedOwner;
        }

        private string TryGetBlackboardString(IBlackboard blackboard, string suffix)
        {
            if (blackboard == null || blackboard.variables == null)
                return null;

            foreach (KeyValuePair<string, Variable> pair in blackboard.variables)
            {
                if (pair.Value == null || !pair.Key.EndsWith(suffix, System.StringComparison.Ordinal))
                    continue;

                return pair.Value.value != null ? pair.Value.value.ToString() : null;
            }

            return null;
        }

        private void BuildMaterialEntries(PlayerCharacter player, CraftData craft, List<DebugMaterialEntry> entries)
        {
            CraftCostData cost = craft.GetCraftCost();
            Dictionary<GroupData, int> exactItemGroups = new Dictionary<GroupData, int>();

            foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
            {
                AddItemGroups(exactItemGroups, pair.Key, pair.Value);
                entries.Add(new DebugMaterialEntry
                {
                    icon = pair.Key != null ? pair.Key.icon : null,
                    name = pair.Key != null ? pair.Key.title : "Missing item",
                    required = pair.Value,
                    owned = player != null && pair.Key != null ? player.Inventory.CountItem(pair.Key) : 0
                });
            }

            foreach (KeyValuePair<GroupData, int> pair in cost.craft_fillers)
            {
                int required = pair.Value + CountGroup(exactItemGroups, pair.Key);
                entries.Add(new DebugMaterialEntry
                {
                    icon = pair.Key != null ? pair.Key.icon : null,
                    name = pair.Key != null ? pair.Key.title : "Missing group",
                    required = required,
                    owned = player != null && pair.Key != null ? player.Inventory.CountItemInGroup(pair.Key) : 0
                });
            }

            foreach (KeyValuePair<CraftData, int> pair in cost.craft_requirements)
            {
                entries.Add(new DebugMaterialEntry
                {
                    icon = pair.Key != null ? pair.Key.icon : null,
                    name = pair.Key != null ? pair.Key.title : "Missing requirement",
                    required = pair.Value,
                    owned = player != null && pair.Key != null ? player.Crafting.CountRequirements(pair.Key) : 0
                });
            }

            if (cost.craft_near != null)
            {
                bool isNear = player != null && (player.IsNearGroup(cost.craft_near) || player.EquipData.HasItemInGroup(cost.craft_near));
                entries.Add(new DebugMaterialEntry
                {
                    icon = cost.craft_near.icon,
                    name = cost.craft_near.title,
                    required = 1,
                    owned = isNear ? 1 : 0
                });
            }

        }

        private void SetRows(List<DebugMaterialEntry> entries)
        {
            while (rows.Count < entries.Count)
                rows.Add(CreateMaterialRow(materialRoot));

            for (int i = 0; i < rows.Count; i++)
            {
                bool active = i < entries.Count;
                rows[i].root.gameObject.SetActive(active);
                if (active)
                    rows[i].Set(entries[i]);
            }
        }

        private MaterialRow CreateMaterialRow(Transform parent)
        {
            RectTransform root = CreateRect("MaterialRow", parent);
            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.06f);
            HorizontalLayoutGroup layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 5, 5);
            layout.spacing = 8f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            SetLayout(root.gameObject, -1f, 42f);

            Image icon = CreateImage("Icon", root);
            SetLayout(icon.gameObject, 32f, 32f);

            Text name = CreateText("Name", root, "", 12, FontStyle.Normal);
            name.color = Color.white;
            SetLayout(name.gameObject, 210f, 32f);

            Text quantity = CreateText("Quantity", root, "", 12, FontStyle.Bold);
            quantity.alignment = TextAnchor.MiddleRight;
            SetLayout(quantity.gameObject, 70f, 32f);

            return new MaterialRow(root, icon, name, quantity);
        }

        private void AddItemGroups(Dictionary<GroupData, int> groups, ItemData item, int quantity)
        {
            if (item == null || item.groups == null)
                return;

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

        private RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private Text CreateText(string name, Transform parent, string text, int size, FontStyle style)
        {
            Text label = CreateRect(name, parent).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.color = Color.white;
            return label;
        }

        private Image CreateImage(string name, Transform parent)
        {
            Image image = CreateRect(name, parent).gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.color = Color.white;
            return image;
        }

        private void SetLayout(GameObject go, float preferredWidth, float preferredHeight)
        {
            LayoutElement layout = go.GetComponent<LayoutElement>();
            if (layout == null)
                layout = go.AddComponent<LayoutElement>();

            if (preferredWidth > 0f)
                layout.preferredWidth = preferredWidth;
            layout.preferredHeight = preferredHeight;
        }

        private sealed class DebugMaterialEntry
        {
            public Sprite icon;
            public string name;
            public int required;
            public int owned;
        }

        private sealed class MaterialRow
        {
            public readonly RectTransform root;
            private readonly Image icon;
            private readonly Text name;
            private readonly Text quantity;

            public MaterialRow(RectTransform root, Image icon, Text name, Text quantity)
            {
                this.root = root;
                this.icon = icon;
                this.name = name;
                this.quantity = quantity;
            }

            public void Set(DebugMaterialEntry entry)
            {
                icon.sprite = entry.icon;
                icon.enabled = entry.icon != null;
                name.text = string.IsNullOrEmpty(entry.name) ? "Unnamed" : entry.name;
                quantity.text = string.Format("{0}/{1}", entry.owned, entry.required);
                quantity.color = entry.owned >= entry.required
                    ? new Color(0.55f, 1f, 0.68f, 1f)
                    : new Color(1f, 0.56f, 0.48f, 1f);
            }
        }
    }
}
