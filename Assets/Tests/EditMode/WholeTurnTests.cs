using NUnit.Framework;
using UnityEngine;
using Cubo.Domain;
using Cubo.View;

namespace Cubo.Tests
{
    // Fase 10: o giro do cubo inteiro (Move com Slice = -1), que a entrega da face usa.
    public class WholeTurnTests
    {
        private CubeView _cube;

        [SetUp] public void SetUp()
        {
            _cube = CubeRigFixture.Create();
            _cube.Build(5);
        }

        [TearDown] public void TearDown() { CubeRigFixture.Destroy(_cube); }

        [Test] public void Every_piece_goes_where_the_domain_sends_it()
        {
            var g = _cube.Geo;
            for (int face = 0; face < 4; face++)
            {
                var move = BattleEngine.TurnToFront(g.N, face);
                var turn = _cube.BeginTurn(move);
                turn.SetFraction(1f);
                for (int c = 0; c < g.Total; c++)
                {
                    int to = g.Dest(move.Axis, g.CellPos[c][move.Axis], move.Dir)[c];
                    Assert.AreEqual(0f, (_cube.Piece(c).transform.position - CubeLayout.HomePosition(g, to)).magnitude, 0.001f,
                        "peça " + c + " ao trazer a face " + face + " para a frente");
                }
                turn.End();
                Assert.IsTrue(_cube.AllAtHome());
            }
        }

        [Test] public void The_brought_face_ends_facing_the_camera()
        {
            var g = _cube.Geo;
            var move = BattleEngine.TurnToFront(g.N, 0);      // a da direita
            var turn = _cube.BeginTurn(move);
            turn.SetFraction(1f);
            var piece = _cube.Piece(0 * g.NN + 12);          // uma peça do meio da face 0
            Assert.Less(piece.transform.position.z, -1f, "a face da direita tinha de estar na frente (z negativo, virada para a câmera)");
            turn.End();
        }

        [Test] public void The_core_turns_with_the_pieces_and_comes_back()
        {
            var turn = _cube.BeginTurn(new Move(1, -1, 1));
            Assert.AreEqual(_cube.Pivot, _cube.Core.parent, "no giro inteiro o núcleo vai para o pivô");
            turn.SetFraction(0.5f);
            turn.End();
            Assert.AreEqual(_cube.Root, _cube.Core.parent);
            Assert.AreEqual(0f, Quaternion.Angle(_cube.Core.localRotation, Quaternion.identity), 0.01f);
            Assert.AreEqual(0, _cube.Pivot.childCount);
            Assert.IsTrue(_cube.AllAtHome());
        }
    }
}