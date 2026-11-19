using UnityEngine;
using UnityEngine.UI;

namespace Cubo.View
{
    // Faz um elemento "respirar": o tamanho (e, se quiser, o alpha) oscila bem de leve, num ciclo lento.
    // Na insígnia de fundo, alpha e tamanho. Na arte do herói, só o tamanho (Alpha Swing = 0), porque o alpha
    // dela é do fade de troca de herói; com o pivô nos pés, crescer 1% lembra o peito subindo e descendo.
    // Com PixelScale no mesmo objeto (pixel art) crescer 1% não aparece, então ele sobe e desce em pixels inteiros
    // da arte (Bob Pixels) no lugar do tamanho.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class Breathe : MonoBehaviour
    {
        [Tooltip("Quanto o alpha varia, em fração do alpha original (0,25 = de 75% a 125%). 0 = não mexe na cor.")]
        [SerializeField, Range(0f, 0.6f)] private float alphaSwing = 0.25f;
        [Tooltip("Quanto o tamanho varia (0,015 = 1,5%).")]
        [SerializeField, Range(0f, 0.1f)] private float scaleSwing = 0.015f;
        [Tooltip("Só em pixel art (com PixelScale): quantos pixels da arte ele sobe e desce no lugar do tamanho. 0 = parado.")]
        [SerializeField, Range(0, 4)] private int bobPixels = 1;
        [Tooltip("Segundos de um ciclo completo.")]
        [SerializeField] private float period = 7f;

        private Image _img;
        private Color _base;
        private Vector3 _scale;
        private PixelScale _pixel;
        private RectTransform _rect;
        private Vector2 _position;

        private bool TouchesColor => alphaSwing > 0f;
        private bool Bobs => _pixel != null && bobPixels > 0 && scaleSwing > 0f;

        private void OnEnable()
        {
            _img = GetComponent<Image>();
            _base = _img.color;
            _scale = transform.localScale;
            _pixel = GetComponent<PixelScale>();
            _rect = (RectTransform)transform;
            _position = _rect.anchoredPosition;
        }

        private void OnDisable()
        {
            if (TouchesColor && _img != null) _img.color = _base;
            if (_pixel != null) _rect.anchoredPosition = _position;
            else transform.localScale = _scale;
        }

        private void Update()
        {
            float s = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.5f, period));
            if (TouchesColor)
            {
                var c = _base;
                c.a = Mathf.Clamp01(_base.a * (1f + alphaSwing * s));
                _img.color = c;
            }
            if (_pixel == null) transform.localScale = _scale * (1f + scaleSwing * s);
            else if (Bobs) _rect.anchoredPosition = _position + Vector2.up * (Mathf.Round(s * bobPixels) * _pixel.UnitsPerArtPixel);
        }
    }
}
