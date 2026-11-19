using System.IO;
using System.Linq;
using Cubo.Domain;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Cubo.Core.Tests
{
    public class SpinsTests
    {
        private static readonly JArray Cases =
            (JArray)JObject.Parse(File.ReadAllText(Fixtures.Vector("spins.json")))["cases"];

        // Um caso de teste por índice, para o nome do teste dizer qual caso falhou.
        public static TheoryData<int> Indices()
        {
            var d = new TheoryData<int>();
            for (int i = 0; i < Cases.Count; i++) d.Add(i);
            return d;
        }

        // "0123..." vira sbyte[] (um dígito por peça) e volta.
        public static sbyte[] Parse(string s) => s.Select(ch => (sbyte)(ch - '0')).ToArray();
        public static string Str(sbyte[] c) => new string(c.Select(x => (char)('0' + x)).ToArray());

        [Theory]
        [MemberData(nameof(Indices))]
        public void Spin_matches_reference(int i)
        {
            var c = Cases[i];
            int n = (int)c["n"];
            int axis = (int)c["move"]["axis"], slice = (int)c["move"]["slice"], dir = (int)c["move"]["dir"];
            string expected = (string)c["after"];

            string got = Str(new Geometry(n).Apply(Parse((string)c["before"]), axis, slice, dir));

            Assert.True(expected == got,
                $"caso {i}: n={n} axis={axis} slice={slice} dir={dir}\n esperado {expected}\n obtido   {got}");
        }

        // Não precisa de vetor: girar e desgirar volta ao início, e quatro giros iguais também.
        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void Spin_then_inverse_restores_board(int n)
        {
            var g = new Geometry(n);
            var start = Enumerable.Range(0, g.Total).Select(k => (sbyte)(k % 7)).ToArray();
            for (int axis = 0; axis < 3; axis++)
                for (int slice = 0; slice < n; slice++)
                {
                    var forward = g.Apply(start, axis, slice, 1);
                    Assert.Equal(start, g.Apply(forward, axis, slice, -1));

                    var four = start;
                    for (int r = 0; r < 4; r++) four = g.Apply(four, axis, slice, 1);
                    Assert.Equal(start, four);
                }
        }
    }
}