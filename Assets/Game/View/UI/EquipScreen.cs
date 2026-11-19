using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cubo.App;

namespace Cubo.View
{
    // A aba Arsenal da preparação: os três heróis do time (toque escolhe quem equipar), o equipamento em foco no
    // painel do meio com o botão Equipar/Tirar/Pegar, e a grade com todos os equipamentos (toque mostra no meio).
    // Este script só copia o EquipView para os componentes e avisa o que o jogador tocou; quem muda o equipamento
    // é o GameFlow. A arte vem dos recortes da imagem do PixelLab, arrastados uma vez na seção "Arte".
    public sealed class EquipScreen : MonoBehaviour
    {
        [Header("Grupos")]
        [Tooltip("CanvasGroup da raiz: mostra e esconde a tela inteira.")]
        [SerializeField] private CanvasGroup screen;
        [Tooltip("A tela da Corte (GodsScreen): o Arsenal usa os mesmos retratos dos heróis.")]
        [SerializeField] private GodsScreen court;

        [Tooltip("Opcional: o texto da placa do painel do time (TeamPanel ▸ Nameplate ▸ Title). Recebe \"Time\" / \"Team\" / \"Equipo\".")]
        [SerializeField] private TMP_Text teamTitle;
        [Tooltip("Opcional: o texto da placa do painel do Arsenal (Cabinet ▸ Nameplate ▸ Title). Recebe \"Arsenal\" / \"Armory\".")]
        [SerializeField] private TMP_Text title;

        [Header("Time")]
        [Tooltip("Os três heróis do time, da esquerda para a direita.")]
        [SerializeField] private EquipHeroSlot[] heroes;

        [Header("Painel do meio")]
        [SerializeField] private Image detailIcon;
        [SerializeField] private TMP_Text detailName;
        [SerializeField] private TMP_Text detailEffect;
        [SerializeField] private Button action;
        [SerializeField] private TMP_Text actionLabel;
        [Tooltip("Destaca os números do efeito (+3, 15%) em negrito e nesta cor, como na descrição das habilidades.")]
        [SerializeField] private bool highlightNumbers = true;
        [SerializeField] private Color numberColor = new Color(0.95f, 0.78f, 0.35f, 1f);
        [Tooltip("A cor da linha \"Com o Cavaleiro\" embaixo do efeito.")]
        [SerializeField] private Color ownerColor = new Color(0.85f, 0.62f, 0.35f, 1f);
        [Tooltip("Duração do fade do painel do meio ao mostrar outro equipamento. 0 = sem fade.")]
        [SerializeField] private float revealSeconds = 0.18f;

        [Header("Grade")]
        [Tooltip("Opcional: o Scroll Rect da grade. Volta ao topo cada vez que a tela aparece.")]
        [SerializeField] private ScrollRect scroll;
        [Tooltip("O Content da grade (com o Grid Layout Group): os espaços nascem aqui dentro, um por equipamento.")]
        [SerializeField] private RectTransform grid;
        [Tooltip("O prefab EquipItemSlot, arrastado do Project (não da Hierarchy).")]
        [SerializeField] private EquipItemSlot slotPrefab;

        [Header("Ícones dos equipamentos")]
        [Tooltip("Um item por equipamento: o id de equipment.json e o ícone. Quem não tiver ícone mostra o nome.")]
        [SerializeField] private ItemIcon[] icons;
        [Tooltip("O tamanho da tela de desenho dos ícones, em pixels. Cada ícone aparece como se estivesse centrado numa " +
                 "tela deste tamanho, mesmo recortado menor: todos ficam com o mesmo tamanho de pixel. 0 = cada um enche o espaço.")]
        [SerializeField] private int iconCanvas = 48;

        [Header("Arte (os recortes da imagem do PixelLab)")]
        [SerializeField] private Sprite panelArt;
        [SerializeField] private Sprite portraitFrameArt;
        [SerializeField] private Sprite portraitFrameSelectedArt;
        [SerializeField] private Sprite socketArt;
        [SerializeField] private Sprite socketBarArt;
        [SerializeField] private Sprite dividerArt;
        [SerializeField] private Sprite medallionArt;
        [SerializeField] private Sprite textBarArt;
        [SerializeField] private Sprite buttonArt;
        [SerializeField] private Sprite slotArt;
        [SerializeField] private Sprite slotSelectedArt;
        [SerializeField] private Sprite ownerBadgeArt;
        [Tooltip("A cor do selo do dono (o círculo atrás do rosto). Com o sprite Knob da Unity, é a cor da linha fina em volta.")]
        [SerializeField] private Color ownerBadgeTint = Color.white;
        [Tooltip("Sem a arte \"acesa\" (Selected) da moldura ou do espaço, a arte normal aparece tingida desta cor.")]
        [SerializeField] private Color selectedTint = new Color(1f, 0.62f, 0.38f, 1f);

        [Header("Onde a arte vai")]
        [SerializeField] private Image[] panels;
        [SerializeField] private Image[] socketBars;
        [SerializeField] private Image divider;
        [SerializeField] private Image medallion;
        [SerializeField] private Image[] textBars;
        [SerializeField] private Image buttonImage;

        [Header("Entrada da tela")]
        [Tooltip("Duração do fade da tela inteira ao aparecer. 0 = aparece na hora.")]
        [SerializeField] private float fadeInSeconds = 0.2f;

        [Serializable]
        public struct ItemIcon
        {
            public string itemId;      // o id de equipment.json: lamina, anel, cetro...
            public Sprite sprite;
        }

        public event Action<string> HeroSelected;      // tocou num herói do time
        public event Action<string> ItemFocused;       // tocou num equipamento da grade
        public event Action ActionPressed;             // tocou no botão do painel do meio

        public bool Visible => screen != null && screen.blocksRaycasts;
        public EquipHeroSlot Hero(int index) => heroes[index];
        public EquipItemSlot Item(int index) => _items[index];
        public int ItemCount => _items.FindAll(i => i.gameObject.activeSelf).Count;
        public string DetailName => detailName.text;
        public string ActionText => actionLabel.text;

        private readonly List<EquipItemSlot> _items = new List<EquipItemSlot>();
        private bool _wired;
        private string _revealed;
        private Tween _fadeTween;
        private static readonly Color Placeholder = new Color(0.22f, 0.24f, 0.28f, 0.9f);

        private void Awake()
        {
            var missing = MissingReference();
            if (missing != null)
            {
                Debug.LogError("EquipScreen: falta ligar no Inspector: " + missing, this);
                enabled = false;
                return;
            }
            ApplyArt();
            Wire();
            Hide();
        }

#if UNITY_EDITOR
        // Trocou um sprite na seção Arte: a tela já mostra no editor, sem precisar dar Play.
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () => { if (this != null && MissingReference() == null) ApplyArt(); };
        }
#endif

        private void Wire()
        {
            if (_wired) return;
            _wired = true;
            foreach (var h in heroes) h.Clicked += id => HeroSelected?.Invoke(id);
            action.onClick.AddListener(() => ActionPressed?.Invoke());
        }

        // A arte que não muda com o estado: painéis, barras, divisor, medalhão, caixas de texto, botão e selos.
        // Sem sprite, a peça fica num cinza de rascunho, para a tela continuar visível enquanto a arte não chega.
        public void ApplyArt()
        {
            foreach (var p in panels) Paint(p, panelArt, true);
            foreach (var b in socketBars) Paint(b, socketBarArt, false);
            Paint(divider, dividerArt, false);
            Paint(medallion, medallionArt, false);
            foreach (var t in textBars) Paint(t, textBarArt, true);
            Paint(buttonImage, buttonArt, true);
            foreach (var h in heroes) Paint(h.Socket, socketArt, false);
            foreach (var i in _items) PaintSlot(i);
        }

        private void PaintSlot(EquipItemSlot slot)
        {
            Paint(slot.Owner, ownerBadgeArt, false);
            if (slot.Owner != null && ownerBadgeArt != null) slot.Owner.color = ownerBadgeTint;
        }

        // Os espaços já criados são reaproveitados; os que sobram ficam desligados.
        private EquipItemSlot TakeSlot(int index)
        {
            if (index == _items.Count)
            {
                var slot = Instantiate(slotPrefab, grid, false);
                slot.Clicked += id => ItemFocused?.Invoke(id);
                PaintSlot(slot);
                _items.Add(slot);
            }
            var s = _items[index];
            s.gameObject.SetActive(true);
            return s;
        }

        // Peças que esticam (painéis, caixas, botão) usam 9-slice quando o sprite tem bordas marcadas no Sprite Editor.
        private static void Paint(Image img, Sprite sprite, bool stretch)
        {
            if (img == null) return;
            img.sprite = sprite;
            img.color = sprite != null ? Color.white : Placeholder;
            bool sliced = stretch && sprite != null && sprite.border != Vector4.zero;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = sprite != null && !stretch;
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
            if (appearing && scroll != null) scroll.verticalNormalizedPosition = 1f;
            screen.interactable = visible;
            screen.blocksRaycasts = visible;
            if (!visible) _revealed = null;
        }

        public void Show(EquipView view)
        {
            Wire();
            if (teamTitle != null) teamTitle.text = view.TeamTitle ?? "";
            if (title != null) title.text = view.Title ?? "";
            for (int i = 0; i < heroes.Length; i++)
            {
                var h = i < view.Heroes.Count ? view.Heroes[i] : new EquipHeroView();
                bool lit = h.Selected && portraitFrameSelectedArt != null;
                heroes[i].Show(h.Id, h.Name, Portrait(h.Id), lit ? portraitFrameSelectedArt : portraitFrameArt,
                               h.Selected && !lit ? selectedTint : Color.white, IconOf(h.ItemId), iconCanvas);
            }
            for (int i = 0; i < view.Items.Count; i++)
            {
                var it = view.Items[i];
                bool owned = it.Owner != null;      // o rosto de quem leva, seja o herói escolhido ou outro
                bool lit = it.Focused && slotSelectedArt != null;
                TakeSlot(i).Show(it.Id, it.Name, IconOf(it.Id), lit ? slotSelectedArt : slotArt, it.Focused && !lit ? selectedTint : Color.white,
                              owned ? Portrait(it.Owner) : null, owned, iconCanvas);
            }
            for (int i = view.Items.Count; i < _items.Count; i++) _items[i].gameObject.SetActive(false);

            var icon = IconOf(view.DetailId);
            SetIcon(detailIcon, icon, iconCanvas);
            detailName.text = view.DetailName ?? "";
            string effect = KeepLastWordsTogether(view.DetailEffect ?? "");
            if (highlightNumbers) effect = RichNumbers.Highlight(effect, numberColor);
            if (!string.IsNullOrEmpty(view.DetailOwner))
                effect += "\n<color=#" + ColorUtility.ToHtmlStringRGB(ownerColor) + ">" + view.DetailOwner + "</color>";
            detailEffect.text = effect;
            actionLabel.text = view.ActionText ?? "";
            action.interactable = view.CanAct;

            SetVisible(true);
            if (view.DetailId != _revealed) { _revealed = view.DetailId; Reveal(); }
        }

        // Outro equipamento no painel do meio: ícone e textos surgem do transparente.
        private void Reveal()
        {
            if (revealSeconds <= 0f || !Application.isPlaying) return;
            foreach (var g in new Graphic[] { detailIcon, detailName, detailEffect })
            {
                g.canvasRenderer.SetAlpha(0f);
                g.CrossFadeAlpha(1f, revealSeconds, true);
            }
        }

        // Junta as duas últimas palavras com um espaço que não quebra: o "+3." nunca fica sozinho na última linha.
        private static string KeepLastWordsTogether(string text)
        {
            int i = text.TrimEnd().LastIndexOf(' ');
            return i > 0 ? text.Substring(0, i) + "\u00A0" + text.Substring(i + 1) : text;
        }

        // Põe o ícone mantendo a proporção e, com canvas > 0, encolhe-o como se estivesse numa tela canvas×canvas:
        // uma espada de 15×30 e um anel de 23×25 saem com o mesmo tamanho de pixel, cada um centrado no seu espaço.
        public static void SetIcon(Image img, Sprite sprite, int canvas)
        {
            img.sprite = sprite;
            img.preserveAspect = true;
            img.enabled = sprite != null;
            float side = sprite != null ? Mathf.Max(sprite.rect.width, sprite.rect.height) : 0f;
            float k = canvas > 0 && side > 0f ? Mathf.Min(1f, side / canvas) : 1f;
            img.rectTransform.localScale = new Vector3(k, k, 1f);
        }

        private Sprite Portrait(string heroId) => heroId == null || court == null ? null : court.CardPortrait(heroId);

        private Sprite IconOf(string itemId)
        {
            if (itemId == null || icons == null) return null;
            foreach (var i in icons) if (i.itemId == itemId) return i.sprite;
            return null;
        }

        // O nome do primeiro campo sem ligar, ou null se está tudo ligado.
        public string MissingReference()
        {
            if (screen == null) return "screen";
            if (heroes == null || heroes.Length != 3) return "heroes (precisa de exatamente 3)";
            for (int i = 0; i < heroes.Length; i++)
            {
                if (heroes[i] == null) return "heroes[" + i + "]";
                if (heroes[i].MissingReference() is string h) return "heroes[" + i + "]." + h;
            }
            if (detailIcon == null) return "detailIcon";
            if (detailName == null) return "detailName";
            if (detailEffect == null) return "detailEffect";
            if (action == null) return "action";
            if (actionLabel == null) return "actionLabel";
            if (grid == null) return "grid";
            if (scroll != null && scroll.content != grid) return "scroll.content (o Content do Scroll Rect precisa ser o mesmo objeto do campo Grid)";
            if (slotPrefab == null) return "slotPrefab";
            if (slotPrefab.MissingReference() is string s) return "slotPrefab." + s;
            if (panels == null || textBars == null || socketBars == null) return "panels / textBars / socketBars";
            return null;
        }
    }
}
