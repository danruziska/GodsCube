using UnityEngine;
using UnityEngine.UI;

namespace Cubo.View
{
    // Fundo em gradiente vertical, sem arte: pinta o retângulo do Image com as cores do Gradient (tempo 0 = topo,
    // tempo 1 = embaixo). Use num Image sem sprite; a Color do Image multiplica o gradiente (branco = como está).
    // Pixel art: ligue Hard Bands e o gradiente vira faixas de cor chapada (poucas, 3 a 6), como numa paleta curta.
    [RequireComponent(typeof(Image))]
    public sealed class VerticalGradient : BaseMeshEffect
    {
        [Tooltip("Tempo 0 = topo da tela, tempo 1 = embaixo.")]
        [SerializeField] private Gradient gradient = DefaultGradient();
        [Tooltip("Quantas faixas o retângulo ganha. Mais faixas = transição mais suave.")]
        [Range(2, 64)] [SerializeField] private int slices = 32;
        [Tooltip("Pixel art: cada faixa tem uma cor só, sem transição entre elas. Slices vira o número de faixas.")]
        [SerializeField] private bool hardBands;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || gradient == null) return;
            var rect = graphic.rectTransform.rect;
            var tint = graphic.color;
            vh.Clear();
            if (hardBands) { Bands(vh, rect, tint); return; }
            for (int i = 0; i <= slices; i++)
            {
                float t = i / (float)slices;
                float y = Mathf.Lerp(rect.yMax, rect.yMin, t);
                Color c = gradient.Evaluate(t) * tint;
                vh.AddVert(new Vector3(rect.xMin, y), c, Vector2.zero);
                vh.AddVert(new Vector3(rect.xMax, y), c, Vector2.zero);
                if (i == 0) continue;
                int k = (i - 1) * 2;
                vh.AddTriangle(k, k + 1, k + 2);
                vh.AddTriangle(k + 2, k + 1, k + 3);
            }
        }

        // Uma faixa = um retângulo com a cor do meio dela. Os vértices não são compartilhados, então não há mistura.
        private void Bands(VertexHelper vh, Rect rect, Color tint)
        {
            for (int i = 0; i < slices; i++)
            {
                float t0 = i / (float)slices, t1 = (i + 1) / (float)slices;
                float y0 = Mathf.Lerp(rect.yMax, rect.yMin, t0), y1 = Mathf.Lerp(rect.yMax, rect.yMin, t1);
                Color c = gradient.Evaluate((t0 + t1) * 0.5f) * tint;
                int k = i * 4;
                vh.AddVert(new Vector3(rect.xMin, y0), c, Vector2.zero);
                vh.AddVert(new Vector3(rect.xMax, y0), c, Vector2.zero);
                vh.AddVert(new Vector3(rect.xMin, y1), c, Vector2.zero);
                vh.AddVert(new Vector3(rect.xMax, y1), c, Vector2.zero);
                vh.AddTriangle(k, k + 1, k + 2);
                vh.AddTriangle(k + 2, k + 1, k + 3);
            }
        }

        private static Gradient DefaultGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color32(0x1E, 0x18, 0x15, 255), 0f),
                    new GradientColorKey(new Color32(0x2E, 0x26, 0x21, 255), 0.5f),
                    new GradientColorKey(new Color32(0x75, 0x64, 0x57, 255), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }
    }
}