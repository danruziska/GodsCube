using System.Text.RegularExpressions;
using UnityEngine;

namespace Cubo.View
{
    // Destaca os números de um texto (10, +15%, 0,3, −2) em negrito e na cor dada, em qualquer idioma.
    // Tags do TextMeshPro que já estejam no texto (<sprite=2>, <size=80%>) ficam intactas.
    // Um número entre parênteses é o valor ANTIGO de uma melhoria ("2 (1)"): fica apagado, sem negrito, na cor `old`.
    public static class RichNumbers
    {
        // Grupo 1: uma tag inteira, copiada como está. Grupo 2: um valor antigo entre parênteses. Grupo 3: um número
        // com sinal, decimal e % opcionais.
        private static readonly Regex Pattern = new Regex(
            @"(<[^>]*>)|(\([+\-−]?\d+(?:[.,]\d+)?%?\))|([+\-−]?\d+(?:[.,]\d+)?%?)", RegexOptions.Compiled);

        public static readonly Color OldColor = new Color(0.6f, 0.6f, 0.66f, 1f);

        public static string Highlight(string text, Color color) => Highlight(text, color, OldColor);

        public static string Highlight(string text, Color color, Color old)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string open = "<b><color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">";
            string dim = "<color=#" + ColorUtility.ToHtmlStringRGBA(old) + ">";
            return Pattern.Replace(text, m => m.Groups[1].Success ? m.Value
                                            : m.Groups[2].Success ? dim + m.Value + "</color>"
                                            : open + m.Value + "</color></b>");
        }
    }
}
