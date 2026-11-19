using NUnit.Framework;
using UnityEngine;
using Cubo.View;

namespace Cubo.Tests
{
    // As contas da interface que decidem onde o cubo e os painéis ficam em cada aparelho.
    public class UiMathTests
    {
        private const float Eps = 1e-4f;

        [Test]
        public void Full_screen_safe_area_gives_full_anchors()
        {
            UiMath.SafeAnchors(new Rect(0, 0, 1080, 1920), 1080, 1920, out var min, out var max);
            Assert.AreEqual(0f, min.x, Eps); Assert.AreEqual(0f, min.y, Eps);
            Assert.AreEqual(1f, max.x, Eps); Assert.AreEqual(1f, max.y, Eps);
        }

        [Test]
        public void A_notch_on_top_lowers_the_top_anchor()
        {
            UiMath.SafeAnchors(new Rect(0, 0, 1080, 1800), 1080, 1920, out var min, out var max);
            Assert.AreEqual(0f, min.y, Eps);
            Assert.AreEqual(1800f / 1920f, max.y, Eps);
        }

        [Test]
        public void A_gesture_bar_at_the_bottom_raises_the_bottom_anchor()
        {
            UiMath.SafeAnchors(new Rect(0, 60, 1080, 1860), 1080, 1920, out var min, out var max);
            Assert.AreEqual(60f / 1920f, min.y, Eps);
            Assert.AreEqual(1f, max.y, Eps);
        }

        [Test]
        public void Zero_sized_screen_falls_back_to_the_whole_screen()
        {
            UiMath.SafeAnchors(new Rect(0, 0, 0, 0), 0, 0, out var min, out var max);
            Assert.AreEqual(0f, min.x, Eps); Assert.AreEqual(1f, max.y, Eps);
            var r = UiMath.ToViewport(new Vector2(10, 10), new Vector2(20, 20), 0, 0);
            Assert.AreEqual(1f, r.width, Eps); Assert.AreEqual(1f, r.height, Eps);
        }

        [Test]
        public void Viewport_is_the_corners_over_the_screen_size()
        {
            // a faixa do cubo do layout padrão: de 36% a 93% da altura, largura inteira
            var r = UiMath.ToViewport(new Vector2(0, 0.36f * 1920), new Vector2(1080, 0.93f * 1920), 1080, 1920);
            Assert.AreEqual(0f, r.x, Eps);
            Assert.AreEqual(0.36f, r.y, Eps);
            Assert.AreEqual(1f, r.width, Eps);
            Assert.AreEqual(0.57f, r.height, Eps);
        }

        [Test]
        public void Viewport_is_clamped_to_the_screen()
        {
            var r = UiMath.ToViewport(new Vector2(-50, -50), new Vector2(2000, 3000), 1080, 1920);
            Assert.AreEqual(0f, r.x, Eps); Assert.AreEqual(0f, r.y, Eps);
            Assert.AreEqual(1f, r.width, Eps); Assert.AreEqual(1f, r.height, Eps);
        }

        [TestCase(0f, 1f, 48f, 48f)]     // tela curva sem área segura embaixo: sobe a margem inteira
        [TestCase(24f, 1f, 48f, 24f)]    // o aparelho já reserva 24 px: falta o resto
        [TestCase(120f, 1f, 48f, 0f)]    // barra de gestos maior que a margem: nada a mais
        [TestCase(48f, 2f, 48f, 24f)]    // Canvas em escala 2: 48 px de tela são 24 unidades
        [TestCase(30f, 1f, 0f, 0f)]      // sem margem mínima: só a área segura
        [TestCase(0f, 0f, 48f, 48f)]     // escala ainda não calculada: a margem inteira
        public void Min_inset_lifts_content_only_by_what_the_safe_area_misses(float safePixels, float scale, float min, float expected)
        {
            Assert.AreEqual(expected, UiMath.MinInset(safePixels, scale, min), Eps);
        }

        [TestCase(0f, 0.9f, 1f)]        // começa aceso: a escolha aparece na hora
        [TestCase(0.45f, 0.9f, 0f)]     // meio ciclo: apagado
        [TestCase(0.9f, 0.9f, 1f)]      // ciclo inteiro: aceso de novo
        [TestCase(0.225f, 0.9f, 0.5f)]  // um quarto: no meio do caminho
        [TestCase(3f, 0f, 1f)]          // sem período: parado aceso
        public void Pulse_breathes_between_lit_and_dim(float seconds, float period, float expected)
        {
            Assert.AreEqual(expected, UiMath.Pulse(seconds, period), Eps);
        }

        [TestCase(50f, 100f, 0.5f)]
        [TestCase(5f, 0f, 0f)]
        [TestCase(120f, 100f, 1f)]
        [TestCase(-3f, 100f, 0f)]
        public void Fraction_is_clamped_between_zero_and_one(float value, float max, float expected)
        {
            Assert.AreEqual(expected, UiMath.Fraction(value, max), Eps);
        }

        [TestCase(6, 1f, 6f)]          // tela igual à referência: 6 px de tela por pixel da arte
        [TestCase(6, 0.5f, 6f)]        // tela pela metade: 3 px inteiros, escala 6 no Canvas
        [TestCase(6, 1.3333f, 6.0001f)] // 1440 de largura: 8 px inteiros
        [TestCase(6, 1.1f, 6.3636f)]   // 6,6 px arredonda para 7
        [TestCase(1, 0.3f, 3.3333f)]   // nunca menos de 1 px de tela
        public void Pixel_art_scale_lands_on_whole_screen_pixels(int artPixel, float canvasScale, float expected)
        {
            float s = UiMath.PixelArtScale(artPixel, canvasScale);
            Assert.AreEqual(expected, s, 1e-3f);
            float screenPixels = s * canvasScale;
            Assert.AreEqual(Mathf.Round(screenPixels), screenPixels, Eps);
        }

        [Test]
        public void A_small_highlight_still_grows_one_screen_pixel()
        {
            // 6 px de repouso; 1,05 daria 6,3 e sumiria no arredondamento.
            Assert.AreEqual(7f, UiMath.PixelArtScale(6, 1f, 1f, 1.05f), Eps);
        }

        [Test]
        public void A_small_press_still_shrinks_one_screen_pixel()
        {
            Assert.AreEqual(5f, UiMath.PixelArtScale(6, 1f, 1f, 0.97f), Eps);
        }

        [Test]
        public void The_end_of_an_overshoot_counts_as_rest()
        {
            Assert.AreEqual(6f, UiMath.PixelArtScale(6, 1f, 1f, 1.002f), Eps);
        }

        [Test]
        public void A_grown_parent_keeps_whole_pixels_on_screen()
        {
            // Pai em 1,25 (fantasma arrastado): 6 x 1,25 = 7,5 vira 8 px de tela; a escala local compensa o pai.
            float s = UiMath.PixelArtScale(6, 1f, 1.25f);
            Assert.AreEqual(8f, s * 1.25f, Eps);
        }

        [Test]
        public void Zero_multiplier_hides_the_element()
        {
            Assert.AreEqual(0f, UiMath.PixelArtScale(6, 1f, 1f, 0f), Eps);
        }

        [TestCase(6, 1f, 1f / 6f)]
        [TestCase(6, 1.3333f, 1f / 8f)]
        [TestCase(6, 2f, 0.1f)]    // 12 px passaria do limite do URP
        [TestCase(1, 0.4f, 1f)]
        public void Pixelated_render_scale_matches_the_ui_pixel(int artPixel, float canvasScale, float expected)
        {
            Assert.AreEqual(expected, UiMath.PixelatedRenderScale(artPixel, canvasScale), Eps);
        }

        // O balão da entrega abre no dedo, mas encosta na borda em vez de sair da tela.
        [TestCase(0f, 0f, 0f, 0f)]          // no meio: fica onde o dedo tocou
        [TestCase(-490f, 0f, -400f, 0f)]    // perto da esquerda: encosta (meia largura de 100)
        [TestCase(490f, 950f, 400f, 900f)]  // canto de cima à direita
        [TestCase(0f, -990f, 0f, -900f)]    // embaixo
        public void A_popup_opens_at_the_finger_but_stays_on_screen(float px, float py, float ex, float ey)
        {
            var area = new Rect(-500f, -1000f, 1000f, 2000f);
            var p = UiMath.ClampInside(new Vector2(px, py), new Vector2(200f, 200f), new Vector2(0.5f, 0.5f), area);
            Assert.AreEqual(ex, p.x, Eps);
            Assert.AreEqual(ey, p.y, Eps);
        }

        [Test]
        public void A_popup_bigger_than_the_area_is_centred()
        {
            var p = UiMath.ClampInside(new Vector2(300f, 0f), new Vector2(2000f, 10f), new Vector2(0.5f, 0.5f), new Rect(-500f, -1000f, 1000f, 2000f));
            Assert.AreEqual(0f, p.x, Eps);
        }

        // O cenário da batalha é sorteado; a semente pode ser qualquer número.
        [TestCase(0, 3, 0)]
        [TestCase(7, 3, 1)]
        [TestCase(-1, 3, 2)]
        [TestCase(5, 0, 0)]
        public void PickIndex_always_lands_inside_the_list(int seed, int count, int expected)
        {
            Assert.AreEqual(expected, UiMath.PickIndex(seed, count));
        }

        [TestCase(0, 10, 0f)]
        [TestCase(5, 10, 0.5f)]
        [TestCase(14, 10, 1f)]     // mana acima do custo: cheia, não passa
        [TestCase(3, 0, 1f)]       // custo zero: cheia
        public void ManaFill_is_mana_over_cost_capped_at_full(int mana, int cost, float expected)
        {
            Assert.AreEqual(expected, UiMath.ManaFill(mana, cost), Eps);
        }
    }
}
