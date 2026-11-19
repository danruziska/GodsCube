using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cubo.App;

namespace Cubo.View
{
    // Uma opção de evento (tábua do Ídolo, placa do Acampamento, estandarte do Juramento): a moldura, o rosto do herói
    // (só no sangue do Ídolo), o nome e o efeito. A escolhida sobe e acende; uma opção que não dá fica apagada e diz por
    // quê. Tocar avisa a EventScreen. A Image da raiz precisa de Raycast Target ligado.
    public sealed class EventOptionCard : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image frame;
        [SerializeField] private GameObject glow;
        [Tooltip("Opcional: o espaço do rosto (some quando a opção não é de um herói).")]
        [SerializeField] private GameObject portraitSlot;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text text;
        [SerializeField] private float lift = 18f;
        [Range(0f, 1f)] [SerializeField] private float disabledAlpha = 0.45f;
        [Range(0f, 1f)] [SerializeField] private float othersAlpha = 0.8f;

        public string Key { get; private set; }
        public event Action<string> Clicked;

        private CanvasGroup _group;
        private bool _focused;

        // A arte e as cores da fileira (cada evento tem a sua moldura e a sua tinta de texto).
        // padding: a margem do conteúdo dentro da moldura (esquerda, cima, direita, baixo); zero = a do prefab.
        public void Style(Sprite frameArt, float density, Color titleColor, Color textColor, Color glowColor, Vector4 padding = default)
        {
            if (padding != Vector4.zero && title != null && title.transform.parent is RectTransform content)
            {
                content.offsetMin = new Vector2(padding.x, padding.w);
                content.offsetMax = new Vector2(-padding.z, -padding.y);
            }
            if (frame != null && frameArt != null)
            {
                frame.sprite = frameArt; frame.color = Color.white;
                frame.type = Image.Type.Sliced; frame.fillCenter = true; frame.pixelsPerUnitMultiplier = density;
            }
            if (title != null) title.color = titleColor;
            if (text != null) text.color = textColor;
            if (glow != null && glow.GetComponent<Image>() is Image g) g.color = glowColor;
        }

        public void Show(EventOptionView o, Sprite portraitSprite, bool anyFocused, bool highlightNumbers, Color numberColor)
        {
            Key = o.Key;
            bool face = portraitSprite != null;
            if (portraitSlot != null) portraitSlot.SetActive(face);
            if (portrait != null) { portrait.sprite = portraitSprite; portrait.enabled = face; portrait.preserveAspect = true; }
            if (title != null) title.text = o.Title ?? "";
            string body = o.Enabled ? o.Text ?? "" : (o.Text ?? "") + "\n<i>" + o.Reason + "</i>";
            if (text != null) text.text = highlightNumbers ? RichNumbers.Highlight(body, numberColor) : body;
            if (glow != null) glow.SetActive(o.Focused);
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = !o.Enabled ? disabledAlpha : anyFocused && !o.Focused ? othersAlpha : 1f;
            if (o.Focused != _focused) Lift(o.Focused);
            _focused = o.Focused;
        }

        private void Lift(bool up)
        {
            if (!Application.isPlaying) return;
            transform.DOKill(true);
            transform.DOScale(up ? 1.04f : 1f, 0.15f).SetUpdate(true).SetLink(gameObject);
            var body = transform.Find("Body") as RectTransform;
            if (body != null)
                DOTween.To(() => body.anchoredPosition.y, y => body.anchoredPosition = new Vector2(body.anchoredPosition.x, y), up ? lift : 0f, 0.15f)
                    .SetUpdate(true).SetLink(gameObject);
        }

        public void OnPointerClick(PointerEventData e) => Clicked?.Invoke(Key);

        public string MissingReference() => frame == null ? "frame" : title == null ? "title" : text == null ? "text" : null;
    }
}
