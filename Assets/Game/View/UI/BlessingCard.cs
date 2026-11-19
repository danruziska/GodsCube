using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cubo.App;

namespace Cubo.View
{
    // Uma carta da bênção (na raiz do prefab BlessingCard): a moldura, o retrato do herói (ou o ícone do item), o nível,
    // o nome, a troca de nível e o efeito. Tocar avisa a BlessingScreen. A escolhida sobe um pouco e fica acesa; as
    // outras ficam um pouco apagadas. A Image da raiz precisa de Raycast Target ligado.
    public sealed class BlessingCard : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image frame;
        [Tooltip("Opcional: a luz em volta da carta escolhida (acesa só nela).")]
        [SerializeField] private GameObject glow;
        [SerializeField] private Image portrait;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text tierTag;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text hero;
        [SerializeField] private TMP_Text change;
        [SerializeField] private TMP_Text effect;
        [Tooltip("Quanto a carta escolhida sobe, em unidades da interface.")]
        [SerializeField] private float lift = 24f;
        [Tooltip("A transparência das outras cartas quando uma está escolhida.")]
        [Range(0f, 1f)] [SerializeField] private float othersAlpha = 0.7f;

        public string Id { get; private set; }
        public event Action<string> Clicked;

        private CanvasGroup _group;
        private bool _focused;

        public void Show(BlessingCardView c, Sprite portraitSprite, Sprite iconSprite, bool anyFocused, int iconCanvas,
                         bool highlightNumbers, Color numberColor)
        {
            Id = c.Id;
            bool face = portrait != null && portraitSprite != null;
            if (portrait != null) { portrait.sprite = portraitSprite; portrait.enabled = face; portrait.preserveAspect = true; }
            if (icon != null) EquipScreen.SetIcon(icon, face ? null : iconSprite, iconCanvas);
            if (tierTag != null) tierTag.text = c.Tag ?? "";
            if (title != null) title.text = c.Name ?? "";
            if (hero != null) hero.text = c.Hero ?? "";
            if (change != null) change.text = c.Change ?? "";
            if (effect != null) effect.text = highlightNumbers ? RichNumbers.Highlight(c.Effect ?? "", numberColor) : c.Effect ?? "";
            if (glow != null) glow.SetActive(c.Focused);
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();   // não use ?? com componentes: o "nulo falso" da Unity passa
            _group.alpha = anyFocused && !c.Focused ? othersAlpha : 1f;
            if (c.Focused != _focused) Lift(c.Focused);
            _focused = c.Focused;
        }

        // A escolhida sobe e cresce um pouco; a que deixou de ser volta. Quem sobe é o filho Body (a raiz fica presa na
        // fileira pelo Horizontal Layout Group).
        private void Lift(bool up)
        {
            if (!Application.isPlaying) return;
            transform.DOKill(true);
            transform.DOScale(up ? 1.05f : 1f, 0.15f).SetUpdate(true).SetLink(gameObject);
            var body = transform.Find("Body") as RectTransform;
            if (body != null)
                DOTween.To(() => body.anchoredPosition.y, y => body.anchoredPosition = new Vector2(body.anchoredPosition.x, y), up ? lift : 0f, 0.15f)
                    .SetUpdate(true).SetLink(gameObject);
        }

        public void OnPointerClick(PointerEventData e) => Clicked?.Invoke(Id);

        public string MissingReference() => frame == null ? "frame" : title == null ? "title" : effect == null ? "effect" : null;
    }
}
