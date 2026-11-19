using UnityEngine;
using Cubo.Domain;

namespace Cubo.View
{
    // Onde cada peça fica no espaço do Unity. É o único arquivo que conhece a diferença de eixos.
    public static class CubeLayout
    {
        // Centro da casa (x, y, z) do domínio, com o cubo centrado na origem.
        public static Vector3 ToUnity(int[] p, int n)
        {
            float c = (n - 1) * 0.5f;
            return new Vector3(p[0] - c, p[1] - c, -(p[2] - c));
        }

        // Para onde aponta a face (0 a 5), já em Unity.
        public static Vector3 Normal(int face)
        {
            var v = Vector3.zero;
            v[Geometry.FaceAxis[face]] = Geometry.FaceSign[face];
            v.z = -v.z;
            return v;
        }

        // Centro da peça, meio passo para fora da superfície.
        public static Vector3 HomePosition(Geometry g, int cell) =>
            ToUnity(g.CellPos[cell], g.N) + Normal(g.CellFace[cell]) * 0.5f;

        public static Quaternion HomeRotation(Geometry g, int cell) =>
            Quaternion.LookRotation(Normal(g.CellFace[cell]));

        // A peça (Piece.fbx) é uma placa de 0,9 x 0,9 com 0,1 de espessura; o +Z local aponta para fora do cubo.
        public const float PieceSize = 0.9f;
        public const float PieceThickness = 0.1f;

        // A imagem fica um fio fora da placa, para não piscar com ela.
        public const float ImageLift = 0.002f;

        // A imagem da frente fica rente à superfície de fora da placa.
        public const float FaceLift = PieceThickness * 0.5f + ImageLift;

        // Um sprite é visto de frente por quem olha para +Z (a frente dele aponta para -Z).
        // A frente da peça é vista de fora: meia volta em Y. Sem ela, a imagem apareceria espelhada.
        public static readonly Quaternion FaceRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

        // O núcleo partido durante o giro de uma fatia: a fatia que gira leva a sua laje de núcleo, e o resto
        // fica parado dos dois lados. Assim nenhuma peça que gira passa por dentro de núcleo parado (e some).
        // Tudo ao longo do eixo do giro, em unidades do cubo: (início, fim). Uma parte vazia tem fim <= início.
        public const float SlabGap = 0.01f;                 // folga entre a laje que gira e as paradas

        public static Vector2 SliceRange(int n, int axis, int slice)
        {
            float u = slice - (n - 1) * 0.5f;
            if (axis == 2) u = -u;                           // o z do domínio é invertido no Unity (ToUnity)
            return new Vector2(u - 0.5f, u + 0.5f);
        }

        public static void CoreSlabs(int n, int axis, int slice, float coreGap,
                                     out Vector2 lower, out Vector2 turning, out Vector2 upper)
        {
            float lo = -n * 0.5f + coreGap * 0.5f, hi = n * 0.5f - coreGap * 0.5f;
            var r = SliceRange(n, axis, slice);
            lower = new Vector2(lo, r.x - SlabGap);
            turning = new Vector2(Mathf.Max(lo, r.x), Mathf.Min(hi, r.y));
            upper = new Vector2(r.y + SlabGap, hi);
        }

        // Quantos quartos de volta (0 a 3) a imagem da frente das peças de uma face gira para ficar de pé na tela.
        // `viewUp`: o "para cima" de quem olha, no espaço do cubo (o Root). -1 = a face está de lado para quem olha
        // (não dá para saber; mantenha o que estava).
        public static int FaceQuarterTurns(int face, Vector3 viewUp)
        {
            Vector3 up = Quaternion.Inverse(Quaternion.LookRotation(Normal(face))) * viewUp;
            if (up.x * up.x + up.y * up.y < 1e-4f) return -1;
            float angle = Mathf.Atan2(-up.x, up.y) * Mathf.Rad2Deg;
            return ((Mathf.RoundToInt(angle / 90f) % 4) + 4) % 4;
        }

        // A rotação local da imagem da frente, já girada `quarterTurns` quartos de volta no plano da peça.
        public static Quaternion FaceRotationTurned(int quarterTurns) =>
            Quaternion.AngleAxis(90f * quarterTurns, Vector3.forward) * FaceRotation;

        // O brilho da face escolhida (entrega): um painel de luz um fio à frente das imagens, um pouco maior que a face.
        public const float GlowLift = 0.07f;
        public const float GlowMargin = 0.25f;

        // Onde a face do cubo termina dentro do sprite do brilho (0 = centro, 1 = borda do sprite).
        public static float GlowEdge(int n) => n / (n + 2f * GlowMargin);

        // Alpha do brilho num ponto do sprite, com u e v de -1 a 1: luz por toda a face, mais forte perto da borda dela,
        // e um esmaecer suave para fora, até sumir na borda do sprite.
        public static float GlowAlpha(float u, float v, float edge)
        {
            float d = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v));
            if (d >= 1f) return 0f;
            if (d >= edge) return Mathf.Clamp01((1f - d) / Mathf.Max(1e-4f, 1f - edge));
            return 0.55f + 0.45f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge - 0.2f, edge, d));
        }

        // A moldura dos altares (a sua face e a do rival): uma borda pixelada em volta da face, sempre acesa,
        // para achar a sua face de qualquer ângulo. Fica entre as imagens das peças e o brilho da entrega.
        public const float FrameLift = 0.06f;
        public const float FrameThickness = 0.16f;
        public const float FrameInset = 0.04f;          // quanto a borda avança sobre a face (tapa a fresta da beirada)
        public const int FrameBorder = 4;               // pixels da borda na textura

        // Largura e altura da moldura de um cubo n x n, de fora a fora.
        public static float FrameSize(int n) => n + 2f * (FrameThickness - FrameInset);

        // O tom de cada anel da borda, de fora (0) para dentro: contorno escuro, luz, meio-tom, contorno escuro.
        // Negativo = transparente (o miolo). A cor da moldura multiplica estes tons.
        public static float FrameTone(int ring)
        {
            switch (ring)
            {
                case 0: return 0.18f;
                case 1: return 1f;
                case 2: return 0.72f;
                case 3: return 0.18f;
                default: return -1f;
            }
        }

        // A escala que faz um sprite de qualquer tamanho (em unidades, pelo Pixels Per Unit dele) cobrir `size`.
        public static Vector3 FaceScale(Vector2 spriteSize, Vector2 size)
        {
            float x = spriteSize.x > 0f ? size.x / spriteSize.x : 1f;
            float y = spriteSize.y > 0f ? size.y / spriteSize.y : 1f;
            return new Vector3(x, y, 1f);
        }

        public static Vector3 SpinAxis(int axis)
        {
            var v = Vector3.zero; v[axis] = 1f; v.z = -v.z; return v;
        }

        public static float SpinAngle(int dir) => -90f * dir;

        // A face da frente (a 4, o altar do jogador) como a tela a mostra: uma linha por altura,
        // de cima para baixo, da esquerda para a direita. Serve para conferir a tela contra o domínio.
        public static string[] FrontRows(Geometry g, sbyte[] colors)
        {
            var rows = new string[g.N];
            for (int y = g.N - 1; y >= 0; y--)
            {
                var chars = new char[g.N];
                for (int x = 0; x < g.N; x++)
                {
                    int color = colors[4 * g.NN + x * g.N + y];
                    chars[x] = color < 0 ? '-' : (char)('0' + color);
                }
                rows[g.N - 1 - y] = new string(chars);
            }
            return rows;
        }
    }
}