using System.Linq;
using Cubo.App;
using Xunit;

namespace Cubo.Core.Tests
{
    // A tela de heróis (aba Heróis da preparação): o que ela mostra e o que arrastar e tocar faz com o time.
    public class PrepHeroesTests
    {
        private static GameFlow Make(string lang = null, MemoryFileStore store = null)
        {
            var f = new GameFlow(Kit.Data, new Texts(new FileContentSource()), store ?? new MemoryFileStore(), 777);
            f.Load();
            if (lang != null) f.Texts.SetLang(lang);
            return f;
        }

        private static string[] Team(GameFlow f) => f.HeroesScreen().Team;

        [Fact]
        public void Opens_with_every_hero_listed_and_the_saved_team_in_the_slots()
        {
            var v = Make().HeroesScreen();
            Assert.Equal(Kit.Data.HeroIds, v.Heroes.Select(h => h.Id).ToArray());
            Assert.Equal(new[] { "rei", "cavaleiro", "bispo" }, v.Team);
            Assert.Equal(3, v.Heroes.Count(h => h.InTeam));
            Assert.True(v.CanBattle);
        }

        [Fact]
        public void Every_card_carries_the_two_domains_of_its_hero()
        {
            var v = Make().HeroesScreen();
            Assert.Equal(new[] { 2, 4 }, v.Heroes.First(h => h.Id == "cavaleiro").Colors);
            Assert.All(v.Heroes, h => Assert.Equal(Kit.Data.Heroes[h.Id].Colors, h.Colors));
        }

        [Fact]
        public void The_first_team_hero_starts_highlighted_with_two_powers_and_its_passive()
        {
            var v = Make().HeroesScreen();
            Assert.Equal("rei", v.Detail.Id);
            Assert.Equal(1, v.Heroes.Count(h => h.Selected));
            Assert.Equal(2, v.Detail.Abilities.Length);
            Assert.All(v.Detail.Abilities, a => Assert.False(a.Name.StartsWith("⟦")));
            Assert.True(v.Detail.HasPassive);
            Assert.Equal("Decreto real", v.Detail.PassiveName);
            Assert.Equal("Os poderes do Rei custam 1 de mana a menos.", v.Detail.PassiveText);
            Assert.Equal("Controle", v.Detail.Tag);
            Assert.Equal(new[] { 1, 0 }, v.Detail.Abilities.Select(a => a.Color).ToArray());
            Assert.Equal(new[] { 10, 11 }, v.Detail.Abilities.Select(a => a.Cost).ToArray());
            Assert.Equal(new[] { 1, 0 }, v.Detail.Colors);
        }

        [Fact]
        public void Selecting_a_hero_shows_it_without_touching_the_team()
        {
            var f = Make();
            int changes = 0;
            f.Changed += () => changes++;
            f.SelectHero("joker");
            var v = f.HeroesScreen();
            Assert.Equal("joker", v.Detail.Id);
            Assert.False(v.Detail.InTeam);
            Assert.Equal(new[] { "rei", "cavaleiro", "bispo" }, v.Team);
            Assert.Equal(1, changes);
            f.SelectHero("joker");                     // o mesmo de novo: nada muda
            Assert.Equal(1, changes);
        }

        [Fact]
        public void Placing_an_outside_hero_on_a_full_team_replaces_the_one_in_that_slot()
        {
            var f = Make();
            f.PlaceHero("joker", 1);
            Assert.Equal(new[] { "rei", "joker", "bispo" }, Team(f));
            Assert.Equal("joker", f.HeroesScreen().Detail.Id);
            Assert.False(f.HeroesScreen().Heroes.First(h => h.Id == "cavaleiro").InTeam);
        }

        [Fact]
        public void Placing_a_team_hero_on_another_slot_swaps_them()
        {
            var f = Make();
            f.PlaceHero("rei", 2);
            Assert.Equal(new[] { "bispo", "cavaleiro", "rei" }, Team(f));
            f.PlaceHero("rei", 2);                     // já está lá: nada muda
            Assert.Equal(new[] { "bispo", "cavaleiro", "rei" }, Team(f));
        }

        [Fact]
        public void Removing_a_hero_leaves_an_empty_slot_and_blocks_the_battle_with_a_reason()
        {
            var f = Make();
            f.RemoveHero("rei");
            var v = f.HeroesScreen();
            Assert.Equal(new[] { "cavaleiro", "bispo", null }, v.Team);
            Assert.False(v.CanBattle);
            Assert.Equal(f.Texts.T("prep.need.heroes.1"), v.Hint);
            Assert.Equal("rei", v.Detail.Id);           // o destaque fica
            Assert.False(v.Detail.InTeam);
        }

        [Fact]
        public void An_outside_hero_dropped_on_an_empty_slot_goes_to_the_end_and_the_team_is_whole_again()
        {
            var f = Make();
            f.RemoveHero("rei");
            f.PlaceHero("joker", 2);                   // o espaço 2 está vazio: entra no fim
            Assert.Equal(new[] { "cavaleiro", "bispo", "joker" }, Team(f));
            f.RemoveHero("cavaleiro");
            f.PlaceHero("rei", 0);                     // o time é [bispo, joker]: o espaço 0 é do bispo, que sai
            Assert.Equal(new[] { "rei", "joker", null }, Team(f));
        }

        // Trocar um herói muda quais cartas jogam: o deck pode ficar abaixo do mínimo. A tela não corrige sozinha;
        // a Batalha fica desligada, diz o que falta, e a aba Arsenal tem os equipamentos.

        [Fact]
        public void The_slot_order_is_the_order_the_run_uses()
        {
            var f = Make();
            f.PlaceHero("bispo", 0);
            Assert.Equal(new[] { "bispo", "cavaleiro", "rei" }, Team(f));
            f.StartRun();
            Assert.Equal(Stage.Battle, f.Stage);
            Assert.Equal(new[] { "bispo", "cavaleiro", "rei" }, f.Run.Team);
            Assert.Equal("bispo", f.Session.State.Teams[0][0].Id);
        }

        [Fact]
        public void Bad_input_and_the_wrong_stage_change_nothing()
        {
            var f = Make();
            int changes = 0;
            f.Changed += () => changes++;
            f.PlaceHero("nao_existe", 0);
            f.PlaceHero("rainha", 0);                  // o rival não entra no time
            f.PlaceHero("joker", -1);
            f.PlaceHero("joker", 3);
            f.SelectHero("nao_existe");
            f.RemoveHero("joker");                     // não está no time
            Assert.Equal(0, changes);
            Assert.Equal(new[] { "rei", "cavaleiro", "bispo" }, Team(f));
            f.StartRun();                              // agora é batalha
            f.PlaceHero("joker", 0);
            f.RemoveHero("rei");
            f.SelectHero("joker");
            Assert.Equal(new[] { "rei", "cavaleiro", "bispo" }, f.Run.Team);
        }

        [Fact]
        public void Every_text_on_the_screen_follows_the_language()
        {
            var pt = Make("pt").HeroesScreen();
            var en = Make("en").HeroesScreen();
            var es = Make("es").HeroesScreen();
            Assert.Equal(new[] { "Poderes", "Passiva", "Time", "Corte", "Batalha", "Arsenal" },
                new[] { pt.AbilitiesLabel, pt.PassiveLabel, pt.TeamLabel, pt.TabHeroes, pt.TabBattle, pt.TabEquip });
            Assert.Equal(new[] { "Powers", "Passive", "Team", "Court", "Battle", "Armory" },
                new[] { en.AbilitiesLabel, en.PassiveLabel, en.TeamLabel, en.TabHeroes, en.TabBattle, en.TabEquip });
            Assert.Equal(new[] { "Poderes", "Pasiva", "Equipo", "Corte", "Batalla", "Arsenal" },
                new[] { es.AbilitiesLabel, es.PassiveLabel, es.TeamLabel, es.TabHeroes, es.TabBattle, es.TabEquip });
            Assert.Equal("The King's powers cost 1 less mana.", en.Detail.PassiveText);
            Assert.Equal("Arrastra un héroe", es.EmptySlot);
            Assert.Equal("Control", en.Detail.Tag);
            Assert.Equal("King", en.Heroes[0].Name);
        }

        // Fechar o app e abrir de novo: o time montado na tela continua lá, sem precisar iniciar uma batalha.
        [Fact]
        public void The_team_is_saved_on_every_change_and_survives_reopening()
        {
            var store = new MemoryFileStore();
            var f = Make(null, store);
            f.PlaceHero("joker", 1);
            Assert.Equal(new[] { "rei", "joker", "bispo" }, Team(Make(null, store)));
            f.PlaceHero("rei", 2);                     // troca de lugar
            Assert.Equal(new[] { "bispo", "joker", "rei" }, Team(Make(null, store)));
        }

        [Fact]
        public void A_team_with_an_empty_slot_is_not_saved_so_reopening_gives_the_last_whole_team()
        {
            var store = new MemoryFileStore();
            var f = Make(null, store);
            f.RemoveHero("rei");
            Assert.Equal(new[] { "cavaleiro", "bispo", null }, Team(f));
            Assert.Equal(new[] { "rei", "cavaleiro", "bispo" }, Team(Make(null, store)));
            f.PlaceHero("joker", 2);                   // completou: agora guarda
            Assert.Equal(new[] { "cavaleiro", "bispo", "joker" }, Team(Make(null, store)));
        }

    }
}