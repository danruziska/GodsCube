using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    // O crítico como num RPG (Fase 10): cada ataque sorteia; começa em 5% para os dois lados; cartas, poderes e
    // runas mudam a chance (por alguns turnos, só nesta jogada ou até o fim da batalha). Não há peça crítica.
    public class CritTests
    {
        private static readonly BattleEngine E = Kit.Engine;

        [Fact]
        public void Every_attack_rolls_about_five_percent_for_either_side()
        {
            var st = Kit.New(1);
            var rng = new Mulberry32(99);
            int hits = 0, enemy = 0, n = 6000;
            for (int i = 0; i < n; i++) { if (E.RollCrit(st, Side.Player, rng)) hits++; if (E.RollCrit(st, Side.Enemy, rng)) enemy++; }
            Assert.InRange(hits / (double)n, 0.04, 0.06);
            Assert.InRange(enemy / (double)n, 0.04, 0.06);
        }

        [Fact]
        public void Card_effects_raise_the_chance_for_some_turns_or_until_the_end_and_never_past_the_cap()
        {
            var st = Kit.New(2);
            E.AddFx(st, "crit", 0.3, 1, true);              // só neste turno
            E.AddFx(st, "crit", 0.1, null, true);           // até o fim da batalha
            Assert.Equal(0.45, E.CritChance(st, Side.Player), 6);
            E.TickFx(st, Side.Player);
            Assert.Equal(0.15, E.CritChance(st, Side.Player), 6);
            E.AddFx(st, "crit", 0.9, null, true);
            Assert.Equal(Kit.Data.Rules.CritMax, E.CritChance(st, Side.Player));
            Assert.Equal(0.05, E.CritChance(st, Side.Enemy), 6);   // o do rival não muda
        }

        [Fact]
        public void The_rivals_crit_effects_live_in_its_own_list_and_count_its_own_turns()
        {
            var st = Kit.New(3);
            E.AddFx(st, Side.Enemy, "crit", 0.2, 2, true);
            Assert.Equal(0.25, E.CritChance(st, Side.Enemy), 6);
            E.TickFx(st, Side.Player);
            Assert.Equal(0.25, E.CritChance(st, Side.Enemy), 6);
            E.TickFx(st, Side.Enemy);
            E.TickFx(st, Side.Enemy);
            Assert.Equal(0.05, E.CritChance(st, Side.Enemy), 6);
        }

        [Fact]
        public void A_sure_crit_wins_the_roll_once_and_the_roll_always_uses_one_number()
        {
            var st = Kit.New(4);
            st.Fx.SetNextCrit(true);
            var a = new Mulberry32(7); var b = new Mulberry32(7);
            Assert.True(E.RollCrit(st, Side.Player, a));
            Assert.False(st.Fx.NextCrit == true);
            st.Crit[0] = 0;
            Assert.False(E.RollCrit(st, Side.Player, a));
            b.Next(); b.Next();
            Assert.Equal(b.State, a.State);
        }

        [Fact]
        public void The_roll_decides_the_fire_and_a_crit_doubles_its_damage()
        {
            int Damage(bool sure, out bool crit)
            {
                var st = Kit.New(5);
                Kit.NoGroups(st);
                Kit.Front(st, "555..");
                st.Crit[0] = 0;
                if (sure) st.Fx.SetNextCrit(true);
                var r = E.Fire(st, Side.Player, Kit.Cell(st, 0, 4), new Mulberry32(1));
                crit = r.Crit;
                return r.Damage;
            }
            int plain = Damage(false, out bool c0);
            int doubled = Damage(true, out bool c1);
            Assert.False(c0);
            Assert.True(c1);
            Assert.Equal(plain * 2, doubled);
            var high = Kit.New(5);
            high.Crit[0] = 0.9;                                                     // a base também respeita o teto
            Assert.Equal(Kit.Data.Rules.CritMax, E.CritChance(high, Side.Player));
        }

        [Fact]
        public void Damage_powers_crit_only_when_they_say_so()
        {
            int crits = 0, n = 2000;
            for (uint seed = 1; seed <= n; seed++)
            {
                var st = Kit.New(seed);
                Kit.FillMana(st, Side.Player);
                var r = E.Cast(st, Side.Player, 2, new Mulberry32(seed));          // Lança: 10 de dano, 30% de crítico
                int lance = JsMath.RoundInt(10 * E.ElementMult(st, Side.Player, 2, r.Target));   // a Lança é de fogo
                Assert.Equal(System.Math.Min(r.Crit ? JsMath.RoundInt(20 * E.ElementMult(st, Side.Player, 2, r.Target)) : lance, st.HeroMax[1][r.Target]), r.Amount);
                if (r.Crit) crits++;
            }
            Assert.InRange(crits / (double)n, 0.26, 0.34);
            for (uint seed = 1; seed <= 200; seed++)
            {
                var st = Kit.New(seed);
                st.Turn = Side.Enemy;
                Kit.FillMana(st, Side.Enemy);
                var r = E.Cast(st, Side.Enemy, 3, new Mulberry32(seed));           // Catapulta: dano sem crítico
                Assert.False(r.Crit);
                Assert.Equal(JsMath.RoundInt(10 * E.ElementMult(st, Side.Enemy, st.Power(Side.Enemy, 3).Color, r.Target)), r.Amount);
            }
        }

        [Fact]
        public void Crit_powers_raise_the_chance_until_the_end_or_make_the_next_attack_sure()
        {
            var st = Kit.New(6, 5, Kit.Tricky);                                     // Joker, Cavaleiro, Bispo
            Kit.FillMana(st, Side.Player);
            E.Cast(st, Side.Player, 3, new Mulberry32(1));                          // Grito de guerra: +10%
            Assert.Equal(0.15, E.CritChance(st, Side.Player), 6);
            E.TickFx(st, Side.Player);
            Assert.Equal(0.15, E.CritChance(st, Side.Player), 6);                   // até o fim da batalha
            E.Cast(st, Side.Player, 0, new Mulberry32(1));                          // Ás na manga
            Assert.True(st.Fx.NextCrit == true);
            st.Crit[0] = 0; E.AddFx(st, "crit", -0.1, null, true);
            Kit.FillFront(st, 4);                                                   // um ataque de verdade (um par não é ataque)
            int cell = E.FindGroups(st)[0][0];
            Assert.True(E.Fire(st, Side.Player, cell, new Mulberry32(1)).Crit);
        }


        [Fact]
        public void The_star_rune_adds_crit_chance_for_the_whole_next_battle()
        {
            var runs = new RunService(Kit.Data, new CollectionService(Kit.Data), E);
            var st = Kit.New(8);
            var run = new RunState { Runes = new System.Collections.Generic.List<string> { "estrela" }, Team = Kit.Royal };
            Assert.Equal(new[] { "estrela" }, runs.Boost(run, st).ToArray());
            Assert.Equal(0.2, E.CritChance(st, Side.Player), 6);
            E.TickFx(st, Side.Player);
            Assert.Equal(0.2, E.CritChance(st, Side.Player), 6);
        }
    }
}