using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cubo.View
{
    // Um botão com um texto. É a base do prefab TextButton e da variante PowerButton.
    public sealed class TextButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        public Button Button => button;

        public string Text
        {
            get => label.text;
            set => label.text = value;
        }

        public bool Interactable
        {
            get => button.interactable;
            set => button.interactable = value;
        }

        public void OnClick(Action action) => button.onClick.AddListener(() => action());

        // Pinta o botão a partir de uma cor só: realce mais claro, pressionado mais escuro, desligado bem apagado.
        public void Tint(Color normal)
        {
            var c = button.colors;
            c.normalColor = Scale(normal, 1f);
            c.highlightedColor = Scale(normal, 1.15f);
            c.selectedColor = Scale(normal, 1f);
            c.pressedColor = Scale(normal, 0.8f);
            c.disabledColor = Scale(normal, 0.45f);
            button.colors = c;
        }

        private static Color Scale(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

        public string MissingReference() => button == null ? "button" : label == null ? "label" : null;
    }
}