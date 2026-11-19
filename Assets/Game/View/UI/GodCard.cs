using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cubo.App;

namespace Cubo.View
{
    // Um herói na grade (o script mantém o nome GodCard por causa do prefab). Toque: mostra os detalhes.
    // Arrasto para o lado: leva o herói ao time. As gemas mostram os dois domínios do herói, na cor do cubo.
    // O cartão mora dentro de um Scroll Rect; arrasto mais vertical que horizontal é rolagem, e o cartão o repassa.
    public sealed class GodCard : MonoBehaviour, IGodSource, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image art;
        [Tooltip("A moldura do cartão: o Image do filho Frame, desenhado por cima do retrato.")]
        [SerializeField] private Image frame;
        [Tooltip("A cor que tinge a moldura do herói em destaque. Sem destaque, a moldura aparece com as cores do PNG.")]
        [SerializeField] private Color selectedTint = new Color(1f, 0.62f, 0.38f);
        [Tooltip("O selo de quem já está no time.")]
        [SerializeField] private GameObject teamMark;
        [SerializeField] private TMP_Text label;
        [Tooltip("Opcional: as gemas dos domínios do herói, na ordem dos poderes. Cada Image (de sprite branco) é tingido com a cor do domínio.")]
        [SerializeField] private Image[] gems;
        [Tooltip("Opcional: o brilho que acende atrás do cartão do herói em destaque.")]
        [SerializeField] private GameObject selectedGlow;
        [Tooltip("Opcional: a silhueta mostrada no lugar do retrato de quem ainda não tem arte.")]
        [SerializeField] private Sprite missingPortrait;
        [Tooltip("Opcional: o UiBounce da raiz do cartão (aperto, entrada e destaque).")]
        [SerializeField] private UiBounce bounce;
        [Tooltip("Tamanho do cartão em destaque (1,06 = 6% maior).")]
        [SerializeField] private float selectedScale = 1.06f;

        public event Action<string> Clicked;
        public event Action<IGodSource, PointerEventData> DragStarted, Dragging, DragEnded;

        public string GodId { get; private set; }
        public string Label => label.text;
        public Color ArtColor => art.color;
        public Sprite ArtSprite => art.sprite;
        public bool Selected { get; private set; }
        public bool InTeam => teamMark.activeSelf;
        public ScrollRect Scroll { get; set; }          // quem rola a grade: recebe o arrasto que não é do herói
        public Func<int, Color> Palette { get; set; }   // a cor de cada domínio (0 a 5), a mesma das peças do cubo

        private bool _own;                               // este arrasto é do herói (para o lado), não da rolagem
        private Color _placeholderColor = Color.white;   // a cor do prefab: vale para quem ainda não tem retrato

        private void Awake()
        {
            if (art != null) _placeholderColor = art.color;
        }

        public void Show(HeroCardView view) => Show(view, null);

        // portrait: o retrato do cartão; null mostra a silhueta (ou, sem ela, o quadrado colorido de antes).
        public void Show(HeroCardView view, Sprite portrait)
        {
            GodId = view.Id;
            label.text = view.Name;
            Selected = view.Selected;
            frame.color = Selected ? selectedTint : Color.white;
            teamMark.SetActive(view.InTeam);
            if (selectedGlow != null) selectedGlow.SetActive(Selected);
            if (bounce != null) bounce.SetRest(Selected ? selectedScale : 1f);
            ShowGems(view.Colors);

            // Sem retrato: a silhueta, se houver; senão o quadrado colorido de antes.
            var sprite = portrait != null ? portrait : missingPortrait;
            art.sprite = sprite;
            art.preserveAspect = sprite != null;
            art.color = sprite != null ? Color.white : _placeholderColor;     // branco: o sprite aparece com as cores dele
        }

        // Uma gema por domínio; as que sobram (ou todas, sem domínios) ficam desligadas.
        private void ShowGems(int[] colors)
        {
            if (gems == null) return;
            for (int i = 0; i < gems.Length; i++)
            {
                if (gems[i] == null) continue;
                bool has = colors != null && i < colors.Length && Palette != null;
                gems[i].gameObject.SetActive(has);
                if (has) gems[i].color = Palette(colors[i]);
            }
        }

        public void Click() => Clicked?.Invoke(GodId);

        public void OnPointerClick(PointerEventData e) => Click();

        // A direção do primeiro movimento decide de quem é o arrasto.
        public void OnBeginDrag(PointerEventData e)
        {
            _own = Mathf.Abs(e.delta.x) > Mathf.Abs(e.delta.y);
            if (_own) DragStarted?.Invoke(this, e);
            else if (Scroll != null) Scroll.OnBeginDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (_own) Dragging?.Invoke(this, e);
            else if (Scroll != null) Scroll.OnDrag(e);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (_own) DragEnded?.Invoke(this, e);
            else if (Scroll != null) Scroll.OnEndDrag(e);
            _own = false;
        }

        public string MissingReference() =>
            art == null ? "art" : frame == null ? "frame" : teamMark == null ? "teamMark" : label == null ? "label" : null;
    }
}