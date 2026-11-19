using UnityEngine;

namespace Cubo.View
{
    // Contas da interface que não dependem de cena nem de quadro: área segura, região da câmera e frações.
    // Ficam separadas para serem testadas no EditMode sem montar Canvas nenhum.
    public static class UiMath
    {
        // A área segura do aparelho (em pixels) vira as âncoras (0 a 1) do painel que a segue.
        // Tela de tamanho zero (acontece no primeiro quadro em alguns aparelhos): a tela toda.
        public static void SafeAnchors(Rect safe, int screenWidth, int screenHeight, out Vector2 min, out Vector2 max)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                min = Vector2.zero;
                max = Vector2.one;
                return;
            }
            min = new Vector2(Mathf.Clamp01(safe.x / screenWidth), Mathf.Clamp01(safe.y / screenHeight));
            max = new Vector2(Mathf.Clamp01((safe.x + safe.width) / screenWidth), Mathf.Clamp01((safe.y + safe.height) / screenHeight));
        }

        // Dois cantos em pixels de tela (baixo-esquerda e cima-direita) viram um retângulo de 0 a 1,
        // que é a unidade de Camera.rect. Num Canvas Screen Space Overlay, os cantos de um RectTransform já são pixels de tela.
        public static Rect ToViewport(Vector2 bottomLeft, Vector2 topRight, int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0) return new Rect(0f, 0f, 1f, 1f);
            float x0 = Mathf.Clamp01(bottomLeft.x / screenWidth), y0 = Mathf.Clamp01(bottomLeft.y / screenHeight);
            float x1 = Mathf.Clamp01(topRight.x / screenWidth), y1 = Mathf.Clamp01(topRight.y / screenHeight);
            return new Rect(x0, y0, Mathf.Max(0f, x1 - x0), Mathf.Max(0f, y1 - y0));
        }

        // Quanto falta, em unidades do Canvas, para a borda da área segura ficar a pelo menos minUnits da borda da tela.
        // Cantos arredondados e telas curvas não entram no Screen.safeArea do Android: a margem mínima cobre isso.
        // safeInsetPixels é a distância (em pixels de tela) que o aparelho já reserva; canvasScale, o Canvas.scaleFactor.
        public static float MinInset(float safeInsetPixels, float canvasScale, float minUnits)
        {
            if (minUnits <= 0f) return 0f;
            if (canvasScale <= 0f) return minUnits;
            return Mathf.Max(0f, minUnits - Mathf.Max(0f, safeInsetPixels) / canvasScale);
        }

        // Um pulso suave e contínuo: 1 no começo (aceso), 0 na metade do ciclo, 1 de novo no fim. Período zero: parado aceso.
        // Põe um retângulo (tamanho e pivô) no ponto, mas sem sair da área: perto da borda ele encosta nela.
        // Se não couber, fica centrado na área.
        public static Vector2 ClampInside(Vector2 point, Vector2 size, Vector2 pivot, Rect area)
        {
            return new Vector2(Clamp1(point.x, size.x, pivot.x, area.xMin, area.xMax),
                               Clamp1(point.y, size.y, pivot.y, area.yMin, area.yMax));
        }

        private static float Clamp1(float p, float size, float pivot, float min, float max)
        {
            float lo = min + size * pivot, hi = max - size * (1f - pivot);
            return lo > hi ? (min + max) * 0.5f + size * (pivot - 0.5f) : Mathf.Clamp(p, lo, hi);
        }

        // Um índice de 0 a count-1 a partir de uma semente qualquer (negativa também). count <= 0 dá 0.
        public static int PickIndex(int seed, int count) => count <= 0 ? 0 : ((seed % count) + count) % count;

        // A barra de mana de um poder: 0 a 1. Custo zero = cheia.
        public static float ManaFill(int mana, int cost) => cost <= 0 ? 1f : Mathf.Clamp01(mana / (float)cost);

        public static float Pulse(float seconds, float period) =>
            period <= 0f ? 1f : 0.5f + 0.5f * Mathf.Cos(seconds / period * 2f * Mathf.PI);

        // Quanto de uma barra está cheio (0 a 1). Máximo zero ou negativo: vazia.
        public static float Fraction(float value, float max) => max <= 0f ? 0f : Mathf.Clamp01(value / max);

        // Pixel art: a escala local que faz cada pixel da arte ocupar um número inteiro de pixels de tela.
        // artPixel é o tamanho desejado de um pixel da arte em unidades da resolução de referência (1080x1920);
        // canvasScale é o Canvas.scaleFactor; parentScale, a escala do pai em relação ao Canvas (um cartão que cresce
        // em destaque); multiplier, o efeito pedido no próprio elemento (aperto, soco, entrada).
        // Em repouso arredonda para o inteiro mais perto. Crescendo ou encolhendo, anda pelo menos um pixel de tela
        // para o lado pedido: um destaque de 5% não some no arredondamento.
        public static float PixelArtScale(int artPixel, float canvasScale, float parentScale = 1f, float multiplier = 1f)
        {
            artPixel = Mathf.Max(1, artPixel);
            if (multiplier <= 0f) return 0f;
            if (canvasScale <= 0f || parentScale <= 0f) return artPixel * multiplier;
            int rest = Mathf.Max(1, Mathf.RoundToInt(artPixel * canvasScale));
            float relative = parentScale * multiplier;
            int screenPixels = rest;
            if (relative > 1f + PixelTolerance) screenPixels = Mathf.Max(rest + 1, Mathf.RoundToInt(rest * relative));
            else if (relative < 1f - PixelTolerance) screenPixels = Mathf.Min(rest - 1, Mathf.RoundToInt(rest * relative));
            return Mathf.Max(1, screenPixels) / (canvasScale * parentScale);
        }

        // O Render Scale do URP que faz um pixel do 3D virar artPixel unidades da referência, arredondado para pixels
        // de tela inteiros (igual à interface). O URP não aceita menos de 0,1: no máximo 10 px de tela por pixel.
        public static float PixelatedRenderScale(int artPixel, float canvasScale)
        {
            int screenPixels = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, artPixel) * Mathf.Max(0f, canvasScale)));
            return Mathf.Clamp(1f / screenPixels, 0.1f, 1f);
        }

        // Abaixo disso a escala conta como repouso: o fim de um OutBack (1,0004) não vira um pixel a mais piscando.
        private const float PixelTolerance = 0.005f;
    }
}