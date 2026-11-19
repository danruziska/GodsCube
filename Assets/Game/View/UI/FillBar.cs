using UnityEngine;
using UnityEngine.UI;

namespace Cubo.View
{
    // Uma barra que enche da esquerda para a direita. A parte cheia é uma Image filha esticada no pai;
    // encher é mover a âncora direita dela. Não precisa de sprite nem de Image do tipo Filled.
    public sealed class FillBar : MonoBehaviour
    {
        [Tooltip("A Image da parte cheia: filha desta barra, com âncoras (0,0)-(1,1) e Left/Right/Top/Bottom = 0.")]
        [SerializeField] private Image fill;

        public Color Color
        {
            get => fill.color;
            set => fill.color = value;
        }

        public void Set(float fraction)
        {
            var r = fill.rectTransform;
            r.anchorMax = new Vector2(Mathf.Clamp01(fraction), r.anchorMax.y);
        }

        public string MissingReference() => fill == null ? "fill" : null;
    }
}