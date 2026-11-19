using UnityEngine;
using UnityEngine.UI;

namespace Cubo.View
{
    // Mostra uma Image de pixel art sem pixels tortos: cada pixel da arte vira um bloco inteiro de pixels de tela
    // (6x6, 7x7...), qualquer que seja a tela, e o canto do elemento cai em pixel inteiro.
    // A sprite precisa vir da pasta ArtPixel (filtro Point, sem compressão, 100 pixels por unidade).
    //
    // É o dono da escala do próprio objeto. Quem quer mexer no tamanho dele (UiBounce, Breathe) mexe em Multiplier,
    // e a escala anda em degraus de um pixel de tela. Pai crescendo (cartão em destaque, fantasma arrastado)
    // também é levado em conta: o pixel continua inteiro.
    // Image Sliced/Tiled (molduras de 9 fatias): a escala fica com o layout; o que vira pixel inteiro é a borda.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class PixelScale : MonoBehaviour
    {
        [Tooltip("Tamanho de um pixel da arte, em unidades da resolução de referência (1080x1920). " +
                 "Retrato de 64 px com 6 = 384 de largura, um terço da tela. Use o mesmo número em toda a interface.")]
        [SerializeField, Min(1)] private int artPixel = 6;
        [Tooltip("Ajusta o RectTransform ao tamanho da sprite (e de novo quando a sprite muda). " +
                 "Desligue em imagens esticadas pelo layout.")]
        [SerializeField] private bool nativeSize = true;

        private Image _image;
        private Canvas _canvas;
        private Sprite _sprite;
        private float _multiplier = 1f;
        private readonly Vector3[] _corners = new Vector3[4];

        public int ArtPixel
        {
            get => artPixel;
            set => artPixel = Mathf.Max(1, value);
        }

        // O efeito pedido por fora (1 = repouso). Não é a escala final: essa sai em degraus de pixel inteiro.
        public float Multiplier
        {
            get => _multiplier;
            set => _multiplier = Mathf.Max(0f, value);
        }

        // Quantas unidades do pai um pixel da arte ocupa agora (para mover algo um pixel da arte por vez).
        public float UnitsPerArtPixel => Sliced ? 1f / Mathf.Max(0.01f, Image.pixelsPerUnitMultiplier) : transform.localScale.y;

        private Image Image => _image != null ? _image : _image = GetComponent<Image>();
        private bool Sliced => Image.type == Image.Type.Sliced || Image.type == Image.Type.Tiled;

        private void OnEnable()
        {
            var canvas = GetComponentInParent<Canvas>();
            _canvas = canvas != null ? canvas.rootCanvas : null;
            _sprite = null;
            LateUpdate();
        }

        private void OnTransformParentChanged() => OnEnable();

        // LateUpdate: roda depois do CanvasScaler e das animações, que mexem em posição e tamanho no Update.
        private void LateUpdate()
        {
            if (_canvas == null) return;
            if (nativeSize && !Sliced && Image.sprite != _sprite)
            {
                _sprite = Image.sprite;
                if (_sprite != null) Image.SetNativeSize();
            }

            float canvasScale = _canvas.scaleFactor;
            float parentScale = ParentScale();
            if (Sliced)
            {
                SetScale(_multiplier);
                float s = UiMath.PixelArtScale(artPixel, canvasScale, parentScale * _multiplier);
                float ppu = s > 0f ? 1f / s : 1f;
                if (!Mathf.Approximately(Image.pixelsPerUnitMultiplier, ppu)) Image.pixelsPerUnitMultiplier = ppu;
            }
            else SetScale(UiMath.PixelArtScale(artPixel, canvasScale, parentScale, _multiplier));

            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) Snap();
        }

        // A escala do pai em relação ao Canvas raiz (1 = o pai não mexeu no tamanho).
        private float ParentScale()
        {
            var parent = transform.parent;
            float root = _canvas.transform.lossyScale.y;
            if (parent == null || Mathf.Approximately(root, 0f)) return 1f;
            return Mathf.Abs(parent.lossyScale.y / root);
        }

        private void SetScale(float s)
        {
            var current = transform.localScale;
            if (Mathf.Approximately(current.x, s) && Mathf.Approximately(current.y, s)) return;
            transform.localScale = new Vector3(s, s, 1f);
        }

        // No Canvas Overlay os cantos já estão em pixels de tela: empurra o canto de baixo para o pixel inteiro mais perto.
        private void Snap()
        {
            ((RectTransform)transform).GetWorldCorners(_corners);
            var c = _corners[0];
            var delta = new Vector3(Mathf.Round(c.x) - c.x, Mathf.Round(c.y) - c.y, 0f);
            if (delta.sqrMagnitude > 1e-6f) transform.position += delta;
        }
    }
}
