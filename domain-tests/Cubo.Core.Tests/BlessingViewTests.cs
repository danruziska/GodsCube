using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    // A bênção desenhada: as cartas, escolher uma e aceitar.
    public class BlessingViewTests
    {
        private static GameFlow AtBlessing(uint seed = 3)
        {
            var f = new GameFlow(Kit.Data, new Texts(new FileContentSource()), new MemoryFileStore(), seed);
            f.Load(); f.Texts.SetLang("en"); f.StartRun();
            f.Session.State.Winner = Side.Player; f.BattleFinished();
            Assert.Equal(Stage.Blessing, f.Stage);
            return f;
        }

        [Fact]
        public void The_cards_show_each_option_with_hero_change_and_old_values()
        {
            var f = AtBlessing();
            var v = f.BlessingView();
            Assert.Equal("Blessing", v.Title);
            Assert.Equal(f.Run.Blessing, v.Cards.Select(c => c.Id));
            Assert.All(v.Cards, c => Assert.Contains("→", c.Change));
            Assert.All(v.Cards, c => Assert.False(string.IsNullOrEmpty(c.Effect)));
            Assert.All(v.Cards, c => Assert.True(c.HeroId != null || c.IconId != null));
            Assert.False(v.CanAccept);                                            // nada escolhido ainda
            Assert.Equal("Choose a blessing", v.ActionText);
        }

        [Fact]
        public void Choosing_a_card_then_accepting_upgrades_it_and_moves_on()
        {
            var f = AtBlessing();
            var id = f.Run.Blessing[1];
            f.FocusBlessing(id);
            var v = f.BlessingView();
            Assert.True(v.Cards.Single(c => c.Focused).Id == id);
            Assert.True(v.CanAccept);
            Assert.Equal("Accept blessing", v.ActionText);
            f.AcceptBlessing();
            Assert.NotEqual(Stage.Blessing, f.Stage);
            Assert.Equal(1, f.Run.Tiers[id]);
        }

        [Fact]
        public void Accepting_without_a_choice_does_nothing()
        {
            var f = AtBlessing();
            f.AcceptBlessing();
            Assert.Equal(Stage.Blessing, f.Stage);
            Assert.Empty(f.Run.Tiers);
        }
    }
}
