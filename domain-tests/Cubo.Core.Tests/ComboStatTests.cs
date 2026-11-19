using System.Linq;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    // A chance de combo: cada lado tem a sua (a de uma peça nova copiar a cor de uma vizinha). Começa baixa e o
    // jogador a aumenta na corrida; as peças novas usam sempre a de quem as fez entrar.
    public class ComboStatTests
    {
        private static BattleEngine E => Kit.Engine;

        [Fact]
        public void A_battle_starts_with_the_base_combo_chance_of_the_rules()
        {
            var st = Kit.New(1);
            Assert.Equal(Kit.Data.Rules.ComboChance[0], E.ComboChance(st, Side.Player));
            Assert.Equal(Kit.Data.Rules.ComboChance[1], E.ComboChance(st, Side.Enemy));
        }

        [Fact]
        public void The_battle_options_can_set_each_side()
        {
            var st = E.NewState(new BattleOptions { Size = 5, Mode = "facil", ComboPlayer = 0.3, ComboEnemy = 0.1 },
                                Kit.Royal, new Mulberry32(1));
            Assert.Equal(0.3, E.ComboChance(st, Side.Player), 6);
            Assert.Equal(0.1, E.ComboChance(st, Side.Enemy), 6);
        }

        [Fact]
        public void Combo_effects_add_up_per_side_and_stop_at_the_cap()
        {
            var st = Kit.New(1);
            E.AddFx(st, Side.Player, "combo", 0.25, null, true);
            Assert.Equal(st.Combo[0] + 0.25, E.ComboChance(st, Side.Player), 6);
            Assert.Equal(st.Combo[1], E.ComboChance(st, Side.Enemy), 6);              // o do rival não muda
            E.AddFx(st, Side.Player, "combo", 5, null, true);
            Assert.Equal(Kit.Data.Rules.ComboMax, E.ComboChance(st, Side.Player), 6);
        }

        // As peças que o rival faz entrar usam a chance dele: a do jogador não muda nada no turno do rival.
        [Fact]
        public void The_rival_never_uses_the_players_combo_chance()
        {
            for (uint seed = 1; seed <= 50; seed++)
            {
                var a = Kit.New(seed);
                var b = Kit.New(seed);
                b.Combo[0] = 0.8;
                a.Turn = b.Turn = Side.Enemy;
                int cell = E.FindGroups(a)[0][0];
                E.Fire(a, Side.Enemy, cell, new Mulberry32(seed));
                E.Fire(b, Side.Enemy, cell, new Mulberry32(seed));
                Assert.Equal(a.Colors, b.Colors);
            }
        }

        [Fact]
        public void A_higher_combo_chance_means_more_chains()
        {
            double Links(double combo)
            {
                int total = 0;
                for (uint seed = 1; seed <= 400; seed++)
                {
                    var st = Kit.New(seed);
                    st.Combo[0] = combo;
                    total += E.Fire(st, Side.Player, E.FindGroups(st)[0][0], new Mulberry32(seed)).Chain.Count;
                }
                return total / 400.0;
            }
            double low = Links(0), high = Links(0.6);
            Assert.True(high > low * 1.5, $"combo 0: {low:F2} elos por toque; combo 0,6: {high:F2}");
        }
    }
}
