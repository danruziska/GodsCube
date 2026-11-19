using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cubo.App;

namespace Cubo.View
{
    // A loja entre os andares: o vendedor atrás do balcão com a fala dele, as prateleiras (melhorias, runas e serviços),
    // o detalhe da oferta em foco com o botão Comprar, a bolsa de runas e o botão para seguir. Este script só copia o
    // ShopView para os componentes e avisa o que o jogador tocou; quem compra é o GameFlow. A arte fica nas próprias
    // Images do prefab (Source Image); aqui só entram os ícones, que mudam com a oferta.
    public sealed class ShopScreen : MonoBehaviour
    {
        [Header("Grupos")]
        [Tooltip("CanvasGroup da raiz: mostra e esconde a tela inteira.")]
        [SerializeField] private CanvasGroup screen;
        [Tooltip("A tela da Corte (GodsScreen): as melhorias de habilidade mostram o rosto do herói, o mesmo da Corte.")]
        [SerializeField] private GodsScreen court;

        [Header("Topo")]
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text gold;
        [Tooltip("Opcional (antigo): a vida do time somada (72/125). A loja nova usa Hero Chips.")]
        [SerializeField] private TMP_Text health;
        [Tooltip("A vida de cada herói do time (3), da esquerda para a direita.")]
        [SerializeField] private ShopHeroChip[] heroChips = new ShopHeroChip[0];
        [Tooltip("Opcional: o andar (Andar 3/6).")]
        [SerializeField] private TMP_Text floor;

        [Header("Vendedor")]
        [Tooltip("O texto do balão de fala.")]
        [SerializeField] private TMP_Text line;
        [Tooltip("Opcional: o balão inteiro; dá um pulinho quando a fala muda.")]
        [SerializeField] private RectTransform bubble;

        [Header("Prateleiras")]
        [SerializeField] private TMP_Text upgradesTitle;
        [SerializeField] private TMP_Text runesTitle;
        [SerializeField] private TMP_Text servicesTitle;
        [Tooltip("As linhas das prateleiras (cada uma com Horizontal Layout Group): as ofertas nascem aqui dentro.")]
        [SerializeField] private RectTransform upgradeRow;
        [SerializeField] private RectTransform runeRow;
        [SerializeField] private RectTransform serviceRow;
        [Tooltip("O prefab ShopOffer, arrastado do Project (não da Hierarchy).")]
        [SerializeField] private ShopOfferSlot offerPrefab;

        [Header("Detalhe")]
        [SerializeField] private Image detailIcon;
        [Tooltip("Opcional: o rosto do herói, quando a oferta em foco é uma melhoria de habilidade.")]
        [SerializeField] private Image detailPortrait;
        [SerializeField] private TMP_Text detailName;
        [SerializeField] private TMP_Text detailEffect;
        [SerializeField] private Button action;
        [SerializeField] private TMP_Text actionLabel;

        [Header("Bolsa de runas e saída")]
        [SerializeField] private TMP_Text bagTitle;
        [Tooltip("Um ícone por espaço da bolsa (3), da esquerda para a direita.")]
        [SerializeField] private Image[] bagIcons = new Image[3];
        [Tooltip("Opcional: o balão de detalhe (o mesmo PowerTip da batalha). Tocar numa runa da bolsa mostra o que ela faz.")]
        [SerializeField] private PowerTip tip;
        [SerializeField] private Button leave;
        [SerializeField] private TMP_Text leaveLabel;

        [Header("Ícones")]
        [Tooltip("Um item por ícone: o id e a imagem. Ids: as runas (folego, estrela, fluxo, escudo, vida), os itens que " +
                 "melhoram (presa, brasa), potion e reroll.")]
        [SerializeField] private IconEntry[] icons = new IconEntry[0];
        [Tooltip("O tamanho da tela de desenho dos ícones, em pixels (como no Arsenal).")]
        [SerializeField] private int iconCanvas = 48;

        [Header("Cores")]
        [SerializeField] private Color focusTint = new Color(1f, 0.62f, 0.38f, 1f);
        [Tooltip("O preço na etiqueta (tinta escura sobre o pergaminho).")]
        [SerializeField] private Color priceColor = new Color(0.227f, 0.165f, 0.102f, 1f);
        [Tooltip("O preço de uma oferta que o ouro não paga.")]
        [SerializeField] private Color poorColor = new Color(0.69f, 0.19f, 0.165f, 1f);
        [SerializeField] private bool highlightNumbers = true;
        [SerializeField] private Color numberColor = new Color(0.95f, 0.78f, 0.35f, 1f);

        [Header("Animação")]
        [SerializeField] private float fadeInSeconds = 0.2f;
        [Tooltip("Quanto tempo o número do ouro leva para contar até o valor novo.")]
        [SerializeField] private float goldSeconds = 0.35f;

        [Serializable]
        public struct IconEntry
        {
            public string id;
            public Sprite sprite;
        }

        public event Action<string> OfferFocused;
        public event Action BuyPressed;
        public event Action LeavePressed;

        public bool Visible => screen != null && screen.blocksRaycasts;

        private readonly List<ShopOfferSlot> _slots = new List<ShopOfferSlot>();
        private List<string> _bagInfo = new List<string>();
        private int _tipSlot = -1;
        private bool _wired;
        private string _lastLine, _revealed;
        private int _goldShown = -1;
        private Tween _fadeTween, _goldTween;

        private void Awake()
        {
            var missing = MissingReference();
            if (missing != null)
            {
                Debug.LogError("ShopScreen: falta ligar no Inspector: " + missing, this);
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
            action.onClick.AddListener(() => BuyPressed?.Invoke());
            leave.onClick.AddListener(() => LeavePressed?.Invoke());
            if (tip != null)
            {
                tip.Init();
                tip.Closed += () => _tipSlot = -1;
            }
            // cada espaço da bolsa (o pai do ícone) recebe o toque; a Image dele precisa de Raycast Target
            for (int i = 0; bagIcons != null && i < bagIcons.Length; i++)
            {
                var socket = bagIcons[i] != null ? bagIcons[i].transform.parent : null;
                if (socket == null) continue;
                var tap = socket.GetComponent<StageTap>();
                if (tap == null) tap = socket.gameObject.AddComponent<StageTap>();   // não use ?? com componentes: o "nulo falso" da Unity passa
                tap.Index = i;
                tap.Tapped += OnBagTapped;
            }
        }

        public void Hide() => SetVisible(false);

        private void SetVisible(bool visible)
        {
            bool appearing = visible && !Visible;
            if (_fadeTween != null && _fadeTween.IsActive()) _fadeTween.Kill();
            if (appearing && fadeInSeconds > 0f && Application.isPlaying)
            {
                screen.alpha = 0f;
                _fadeTween = DOTween.To(() => screen.alpha, a => screen.alpha = a, 1f, fadeInSeconds)
                    .SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            }
            else screen.alpha = visible ? 1f : 0f;
            screen.interactable = visible;
            screen.blocksRaycasts = visible;
            if (!visible) { _lastLine = null; _revealed = null; _goldShown = -1; HideTip(); }
        }

        public void Show(ShopView v)
        {
            Wire();
            bool appearing = !Visible;
            Set(title, v.Title); Set(health, v.Health); Set(floor, v.Floor);
            for (int i = 0; heroChips != null && i < heroChips.Length; i++)
            {
                if (heroChips[i] == null) continue;
                var h = i < v.Heroes.Count ? v.Heroes[i] : null;
                heroChips[i].Show(h, h != null ? Portrait(h.HeroId) : null);
            }
            ShowGold(v.Gold, appearing);
            Set(upgradesTitle, v.UpgradesTitle); Set(runesTitle, v.RunesTitle); Set(servicesTitle, v.ServicesTitle);
            Set(bagTitle, v.BagTitle); Set(leaveLabel, v.LeaveText);

            int used = 0;
            used = Fill(upgradeRow, v.Upgrades, used);
            used = Fill(runeRow, v.Runes, used);
            used = Fill(serviceRow, v.Services, used);
            for (int i = used; i < _slots.Count; i++) _slots[i].gameObject.SetActive(false);

            var portrait = Portrait(v.DetailHeroId);
            if (detailPortrait != null)
            {
                detailPortrait.sprite = portrait;
                detailPortrait.enabled = portrait != null;
                detailPortrait.preserveAspect = true;
            }
            EquipScreen.SetIcon(detailIcon, portrait != null && detailPortrait != null ? null : IconOf(v.DetailIconId), iconCanvas);
            detailName.text = v.DetailName ?? "";
            string effect = v.DetailEffect ?? "";
            detailEffect.text = highlightNumbers ? RichNumbers.Highlight(effect, numberColor) : effect;
            actionLabel.text = v.ActionText ?? "";
            action.interactable = v.CanBuy;

            for (int i = 0; i < bagIcons.Length; i++)
                if (bagIcons[i] != null) EquipScreen.SetIcon(bagIcons[i], i < v.Bag.Count ? IconOf(v.Bag[i]) : null, iconCanvas);
            _bagInfo = v.BagInfo;
            if (_tipSlot >= 0 && (_tipSlot >= _bagInfo.Count || _bagInfo[_tipSlot] == null)) HideTip();

            line.text = v.Line ?? "";
            SetVisible(true);
            if (v.Line != _lastLine) { if (_lastLine != null) Pop(); _lastLine = v.Line; }
            if (v.DetailId != _revealed) { _revealed = v.DetailId; Reveal(); }
        }

        // Tocou numa runa da bolsa: o balão com o nome e o efeito dela (tocar de novo fecha). Espaço vazio: nada.
        private void OnBagTapped(int i)
        {
            if (tip == null || i < 0 || i >= _bagInfo.Count || _bagInfo[i] == null) return;
            if (tip.IsOpen && _tipSlot == i) { HideTip(); return; }
            _tipSlot = i;
            string text = highlightNumbers ? RichNumbers.Highlight(_bagInfo[i], numberColor) : _bagInfo[i];
            tip.Show(text, (RectTransform)bagIcons[i].transform.parent, false);
        }

        private void HideTip()
        {
            _tipSlot = -1;
            if (tip != null && tip.IsOpen) tip.Hide();
        }

        // Uma prateleira: uma cópia do prefab por oferta, na ordem. As cópias são reaproveitadas entre as prateleiras.
        private int Fill(RectTransform row, List<ShopOfferView> offers, int start)
        {
            for (int k = 0; k < offers.Count; k++)
            {
                var o = offers[k];
                var slot = Take(start + k, row);
                slot.transform.SetAsLastSibling();            // na ordem, e sempre por cima da tábua da prateleira (o 1º filho)
                slot.Show(o, IconOf(o.IconId), Portrait(o.HeroId), o.Focused ? focusTint : Color.white,
                          o.Affordable || o.Sold ? priceColor : poorColor, iconCanvas);
            }
            return start + offers.Count;
        }

        private ShopOfferSlot Take(int index, RectTransform row)
        {
            if (index == _slots.Count)
            {
                var s = Instantiate(offerPrefab, row, false);
                s.Clicked += id => OfferFocused?.Invoke(id);
                _slots.Add(s);
            }
            var slot = _slots[index];
            if (slot.transform.parent != row) slot.transform.SetParent(row, false);
            slot.gameObject.SetActive(true);
            return slot;
        }

        // O ouro conta até o valor novo (gastou: desce; num andar novo a tela abre direto no valor).
        private void ShowGold(string text, bool instant)
        {
            if (gold == null) return;
            if (!int.TryParse(text, out int to)) { gold.text = text ?? ""; return; }
            if (_goldTween != null && _goldTween.IsActive()) _goldTween.Kill();
            if (instant || _goldShown < 0 || goldSeconds <= 0f || !Application.isPlaying)
            {
                _goldShown = to; gold.text = to.ToString();
                return;
            }
            int from = _goldShown;
            _goldShown = to;
            _goldTween = DOTween.To(() => (float)from, x => gold.text = Mathf.RoundToInt(x).ToString(), to, goldSeconds)
                .SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
        }

        // A fala nova do vendedor: o balão dá um pulinho.
        private void Pop()
        {
            if (bubble == null || !Application.isPlaying) return;
            bubble.DOKill(true);
            bubble.DOPunchScale(Vector3.one * 0.08f, 0.25f, 6, 0.6f).SetUpdate(true).SetLink(bubble.gameObject);
        }

        // Outra oferta no detalhe: ícone e textos surgem do transparente (como no Arsenal).
        private void Reveal()
        {
            if (!Application.isPlaying) return;
            foreach (var g in new Graphic[] { detailIcon, detailPortrait, detailName, detailEffect })
            {
                if (g == null) continue;
                g.canvasRenderer.SetAlpha(0f);
                g.CrossFadeAlpha(1f, 0.18f, true);
            }
        }

        private static void Set(TMP_Text t, string s) { if (t != null) t.text = s ?? ""; }

        private Sprite Portrait(string heroId) => heroId == null || court == null ? null : court.CardPortrait(heroId);

        private Sprite IconOf(string id)
        {
            if (id == null || icons == null) return null;
            foreach (var i in icons) if (i.id == id) return i.sprite;
            return null;
        }

        public string MissingReference()
        {
            if (screen == null) return "screen";
            if (gold == null) return "gold";
            if (line == null) return "line";
            if (upgradeRow == null) return "upgradeRow";
            if (runeRow == null) return "runeRow";
            if (serviceRow == null) return "serviceRow";
            if (offerPrefab == null) return "offerPrefab";
            if (offerPrefab.MissingReference() is string o) return "offerPrefab." + o;
            if (detailIcon == null) return "detailIcon";
            if (detailName == null) return "detailName";
            if (detailEffect == null) return "detailEffect";
            if (action == null) return "action";
            if (actionLabel == null) return "actionLabel";
            if (leave == null) return "leave";
            return null;
        }
    }
}
