using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cubo.View
{
    // Traduz o dedo (ou o mouse) em toques nas peças. Arrastar (numa peça ou fora do cubo) gira a vista:
    // as fatias não giram mais, o jogo é escolher um grupo da frente e a face que vai para o rival.
    public sealed class CubeInput : MonoBehaviour
    {
        [SerializeField] private CubeView cube;
        [SerializeField] private Camera cam;
        [Tooltip("A vista livre: arrastar gira o cubo inteiro para olhar as outras faces.")]
        [SerializeField] private CubeOrbit orbit;
        [SerializeField] private float tapSlopPx = 12f;         // abaixo disto é toque; acima, arrasto

        public Action<int> Tapped;                  // peça tocada
        public Func<Vector2, bool> IsBlocked;       // o toque é de um painel por cima do cubo (o balão da entrega)
        public Vector2 TapPosition => _start;       // onde o último toque começou, em pixels
        public Action<int> Touched;                 // o dedo desceu numa peça (para destacar o grupo)
        public Action Untouched;                    // o dedo largou, ou virou arrasto
        public bool Locked;                         // a tela trava o toque enquanto toca uma apresentação
        public bool ViewLocked;                     // trava também a vista livre (painel aberto, fim da batalha)

        // Veneno e fogo: com AimMode verdadeiro, segurar o dedo numa peça mira uma área (o dedo é o centro) e arrastar
        // muda a mira em vez de girar a vista; soltar escolhe a última casa mirada.
        public Func<bool> AimMode;
        public Action<int> Aimed;                   // o dedo está nesta casa (a cada casa nova)
        public Action<int> AimDone;                 // o dedo soltou: a casa escolhida

        private enum Mode { Idle, Pressed, Orbiting, Ignored, Aiming }
        private Mode _mode;
        private int _cell;
        private Vector2 _start;

        private void Update()
        {
            var ptr = Pointer.current;
            if (ptr == null || cube.Root == null) return;
            Vector2 pos = ptr.position.ReadValue();

            switch (_mode)
            {
                case Mode.Idle:
                    if ((!Locked || !ViewLocked) && ptr.press.wasPressedThisFrame) Press(pos);
                    break;
                case Mode.Pressed:
                    if (ptr.press.wasReleasedThisFrame) { _mode = Mode.Idle; Untouched?.Invoke(); if (_cell >= 0) Tapped?.Invoke(_cell); }
                    else if ((pos - _start).magnitude > tapSlopPx) BeginDrag();
                    break;
                case Mode.Orbiting:
                    if (orbit != null) orbit.Drag(pos - _start);
                    if (ptr.press.wasReleasedThisFrame) _mode = Mode.Idle;
                    break;
                case Mode.Ignored:
                    if (ptr.press.wasReleasedThisFrame) _mode = Mode.Idle;
                    break;
                case Mode.Aiming:
                    if (ptr.press.wasReleasedThisFrame) { _mode = Mode.Idle; AimDone?.Invoke(_cell); break; }
                    int cell = CellAt(pos);
                    if (cell >= 0 && cell != _cell) { _cell = cell; Aimed?.Invoke(cell); }
                    break;
            }
        }

        private void Press(Vector2 pos)
        {
            // Só vale dentro da área do cubo: o que está nos painéis da interface nunca vira toque no cubo.
            if (!cam.pixelRect.Contains(pos) || (IsBlocked != null && IsBlocked(pos))) { _mode = Mode.Ignored; return; }
            _start = pos;
            _cell = Locked ? -1 : CellAt(pos);   // com a entrada travada (a apresentação toca), o toque não escolhe peça
            if (_cell >= 0 && AimMode != null && AimMode())
            {
                _mode = Mode.Aiming;
                Aimed?.Invoke(_cell);
                return;
            }
            _mode = Mode.Pressed;
            if (_cell >= 0) Touched?.Invoke(_cell);
        }

        // A peça debaixo do dedo, ou -1.
        private int CellAt(Vector2 pos)
        {
            if (!Physics.Raycast(cam.ScreenPointToRay(pos), out var hit, 200f)) return -1;
            var piece = hit.collider.GetComponent<PieceView>();
            return piece != null ? piece.Cell : -1;
        }

        // Passou do limite do toque: é arrasto, e arrasto gira a vista. Com a vista travada, não vale.
        private void BeginDrag()
        {
            Untouched?.Invoke();
            if (ViewLocked || orbit == null) { _mode = Mode.Ignored; return; }
            orbit.BeginDrag();
            _mode = Mode.Orbiting;
        }
    }
}
