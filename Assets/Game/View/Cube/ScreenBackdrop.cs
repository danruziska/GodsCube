using UnityEngine;

namespace Cubo.View
{
    // Uma imagem de fundo que cobre a tela inteira, atrás do cubo, como o fundo da tela dos heróis.
    // Vai na câmera. A câmera do cubo só desenha a faixa do meio (Viewport Rect); a BackgroundCamera, o resto.
    // Com este componente nas duas, cada uma desenha o pedaço da mesma imagem que cai na sua parte da tela,
    // e as duas partes se encaixam sem emenda.
    [RequireComponent(typeof(Camera))]
    public sealed class ScreenBackdrop : MonoBehaviour
    {
        [Tooltip("A imagem de fundo (Sprite). Cobre a tela inteira sem distorcer; o que sobra nas bordas fica de fora.")]
        [SerializeField] private Sprite sprite;
        [Tooltip("Cor que multiplica a imagem. Branco = a imagem como é; mais escuro = fundo mais apagado.")]
        [SerializeField] private Color tint = Color.white;
        [Tooltip("Distância da câmera até o fundo. Tem de ficar atrás do cubo e antes do Far da câmera.")]
        [SerializeField, Min(1f)] private float distance = 500f;

        private const string ChildName = "Backdrop";
        private Camera _cam;
        private SpriteRenderer _renderer;

        private void OnEnable()
        {
            _cam = GetComponent<Camera>();
            var old = transform.Find(ChildName);
            if (old != null) CubeView.Discard(old.gameObject);
            var go = new GameObject(ChildName) { hideFlags = HideFlags.DontSave, layer = gameObject.layer };
            go.transform.SetParent(transform, false);
            _renderer = go.AddComponent<SpriteRenderer>();
            _renderer.sortingOrder = -1000;              // atrás das imagens das peças e do brilho
        }

        private void OnDisable()
        {
            if (_renderer != null) CubeView.Discard(_renderer.gameObject);
            _renderer = null;
        }

        // Depois de o GameBootstrap mudar o Viewport Rect e a posição da câmera.
        private void LateUpdate()
        {
            if (_renderer == null || _cam == null) return;
            _renderer.sprite = sprite;
            _renderer.color = tint;
            _renderer.enabled = sprite != null;
            if (sprite == null) return;
            float d = Mathf.Min(distance, _cam.farClipPlane * 0.95f);
            Place(_cam.rect, _cam.fieldOfView, _cam.aspect, d, sprite.bounds.size, out var pos, out float scale);
            var t = _renderer.transform;
            t.localPosition = pos;
            t.localRotation = Quaternion.identity;
            t.localScale = new Vector3(scale, scale, 1f);
        }

        // Onde pôr a imagem (no espaço da câmera) e quanto ampliá-la para que cubra a TELA INTEIRA, embora a câmera
        // só desenhe `viewport` (0 a 1). `aspect` é largura/altura da parte da câmera, como Camera.aspect.
        public static void Place(Rect viewport, float fovDeg, float aspect, float distance, Vector2 spriteSize,
                                 out Vector3 position, out float scale)
        {
            float vh = 2f * distance * Mathf.Tan(fovDeg * 0.5f * Mathf.Deg2Rad);   // altura que a câmera vê lá longe
            float vw = vh * aspect;
            float w = Mathf.Max(1e-4f, viewport.width), h = Mathf.Max(1e-4f, viewport.height);
            float screenW = vw / w, screenH = vh / h;                               // a tela inteira, na mesma escala
            float x = (0.5f - (viewport.x + w * 0.5f)) * screenW;                   // o centro da tela visto desta parte
            float y = (0.5f - (viewport.y + h * 0.5f)) * screenH;
            position = new Vector3(x, y, distance);
            float sx = spriteSize.x > 0f ? screenW / spriteSize.x : 1f;
            float sy = spriteSize.y > 0f ? screenH / spriteSize.y : 1f;
            scale = Mathf.Max(sx, sy);                                               // cobre, sem distorcer
        }
    }
}
