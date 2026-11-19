using NUnit.Framework;
using UnityEngine;
using Cubo.View;

namespace Cubo.Tests
{
    public class OrbitMathTests
    {
        private static void AssertNear(Vector3 expected, Vector3 actual, string msg) =>
            Assert.Less(Vector3.Distance(expected, actual), 1e-3f, msg + " (veio " + actual + ")");

        [TestCase(190f, -170f)]
        [TestCase(-190f, 170f)]
        [TestCase(360f, 0f)]
        [TestCase(540f, 180f)]
        [TestCase(-180f, 180f)]
        [TestCase(0f, 0f)]
        public void Wrap_keeps_angles_in_minus_180_to_180(float angle, float expected) =>
            Assert.AreEqual(expected, OrbitMath.Wrap(angle), 1e-3f);

        [TestCase(0f, false)]
        [TestCase(89f, false)]
        [TestCase(91f, true)]
        [TestCase(180f, true)]
        [TestCase(-120f, true)]
        [TestCase(-60f, false)]
        public void FacingBack_is_true_past_ninety_degrees(float yaw, bool expected) =>
            Assert.AreEqual(expected, OrbitMath.FacingBack(yaw));

        [TestCase(10f, 350f, -20f)]
        [TestCase(350f, 10f, 20f)]
        [TestCase(0f, 0f, 0f)]
        [TestCase(90f, -90f, 180f)]
        public void ShortestDelta_takes_the_short_way_round(float from, float to, float expected) =>
            Assert.AreEqual(expected, OrbitMath.ShortestDelta(from, to), 1e-3f);

        [Test]
        public void Pitch_never_passes_the_limit()
        {
            Assert.AreEqual(77f, OrbitMath.ClampPitch(100f));
            Assert.AreEqual(-77f, OrbitMath.ClampPitch(-100f));
            Assert.AreEqual(30f, OrbitMath.ClampPitch(30f));
            Assert.AreEqual(77f, OrbitMath.DragPitch(70f, 1000f, 1080f));
        }

        [Test]
        public void Drag_turns_the_same_angle_on_any_screen_width() =>
            Assert.AreEqual(105f, OrbitMath.DragYaw(0f, 540f, 1080f), 1e-3f);   // meia tela = 105 graus

        [Test]
        public void Dragging_right_moves_the_front_of_the_cube_to_the_right() =>
            AssertNear(Vector3.right, OrbitMath.Rotation(90f, 0f) * Vector3.back, "a face da frente deveria ir para a direita");

        [Test]
        public void Dragging_down_brings_the_top_toward_the_camera() =>
            AssertNear(Vector3.back, OrbitMath.Rotation(0f, 90f) * Vector3.up, "o topo deveria vir para a câmera");

        [Test]
        public void The_tilt_still_shows_the_top_when_facing_the_rival() =>
            AssertNear(Vector3.back, OrbitMath.Rotation(180f, 90f) * Vector3.up, "de costas, o topo também deveria vir para a câmera");

        [Test]
        public void The_home_view_is_the_old_fixed_tilt() =>
            AssertNear(Quaternion.Euler(-12f, 0f, 0f) * Vector3.up, OrbitMath.Rotation(0f, OrbitMath.HomePitch) * Vector3.up,
                "a vista inicial deveria ser a inclinação de 12 graus de antes");
    }
}