using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cubo.View
{
    // The tab bar at the bottom of the prep screen. Every tab gets the same width, with a separator
    // between neighbours. A tab is a background (normal or active), a frame in the middle and the
    // tab's symbol inside the frame, plus an optional label under the frame.
    //
    // The bar only reports presses; GameBootstrap decides what each tab opens. Labels, locks and the
    // selected tab can be set before the buttons exist: they are kept and applied when the bar builds.
    public sealed class TabBar : MonoBehaviour
    {
        [Serializable]
        public sealed class Tab
        {
            public string Label;
            public Sprite Symbol;
        }

        [Tooltip("One entry per tab, left to right.")]
        [SerializeField] private List<Tab> tabs = new List<Tab>();

        [Header("Art")]
        [SerializeField] private Sprite background;
        [SerializeField] private Sprite activeBackground;
        [SerializeField] private Sprite frame;
        [SerializeField] private Sprite activeFrame;
        [SerializeField] private Sprite separator;
        [SerializeField] private float separatorWidth = 8f;
        [SerializeField] private Vector2 frameSize = new Vector2(144f, 144f);
        [SerializeField] private Vector2 symbolSize = new Vector2(96f, 96f);

        [Header("Labels")]
        [SerializeField] private bool showLabels;
        [Tooltip("A TextMeshPro text copied under each frame when Show Labels is on.")]
        [SerializeField] private TMP_Text labelPrefab;
        [SerializeField] private float labelGap = 4f;
        [SerializeField] private Color activeLabelColor = new Color(0.165f, 0.094f, 0.063f, 1f);

        // Fired when the player presses a tab that is not locked and not already selected.
        // The tab is already selected when this runs.
        public event Action<int> Pressed;

        private sealed class Built
        {
            public Button Button;
            public Image Background;
            public Image Frame;
            public TMP_Text Label;
            public Color LabelColor;
        }

        private readonly List<Built> _built = new List<Built>();
        private readonly HashSet<int> _locked = new HashSet<int>();
        private int _selected;
        private CanvasGroup _group;

        public IReadOnlyList<Tab> Tabs => tabs;
        public int SelectedIndex => _selected;
        public bool Visible => Group().blocksRaycasts;

        private void Awake()
        {
            Build();
            Refresh();
        }

        public void SetLabel(int index, string label)
        {
            if (!Valid(index)) return;
            tabs[index].Label = label;
            if (index < _built.Count && _built[index].Label != null) _built[index].Label.text = label;
        }

        // A locked tab ignores presses. It looks the same; the screen behind it explains why.
        public void SetLocked(int index, bool locked)
        {
            if (!Valid(index)) return;
            if (locked) _locked.Add(index); else _locked.Remove(index);
            if (index < _built.Count) _built[index].Button.interactable = !locked;
        }

        public bool IsLocked(int index) => _locked.Contains(index);

        public void SetIndex(int index)
        {
            if (tabs.Count == 0) return;
            _selected = Mathf.Clamp(index, 0, tabs.Count - 1);
            Refresh();
        }

        // Hides through a CanvasGroup so the layout and the selected tab survive while hidden.
        public void SetVisible(bool visible)
        {
            var group = Group();
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        // For the prefab tests: the name of the first missing art field, or null when all are set.
        public string MissingArt()
        {
            if (background == null) return nameof(background);
            if (activeBackground == null) return nameof(activeBackground);
            if (frame == null) return nameof(frame);
            if (activeFrame == null) return nameof(activeFrame);
            if (separator == null) return nameof(separator);
            if (labelPrefab == null) return nameof(labelPrefab);
            for (int i = 0; i < tabs.Count; i++)
                if (tabs[i].Symbol == null) return "tabs[" + i + "].Symbol";
            return null;
        }

        private bool Valid(int index) => index >= 0 && index < tabs.Count;

        private CanvasGroup Group()
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            return _group;
        }

        private void Build()
        {
            var row = GetComponent<HorizontalLayoutGroup>();
            if (row == null) row = gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 0f;
            row.padding = new RectOffset(0, 0, 0, 0);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            for (int i = 0; i < tabs.Count; i++)
            {
                if (i > 0 && separator != null) AddSeparator();
                _built.Add(AddTab(i));
            }
        }

        private Built AddTab(int index)
        {
            var root = NewChild("Tab " + index, transform);
            var cell = root.gameObject.AddComponent<LayoutElement>();
            cell.flexibleWidth = 1f;
            cell.flexibleHeight = 1f;

            // The background is the hit area for the whole tab.
            var bg = root.gameObject.AddComponent<Image>();
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = bg;
            button.interactable = !IsLocked(index);
            button.onClick.AddListener(() => Press(index));

            var frameRt = NewChild("Frame", root);
            frameRt.sizeDelta = frameSize;
            var frameImg = frameRt.gameObject.AddComponent<Image>();
            frameImg.raycastTarget = false;

            var symbolRt = NewChild("Symbol", frameRt);
            symbolRt.sizeDelta = symbolSize;
            var symbolImg = symbolRt.gameObject.AddComponent<Image>();
            symbolImg.sprite = tabs[index].Symbol;
            symbolImg.preserveAspect = true;
            symbolImg.raycastTarget = false;
            symbolImg.enabled = symbolImg.sprite != null;

            var built = new Built { Button = button, Background = bg, Frame = frameImg };
            if (showLabels && labelPrefab != null)
            {
                var label = Instantiate(labelPrefab, root);
                var rt = label.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -frameSize.y * 0.5f - labelGap);
                label.text = tabs[index].Label;
                label.raycastTarget = false;
                built.Label = label;
                built.LabelColor = label.color;
            }
            return built;
        }

        private void AddSeparator()
        {
            var rt = NewChild("Separator", transform);
            var size = rt.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = Mathf.Max(1f, separatorWidth);
            size.flexibleWidth = 0f;
            size.flexibleHeight = 1f;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = separator;
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
        }

        private static RectTransform NewChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            return rt;
        }

        private void Press(int index)
        {
            if (IsLocked(index) || index == _selected) return;
            SetIndex(index);
            Pressed?.Invoke(index);
        }

        private void Refresh()
        {
            for (int i = 0; i < _built.Count; i++)
            {
                bool on = i == _selected;
                var b = _built[i];
                b.Background.sprite = on && activeBackground != null ? activeBackground : background;
                b.Background.color = b.Background.sprite != null ? Color.white : Color.clear;
                b.Frame.sprite = on && activeFrame != null ? activeFrame : frame;
                b.Frame.enabled = b.Frame.sprite != null;
                if (b.Label != null) b.Label.color = on ? activeLabelColor : b.LabelColor;
            }
        }
    }
}
