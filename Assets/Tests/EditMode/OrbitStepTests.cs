using NUnit.Framework;
using UnityEngine;
using Cubo.View;

namespace Cubo.Tests
{
    // Fase 10: as setas giram a vista um quarto de volta, como arrastar o dedo naquela direção.
    public class OrbitStepTests
    {
        private static void AssertNear(Vector3 expected, Vector3 actual, string msg) =>
            Assert.Less(Vector3.Distance(expected, actual), 1e-3f, msg + " (veio " + actual + ")");

        [Test]
        public void Right_arrow_moves_the_front_to_the_right_and_shows_the_left_face()
        {
            var (yaw, pitch) = OrbitMath.Step(0f, OrbitMath.HomePitch, 1, 0);
            Assert.AreEqual(90f, yaw, 1e-3f);
            Assert.AreEqual(OrbitMath.HomePitch, pitch, 1e-3f);
            AssertNear(Vector3.back, OrbitMath.Rotation(yaw, 0f) * Vector3.left, "a face da esquerda deveria ficar de frente para a câmera");
        }

        [Test]
        public void Side_steps_snap_to_quarter_turns_and_reset_the_tilt()
        {
            Assert.AreEqual(90f, OrbitMath.Step(37f, 50f, 1, 0).yaw, 1e-3f);       // 37 arredonda para 0
            Assert.AreEqual(-90f, OrbitMath.Step(0f, 50f, -1, 0).yaw, 1e-3f);
            Assert.AreEqual(OrbitMath.HomePitch, OrbitMath.Step(0f, 50f, -1, 0).pitch, 1e-3f);
        }

        [Test]
        public void Up_shows_the_bottom_down_shows_the_top_and_the_opposite_arrow_comes_back()
        {
            float p = OrbitMath.HomePitch;
            var up = OrbitMath.Step(0f, p, 0, 1);
            Assert.AreEqual(-OrbitMath.MaxPitch, up.pitch, 1e-3f);                 // a base vem para a câmera
            Assert.AreEqual(p, OrbitMath.Step(0f, up.pitch, 0, -1).pitch, 1e-3f);  // volta ao meio
            var down = OrbitMath.Step(0f, p, 0, -1);
            Assert.AreEqual(OrbitMath.MaxPitch, down.pitch, 1e-3f);                // o topo vem para a câmera
            Assert.AreEqual(p, OrbitMath.Step(0f, down.pitch, 0, 1).pitch, 1e-3f);
        }

        [Test]
        public void Home_takes_the_short_way_back_to_the_front()
        {
            Assert.AreEqual(360f, OrbitMath.HomeYawNear(350f), 1e-3f);    // de 350 sobe 10, não desce 350
            Assert.AreEqual(0f, OrbitMath.HomeYawNear(-80f), 1e-3f);
            Assert.AreEqual(0f, OrbitMath.HomeYawNear(170f), 1e-3f);
        }
    }
}