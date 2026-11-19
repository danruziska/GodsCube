using System.Linq;
using Cubo.App;
using Xunit;

namespace Cubo.Core.Tests
{
    // O texto das melhorias: o valor novo e, logo depois, o antigo entre parênteses.
    public class StatTextTests
    {
        [Fact]
        public void Each_changed_number_gets_the_old_value_right_after_the_new_one()
        {
            Assert.Equal("Sweeps War and Harvest from the front, 2 (1) damage per piece · cost 6 (8)",
                StatText.WithOld("Sweeps War and Harvest from the front, 2 damage per piece · cost 6",
                                 "Sweeps War and Harvest from the front, 1 damage per piece · cost 8"));
        }

        [Fact]
        public void Numbers_that_do_not_change_stay_alone()
        {
            Assert.Equal("Heals 12 (8) · cost 5", StatText.WithOld("Heals 12 · cost 5", "Heals 8 · cost 5"));
            Assert.Equal("Heals 8 · cost 5", StatText.WithOld("Heals 8 · cost 5", "Heals 8 · cost 5"));
        }

        [Fact]
        public void Where_the_new_text_adds_a_sentence_the_rest_stays_as_new()
        {
            Assert.Equal("Deals 14 (10) damage, and +3 mana for this turn",
                StatText.WithOld("Deals 14 damage, and +3 mana for this turn", "Deals 10 damage"));
        }

        [Fact]
        public void Percentages_and_signs_count_as_numbers()
        {
            Assert.Equal("+20% (+15%) crit chance", StatText.WithOld("+20% crit chance", "+15% crit chance"));
        }

        [Fact]
        public void A_shop_upgrade_shows_its_old_values_in_parentheses()
        {
            for (uint seed = 1; seed <= 40; seed++)
            {
                var f = new GameFlow(Kit.Data, new Texts(new FileContentSource()), new MemoryFileStore(), seed);
                f.Load(); f.Texts.SetLang("en"); f.StartRun();
                for (int g = 0; g < 50 && f.Stage != Stage.Shop; g++)
                {
                    if (f.Stage == Stage.Battle) { f.Session.State.Winner = Cubo.Domain.Side.Player; f.BattleFinished(); }
                    else if (f.Stage == Stage.Blessing) f.Screen().Buttons.First().Click();
                    else if (f.Stage == Stage.Event) f.Screen().Buttons.First(b => b.Enabled).Click();
                    else if (f.Stage == Stage.Door) f.PickDoor(false);
                }
                var up = f.ShopScreen().Upgrades.FirstOrDefault(u => u.Effect.Contains("("));
                if (up == null) continue;
                Assert.Matches(@"\d+%? \(\S+\)", up.Effect);                      // "novo (antigo)"
                return;
            }
            Assert.Fail("nenhuma loja com melhoria que muda um número");
        }
    }
}
