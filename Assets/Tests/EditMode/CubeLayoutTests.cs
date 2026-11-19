using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Cubo.Domain;
using Cubo.View;

namespace Cubo.Tests
{
    public class CubeLayoutTests
    {
        private static readonly Geometry G = Geometry.For(5);

        private static void AssertVec(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f);
            Assert.AreEqual(expected.y, actual.y, 0.001f);
            Assert.AreEqual(expected.z, actual.z, 0.001f);
        }

        [Test] public void Every_piece_has_its_own_place()
        {
            var places = new HashSet<Vector3Int>();
            for (int c = 0; c < G.Total; c++)
                places.Add(Vector3Int.RoundToInt(CubeLayout.HomePosition(G, c) * 2f));   // múltiplos de 0,5
            Assert.AreEqual(G.Total, places.Count);
        }

        [Test] public void Pieces_sit_on_the_surface()
        {
            for (int c = 0; c < G.Total; c++)
            {
                var p = CubeLayout.HomePosition(G, c);
                Assert.AreEqual(2.5f, Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z)), 0.001f);
            }
        }

        [Test] public void Faces_point_where_the_player_expects()
        {
            AssertVec(new Vector3(0, 0, -1), CubeLayout.Normal(4));    // frente: o altar do jogador, voltado à câmera
            AssertVec(new Vector3(1, 0, 0), CubeLayout.Normal(0));     // direita
            AssertVec(new Vector3(0, 1, 0), CubeLayout.Normal(2));     // topo
            for (int c = 4 * G.NN; c < 5 * G.NN; c++)
                Assert.AreEqual(-2.5f, CubeLayout.HomePosition(G, c).z, 0.001f);
        }

        [Test] public void Pieces_face_outward()
        {
            for (int c = 0; c < G.Total; c++)
                AssertVec(CubeLayout.Normal(G.CellFace[c]), CubeLayout.HomeRotation(G, c) * Vector3.forward);
        }

        [Test] public void Spin_axis_and_angle()
        {
            AssertVec(new Vector3(1, 0, 0), CubeLayout.SpinAxis(0));
            AssertVec(new Vector3(0, 0, -1), CubeLayout.SpinAxis(2));
            Assert.AreEqual(-90f, CubeLayout.SpinAngle(1));
            Assert.AreEqual(90f, CubeLayout.SpinAngle(-1));
        }

        [Test] public void Front_rows_read_x_to_the_right()
        {
            var colors = new sbyte[G.Total];
            for (int x = 0; x < 5; x++) for (int y = 0; y < 5; y++) colors[4 * G.NN + x * 5 + y] = (sbyte)x;
            Assert.AreEqual(new[] { "01234", "01234", "01234", "01234", "01234" }, CubeLayout.FrontRows(G, colors));
        }

        [Test] public void Front_rows_read_y_upward()
        {
            var colors = new sbyte[G.Total];
            for (int x = 0; x < 5; x++) for (int y = 0; y < 5; y++) colors[4 * G.NN + x * 5 + y] = (sbyte)y;
            Assert.AreEqual(new[] { "44444", "33333", "22222", "11111", "00000" }, CubeLayout.FrontRows(G, colors));
        }

        [TestCase(0.32f, 0.32f, 0.9f, 0.9f, 2.8125f, 2.8125f)]   // 32 px a 100 Pixels Per Unit
        [TestCase(1f, 1f, 0.9f, 0.9f, 0.9f, 0.9f)]               // 32 px a 32 Pixels Per Unit
        [TestCase(0.64f, 0.32f, 0.9f, 0.9f, 1.40625f, 2.8125f)]  // imagem mais larga que alta: cobre a peça igual
        [TestCase(0f, 0f, 0.9f, 0.9f, 1f, 1f)]                   // sprite vazio: não estoura
        public void A_face_image_of_any_size_covers_the_piece(float w, float h, float tw, float th, float sx, float sy)
        {
            AssertVec(new Vector3(sx, sy, 1f), CubeLayout.FaceScale(new Vector2(w, h), new Vector2(tw, th)));
        }

        [Test] public void The_face_image_sits_just_outside_the_piece_and_faces_out()
        {
            Assert.Greater(CubeLayout.FaceLift, CubeLayout.PieceThickness * 0.5f);   // a placa vai até 0,05
            Assert.Less(CubeLayout.FaceLift, 0.06f);
            AssertVec(Vector3.forward, CubeLayout.FaceRotation * Vector3.back);       // a frente do sprite olha para fora
            AssertVec(Vector3.up, CubeLayout.FaceRotation * Vector3.up);
        }

        // No giro de uma fatia, o núcleo vira três lajes: a da fatia (gira) e as paradas dos dois lados, sem se cruzarem.
        [TestCase(5, 0, 0)]
        [TestCase(5, 1, 2)]
        [TestCase(5, 2, 4)]
        [TestCase(5, 2, 0)]
        [TestCase(3, 1, 1)]
        public void The_core_splits_so_the_turning_slice_carries_its_own_slab(int n, int axis, int slice)
        {
            const float gap = 0.1f;
            CubeLayout.CoreSlabs(n, axis, slice, gap, out var lower, out var turning, out var upper);
            var r = CubeLayout.SliceRange(n, axis, slice);
            Assert.AreEqual(1f, r.y - r.x, 0.0001f);
            // a laje que gira é a fatia (cortada só na casca de fora do núcleo)
            Assert.AreEqual(Mathf.Max(r.x, -n * 0.5f + gap * 0.5f), turning.x, 0.0001f);
            Assert.AreEqual(Mathf.Min(r.y, n * 0.5f - gap * 0.5f), turning.y, 0.0001f);
            // as partes paradas não entram na faixa da fatia
            Assert.LessOrEqual(lower.y, turning.x);
            Assert.GreaterOrEqual(upper.x, turning.y);
            // juntas cobrem o núcleo todo (fora as folguinhas)
            float covered = Mathf.Max(0f, lower.y - lower.x) + (turning.y - turning.x) + Mathf.Max(0f, upper.y - upper.x);
            Assert.AreEqual(n - gap, covered, 2f * CubeLayout.SlabGap + 0.0001f);
        }

        [Test] public void Slice_ranges_follow_the_pieces()
        {
            // a casa x = 0 do domínio fica na esquerda (-2 no Unity); o z do domínio é invertido
            for (int c = 0; c < G.Total; c++)
                for (int axis = 0; axis < 3; axis++)
                {
                    var r = CubeLayout.SliceRange(5, axis, G.CellPos[c][axis]);
                    float p = CubeLayout.HomePosition(G, c)[axis];
                    if (Mathf.Abs(Mathf.Abs(p) - 2.5f) < 0.001f) continue;       // a peça está na face perpendicular a este eixo
                    Assert.That(p, Is.InRange(r.x, r.y), "peça " + c + ", eixo " + axis);
                }
        }

        [Test] public void Face_images_turn_to_stand_upright_for_the_viewer()
        {
            var ups = new[]
            {
                Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left,
                new Vector3(0.3f, 0.9f, 0.3f).normalized, new Vector3(-0.7f, 0.2f, 0.68f).normalized
            };
            for (int f = 0; f < 6; f++)
                foreach (var viewUp in ups)
                {
                    int k = CubeLayout.FaceQuarterTurns(f, viewUp);
                    Vector3 n = CubeLayout.Normal(f);
                    Vector3 onFace = viewUp - Vector3.Dot(viewUp, n) * n;
                    if (onFace.sqrMagnitude < 1e-4f) { Assert.AreEqual(-1, k, "face " + f + " vista de lado"); continue; }
                    Assert.That(k, Is.InRange(0, 3));
                    // o "para cima" da imagem, no espaço do cubo, aponta para o "para cima" de quem olha (até 45°)
                    Vector3 imageUp = Quaternion.LookRotation(n) * (CubeLayout.FaceRotationTurned(k) * Vector3.up);
                    Assert.LessOrEqual(Vector3.Angle(imageUp, onFace), 45.01f, "face " + f + ", olhando com " + viewUp);
                    // e a imagem continua virada para fora
                    AssertVec(n, Quaternion.LookRotation(n) * (CubeLayout.FaceRotationTurned(k) * Vector3.back));
                }
        }

        [Test] public void The_altar_frame_hugs_the_face_edge_between_the_images_and_the_glow()
        {
            float s = CubeLayout.FrameSize(5);
            float inner = s * 0.5f - CubeLayout.FrameThickness;
            Assert.Less(inner, 2.5f);                                                   // tapa a fresta da beirada
            Assert.Greater(inner, 2.5f - (1f - CubeLayout.PieceSize));                 // sem cobrir os símbolos
            Assert.Greater(s * 0.5f, 2.5f);                                             // e passa um pouco da face
            Assert.Greater(CubeLayout.FrameLift, CubeLayout.FaceLift);
            Assert.Less(CubeLayout.FrameLift, CubeLayout.GlowLift);
            Assert.Less(CubeLayout.FrameTone(0), CubeLayout.FrameTone(1));             // contorno escuro, luz por dentro
            Assert.Less(CubeLayout.FrameTone(CubeLayout.FrameBorder), 0f);             // o miolo é transparente
        }

        [Test] public void The_face_glow_lights_the_whole_face_brighter_at_its_rim_and_fades_out()
        {
            float edge = CubeLayout.GlowEdge(5);
            Assert.AreEqual(5f / 5.5f, edge, 0.0001f);                                  // a face ocupa 5 de 5,5 unidades
            float center = CubeLayout.GlowAlpha(0f, 0f, edge);
            float rim = CubeLayout.GlowAlpha(edge, 0f, edge);
            Assert.Greater(center, 0.4f);                                               // a face inteira acende
            Assert.AreEqual(1f, rim, 0.0001f);                                          // mais forte na borda da face
            Assert.Greater(rim, center);
            Assert.Less(CubeLayout.GlowAlpha((1f + edge) * 0.5f, 0f, edge), rim);      // esmaece para fora
            Assert.AreEqual(0f, CubeLayout.GlowAlpha(1f, 0.3f, edge), 0.0001f);        // some na borda do sprite
            Assert.AreEqual(CubeLayout.GlowAlpha(0.3f, -0.6f, edge), CubeLayout.GlowAlpha(-0.6f, 0.3f, edge), 0.0001f);
        }
    }
}