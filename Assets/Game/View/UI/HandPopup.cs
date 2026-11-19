using System;
using UnityEngine;
using UnityEngine.UI;

namespace Cubo.View
{
    // O balão da entrega: aparece onde o dedo tocou a face escolhida, com Confirmar (✓) e Cancelar (✕).
    // Fica dentro da tela mesmo quando o toque é perto da borda. Quem decide o que fazer é a BattleUi.
    // Some e aparece pelo CanvasGroup, sem desativar o objeto: desativar botões no começo do Play, com o
    // "Reload Scene" desligado nas Enter Play Mode Options, estoura um erro dentro do próprio Unity (Selectable.OnDisable).
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class HandPopup : MonoBehaviour
    {
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [Tooltip("Deslocamento do balão a partir do ponto tocado, em unidades da interface. (0, 0) = centrado no dedo; " +
                 "y positivo = acima do dedo.")]
        [SerializeField] private Vector2 offset = Vector2.zero;

        public event Action Confirmed;
        public event Action Cancelled;

        private bool _ready;
        private bool _open;
        private CanvasGroup _group;

        public bool IsOpen => _open;
        private RectTransform Rect => (RectTransform)transform;

        public void Init()
        {
            if (_ready) return;
            _ready = true;
            if (!gameObject.activeSelf) gameObject.SetActive(true);   // fica sempre ativo; quem esconde é o CanvasGroup
            if (confirmButton != null) confirmButton.onClick.AddListener(() => { Hide(); Confirmed?.Invoke(); });
            if (cancelButton != null) cancelButton.onClick.AddListener(() => { Hide(); Cancelled?.Invoke(); });
        }

        // Abre (ou muda de lugar) no ponto da tela, em pixels.
        public void Show(Vector2 screenPos)
        {
            SetShown(true);
            transform.SetAsLastSibling();                 // por cima do resto da interface
            var parent = Rect.parent as RectTransform;
            if (parent == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, CanvasCamera(), out var local)) return;
            var p = UiMath.ClampInside(local + offset, Rect.rect.size, Rect.pivot, parent.rect);
            Rect.localPosition = new Vector3(p.x, p.y, 0f);
        }

        public void Hide()
        {
            SetShown(false);
        }

        private void SetShown(bool shown)
        {
            _open = shown;
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();   // prefab montado antes de o componente exigir um
            _group.alpha = shown ? 1f : 0f;
            _group.interactable = shown;
            _group.blocksRaycasts = shown;
        }

        // O ponto está em cima do balão aberto? (o toque é do balão, não do cubo)
        public bool Contains(Vector2 screenPos) =>
            IsOpen && RectTransformUtility.RectangleContainsScreenPoint(Rect, screenPos, CanvasCamera());

        private Camera CanvasCamera()
        {
            var canvas = GetComponentInParent<Canvas>();
            return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        public string MissingReference() => confirmButton == null ? "confirmButton" : cancelButton == null ? "cancelButton" : null;
    }
}
