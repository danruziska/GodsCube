using UnityEngine;
using UnityEngine.UI;

namespace Cubo.View
{
    // Uma faixa de texto que o jogador pode fechar para ver o cenário inteiro: o X no canto de cima à direita esconde a
    // faixa e acende um botão pequeno no mesmo canto, que traz a faixa de volta. Toda vez que a tela abre de novo, a
    // faixa volta aberta (a tela chama ShowPanel). Usado na bênção e nos eventos.
    public sealed class TextPanelToggle : MonoBehaviour
    {
        [Tooltip("A faixa com o texto (o que some).")]
        [SerializeField] private GameObject panel;
        [Tooltip("O X dentro da faixa, no canto de cima à direita.")]
        [SerializeField] private Button closeButton;
        [Tooltip("O botão que traz a faixa de volta (fora da faixa, no mesmo canto).")]
        [SerializeField] private Button openButton;

        private bool _wired;

        private void Awake() => Wire();

        private void Wire()
        {
            if (_wired) return;
            _wired = true;
            if (closeButton != null) closeButton.onClick.AddListener(() => Set(false));
            if (openButton != null) openButton.onClick.AddListener(() => Set(true));
        }

        public void ShowPanel() { Wire(); Set(true); }

        private void Set(bool open)
        {
            if (panel != null) panel.SetActive(open);
            if (openButton != null) openButton.gameObject.SetActive(!open);
        }
    }
}
