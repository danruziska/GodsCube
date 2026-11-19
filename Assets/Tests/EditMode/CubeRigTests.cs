using NUnit.Framework;
using UnityEngine;
using Cubo.View;

namespace Cubo.Tests
{
    // O prefab CubeRig, montado no editor, faz o que o código espera dele?
    public class CubeRigTests
    {
        private CubeView _cube;

        [SetUp] public void SetUp() { _cube = CubeRigFixture.Create(); }
        [TearDown] public void TearDown() { CubeRigFixture.Destroy(_cube); }

        [Test] public void Rig_has_everything_wired_in_the_inspector()
        {
            Assert.IsNull(_cube.MissingReference(), "falta ligar no Inspector do CubeRig: " + _cube.MissingReference());
            Assert.IsTrue(_cube.Root.parent == _cube.transform, "Root deve ser filho direto do CubeRig");
            Assert.IsTrue(_cube.Pivot.parent == _cube.Root, "Pivot deve ser filho do Root");
            Assert.IsNotNull(_cube.Root.Find("Core"), "Core deve ser filho do Root");
        }

        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        public void Build_puts_one_piece_on_every_cell(int n)
        {
            _cube.Build(n);
            Assert.AreEqual(6 * n * n, _cube.PieceCount);
            for (int c = 0; c < _cube.PieceCount; c++)
            {
                var p = _cube.Piece(c);
                Assert.AreEqual(c, p.Cell);
                Assert.IsTrue(p.transform.parent == _cube.Root, "peça " + c + " fora do Root");
                Assert.AreEqual(0f, (p.transform.localPosition - CubeLayout.HomePosition(_cube.Geo, c)).magnitude, 0.0001f, "peça " + c);
            }
            Assert.AreEqual(n - 0.1f, _cube.Root.Find("Core").localScale.x, 0.0001f, "tamanho do núcleo");
        }

        [Test] public void Building_again_replaces_the_pieces_instead_of_piling_up()
        {
            _cube.Build(4);
            _cube.Build(5);
            Assert.AreEqual(150, _cube.PieceCount);
            Assert.AreEqual(150, _cube.Root.GetComponentsInChildren<PieceView>(true).Length);
        }

        [Test] public void Clear_removes_every_piece_and_keeps_core_and_pivot()
        {
            _cube.Build(4);
            _cube.Clear();
            Assert.AreEqual(0, _cube.PieceCount);
            Assert.AreEqual(0, _cube.Root.GetComponentsInChildren<PieceView>(true).Length);
            Assert.IsNotNull(_cube.Root.Find("Core"));
            Assert.IsNotNull(_cube.Pivot);
        }
    }
}