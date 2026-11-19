using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cubo.View
{
    // O balão com o detalhe de um poder: abre logo abaixo da linha tocada e fecha com um toque fora dele.
    // O botão Usar só aparece nos poderes do jogador que estão prontos.
    // Some pelo CanvasGroup (desativar botões no Play, sem recarregar a cena, estoura erro no Unity; ver HandPopup).
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PowerTip : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [Tooltip("O botão Usar. Fica escondido quando o poder não pode ser usado (e sempre nos do rival).")]
        [SerializeField] private Button useButton;
        [Tooltip("Distância entre a linha tocada e o balão, em unidades da interface.")]
        [SerializeField] private float gap = 8f;

        public event Action Used;
        public event Action Closed;                 // fechou por um toque fora

        private CanvasGroup _group;
        private RectTransform _source;
        private bool _open, _wired;

        public bool IsOpen => _open;
        private RectTransform Rect => (RectTransform)transform;

        public void Init()
        {
            if (_wired) return;
            _wired = true;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (useButton != null) useButton.onClick.AddListener(() => Used?.Invoke());
            SetShown(false);
        }

        // Abre abaixo de `source` (a linha do poder). `canUse` mostra o botão Usar.
        public void Show(string body, RectTransform source, bool canUse)
        {
            _source = source;
            text.text = body;
            if (useButton != null) useButton.gameObject.SetActive(canUse);
            SetShown(true);
            transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            Place();
        }

        // Troca só o texto e o botão (a mana mudou com o balão aberto).
        public void UpdateBody(string body, bool canUse)
        {
            text.text = body;
            if (useButton != null) useButton.gameObject.SetActive(canUse);
        }

        public void Hide()
        {
            _source = null;
            SetShown(false);
        }

        private void Place()
        {
            var parent = Rect.parent as RectTransform;
            if (parent == null || _source == null) return;
            var corners = new Vector3[4];
            _source.GetWorldCorners(corners);               // 0 baixo-esq, 1 cima-esq, 2 cima-dir, 3 baixo-dir
            var cam = CanvasCamera();
            Vector2 below = RectTransformUtility.WorldToScreenPoint(cam, (corners[0] + corners[3]) * 0.5f);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, below, cam, out var local)) return;
            local.y -= gap;
            // o topo do balão encosta logo abaixo da linha; perto da borda, ele encosta na borda
            var pivot = Rect.pivot;
            var size = Rect.rect.size;
            var anchorPoint = local + new Vector2((pivot.x - 0.5f) * size.x, (pivot.y - 1f) * size.y);
            var p = UiMath.ClampInside(anchorPoint, size, pivot, parent.rect);
            Rect.localPosition = new Vector3(p.x, p.y, 0f);
        }

        // Um toque fora do balão e fora da linha que o abriu fecha o balão.
        private void Update()
        {
            if (!_open) return;
            var ptr = Pointer.current;
            if (ptr == null || !ptr.press.wasPressedThisFrame) return;
            Vector2 pos = ptr.position.ReadValue();
            var cam = CanvasCamera();
            if (RectTransformUtility.RectangleContainsScreenPoint(Rect, pos, cam)) return;
            if (_source != null && RectTransformUtility.RectangleContainsScreenPoint(_source, pos, cam)) return;
            Hide();
            Closed?.Invoke();
        }

        private void SetShown(bool shown)
        {
            _open = shown;
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = shown ? 1f : 0f;
            _group.interactable = shown;
            _group.blocksRaycasts = shown;
        }

        private Camera CanvasCamera()
        {
            var canvas = GetComponentInParent<Canvas>();
            return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        public string MissingReference() => text == null ? "text" : null;
    }
}
