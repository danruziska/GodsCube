using UnityEngine;

namespace Cubo.View
{
    // As contas da vista livre do cubo, sem cena. Ângulos em graus: yaw gira em volta da vertical
    // (0 = o seu altar de frente, 180 = o altar do rival), pitch inclina para mostrar o topo.
    public static class OrbitMath
    {
        public const float HomePitch = 12f;                     // a inclinação de sempre: um pouco do topo à mostra
        public const float MaxPitch = 77f;                      // quase na vertical, mas nunca de cabeça para baixo
        public const float DegreesPerScreenWidth = 210f;        // arrastar de lado a lado da tela gira 210 graus, em qualquer aparelho

        public static float ClampPitch(float pitch) => Mathf.Clamp(pitch, -MaxPitch, MaxPitch);

        // Leva qualquer ângulo para o intervalo (-180, 180].
        public static float Wrap(float degrees)
        {
            float d = degrees % 360f;
            if (d > 180f) d -= 360f;
            else if (d <= -180f) d += 360f;
            return d;
        }

        // O caminho mais curto de um ângulo até outro, com sinal.
        public static float ShortestDelta(float from, float to) => Wrap(to - from);

        // Vendo o altar do rival? (a face de trás está virada para a câmera)
        public static bool FacingBack(float yaw) => Mathf.Abs(Wrap(yaw)) > 90f;

        public static float HomeYaw(bool back) => back ? 180f : 0f;

        // O dedo andou dxPx para a direita: a frente do cubo anda junto. Idem para baixo e o topo.
        public static float DragYaw(float startYaw, float dxPx, float screenWidth) =>
            startYaw + dxPx / screenWidth * DegreesPerScreenWidth;

        public static float DragPitch(float startPitch, float dyDownPx, float screenWidth) =>
            ClampPitch(startPitch + dyDownPx / screenWidth * DegreesPerScreenWidth);

        // As setas (Fase 10): um quarto de volta de cada vez, como arrastar o dedo nessa direção. `dx` +1 = seta
        // para a direita (a frente vai para a direita e a face da esquerda aparece), -1 = esquerda; `dy` +1 = seta
        // para cima (a frente sobe e aparece a de baixo), -1 = para baixo (aparece a de cima). De lado, a vista volta
        // para a inclinação de sempre; para cima ou para baixo, ela vai até quase a vertical ou volta ao meio.
        public static (float yaw, float pitch) Step(float yaw, float pitch, int dx, int dy)
        {
            if (dx != 0) return (Mathf.Round(yaw / 90f) * 90f + 90f * Mathf.Sign(dx), HomePitch);
            if (dy > 0) return (yaw, pitch > HomePitch + 1f ? HomePitch : -MaxPitch);
            if (dy < 0) return (yaw, pitch < HomePitch - 1f ? HomePitch : MaxPitch);
            return (yaw, pitch);
        }

        // De volta à frente pelo caminho mais curto: o yaw equivalente a 0 mais perto do atual.
        public static float HomeYawNear(float yaw) => yaw + ShortestDelta(yaw, 0f);

        // Primeiro gira em volta da vertical do cubo, depois inclina em relação à câmera. Nessa ordem a
        // inclinação mostra o topo também quando se olha o altar do rival.
        public static Quaternion Rotation(float yaw, float pitch) =>
            Quaternion.Euler(-pitch, 0f, 0f) * Quaternion.Euler(0f, -yaw, 0f);
    }
}