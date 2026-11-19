using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cubo.View
{
    // O dono da escala de um elemento tocável (cartão, espaço do time, botão). Junta numa conta só tudo o que
    // mexe no tamanho, para um efeito não brigar com o outro:
    //   repouso (1, ou maior quando o elemento está em destaque) × aperto (menor enquanto o dedo está em cima).
    // Também faz a entrada (cresce do pequeno até o repouso) e o "soco" (um pulo rápido, ao receber um herói).
    // A escala não mexe no layout, então funciona dentro de Grid/Horizontal Layout Group.
    // Com PixelScale no mesmo objeto (pixel art), o pedido vai para PixelScale.Multiplier e o tamanho anda em degraus
    // de pixel inteiro, sem deformar a arte.
    // Fora do Play (editor parado, testes de EditMode) tudo vai direto ao estado final, sem animar.
    [DisallowMultipleComponent]
    public sealed class UiBounce : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Aperto")]
        [Tooltip("Escala enquanto o dedo está em cima (0,94 = 6% menor).")]
        [SerializeField, Range(0.8f, 1f)] private float pressScale = 0.94f;
        [SerializeField] private float pressSeconds = 0.08f;
        [Tooltip("Duração da volta ao soltar, com um leve passar do ponto (OutBack).")]
        [SerializeField] private float releaseSeconds = 0.28f;

        [Header("Repouso")]
        [Tooltip("Duração da troca de tamanho em repouso (ex.: o cartão em destaque cresce).")]
        [SerializeField] private float restSeconds = 0.22f;

        [Header("Entrada")]
        [Tooltip("De que escala o elemento cresce ao entrar na tela.")]
        [SerializeField, Range(0f, 1f)] private float popFrom = 0.7f;
        [SerializeField] private float popSeconds = 0.32f;

        [Header("Soco")]
        [Tooltip("Quanto o elemento pula ao receber algo (0,12 = 12%).")]
        [SerializeField, Range(0f, 0.5f)] private float punch = 0.12f;
        [SerializeField] private float punchSeconds = 0.3f;

        private float _rest = 1f;
        private float _press = 1f;
        private Tween _tween;
        private PixelScale _pixel;
        private bool _pixelChecked;

        private static bool Animating => Application.isPlaying;

        // O tamanho em repouso: 1 normal, maior em destaque. Sem mudança, não anima de novo.
        public void SetRest(float rest)
        {
            if (Mathf.Approximately(rest, _rest)) return;
            _rest = rest;
            To(Target, restSeconds, Ease.OutBack);
        }

        // Entra na tela crescendo até o repouso, depois de delay segundos (para entradas em cascata).
        public void Pop(float delay)
        {
            Kill();
            _press = 1f;
            if (!Animating) { Apply(Target); return; }
            Apply(Target * popFrom);
            _tween = ScaleTo(Target, popSeconds).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        // Um pulo rápido acima do repouso e de volta (ao receber um herói, por exemplo).
        public void Punch()
        {
            Kill();
            Apply(Target);
            if (!Animating) return;
            _tween = DOTween.Punch(() => Scale, v => Scale = v, Vector3.one * (punch * Target), punchSeconds, 6, 0.6f)
                .SetUpdate(true).SetLink(gameObject);
        }

        public void OnPointerDown(PointerEventData e)
        {
            _press = pressScale;
            To(Target, pressSeconds, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData e)
        {
            _press = 1f;
            To(Target, releaseSeconds, Ease.OutBack);
        }

        private void OnDisable()
        {
            Kill();
            _press = 1f;
            Apply(Target);                                   // desligado no meio de um efeito: volta inteiro
        }

        private float Target => _rest * _press;

        private void To(float scale, float seconds, Ease ease)
        {
            Kill();
            if (!Animating || !isActiveAndEnabled) { Apply(scale); return; }
            _tween = ScaleTo(scale, seconds).SetEase(ease).SetUpdate(true).SetLink(gameObject);
        }

        private void Apply(float scale) => Scale = Vector3.one * scale;

        // Igual a transform.DOScale, mas passando pela propriedade Scale (que respeita o PixelScale).
        private Tweener ScaleTo(float scale, float seconds) =>
            DOTween.To(() => Scale, v => Scale = v, Vector3.one * scale, seconds);

        private Vector3 Scale
        {
            get => Pixel != null ? Vector3.one * Pixel.Multiplier : transform.localScale;
            set
            {
                if (Pixel != null) Pixel.Multiplier = value.x;
                else transform.localScale = value;
            }
        }

        private PixelScale Pixel
        {
            get
            {
                if (!_pixelChecked) { _pixel = GetComponent<PixelScale>(); _pixelChecked = true; }
                return _pixel;
            }
        }

        private void Kill()
        {
            if (_tween != null && _tween.IsActive()) _tween.Kill();
            _tween = null;
        }
    }
}
