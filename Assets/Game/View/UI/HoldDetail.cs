using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Cubo.View
{
    // Pede para mostrar o detalhe de um botão: com mouse, enquanto o cursor está em cima; com o dedo,
    // depois de segurar holdSeconds. Quem trata o clique pergunta ConsumeHold(): se foi uma segurada, não é para usar o botão.
    public sealed class HoldDetail : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("Quanto tempo o dedo fica parado no botão para mostrar o detalhe, em segundos.")]
        [SerializeField] private float holdSeconds = 0.35f;

        public event Action<bool> Showing;     // true: mostrar o detalhe; false: esconder

        private bool _down, _hover, _wasHold, _shown;
        private float _held;

        // No celular o "entrar" acontece junto com o toque; ali quem manda é o tempo segurado.
        private static bool IsTouch(PointerEventData e) =>
            e is ExtendedPointerEventData x && x.pointerType == UIPointerType.Touch;

        public void OnPointerEnter(PointerEventData e)
        {
            if (IsTouch(e)) return;
            _hover = true;
            Set(true);
        }

        public void OnPointerExit(PointerEventData e)
        {
            _hover = false;
            _down = false;
            Set(false);
        }

        public void OnPointerDown(PointerEventData e)
        {
            _down = true;
            _held = 0f;
            _wasHold = false;
        }

        public void OnPointerUp(PointerEventData e)
        {
            _wasHold = _down && _held >= holdSeconds;
            _down = false;
            if (!_hover) Set(false);
        }

        public bool ConsumeHold()
        {
            bool hold = _wasHold;
            _wasHold = false;
            return hold;
        }

        private void OnDisable()
        {
            _down = _hover = _wasHold = false;
            Set(false);
        }

        private void Update()
        {
            if (!_down) return;
            _held += Time.unscaledDeltaTime;
            if (_held >= holdSeconds) Set(true);
        }

        private void Set(bool on)
        {
            if (on == _shown) return;
            _shown = on;
            Showing?.Invoke(on);
        }
    }
}