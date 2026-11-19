using NUnit.Framework;
using UnityEngine;
using Cubo.View;

namespace Cubo.Tests
{
    public class CubeCameraTests
    {
        // Em cada tela, a face da frente (lado n) tem de caber com a margem pedida, na largura e na altura.
        [TestCase(4, 0.5f)] [TestCase(5, 0.5f)] [TestCase(8, 0.5f)] [TestCase(5, 1f)] [TestCase(5, 2f)]
        public void Front_face_fits_with_margin(int n, float aspect)
        {
            float d = CubeCamera.Distance(n, 60f, aspect, 1.15f);
            float half = n * 0.5f;
            float visibleH = (d - half) * Mathf.Tan(30f * Mathf.Deg2Rad);
            float visibleW = visibleH * aspect;
            Assert.GreaterOrEqual(visibleW, half * 1.15f - 0.001f);
            Assert.GreaterOrEqual(visibleH, half * 1.15f - 0.001f);
        }

        [Test] public void Frame_puts_the_camera_on_the_minus_z_axis_looking_at_the_cube()
        {
            var go = new GameObject("cam");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.aspect = 9f / 16f;
            CubeCamera.Frame(cam, 5);
            Assert.Less(cam.transform.position.z, -2.5f);
            Assert.AreEqual(0f, cam.transform.position.x, 0.001f);
            Assert.AreEqual(0f, cam.transform.position.y, 0.001f);
            Assert.AreEqual(1f, Vector3.Dot(cam.transform.forward, Vector3.forward), 0.001f);
            Object.DestroyImmediate(go);
        }
    }
}