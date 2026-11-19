using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cubo.App;

namespace Cubo.View
{
    // A vida de um herói do time no topo da loja: o rosto, uma barrinha e "28/42" (ou "Caído", com o rosto apagado).
    public sealed class ShopHeroChip : MonoBehaviour
    {
        [SerializeField] private Image face;
        [Tooltip("Opcional: a barra (um FillBar).")]
        [SerializeField] private FillBar bar;
        [SerializeField] private TMP_Text label;
        [Tooltip("A cor da parte cheia da barra.")]
        [SerializeField] private Color barColor = new Color(0.36f, 0.75f, 0.29f, 1f);
        [Tooltip("A transparência do rosto de um herói caído.")]
        [Range(0f, 1f)] [SerializeField] private float fallenAlpha = 0.35f;

        public void Show(ShopHeroView h, Sprite portrait)
        {
            gameObject.SetActive(h != null);
            if (h == null) return;
            if (face != null)
            {
                face.sprite = portrait;
                face.enabled = portrait != null;
                face.preserveAspect = true;
                face.color = new Color(1f, 1f, 1f, h.Alive ? 1f : fallenAlpha);
            }
            if (bar != null)
            {
                bar.Color = barColor;
                bar.Set(h.Max > 0 ? (float)h.Hp / h.Max : 0f);
            }
            if (label != null) label.text = h.Text ?? "";
        }

        public string MissingReference() => label == null ? "label" : null;
    }
}
