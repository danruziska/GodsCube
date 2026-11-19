using NUnit.Framework;
using UnityEngine;
using Cubo.Domain;
using Cubo.View;

namespace Cubo.Tests
{
    public class SliceTurnTests
    {
        private CubeView _cube;

        [SetUp] public void SetUp()
        {
            _cube = CubeRigFixture.Create();      // o prefab CubeRig de verdade
            _cube.Build(4);
        }

        [TearDown] public void TearDown() { CubeRigFixture.Destroy(_cube); }

        [Test] public void A_full_turn_puts_each_piece_where_the_domain_sends_it()
        {
            var g = _cube.Geo;
            foreach (var move in new[] { new Move(0, 1, 1), new Move(1, 0, -1), new Move(2, 3, 1) })
            {
                var dest = g.Dest(move.Axis, move.Slice, move.Dir);
                var turn = _cube.BeginTurn(move);
                turn.SetFraction(1f);
                for (int c = 0; c < g.Total; c++)
                {
                    var expected = g.CellPos[c][move.Axis] == move.Slice
                        ? CubeLayout.HomePosition(g, dest[c])     // a fatia foi para onde o domínio manda
                        : CubeLayout.HomePosition(g, c);          // o resto não se mexeu
                    Assert.AreEqual(0f, (_cube.Piece(c).transform.position - expected).magnitude, 0.001f,
                        "peça " + c + " no giro " + move.Axis + "/" + move.Slice + "/" + move.Dir);
                }
                turn.End();
                Assert.IsTrue(_cube.AllAtHome());
            }
        }

        [Test] public void A_cancelled_turn_leaves_everything_at_home()
        {
            var turn = _cube.BeginTurn(new Move(1, 2, -1));
            turn.SetFraction(0.5f);
            turn.End();
            Assert.IsTrue(_cube.AllAtHome());
            Assert.AreEqual(0, _cube.Pivot.childCount, "o pivô tem de ficar vazio para o próximo giro");
            Assert.AreEqual(0f, Quaternion.Angle(_cube.Pivot.localRotation, Quaternion.identity), 0.01f);
        }

        [Test] public void Only_the_slice_moves_during_a_turn()
        {
            var g = _cube.Geo;
            var turn = _cube.BeginTurn(new Move(0, 0, 1));
            turn.SetFraction(0.5f);
            for (int c = 0; c < g.Total; c++)
                if (g.CellPos[c][0] != 0)
                    Assert.AreEqual(0f, (_cube.Piece(c).transform.position - CubeLayout.HomePosition(g, c)).magnitude, 0.001f);
            turn.End();
        }
    }
}