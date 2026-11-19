using System;
using UnityEngine;

namespace Cubo.View
{
    // Faz este RectTransform ocupar a área útil da janela (Screen.safeArea), com um respiro em cada borda.
    // Tudo o que estiver dentro dele fica visível em qualquer tamanho de janela. Avisa quando a área muda.
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        [Tooltip("Margem mínima embaixo, em unidades do Canvas (referência 1080x1920). Garante um respiro " +
                 "na borda de baixo da janela, para o conteúdo de baixo não ficar colado nela. Zero: sem margem extra.")]
        [SerializeField] private float minBottom;
        [Header("Respiro por dentro da área segura")]
        [Tooltip("Espaço a mais em cada lado (esquerda e direita), em unidades do Canvas, por dentro da área segura: o conteúdo " +
                 "não encosta na borda da janela. O fundo da tela fica fora da SafeArea e não muda.")]
        [SerializeField] private float sidePadding = 24f;
        [Tooltip("Espaço a mais em cima, por dentro da área segura.")]
        [SerializeField] private float topPadding = 16f;
        [Tooltip("Espaço a mais embaixo, por dentro da área segura (somado ao Min Bottom).")]
        [SerializeField] private float bottomPadding = 16f;

        public event Action Changed;

        private RectTransform _rect;
        private Canvas _canvas;
        private Rect _safe;
        private Vector2Int _screen = new Vector2Int(-1, -1);
        private float _scale = -1f;
        private float _minBottom = -1f;
        private Vector3 _padding = new Vector3(-1f, -1f, -1f);

        private void OnEnable() => Apply(true);

        // LateUpdate: roda depois do CanvasScaler, que ajusta a escala do Canvas no Update.
        private void LateUpdate() => Apply(false);

        public void Apply(bool force)
        {
            var safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            float scale = _canvas != null ? _canvas.rootCanvas.scaleFactor : 1f;
            var padding = new Vector3(sidePadding, topPadding, bottomPadding);
            if (!force && safe == _safe && screen == _screen && Mathf.Approximately(scale, _scale) && Mathf.Approximately(minBottom, _minBottom)
                && padding == _padding) return;
            _padding = padding;
            _safe = safe;
            _screen = screen;
            _scale = scale;
            _minBottom = minBottom;

            if (_rect == null) _rect = GetComponent<RectTransform>();
            UiMath.SafeAnchors(safe, screen.x, screen.y, out var min, out var max);
            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = new Vector2(sidePadding, UiMath.MinInset(safe.y, scale, minBottom) + bottomPadding);
            _rect.offsetMax = new Vector2(-sidePadding, -topPadding);
            Changed?.Invoke();
        }
    }
}
