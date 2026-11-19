using System.Collections.Generic;
using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    // A cascata: as peças que caem ou entram e formam uma mancha de CascadeMin (4) ou mais disparam sozinhas, uma por
    // vez, sem gastar a vez. As manchas menores ficam como opções para o próximo toque.
    public class CascadeTests
    {
        private static BattleEngine E => Kit.Engine;

        // Todas as casas que mudaram desde o toque (peças que caíram ou entraram), no grupo tocado e em cada elo.
        private static HashSet<int> Fresh(FireResult r)
        {
            var set = new HashSet<int>(r.Drop.Changed);
            foreach (var l in r.Chain) set.UnionWith(l.Drop.Changed);
            return set;
        }

        [Fact]
        public void After_a_tap_no_new_piece_is_left_in_an_unfired_group()
        {
            int chains = 0;
            for (uint seed = 1; seed <= 300; seed++)
            {
                var st = Kit.New(seed);
                var r = E.Fire(st, Side.Player, E.FindGroups(st)[0][0], new Mulberry32(seed));
                if (r.Chain.Count > 0) chains++;
                if (st.Winner != null) continue;
                var fresh = Fresh(r);
                foreach (var grp in E.GroupsIn(st.Colors, Geometry.For(st.N), Kit.Data.Rules.CascadeMin))
                    Assert.DoesNotContain(grp, fresh.Contains);
            }
            Assert.True(chains > 0, "nenhuma semente formou cascata: o teste não provou nada");
        }

        // Um par (ou trio) novo não dispara sozinho: fica no tabuleiro como opção para o próximo toque.
        [Fact]
        public void Small_new_patches_stay_on_the_board_as_options()
        {
            int kept = 0;
            for (uint seed = 1; seed <= 300; seed++)
            {
                var st = Kit.New(seed);
                var r = E.Fire(st, Side.Player, E.FindGroups(st)[0][0], new Mulberry32(seed));
                if (st.Winner != null) continue;
                var fresh = Fresh(r);
                foreach (var grp in E.FindGroups(st))
                    if (grp.Any(fresh.Contains)) { Assert.True(grp.Length < Kit.Data.Rules.CascadeMin); kept++; }
            }
            Assert.True(kept > 0);
        }

        [Fact]
        public void Each_link_is_worth_a_full_group_and_the_damage_adds_up()
        {
            for (uint seed = 1; seed <= 300; seed++)
            {
                var st = Kit.New(seed);
                int hp = st.Hp[1];
                var r = E.Fire(st, Side.Player, E.FindGroups(st)[0][0], new Mulberry32(seed));
                if (st.Winner != null) continue;
                Assert.Equal(hp - r.Damage - r.Chain.Sum(l => l.Damage), st.Hp[1]);
                foreach (var l in r.Chain)
                {
                    Assert.True(l.Cells.Length >= Kit.Data.Rules.MinGroup);
                    Assert.Equal(l.HpFrom[1] - l.Damage, l.Hp[1]);
                    Assert.Equal(l.Cells.Length, l.Gain.Gain[0].Pieces);
                }
            }
        }

        [Fact]
        public void The_chain_does_not_spend_the_move()
        {
            for (uint seed = 1; seed <= 300; seed++)
            {
                var st = Kit.New(seed);
                st.MovesLeft = 2;
                var r = E.Fire(st, Side.Player, E.FindGroups(st)[0][0], new Mulberry32(seed));
                if (r.Chain.Count == 0 || st.Winner != null) continue;
                Assert.Equal(1, st.MovesLeft);
                Assert.Equal(Phase.Match, st.Phase);
                return;
            }
            Assert.Fail("nenhuma semente formou cascata");
        }

        [Fact]
        public void Each_link_rolls_its_own_crit()
        {
            bool critLink = false, plainLink = false;
            for (uint seed = 1; seed <= 400 && !(critLink && plainLink); seed++)
            {
                var st = Kit.New(seed);
                st.Crit[0] = 0.5;
                var r = E.Fire(st, Side.Player, E.FindGroups(st)[0][0], new Mulberry32(seed));
                foreach (var l in r.Chain) { if (l.Crit) critLink = true; else plainLink = true; }
            }
            Assert.True(critLink && plainLink);
        }

        // Um grupo parado (numa coluna que nada perdeu, sem peça nova encostando) fica lá, esperando um toque.
        [Fact]
        public void A_group_that_was_already_there_does_not_fire_by_itself()
        {
            int kept = 0;
            for (uint seed = 1; seed <= 200; seed++)
            {
                var st = Kit.New(seed);
                Kit.NoGroups(st);
                Kit.Front(st, "444..", ".....", "....5", "....5", "....5");   // um trio em cima; o tocado, em pé à direita
                var other = new[] { Kit.Cell(st, 0, 4), Kit.Cell(st, 1, 4), Kit.Cell(st, 2, 4) };
                var r = E.Fire(st, Side.Player, Kit.Cell(st, 4, 0), new Mulberry32(seed));
                Assert.Equal(new[] { Kit.Cell(st, 4, 0), Kit.Cell(st, 4, 1), Kit.Cell(st, 4, 2) }, r.Cells);
                int n = st.N, front = Faces.Front * n * n;
                if (r.Chain.Any(l => l.Cells.Any(c => (c - front) / n <= 2))) continue;   // a cascata mexeu nas colunas dele
                Assert.All(other, c => Assert.Equal(4, st.Colors[c]));
                Assert.NotNull(E.GroupAt(st, other[0]));
                kept++;
            }
            Assert.True(kept > 0);
        }

        // Com uma cor só, toda peça nova forma grupo: a cascata segue até o rival cair.
        [Fact]
        public void The_chain_stops_when_the_rival_falls()
        {
            var st = Kit.New(3);
            st.NumColors = 1; st.Combo[0] = 0;
            Kit.FillFront(st);
            var r = E.Fire(st, Side.Player, E.FindGroups(st)[0][0], new Mulberry32(3));
            Assert.Equal(0, st.Hp[1]);
            Assert.Equal(Side.Player, st.Winner);
            Assert.NotEmpty(r.Chain);
            Assert.True(r.Chain.Count < BattleEngine.ChainSafetyStop);
            Assert.Equal(0, r.Chain.Last().Hp[1]);
            Assert.True(r.Chain.Take(r.Chain.Count - 1).All(l => l.Hp[1] > 0));
        }

        [Fact]
        public void An_endless_chain_hits_the_safety_stop()
        {
            var st = Kit.New(3);
            st.NumColors = 1; st.Combo[0] = 0;
            for (int h = 0; h < 3; h++) st.HeroHp[1][h] = st.HeroMax[1][h] = int.MaxValue / 8;
            BattleEngine.SyncHp(st, Side.Enemy);
            Kit.FillFront(st);
            var r = E.Fire(st, Side.Player, E.FindGroups(st)[0][0], new Mulberry32(3));
            Assert.Equal(BattleEngine.ChainSafetyStop, r.Chain.Count);
            Assert.Null(st.Winner);
        }

        [Fact]
        public void A_purge_can_start_a_chain_for_the_caster()
        {
            var st = Kit.New(4);
            int slot = Enumerable.Range(0, BattleEngine.Slots).First(k => st.Power(Side.Player, k).Kind == "purge");
            Kit.FillMana(st, Side.Player);
            st.NumColors = 1; st.Combo[0] = 0;     // as peças novas são todas da mesma cor: formam grupo
            int hp = st.Hp[1];
            var r = E.Cast(st, Side.Player, slot, new Mulberry32(4));
            Assert.NotNull(r);
            if (r.Cells.Count == 0) return;           // nada para varrer nesta frente (não deveria acontecer)
            Assert.NotEmpty(r.Chain);
            Assert.Equal(hp - r.Amount - r.Chain.Sum(l => l.HpFrom[1] - l.Hp[1]), st.Hp[1]);
        }

        // A sessão conta cada elo como um grupo: dispara (Link 1, 2...), vida e mana, peças novas com o
        // tabuleiro daquele momento. O último repinta o tabuleiro final.
        [Fact]
        public void The_session_reports_each_link_with_its_own_board()
        {
            for (uint seed = 5000; seed < 5300; seed++)
            {
                var (s, log) = Kit.Session(seed);
                int cell = E.FindGroups(s.State)[0][0];
                Assert.True(s.TryFire(cell));
                var fired = log.OfType<GroupFired>().ToList();
                var refills = log.OfType<CellsRefilled>().ToList();
                if (fired.Count < 2) continue;
                Assert.Equal(Enumerable.Range(0, fired.Count), fired.Select(f => f.Link));
                Assert.Equal(fired.Count, refills.Count);
                for (int k = 0; k < fired.Count; k++)
                {
                    Assert.Equal(fired[k].Cells.Length, refills[k].Cells.Length);   // entra uma nova para cada uma que saiu
                    Assert.NotNull(refills[k].Falls);
                    // o disparo vem antes das suas peças novas, e depois do refill anterior
                    Assert.True(log.IndexOf(fired[k]) < log.IndexOf(refills[k]));
                    if (k > 0) Assert.True(log.IndexOf(refills[k - 1]) < log.IndexOf(fired[k]));
                }
                Assert.Equal(s.State.Colors, refills.Last().Board);
                Assert.NotEqual(refills.Last().Board, refills[0].Board);
                return;
            }
            Assert.Fail("nenhuma semente formou cascata na sessão");
        }
    }
}
