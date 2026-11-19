using NUnit.Framework;
using UnityEngine;
using Cubo.View;

namespace Cubo.Tests
{
    public class ScreenBackdropTests
    {
        private const float Eps = 0.001f;

        [Test]
        public void A_full_screen_camera_puts_the_backdrop_straight_ahead_covering_the_view()
        {
            ScreenBackdrop.Place(new Rect(0f, 0f, 1f, 1f), 90f, 0.5f, 10f, new Vector2(10f, 20f), out var pos, out float scale);
            Assert.AreEqual(0f, pos.x, Eps);
            Assert.AreEqual(0f, pos.y, Eps);
            Assert.AreEqual(10f, pos.z, Eps);
            Assert.AreEqual(1f, scale, Eps);                 // a 90°, a 10 de distância a câmera vê 20 de altura por 10
        }

        // A câmera do cubo só desenha a faixa do meio: o fundo dela é o pedaço da imagem da tela inteira que cai ali.
        [Test]
        public void A_strip_camera_sees_the_same_full_screen_image_shifted_and_enlarged()
        {
            var strip = new Rect(0f, 0.41f, 1f, 0.52f);
            float fov = 60f, d = 100f, aspect = 0.9f;
            ScreenBackdrop.Place(strip, fov, aspect, d, new Vector2(1f, 1f), out var pos, out float scale);

            float vh = 2f * d * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float screenH = vh / strip.height;
            // o meio da tela fica abaixo do meio da faixa (a faixa está acima do centro)
            Assert.Less(pos.y, 0f);
            // a borda de cima da tela, vista desta câmera, fica acima da borda de cima da faixa na proporção certa
            float screenTop = pos.y + screenH * 0.5f;
            float stripTop = vh * 0.5f;
            Assert.AreEqual((1f - (strip.y + strip.height)) * screenH, screenTop - stripTop, 0.01f);
            Assert.AreEqual(0f, pos.x, Eps);
            Assert.GreaterOrEqual(scale, screenH - Eps);     // cobre a altura da tela inteira
            Assert.GreaterOrEqual(scale, vh * aspect - Eps); // e a largura
        }

        [Test]
        public void A_wide_image_covers_a_tall_screen_without_stretching()
        {
            ScreenBackdrop.Place(new Rect(0f, 0f, 1f, 1f), 90f, 0.5f, 10f, new Vector2(40f, 20f), out _, out float scale);
            Assert.AreEqual(1f, scale, Eps);                 // a altura manda; a largura sobra para os lados
        }
    }
}
