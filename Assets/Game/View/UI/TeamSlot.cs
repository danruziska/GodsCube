using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cubo.View
{
    // Um espaço do time. Recebe um deus arrastado (da grade ou de outro espaço), mostra o deus que está nele
    // e, ocupado, também pode ser arrastado: para outro espaço troca de lugar, para fora do time tira o deus.
    public sealed class TeamSlot : MonoBehaviour, IGodSource, IDropHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image art;
        [Tooltip("A moldura do espaço: o Image do filho Frame, desenhado por cima do retrato.")]
        [SerializeField] private Image frame;
        [Tooltip("A cor que tinge a moldura do herói em destaque. Sem destaque, a moldura aparece com as cores do PNG.")]
        [SerializeField] private Color selectedTint = new Color(1f, 0.62f, 0.38f);
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color emptyColor = new Color(0.16f, 0.14f, 0.22f);
        [Tooltip("Opcional: a arte do espaço vazio (emblema com \"+\"). Sem ela, o espaço vazio é um quadrado na Empty Color.")]
        [SerializeField] private Sprite emptyArt;
        [Tooltip("Opcional: a silhueta mostrada no lugar do retrato de quem ainda não tem arte. O nome do herói aparece por cima.")]
        [SerializeField] private Sprite missingPortrait;
        [Tooltip("Opcional: o UiBounce da raiz do espaço (aperto, entrada, destaque e o pulo ao receber um herói).")]
        [SerializeField] private UiBounce bounce;
        [Tooltip("Tamanho do espaço com o herói em destaque (1,05 = 5% maior).")]
        [SerializeField] private float selectedScale = 1.05f;
        [Tooltip("Opcional: o brilho que acende em todos os espaços enquanto um herói é arrastado.")]
        [SerializeField] private GameObject dropHint;

        public event Action<int, string> Dropped;           // espaço, deus que chegou
        public event Action<string> Clicked;                // tocou num espaço ocupado: mostra o deus
        public event Action<IGodSource, PointerEventData> DragStarted, Dragging, DragEnded;

        public int Index { get; set; }                      // 0 a 2; quem monta a tela numera
        public string GodId { get; private set; }
        public string Label => label.text;
        public Color ArtColor => art.color;
        public Sprite ArtSprite => art.sprite;

        private bool _own;

        // portrait: o retrato do herói; null mostra a silhueta (ou, sem ela, o quadrado colorido). Com retrato, o nome
        // do herói não aparece (o espaço vazio continua mostrando a frase).
        public void Show(string godId, string godName, Color artColor, Sprite portrait, bool selected, string emptyText)
        {
            GodId = godId;
            bool empty = godId == null;
            bool hasPortrait = !empty && portrait != null;
            label.text = empty ? emptyText : godName;
            label.enabled = empty || !hasPortrait;
            var sprite = empty ? emptyArt : hasPortrait ? portrait : missingPortrait;
            art.sprite = sprite;
            art.preserveAspect = sprite != null;
            art.color = sprite != null ? Color.white : empty ? emptyColor : artColor;
            frame.color = selected && !empty ? selectedTint : Color.white;
            if (bounce != null) bounce.SetRest(selected && !empty ? selectedScale : 1f);
        }

        public void SetDropHint(bool on)
        {
            if (dropHint != null) dropHint.SetActive(on);
        }

        // O que o toque e o soltar fazem, separado dos eventos do Unity para o teste chamar direto.
        public void Drop(string godId)
        {
            if (godId == null) return;
            Dropped?.Invoke(Index, godId);                       // o time muda e a tela redesenha o espaço
            if (bounce != null) bounce.Punch();                  // depois: o espaço "recebe" o herói com um pulo
        }
        public void Click() { if (GodId != null) Clicked?.Invoke(GodId); }

        public void OnDrop(PointerEventData e)
        {
            var source = e.pointerDrag != null ? e.pointerDrag.GetComponent<IGodSource>() : null;
            if (source != null) Drop(source.GodId);
        }

        public void OnPointerClick(PointerEventData e) => Click();

        public void OnBeginDrag(PointerEventData e)
        {
            _own = GodId != null;
            if (_own) DragStarted?.Invoke(this, e);
        }

        public void OnDrag(PointerEventData e) { if (_own) Dragging?.Invoke(this, e); }

        public void OnEndDrag(PointerEventData e)
        {
            if (_own) DragEnded?.Invoke(this, e);
            _own = false;
        }

        public string MissingReference() =>
            art == null ? "art" : frame == null ? "frame" : label == null ? "label" : null;
    }
}