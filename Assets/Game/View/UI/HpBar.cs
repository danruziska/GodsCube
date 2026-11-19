using TMPro;
using UnityEngine;

namespace Cubo.View
{
    // Barra de vida com o texto "Rival  80 / 85". A barra anda todo quadro; o texto só é refeito
    // quando o número inteiro, o máximo ou o idioma mudam (montar texto a cada quadro gera lixo de memória).
    public sealed class HpBar : MonoBehaviour
    {
        [SerializeField] private FillBar bar;
        [SerializeField] private TMP_Text label;

        private int _shown = int.MinValue, _max = int.MinValue;
        private string _prefix;

        public void Show(float hp, int max, string prefix)
        {
            bar.Set(UiMath.Fraction(hp, max));
            int whole = Mathf.CeilToInt(hp);
            if (whole == _shown && max == _max && prefix == _prefix) return;
            _shown = whole;
            _max = max;
            _prefix = prefix;
            label.text = prefix + "  " + whole + " / " + max;
        }

        public string MissingReference() =>
            bar == null ? "bar" : label == null ? "label" : bar.MissingReference() is string m ? "bar." + m : null;
    }
}