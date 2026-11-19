using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cubo.App;

namespace Cubo.View
{
    // A bênção depois de um andar, desenhada: a capela ao fundo, a placa, a frase, uma carta por opção (tocar escolhe)
    // e o botão Aceitar. Este script só copia o BlessingView para os componentes e avisa o que foi tocado; quem dá a
    // bênção é o GameFlow.
    public sealed class BlessingScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup screen;
        [Tooltip("A tela da Corte (GodsScreen): as cartas de habilidade mostram o rosto do herói, o mesmo da Corte.")]
        [SerializeField] private GodsScreen court;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text text;
        [Tooltip("A fileira das cartas (com Horizontal Layout Group): as cartas nascem aqui dentro.")]
        [SerializeField] private RectTransform row;
        [Tooltip("O prefab BlessingCard, arrastado do Project.")]
        [SerializeField] private BlessingCard cardPrefab;
        [SerializeField] private Button action;
        [SerializeField] private TMP_Text actionLabel;

        [Header("Ícones dos itens que melhoram")]
        [SerializeField] private ShopScreen.IconEntry[] icons = new ShopScreen.IconEntry[0];
        [SerializeField] private int iconCanvas = 48;
        [SerializeField] private bool highlightNumbers = true;
        [SerializeField] private Color numberColor = new Color(0.95f, 0.78f, 0.35f, 1f);
        [SerializeField] private float fadeInSeconds = 0.25f;
        [Tooltip("Opcional: o X que esconde a faixa do texto (volta aberta toda vez que a tela abre).")]
        [SerializeField] private TextPanelToggle textToggle;

        public event Action<string> CardFocused;
        public event Action AcceptPressed;

        public bool Visible => screen != null && screen.blocksRaycasts;

        private readonly List<BlessingCard> _cards = new List<BlessingCard>();
        private bool _wired;
        private Tween _fade;

        private void Awake()
        {
            var missing = MissingReference();
            if (missing != null)
            {
                Debug.LogError("BlessingScreen: falta ligar no Inspector: " + missing, this);
                enabled = false;
                return;
            }
            Wire();
            Hide();
        }

        private void Wire()
        {
            if (_wired) return;
            _wired = true;
            action.onClick.AddListener(() => AcceptPressed?.Invoke());
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

        public void Show(BlessingView v)
        {
            Wire();
            if (title != null) title.text = v.Title ?? "";
            if (text != null) text.text = v.Text ?? "";
            bool anyFocused = v.Cards.Exists(c => c.Focused);
            for (int i = 0; i < v.Cards.Count; i++)
            {
                var c = v.Cards[i];
                Take(i).Show(c, c.HeroId != null && court != null ? court.CardPortrait(c.HeroId) : null, IconOf(c.IconId),
                             anyFocused, iconCanvas, highlightNumbers, numberColor);
            }
            for (int i = v.Cards.Count; i < _cards.Count; i++) _cards[i].gameObject.SetActive(false);
            actionLabel.text = v.ActionText ?? "";
            action.interactable = v.CanAccept;
            SetVisible(true);
        }

        private BlessingCard Take(int index)
        {
            if (index == _cards.Count)
            {
                var c = Instantiate(cardPrefab, row, false);
                c.Clicked += id => CardFocused?.Invoke(id);
                _cards.Add(c);
            }
            var card = _cards[index];
            card.gameObject.SetActive(true);
            return card;
        }

        private Sprite IconOf(string id)
        {
            if (id == null || icons == null) return null;
            foreach (var i in icons) if (i.id == id) return i.sprite;
            return null;
        }

        public string MissingReference()
        {
            if (screen == null) return "screen";
            if (row == null) return "row";
            if (cardPrefab == null) return "cardPrefab";
            if (cardPrefab.MissingReference() is string c) return "cardPrefab." + c;
            if (action == null) return "action";
            if (actionLabel == null) return "actionLabel";
            return null;
        }
    }
}
