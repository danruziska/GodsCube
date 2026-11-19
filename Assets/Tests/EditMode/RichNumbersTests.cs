using NUnit.Framework;
using UnityEngine;
using Cubo.View;

namespace Cubo.Tests
{
    // O destaque dos números nas descrições: cada número (com sinal, decimal e %) ganha negrito e cor;
    // o resto do texto e as tags que já existiam ficam como estavam.
    public class RichNumbersTests
    {
        private const string Open = "<b><color=#FF0000FF>";
        private const string Close = "</color></b>";

        private static string H(string text) => RichNumbers.Highlight(text, Color.red);

        [Test]
        public void Numbers_and_percentages_are_bold_and_colored()
        {
            Assert.AreEqual(Open + "10" + Close + " de dano (" + Open + "30%" + Close + " de crítico)",
                H("10 de dano (30% de crítico)"));
        }

        [Test]
        public void Signs_and_decimals_stay_inside_the_highlight()
        {
            Assert.AreEqual(Open + "+15%" + Close + " · " + Open + "−2" + Close + " · " + Open + "0,5" + Close,
                H("+15% · −2 · 0,5"));
        }

        [Test]
        public void Existing_tags_are_left_untouched()
        {
            Assert.AreEqual("<sprite=2> " + Open + "12" + Close + " <size=80%>x</size>", H("<sprite=2> 12 <size=80%>x</size>"));
        }

        [Test]
        public void Text_without_numbers_and_empty_text_are_unchanged()
        {
            Assert.AreEqual("Cura o time.", H("Cura o time."));
            Assert.AreEqual("", H(""));
            Assert.IsNull(H(null));
        }
    }
}
