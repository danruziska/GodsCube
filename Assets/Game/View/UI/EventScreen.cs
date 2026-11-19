using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cubo.App;

namespace Cubo.View
{
    // A tela de UM evento (há uma por evento, cada uma com o seu cenário: a caverna do Ídolo, o acampamento, a capela
    // do Juramento). Mostra a história, as opções em fileiras (cada grupo de opções na sua fileira, com a sua moldura),
    // o relatório do batedor quando houver, o botão de confirmar e o de ir embora; depois de escolher, o resultado e
    // Continuar. Este script só copia o EventView; quem decide é o GameFlow.
    public sealed class EventScreen : MonoBehaviour
    {
        [Serializable]
        public struct OptionRow
        {
            [Tooltip("As opções cujo Group é este vão nesta fileira: blood, wrath, ambush, mark, raid, vow.")]
            public string group;
            public RectTransform row;
            public Vector2 cardSize;
            public Sprite frame;
            [Tooltip("O Pixels Per Unit Multiplier da moldura (a espessura da borda).")]
            public float density;
            public Color titleColor, textColor, glowColor;
            [Tooltip("A margem do texto dentro da moldura: X esquerda, Y cima, Z direita, W baixo. Zero = a do prefab.")]
            public Vector4 padding;
        }

        [Tooltip("O id do evento desta tela: idolo, acampamento, juramento.")]
        [SerializeField] private string eventId;
        [SerializeField] private CanvasGroup screen;
        [SerializeField] private GodsScreen court;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text story;
        [Tooltip("Opcional: o aviso da batalha anterior (o voto cumprido ou quebrado).")]
        [SerializeField] private TMP_Text notice;
        [Tooltip("Opcional: o relatório do batedor (só o Acampamento) e o painel dele.")]
        [SerializeField] private TMP_Text info;
        [SerializeField] private GameObject infoPanel;
        [Tooltip("O que some depois de escolher (as fileiras de opções, o botão de ir embora).")]
        [SerializeField] private GameObject choices;
        [Tooltip("O painel do resultado, aceso depois de escolher.")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text result;
        [SerializeField] private OptionRow[] rows = new OptionRow[0];
        [SerializeField] private EventOptionCard cardPrefab;
        [SerializeField] private Button action;
        [SerializeField] private TMP_Text actionLabel;
        [SerializeField] private Button leave;
        [SerializeField] private TMP_Text leaveLabel;
        [SerializeField] private bool highlightNumbers = true;
        [SerializeField] private Color numberColor = new Color(0.95f, 0.78f, 0.35f, 1f);
        [SerializeField] private float fadeInSeconds = 0.25f;
        [Tooltip("Opcional: o X que esconde a faixa da história (volta aberta toda vez que a tela abre).")]
        [SerializeField] private TextPanelToggle textToggle;

        // O Ídolo: o mesmo cenário com um ídolo diferente (Rei, Rainha, Torre). Vazio nos outros eventos.
        [Serializable]
        public struct SceneVariant
        {
            [Tooltip("rei, rainha ou torre")] public string id;
            public Sprite sprite;
        }
        [Tooltip("Opcional: a imagem do cenário (o Image que cobre a tela) e a versão de cada variante.")]
        [SerializeField] private Image scene;
        [SerializeField] private SceneVariant[] variants = new SceneVariant[0];

        public string EventId => eventId;
        public event Action<string> OptionFocused;
        public event Action ActPressed;
        public event Action LeavePressed;
        public bool Visible => screen != null && screen.blocksRaycasts;

        private readonly Dictionary<string, EventOptionCard> _cards = new Dictionary<string, EventOptionCard>();
        private bool _wired;
        private Tween _fade;

        private void Awake()
        {
            var missing = MissingReference();
            if (missing != null) { Debug.LogError("EventScreen (" + eventId + "): falta ligar no Inspector: " + missing, this); enabled = false; return; }
            Wire();
            Hide();
        }

        private void Wire()
        {
            if (_wired) return;
            _wired = true;
            action.onClick.AddListener(() => ActPressed?.Invoke());
            if (leave != null) leave.onClick.AddListener(() => LeavePressed?.Invoke());
        }

        public void Hide() => SetVisible(false);

        private void SetVisible(bool visible)
        {
            bool appearing = visible && !Visible;
            if (appearing && textToggle != null) textToggle.ShowPanel();
            if (_fade != null && _fade.IsActive()) _fade.Kill();
            if (appearing && fadeInSeconds > 0f && Application.isPlaying)
            {
                screen.alpha = 0f;
                _fade = DOTween.To(() => screen.alpha, a => screen.alpha = a, 1f, fadeInSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            }
            else screen.alpha = visible ? 1f : 0f;
            screen.interactable = visible;
            screen.blocksRaycasts = visible;
        }

        public void Show(EventView v)
        {
            Wire();
            if (scene != null && v.Variant != null)
                foreach (var sv in variants) if (sv.id == v.Variant && sv.sprite != null) { scene.sprite = sv.sprite; break; }
            Set(title, v.Title);
            Set(story, v.Story);
            if (notice != null) { notice.gameObject.SetActive(!string.IsNullOrEmpty(v.Notice)); notice.text = v.Notice ?? ""; }
            if (infoPanel != null) infoPanel.SetActive(!v.Done && !string.IsNullOrEmpty(v.Info));
            Set(info, v.Info);
            if (choices != null) choices.SetActive(!v.Done);
            if (resultPanel != null) resultPanel.SetActive(v.Done);
            Set(result, v.Done ? (highlightNumbers ? RichNumbers.Highlight(v.Result ?? "", numberColor) : v.Result) : "");

            bool anyFocused = v.Options.Exists(o => o.Focused);
            var shown = new HashSet<string>();
            foreach (var o in v.Options)
            {
                var row = RowFor(o.Group);
                if (row.row == null) continue;
                var card = Take(o.Key, row);
                card.Show(o, o.HeroId != null && court != null ? court.CardPortrait(o.HeroId) : null, anyFocused, highlightNumbers, numberColor);
                card.transform.SetAsLastSibling();
                shown.Add(o.Key);
            }
            foreach (var kv in _cards) kv.Value.gameObject.SetActive(shown.Contains(kv.Key));

            actionLabel.text = v.ActionText ?? "";
            action.interactable = v.CanAct;
            if (leave != null) leave.gameObject.SetActive(!v.Done);
            Set(leaveLabel, v.LeaveText);
            SetVisible(true);
        }

        private OptionRow RowFor(string group)
        {
            foreach (var r in rows) if (r.group == group) return r;
            return default;
        }

        private EventOptionCard Take(string key, OptionRow row)
        {
            if (!_cards.TryGetValue(key, out var card) || card == null)
            {
                card = Instantiate(cardPrefab, row.row, false);
                card.Clicked += k => OptionFocused?.Invoke(k);
                ((RectTransform)card.transform).sizeDelta = row.cardSize;
                card.Style(row.frame, row.density > 0 ? row.density : 0.29f, row.titleColor, row.textColor, row.glowColor, row.padding);
                _cards[key] = card;
            }
            card.gameObject.SetActive(true);
            return card;
        }

        private static void Set(TMP_Text t, string s) { if (t != null) t.text = s ?? ""; }

        public string MissingReference()
        {
            if (screen == null) return "screen";
            if (cardPrefab == null) return "cardPrefab";
            if (cardPrefab.MissingReference() is string c) return "cardPrefab." + c;
            if (action == null) return "action";
            if (actionLabel == null) return "actionLabel";
            return null;
        }
    }
}
