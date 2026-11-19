using System;
using System.Collections;
using UnityEngine;

namespace Cubo.View
{
    // A vista livre do cubo: o jogador gira o cubo inteiro para ver as outras faces, arrastando ou com as setas
    // (um quarto de volta por toque). Só mexe na rotação do Root do cubo; o jogo não sabe que isto existe.
    public sealed class CubeOrbit : MonoBehaviour
    {
        [SerializeField] private CubeView cube;
        [SerializeField] private float flipSeconds = 0.52f;
        [SerializeField] private float stepSeconds = 0.3f;      // uma seta: um quarto de volta

        public event Action Changed;                        // a vista mudou (o botão troca de texto quando se passa da frente para trás)

        public float Yaw { get; private set; }
        public float Pitch { get; private set; } = OrbitMath.HomePitch;
        public bool Flipping { get; private set; }
        public bool FacingBack => OrbitMath.FacingBack(Yaw);

        private float _startYaw, _startPitch;

        // O dedo desceu fora do cubo: guarda de onde o arrasto parte.
        public void BeginDrag()
        {
            StopFlip();
            _startYaw = Yaw;
            _startPitch = Pitch;
        }

        // `delta`: quanto o dedo andou desde BeginDrag, em pixels (o y da tela cresce para cima).
        public void Drag(Vector2 delta)
        {
            float w = Mathf.Max(1f, Screen.width);
            Set(OrbitMath.DragYaw(_startYaw, delta.x, w), OrbitMath.DragPitch(_startPitch, -delta.y, w));
        }

        // De volta ao altar do jogador (ou ao do rival), com a inclinação de sempre.
        public void ResetView(bool back = false)
        {
            StopFlip();
            Set(OrbitMath.HomeYaw(back), OrbitMath.HomePitch);
        }

        // Do seu altar para o do rival, ou de volta, pelo caminho mais curto.
        public void Flip()
        {
            if (Flipping) return;
            StartCoroutine(FlipRoutine());
        }

        private IEnumerator FlipRoutine()
        {
            Flipping = true;
            float fromYaw = Yaw, fromPitch = Pitch;
            float delta = OrbitMath.ShortestDelta(fromYaw, OrbitMath.HomeYaw(!FacingBack));
            for (float t = 0f; t < flipSeconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / flipSeconds);
                Set(fromYaw + delta * k, Mathf.Lerp(fromPitch, OrbitMath.HomePitch, k));
                yield return null;
            }
            Set(fromYaw + delta, OrbitMath.HomePitch);
            Set(OrbitMath.Wrap(Yaw), Pitch);
            Flipping = false;
        }

        // As setas: um quarto de volta para o lado, ou inclinar para mostrar o topo ou a base (OrbitMath.Step).
        public void Step(int dx, int dy)
        {
            var (yaw, pitch) = OrbitMath.Step(Yaw, Pitch, dx, dy);
            StopFlip();
            StartCoroutine(TweenTo(yaw, pitch, stepSeconds));
        }

        // O botão de casa: volta à sua face, animado, pelo caminho mais curto.
        public void Home()
        {
            StartCoroutine(GoHome(flipSeconds));
        }

        // De volta à frente, animado. A apresentação espera isto antes de girar o cubo inteiro.
        public IEnumerator GoHome(float seconds)
        {
            StopFlip();
            return TweenTo(OrbitMath.HomeYawNear(Yaw), OrbitMath.HomePitch, seconds);
        }

        private IEnumerator TweenTo(float yaw, float pitch, float seconds)
        {
            Flipping = true;
            float fromYaw = Yaw, fromPitch = Pitch;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                Set(Mathf.Lerp(fromYaw, yaw, k), Mathf.Lerp(fromPitch, pitch, k));
                yield return null;
            }
            Set(OrbitMath.Wrap(yaw), pitch);
            Flipping = false;
        }

        private void StopFlip()
        {
            StopAllCoroutines();
            Flipping = false;
        }

        private void Set(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = pitch;
            if (cube != null && cube.Root != null) cube.Root.localRotation = OrbitMath.Rotation(yaw, pitch);
            Changed?.Invoke();
        }

        public string MissingReference() => cube == null ? "cube" : null;

        private void Awake()
        {
            var missing = MissingReference();
            if (missing != null) Debug.LogError("CubeOrbit: falta ligar no Inspector: " + missing, this);
        }
    }
}