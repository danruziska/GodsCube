using System.IO;
using System.Linq;
using Cubo.Domain;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Cubo.Core.Tests
{
    public class GeometryTests
    {
        private static JObject Reference(int n) =>
            (JObject)JObject.Parse(File.ReadAllText(Fixtures.Content("geometry")))[n.ToString()];

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void Cells_match_reference(int n)
        {
            var r = Reference(n);
            var g = new Geometry(n);
            var cells = (JArray)r["cells"];
            Assert.Equal((int)r["total"], g.Total);
            Assert.Equal(g.Total, cells.Count);
            for (int i = 0; i < g.Total; i++)
            {
                // Cada entrada: [face, x, y, z, u, v].
                Assert.True((int)cells[i][0] == g.CellFace[i],
                    $"n={n} peça {i}: face esperada {(int)cells[i][0]}, obtida {g.CellFace[i]}");
                for (int k = 0; k < 3; k++)
                    Assert.True((int)cells[i][1 + k] == g.CellPos[i][k],
                        $"n={n} peça {i}: eixo {k} esperado {(int)cells[i][1 + k]}, obtido {g.CellPos[i][k]}");
            }
        }

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void Perms_match_reference(int n)
        {
            var plus = (JArray)Reference(n)["spinPlus"];   // [eixo][fatia][antigo] = novo
            var g = new Geometry(n);
            for (int axis = 0; axis < 3; axis++)
                for (int slice = 0; slice < n; slice++)
                {
                    var expected = ((JArray)plus[axis][slice]).Select(t => (int)t).ToArray();
                    Assert.True(expected.SequenceEqual(g.Perms[axis][slice]),
                        $"n={n} eixo={axis} fatia={slice}: permutação diferente");
                }
        }

                [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void Rings_match_reference(int n)
        {
            var g = new Geometry(n);
            foreach (var ring in (JArray)Reference(n)["rings"])
            {
                int axis = (int)ring["axis"], slice = (int)ring["slice"];
                var expected = ((JArray)ring["cells"]).Select(t => (int)t).ToArray();
                Assert.True(expected.SequenceEqual(g.Rings[axis][slice]),
                    $"n={n} anel eixo={axis} fatia={slice}: peças diferentes");
            }
        }

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void Neighbors_match_reference(int n)
        {
            var g = new Geometry(n);
            var all = (JArray)Reference(n)["neighbors"];     // [peça] = [[vizinha, cross 0/1], ...]
            for (int i = 0; i < g.Total; i++)
            {
                var exp = (JArray)all[i];
                Assert.True(exp.Count == g.Neighbors[i].Length,
                    $"n={n} peça {i}: {exp.Count} vizinhas esperadas, {g.Neighbors[i].Length} obtidas");
                for (int k = 0; k < exp.Count; k++)
                {
                    Assert.True((int)exp[k][0] == g.Neighbors[i][k],
                        $"n={n} peça {i} vizinha {k}: esperada {(int)exp[k][0]}, obtida {g.Neighbors[i][k]}");
                    Assert.True(((int)exp[k][1] == 1) == g.NeighborCross[i][k],
                        $"n={n} peça {i} vizinha {k}: cross diferente");
                }
            }
        }
    }
}