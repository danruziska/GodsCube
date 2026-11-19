using System.Collections.Generic;
using System.Linq;
using Cubo.Data;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    public class ContentLoaderTests
    {
        private static readonly GameData Data = ContentLoader.Load(new FileContentSource());

        [Fact]
        public void Loads_expected_counts()
        {
            Assert.Equal(new[] { "rei", "cavaleiro", "joker", "bispo" }, Data.HeroIds);
            Assert.Equal(new[] { "rainha", "torre", "peao" }, Data.EnemyTeam.Select(h => h.Id).ToArray());
            Assert.Equal(new[] { "rei", "cavaleiro", "bispo" }, Data.DefaultTeam);
            Assert.Equal(new[] { "folego", "estrela", "fluxo", "escudo", "vida" }, Data.Runes.Select(r => r.Id).ToArray());
            Assert.Equal(new[] { "idolo", "acampamento", "juramento" }, Data.EventIds);
        }

        [Fact]
        public void Rules_are_read_with_the_right_types()
        {
            var r = Data.Rules;
            Assert.Equal(6, r.NumColors);
            Assert.Equal(2, r.MinGroup);
            Assert.Equal(3, r.CascadeMin);
            Assert.Equal(3, r.BonusFrom);
            Assert.Equal(110, r.HpEasy[4]);
            Assert.Equal(160, r.HpHard[8]);
            Assert.Equal(1.15, r.CostScale["dificil"][7]);
            Assert.Equal(new[] { 0.0, 0.0 }, r.ComboChance);
            Assert.True(r.ComboMax > 0 && r.ComboMax < 1);
            Assert.Equal(0.05, r.CritChance[0]);
            Assert.Equal(0.05, r.CritChance[1]);
            Assert.Equal(0.6, r.CritMax);
            Assert.Equal(2, r.CritMultiplier);
            Assert.Equal(1, r.SecondMana);
            Assert.Equal(3, r.AiLevels["distraido"]);
            Assert.Equal(4, r.AiHandLook["implacavel"]);
            Assert.Equal(4, r.ChargeMinGroup);
            Assert.Equal(0.15, r.ChargeBonus);
        }

        // As cores dos heróis são as do protótipo, pelo nome, na Palette do Unity:
        // 0 amarelo, 1 azul, 2 vermelho, 3 roxo, 4 verde, 5 azul-claro (nenhum herói usa o azul-claro).
        [Fact]
        public void Every_hero_has_two_colors_two_powers_in_those_colors_and_a_passive()
        {
            Assert.Equal(new[] { 1, 0 }, Data.Heroes["rei"].Colors);
            Assert.Equal(new[] { 2, 4 }, Data.Heroes["cavaleiro"].Colors);
            Assert.Equal(new[] { 0, 3 }, Data.Heroes["joker"].Colors);
            Assert.Equal(new[] { 3, 1 }, Data.Heroes["bispo"].Colors);
            foreach (var h in Data.Heroes.Values.Concat(Data.EnemyTeam))
            {
                Assert.Equal(2, h.Powers.Length);
                for (int q = 0; q < 2; q++) Assert.Equal(h.Colors[q], h.Powers[q].Color);
                foreach (var p in h.Powers)
                {
                    Assert.Contains(p.Kind, new[] { "damage", "heal", "extra", "purge", "critUp", "critNext" });
                    if (p.Kind == "purge") Assert.True(p.PurgeColors != null && p.PurgeColors.Length == 2, h.Id + " sem purgeColors");
                    if (p.Kind == "critUp") Assert.True(p.V > 0, h.Id + " critUp sem v");
                }
            }
            Assert.Equal(new[] { Passives.Decree, Passives.Charge, Passives.Trick, Passives.Communion },
                Data.HeroIds.Select(id => Data.Heroes[id].Passive).ToArray());
            Assert.All(Data.EnemyTeam, h => Assert.Null(h.Passive));
            Assert.Equal(0.3, Data.Heroes["cavaleiro"].Powers[0].Crit);
        }

        [Fact]
        public void Team_resolves_valid_ids_and_falls_back_to_default()
        {
            Assert.Equal("joker", Data.ResolveTeam(new[] { "joker", "bispo", "rei" })[0].Id);
            Assert.Equal("rei", Data.ResolveTeam(new[] { "joker", "joker", "bispo" })[0].Id);
            Assert.Equal("rei", Data.ResolveTeam(new[] { "joker", "rainha", "bispo" })[0].Id);   // o rival não entra no time
            Assert.Equal("rei", Data.ResolveTeam(null)[0].Id);
        }

    }
}