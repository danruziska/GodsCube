using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cubo.App;

namespace Cubo.View
{
    // Mostra qualquer ScreenModel: um título, o botão de idioma e uma lista que rola com títulos de seção,
    // textos e botões. O layout mora no prefab Screens, montado no editor; as linhas são cópias de três
    // prefabs pequenos (ScreenHeading, ScreenText, ScreenButton). Não sabe o que é uma carta, uma loja
    // ou uma porta: só desenha as linhas que o GameFlow descreveu.
    public sealed class ScreenUi : MonoBehaviour
    {
        [Header("Grupos")]
        [Tooltip("CanvasGroup da raiz: mostra e esconde a tela inteira.")]
        [SerializeField] private CanvasGroup screen;
        [SerializeField] private ScrollRect scroll;
        [Tooltip("O Content da lista: as linhas nascem aqui dentro.")]
        [SerializeField] private RectTransform content;

        [Header("Partes")]
        [SerializeField] private TMP_Text title;
        [SerializeField] private TextButton langButton;
        [SerializeField] private UiTheme theme;

        [Header("Modelos de linha (prefabs da pasta Prefabs/UI)")]
        [SerializeField] private TMP_Text headingRow;
        [SerializeField] private TMP_Text textRow;
        [SerializeField] private TextButton buttonRow;

        public event Action LanguageRequested;                     // o botão de idioma; quem grava o ajuste é o fluxo
        public bool Visible => screen != null && screen.blocksRaycasts;

        // As linhas já criadas ficam guardadas e são reaproveitadas: trocar de tela não cria nem destrói nada
        // depois da primeira vez. Linha que sobra fica desligada (e um grupo de layout ignora objeto desligado).
        private readonly List<TMP_Text> _headings = new List<TMP_Text>();
        private readonly List<TMP_Text> _texts = new List<TMP_Text>();
        private readonly List<TextButton> _buttons = new List<TextButton>();
        private readonly List<Action> _clicks = new List<Action>();    // o que cada botão da lista chama agora
        private string _lastTitle, _lastLang;
        private bool _wired;

        public int RowCount => content == null ? 0 : CountActive();

        private void Awake()
        {
            var missing = MissingReference();
            if (missing != null)
            {
                Debug.LogError("Screens: falta ligar no Inspector: " + missing, this);
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
            langButton.OnClick(() => LanguageRequested?.Invoke());
        }

        public void Hide() => SetVisible(false);

        private void SetVisible(bool visible)
        {
            screen.alpha = visible ? 1f : 0f;
            screen.interactable = visible;
            screen.blocksRaycasts = visible;
        }

        // Desenha a tela. Se for a mesma tela de antes (um toque só mudou uma linha, ou só o idioma mudou),
        // mantém o ponto da rolagem; numa tela nova, volta ao topo.
        public void Show(ScreenModel model, string lang)
        {
            Wire();
            bool same = model.Title == _lastTitle || lang != _lastLang;
            float keep = same ? content.anchoredPosition.y : 0f;
            _lastTitle = model.Title;
            _lastLang = lang;
            title.text = model.Title;
            langButton.Text = lang.ToUpperInvariant();

            int h = 0, t = 0, b = 0;
            for (int i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];
                Component row;
                switch (item.Kind)
                {
                    case ItemKind.Heading:
                    {
                        var label = Take(_headings, headingRow, h++);
                        label.text = item.Text;
                        row = label;
                        break;
                    }
                    case ItemKind.Text:
                    {
                        var label = Take(_texts, textRow, t++);
                        label.text = item.Text;
                        row = label;
                        break;
                    }
                    default:
                    {
                        var button = TakeButton(b);
                        _clicks[b] = item.Click;
                        b++;
                        button.Text = item.Text;
                        button.Interactable = item.Enabled;
                        button.Tint(theme.ButtonTint(item.On));
                        row = button;
                        break;
                    }
                }
                row.transform.SetSiblingIndex(i);                   // a ordem na Hierarchy é a ordem na lista
            }
            HideFrom(_headings, h);
            HideFrom(_texts, t);
            for (int i = b; i < _buttons.Count; i++) { _buttons[i].gameObject.SetActive(false); _clicks[i] = null; }

            SetVisible(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);   // a altura das linhas segue o texto já agora
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, keep);
        }

        private T Take<T>(List<T> pool, T prefab, int index) where T : Component
        {
            if (index == pool.Count) pool.Add(Instantiate(prefab, content, false));
            var row = pool[index];
            row.gameObject.SetActive(true);
            return row;
        }

        // Um botão novo é ligado uma vez só, ao seu lugar na lista; o que ele chama muda a cada Show.
        private TextButton TakeButton(int index)
        {
            if (index == _buttons.Count)
            {
                var button = Instantiate(buttonRow, content, false);
                int k = index;
                button.OnClick(() => _clicks[k]?.Invoke());
                _buttons.Add(button);
                _clicks.Add(null);
            }
            var row = _buttons[index];
            row.gameObject.SetActive(true);
            return row;
        }

        private static void HideFrom<T>(List<T> pool, int from) where T : Component
        {
            for (int i = from; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
        }

        private int CountActive()
        {
            int n = 0;
            for (int i = 0; i < content.childCount; i++) if (content.GetChild(i).gameObject.activeSelf) n++;
            return n;
        }

        // O nome do primeiro campo sem ligar, ou null se está tudo ligado. Usado no Awake e no teste do prefab.
        public string MissingReference()
        {
            if (screen == null) return "screen";
            if (scroll == null) return "scroll";
            if (content == null) return "content";
            if (scroll.content != content) return "scroll.content (o Content do Scroll Rect precisa ser o mesmo objeto do campo Content)";
            if (title == null) return "title";
            if (langButton == null) return "langButton";
            if (langButton.MissingReference() is string l) return "langButton." + l;
            if (theme == null) return "theme";
            if (headingRow == null) return "headingRow";
            if (textRow == null) return "textRow";
            if (buttonRow == null) return "buttonRow";
            if (buttonRow.MissingReference() is string b) return "buttonRow." + b;
            return null;
        }
    }
}