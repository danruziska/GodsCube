using System;
using UnityEngine;
using UnityEngine.UI;

namespace Cubo.View
{
    // Uma linha do painel de poderes: o ícone do elemento e, à direita, a barra de mana.
    // Tocar mostra o detalhe (quem decide é o PowerBoard). Serve para o jogador e para o rival.
    public sealed class PowerRow : MonoBehaviour
    {
        [Tooltip("O botão que recebe o toque (a linha inteira ou só o ícone).")]
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private FillBar bar;
        [Tooltip("Opcional: um brilho (Image) que aparece quando o poder está pronto para usar (só nos do jogador). " +
                 "Ganha a cor do elemento; com Breathe no mesmo objeto, ele pulsa.")]
        [SerializeField] private Image ready;
        [Tooltip("Força do brilho (alpha da cor do elemento).")]
        [SerializeField, Range(0f, 1f)] private float readyAlpha = 0.45f;

        public event Action Tapped;

        private bool _wired;

        public RectTransform Rect => (RectTransform)transform;

        public void Init()
        {
            if (_wired) return;
            _wired = true;
            button.onClick.AddListener(() => Tapped?.Invoke());
        }

        public void Show(Sprite iconSprite, Color barColor, float fill, bool usable)
        {
            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;
            icon.preserveAspect = true;
            bar.Color = barColor;
            bar.Set(fill);
            if (ready != null)
            {
                // desliga antes de pintar: o Breathe guarda a cor ao ligar e pulsa a partir dela
                ready.gameObject.SetActive(false);
                var c = barColor;
                c.a = readyAlpha;
                ready.color = c;
                ready.gameObject.SetActive(usable);
            }
        }

        // Só a barra, durante a animação do lote.
        public void SetFill(float fill) => bar.Set(fill);

        public string MissingReference()
        {
            if (button == null) return "button";
            if (icon == null) return "icon";
            if (bar == null) return "bar";
            return bar.MissingReference() is string m ? "bar." + m : null;
        }
    }
}
