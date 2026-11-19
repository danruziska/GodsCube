using UnityEngine;
using UnityEngine.UI;

namespace Cubo.View
{
    // A luz atrás do herói em destaque: duas manchas radiais, uma na cor de cada domínio do herói, desenhadas
    // por código (sem arte). Ao trocar de herói as cores passam suavemente de umas para as outras; acesa, a luz
    // respira devagar. Vai em dois Images atrás do GodArt, com Raycast Target desmarcado.
    public sealed class HeroGlow : MonoBehaviour
    {
        private const int Size = 256;
        private static Sprite _sprite;

        [Tooltip("A mancha do primeiro domínio do herói.")]
        [SerializeField] private Image first;
        [Tooltip("A mancha do segundo domínio do herói.")]
        [SerializeField] private Image second;
        [Tooltip("Opacidade da luz acesa.")]
        [Range(0f, 1f)] [SerializeField] private float strength = 0.45f;
        [Tooltip("Quanto a opacidade oscila quando acesa (0 = parada).")]
        [Range(0f, 0.5f)] [SerializeField] private float pulse = 0.15f;
        [Tooltip("Duração de uma respiração, em segundos.")]
        [SerializeField] private float pulsePeriod = 4f;
        [Tooltip("Duração da troca de cor e do fade ao acender e apagar. 0 = instantâneo.")]
        [SerializeField] private float fadeSeconds = 0.35f;

        private Color _fromA, _fromB, _toA, _toB;
        private float _level, _target;                 // 0 = apagada, 1 = acesa
        private float _blend = 1f;                     // 0 = cores antigas, 1 = cores novas
        private float _time;

        public bool On => _target > 0f;

        private void Awake()
        {
            Prepare(first);
            Prepare(second);
            Apply();
        }

        // Acende com as cores dos dois domínios do herói. Já acesa, as cores atuais viram as novas aos poucos.
        public void Show(Color a, Color b)
        {
            _fromA = Current(_fromA, _toA);
            _fromB = Current(_fromB, _toB);
            if (_level <= 0f) { _fromA = a; _fromB = b; }   // apagada: acende já com as cores novas
            _toA = a;
            _toB = b;
            _blend = 0f;
            _target = 1f;
            Settle();
        }

        public void Hide()
        {
            _target = 0f;
            Settle();
        }

        // Sem fade, ou com o objeto desligado (o Update não roda), o estado final vale na hora.
        private void Settle()
        {
            if (fadeSeconds <= 0f || !isActiveAndEnabled) { _level = _target; _blend = 1f; }
            Apply();
        }

        private void Update()
        {
            if (_target <= 0f && _level <= 0f) return;   // apagada e parada: nada a fazer
            float step = fadeSeconds <= 0f ? 1f : Time.unscaledDeltaTime / fadeSeconds;
            _level = Mathf.MoveTowards(_level, _target, step);
            _blend = Mathf.MoveTowards(_blend, 1f, step);
            _time += Time.unscaledDeltaTime;
            Apply();
        }

        private Color Current(Color from, Color to) => Color.Lerp(from, to, _blend);

        private void Apply()
        {
            float wave = pulse > 0f && pulsePeriod > 0f
                ? 1f - pulse * 0.5f * (1f + Mathf.Sin(_time * 2f * Mathf.PI / pulsePeriod))
                : 1f;
            float k = Mathf.SmoothStep(0f, 1f, _blend);
            float alpha = strength * _level * wave;
            Paint(first, Color.Lerp(_fromA, _toA, k), alpha);
            Paint(second, Color.Lerp(_fromB, _toB, k), alpha);
        }

        private static void Paint(Image image, Color c, float alpha)
        {
            if (image == null) return;
            c.a = alpha;
            image.color = c;
            image.enabled = alpha > 0.001f;
        }

        private static void Prepare(Image image)
        {
            if (image == null) return;
            image.sprite = RadialSprite();
            image.raycastTarget = false;
        }

        // Um círculo branco com queda suave (gaussiana) até a borda. O ruído de ±0,5 nível no alpha (dither)
        // evita as faixas visíveis que um degradê de 8 bits mostra quando esticado sobre fundo escuro.
        private static Sprite RadialSprite()
        {
            if (_sprite != null) return _sprite;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[Size * Size];
            var rng = new System.Random(7);
            float r = (Size - 1) * 0.5f;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float d = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r)) / r;
                    float edge = Mathf.Clamp01((1f - d) / 0.15f);                      // chega a zero exatamente na borda
                    float a = Mathf.Exp(-4.5f * d * d) * edge * 255f;
                    a += (float)rng.NextDouble() - 0.5f;
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.Clamp(Mathf.Round(a), 0f, 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            _sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            _sprite.hideFlags = HideFlags.HideAndDontSave;
            return _sprite;
        }
    }
}
