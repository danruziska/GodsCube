using UnityEngine;
using Cubo.App;

namespace Cubo.View
{
    // Só para testar (Editor e build de desenvolvimento): um botão "Screens" no canto de cima à direita abre uma lista
    // que leva direto a qualquer tela entre os andares (bênção, os 3 eventos, loja, porta, vitória, derrota), mais
    // ouro e heróis feridos para testar a loja. Funciona de qualquer ponto: preparação, batalha ou outra tela.
    public sealed class ScreenPicker : MonoBehaviour
    {
        private const float VirtualWidth = 540f;            // a lista é desenhada nesta largura e escalada para a tela

        private static readonly (string id, string label)[] Screens =
        {
            ("blessing", "Blessing"), ("idolo:rei", "Idol: King"), ("idolo:rainha", "Idol: Queen"), ("idolo:torre", "Idol: Rook"), ("acampamento", "Event: Scout's Camp"),
            ("juramento", "Event: Knight's Vow"), ("shop", "Shop"), ("door", "Door"), ("win", "End: victory"), ("lose", "End: defeat"),
        };

        private GameFlow _flow;
        private bool _open;
        private GUIStyle _button;

        public void Init(GameFlow flow) => _flow = flow;

        private void OnGUI()
        {
            if (_flow == null) return;
            float s = Screen.width / VirtualWidth;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            if (_button == null) _button = new GUIStyle(GUI.skin.button) { fontSize = 16 };

            var safe = Screen.safeArea;
            float top = (Screen.height - safe.yMax) / s + 40f, right = VirtualWidth - 8f;
            if (GUI.Button(new Rect(right - 96f, top, 96f, 34f), _open ? "Close" : "Screens", _button)) _open = !_open;
            if (!_open) return;

            float w = 220f, h = 38f, y = top + 42f, x = right - w;
            GUI.Box(new Rect(x - 6f, y - 6f, w + 12f, (Screens.Length + 2) * (h + 4f) + 12f), GUIContent.none);
            foreach (var (id, label) in Screens)
            {
                if (GUI.Button(new Rect(x, y, w, h), label, _button)) { _open = false; _flow.Preview(id); }
                y += h + 4f;
            }
            GUI.enabled = _flow.Run != null;
            if (GUI.Button(new Rect(x, y, w / 2f - 2f, h), "+500 gold", _button)) _flow.PreviewGold(500);
            if (GUI.Button(new Rect(x + w / 2f + 2f, y, w / 2f - 2f, h), "Hurt team", _button)) _flow.PreviewHurt();
            GUI.enabled = true;
        }
    }
}
