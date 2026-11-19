using System.Collections.Generic;
using System.Linq;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    // A gravidade na frente: as peças de cima caem para tapar o buraco e as novas entram no alto da coluna.
    public class GravityTests
    {
        private static BattleEngine E => Kit.Engine;

        private static BattleState Board()
        {
            var st = Kit.New(1);
            st.Combo[0] = 0;
            Kit.NoGroups(st);
            // coluna 0, de baixo para cima: 1 2 3 4 5 (a primeira linha é a de cima)
            Kit.Front(st, "5....", "4....", "3....", "2....", "1....");
            return st;
        }

        [Fact]
        public void The_pieces_above_a_hole_fall_into_it_and_a_new_one_enters_at_the_top()
        {
            var st = Board();
            var g = Geometry.For(st.N);
            var before = (sbyte[])st.Colors.Clone();
            var d = E.DropFront(st, g, Side.Player, new[] { Kit.Cell(st, 0, 1) }, new Mulberry32(7));
            Assert.Equal(new sbyte[] { 1, 3, 4, 5 }, Enumerable.Range(0, 4).Select(y => st.Colors[Kit.Cell(st, 0, y)]).ToArray());
            Assert.Equal(new[] { Kit.Cell(st, 0, 4) }, d.New);
            Assert.Equal(d.Fills[0], st.Colors[Kit.Cell(st, 0, 4)]);
            Assert.Equal(new[] { (2, 1), (3, 2), (4, 3) }, d.Falls.Select(f => (Y(st, f.From), Y(st, f.To))).ToArray());
            // as outras colunas não mexem
            for (int x = 1; x < st.N; x++)
                for (int y = 0; y < st.N; y++)
                    Assert.Equal(before[Kit.Cell(st, x, y)], st.Colors[Kit.Cell(st, x, y)]);
        }

        [Fact]
        public void A_standing_group_leaves_a_tall_hole_filled_from_the_top_bottom_first()
        {
            var st = Board();
            var g = Geometry.For(st.N);
            var cleared = new[] { Kit.Cell(st, 0, 0), Kit.Cell(st, 0, 1), Kit.Cell(st, 0, 2) };
            var d = E.DropFront(st, g, Side.Player, cleared, new Mulberry32(7));
            Assert.Equal(4, st.Colors[Kit.Cell(st, 0, 0)]);
            Assert.Equal(5, st.Colors[Kit.Cell(st, 0, 1)]);
            Assert.Equal(new[] { Kit.Cell(st, 0, 2), Kit.Cell(st, 0, 3), Kit.Cell(st, 0, 4) }, d.New);
            Assert.Equal(2, d.Falls.Count);
            Assert.Equal(new HashSet<int>(Enumerable.Range(0, 5).Select(y => Kit.Cell(st, 0, y))), new HashSet<int>(d.Changed));
        }

        [Fact]
        public void A_lying_group_drops_each_of_its_columns_by_one()
        {
            var st = Board();
            Kit.Front(st, ".....", ".....", ".....", ".....", ".000.");     // um trio deitado embaixo
            var above = Enumerable.Range(1, 3).Select(x => st.Colors[Kit.Cell(st, x, 1)]).ToArray();
            var r = E.Fire(st, Side.Player, Kit.Cell(st, 1, 0), new Mulberry32(3));
            Assert.Equal(3, r.Drop.New.Length);
            Assert.All(r.Drop.New, c => Assert.Equal(4, Y(st, c)));             // as novas entram na linha de cima
            Assert.Equal(12, r.Drop.Falls.Count);                               // 4 peças caem em cada uma das 3 colunas
            if (r.Chain.Count == 0)
                Assert.Equal(above, Enumerable.Range(1, 3).Select(x => st.Colors[Kit.Cell(st, x, 0)]).ToArray());
        }

        // Com gravidade, peças antigas que caem também formam grupos: aqui o 3 do alto da coluna 2 cai ao lado
        // de três 3s e fecha uma mancha de 4 (o tamanho da cascata), sem precisar de peça nova.
        [Fact]
        public void Pieces_that_fall_can_form_a_chain_by_themselves()
        {
            var st = Kit.New(1);
            st.Combo[0] = 0;
            Kit.NoGroups(st);
            Kit.Front(st, "02320",
                          "20402",
                          "02420",
                          "33402",
                          "30120");    // coluna 2, de baixo para cima: 1 4 4 4 3; nenhuma outra peça encosta numa igual
            var g = Geometry.For(st.N);
            var r = E.Fire(st, Side.Player, Kit.Cell(st, 2, 1), new Mulberry32(11));
            Assert.Equal(new[] { Kit.Cell(st, 2, 1), Kit.Cell(st, 2, 2), Kit.Cell(st, 2, 3) }, r.Cells);
            Assert.Contains(r.Drop.Falls, f => f.From == Kit.Cell(st, 2, 4) && f.To == Kit.Cell(st, 2, 1));
            Assert.NotEmpty(r.Chain);
            Assert.Equal(3, r.Chain[0].Color);
            Assert.Contains(Kit.Cell(st, 0, 1), r.Chain[0].Cells);
            Assert.Contains(Kit.Cell(st, 2, 1), r.Chain[0].Cells);
        }

        private static int Y(BattleState st, int cell) => (cell - Faces.Front * st.N * st.N) % st.N;
    }
}
