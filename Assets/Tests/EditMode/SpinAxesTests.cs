using NUnit.Framework;
using UnityEngine;
using Cubo.Domain;
using Cubo.View;

namespace Cubo.Tests
{
    public class SpinAxesTests
    {
        // Girar a peça k no Unity, com o eixo e o ângulo de CubeLayout, tem de levá-la ao lugar de origem
        // da peça que o domínio manda para o seu lugar (Dest). Vale para cada fatia, eixo e direção.
        [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)]
        public void Turning_a_piece_lands_it_where_the_domain_says(int n)
        {
            var g = Geometry.For(n);
            for (int axis = 0; axis < 3; axis++)
                for (int slice = 0; slice < n; slice++)
                    for (int dir = -1; dir <= 1; dir += 2)
                    {
                        var q = Quaternion.AngleAxis(CubeLayout.SpinAngle(dir), CubeLayout.SpinAxis(axis));
                        var dest = g.Dest(axis, slice, dir);
                        for (int k = 0; k < g.Total; k++)
                        {
                            if (g.CellPos[k][axis] != slice) continue;
                            var expected = CubeLayout.HomePosition(g, dest[k]);
                            var got = q * CubeLayout.HomePosition(g, k);
                            Assert.AreEqual(0f, (expected - got).magnitude, 0.001f,
                                "n=" + n + " eixo=" + axis + " fatia=" + slice + " dir=" + dir + " peça=" + k);
                        }
                    }
        }
    }
}