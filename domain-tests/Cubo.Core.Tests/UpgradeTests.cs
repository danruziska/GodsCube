using System.Collections.Generic;
using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Cubo.Core.Tests
{
    // As melhorias de habilidade da corrida: +, ++, +++ somam números; ★ acrescenta um efeito de combo.
    public class UpgradeTests
    {
        private static BattleEngine E => Kit.Engine;
        private static readonly CollectionService Cols = new CollectionService(Kit.Data);
        private static readonly RunService Runs = new RunService(Kit.Data, Cols, Kit.Engine);
        private static readonly SaveCodec Codec = new SaveCodec(Kit.Data, Cols);

        private static PowerDef Power(string id) => Kit.Data.Heroes.Values.SelectMany(h => h.Powers).Single(p => p.Id == id);

        // ---------------------------------------------------------------- o poder melhorado

        [Fact]
        public void Every_player_ability_has_four_tiers_ending_in_a_star()
        {
            foreach (var p in Kit.Data.Heroes.Values.SelectMany(h => h.Powers))
            {
                Assert.Equal(4, E.MaxTier(p.Id));
                Assert.Empty(E.Upgraded(p, 3).Star);
                Assert.NotEmpty(E.Upgraded(p, 4).Star);
            }
        }

        [Fact]
        public void Tiers_add_up_in_order()
        {
            var lance = Power("cavaleiro.0");                        // 10 de dano, 30% de crítico
            Assert.Same(lance, E.Upgraded(lance, 0));
            Assert.Equal(13, E.Upgraded(lance, 1).Amount);
            Assert.Equal(0.45, E.Upgraded(lance, 2).Crit, 6);
            Assert.Equal(16, E.Upgraded(lance, 3).Amount);
            var star = E.Upgraded(lance, 4);
            Assert.Equal(16, star.Amount);
            Assert.Equal("chainDmg", star.Star.Single().Type);
            Assert.Equal(4, star.Tier);
            Assert.Equal(7, E.Upgraded(Power("rei.0"), 3).Cost);
            Assert.Equal(10, lance.Amount);                           // a tabela não muda
            Assert.Equal(4, E.Upgraded(lance, 99).Tier);              // não passa do último
        }

        [Fact]
        public void A_battle_uses_the_upgraded_powers_of_the_player_only()
        {
            var tiers = new Dictionary<string, int> { { "cavaleiro.0", 1 }, { "rei.0", 3 } };
            var st = E.NewState(new BattleOptions { Size = 5, Mode = "facil", Tiers = tiers }, Kit.Royal, new Mulberry32(1));
            Assert.Equal(13, st.Power(Side.Player, 2).Amount);
            Assert.Equal(7 - Kit.Data.Rules.DecreeDiscount, E.CostOf(st, Side.Player, 0));     // Ordem real +++ e o Decreto real
            Assert.Equal(Kit.Data.EnemyTeam[0].Powers[0].Cost, st.Power(Side.Enemy, 0).Cost);
            Assert.Equal(10, Kit.Data.Heroes["cavaleiro"].Powers[0].Amount);
        }

        // ---------------------------------------------------------------- ★

        private static BattleState Starred(string id, string[] team)
        {
            var st = E.NewState(new BattleOptions { Size = 5, Mode = "facil", Tiers = new Dictionary<string, int> { { id, 4 } } },
                                team, new Mulberry32(3));
            Kit.FillMana(st, Side.Player);
            return st;
        }

        private static int SlotOf(BattleState st, string id) =>
            Enumerable.Range(0, BattleEngine.Slots).Single(k => st.Power(Side.Player, k).Id == id);

        [Fact]
        public void War_cry_star_adds_combo_chance_for_the_whole_battle()
        {
            var st = Starred("cavaleiro.1", Kit.Royal);
            Assert.NotNull(E.Cast(st, Side.Player, SlotOf(st, "cavaleiro.1"), new Mulberry32(1)));
            Assert.Equal(0.2, E.ComboChance(st, Side.Player), 6);
            for (int k = 0; k < 4; k++) E.TickFx(st, Side.Player);
            Assert.Equal(0.2, E.ComboChance(st, Side.Player), 6);
        }

        [Fact]
        public void Royal_order_star_makes_its_own_new_pieces_come_in_three_colors()
        {
            var st = Starred("rei.0", Kit.Royal);
            var r = E.Cast(st, Side.Player, SlotOf(st, "rei.0"), new Mulberry32(1));
            Assert.NotNull(r);
            var g = Geometry.For(st.N);
            Assert.NotNull(E.FocusPalette(st, g, Side.Player, new HashSet<int>()));     // vale neste turno
            E.TickFx(st, Side.Player);
            Assert.Null(E.FocusPalette(st, g, Side.Player, new HashSet<int>()));
        }

        [Fact]
        public void Ace_star_gives_combo_chance_this_turn_only()
        {
            var st = Starred("joker.0", Kit.Tricky);
            Assert.NotNull(E.Cast(st, Side.Player, SlotOf(st, "joker.0"), new Mulberry32(1)));
            Assert.Equal(0.4, E.ComboChance(st, Side.Player), 6);
            Assert.True(st.Fx.NextCrit);
            E.TickFx(st, Side.Player);
            Assert.Equal(0, E.ComboChance(st, Side.Player), 6);
        }

        // ---------------------------------------------------------------- bênção e loja

        [Fact]
        public void A_blessing_offers_three_different_team_abilities_below_the_star()
        {
            var run = Runs.Start(Kit.Royal);
            run.Tiers["rei.0"] = 4;                                    // já no ★: não aparece
            for (uint seed = 1; seed <= 40; seed++)
            {
                var offer = Runs.NewBlessing(run, new Mulberry32(seed));
                Assert.Equal(3, offer.Distinct().Count());
                Assert.All(offer, id => Assert.Contains(id, Runs.TeamPowers(run)));
                Assert.DoesNotContain("rei.0", offer);
            }
        }

        [Fact]
        public void Blessing_raises_the_chosen_ability_one_tier_and_closes()
        {
            var run = Runs.Start(Kit.Royal);
            run.Blessing = new List<string> { "cavaleiro.0", "bispo.0", "rei.1" };
            Assert.False(Runs.Bless(run, "joker.0"));                   // não está na oferta
            Assert.True(Runs.Bless(run, "bispo.0"));
            Assert.Equal(1, Runs.TierOf(run, "bispo.0"));
            Assert.Null(run.Blessing);
            Assert.False(Runs.Bless(run, "cavaleiro.0"));               // a bênção já fechou
        }

        [Fact]
        public void The_shop_sells_upgrades_priced_by_the_next_tier()
        {
            var run = Runs.Start(Kit.Royal);
            run.Floor = 2; run.Gold = 200;
            run.Shop = Runs.NewShop(run, new Mulberry32(5));
            Assert.Equal(2, run.Shop.Upgrades.Distinct().Count());
            var id = run.Shop.Upgrades[0];
            run.Tiers[id] = 2;
            int price = Runs.UpgradePrice(run, id);
            Assert.Equal(Kit.Data.Run.UpgradePrices[2], price);
            Assert.True(Runs.BuyUpgrade(run, id));
            Assert.Equal(3, Runs.TierOf(run, id));
            Assert.Equal(200 - price, run.Gold);
            Assert.False(Runs.BuyUpgrade(run, id));                     // uma vez por loja
            var other = run.Shop.Upgrades[1];
            run.Gold = 0;
            Assert.False(Runs.BuyUpgrade(run, other));                  // sem ouro
        }

        [Fact]
        public void Tiers_blessing_and_shop_upgrades_are_saved_with_the_run_and_the_battle()
        {
            var run = Runs.Start(Kit.Royal);
            run.Tiers["cavaleiro.0"] = 2; run.Tiers["naoexiste"] = 3;
            run.Blessing = new List<string> { "rei.1", "bispo.1" };
            run.Shop = new ShopState { Runes = new List<string> { "fluxo" }, Upgrades = new List<string> { "bispo.0" }, Sold = new List<string> { "bispo.0" } };
            var back = Codec.RunFrom(JToken.Parse(Codec.ToJson(run).ToString()));
            Assert.Equal(new Dictionary<string, int> { { "cavaleiro.0", 2 } }, back.Tiers);
            Assert.Equal(run.Blessing, back.Blessing);
            Assert.Equal(new[] { "bispo.0" }, back.Shop.Upgrades);
            Assert.Equal(new[] { "bispo.0" }, back.Shop.Sold);

            var rec = BattleRecordTests.Rec(5);
            rec.Tiers = new Dictionary<string, int> { { "cavaleiro.0", 1 } };
            var rb = Codec.RecordFrom(JToken.Parse(Codec.ToJson(rec, new List<Command>()).ToString()));
            Assert.Equal(rec.Tiers, rb.Tiers);
            var s = BattleBuilder.Replay(Kit.Engine, Runs, rb);
            Assert.Equal(13, s.State.Power(Side.Player, 2).Amount);
        }

        // Vencer o primeiro andar abre a bênção; escolher sobe a habilidade, e a próxima batalha já a usa melhorada.
        [Fact]
        public void Winning_a_floor_opens_the_blessing_and_the_next_battle_uses_the_upgrade()
        {
            var f = new GameFlow(Kit.Data, new Texts(new FileContentSource()), new MemoryFileStore(), 7);
            f.Load();
            f.StartRun();
            f.Session.State.Winner = Side.Player; f.BattleFinished();
            Assert.Equal(Stage.Blessing, f.Stage);
            var buttons = f.Screen().Buttons.ToList();
            Assert.Equal(3, buttons.Count);
            string id = buttons[0].Id.Substring("bless:".Length);
            buttons[0].Click();
            Assert.Equal(1, f.Run.Tiers[id]);
            int guard = 0;
            while (f.Stage != Stage.Battle && guard++ < 10)
            {
                if (f.Stage == Stage.Event) f.Screen().Buttons.First(b => b.Enabled).Click();
                else if (f.Stage == Stage.Shop) f.ShopLeave();
                else if (f.Stage == Stage.Door) f.PickDoor(false);
            }
            var st = f.Session.State;
            int slot = Enumerable.Range(0, BattleEngine.Slots).Single(k => st.Power(Side.Player, k).Id == id);
            Assert.Equal(1, st.Power(Side.Player, slot).Tier);
        }

        // ---------------------------------------------------------------- textos

        [Fact]
        public void Every_ability_at_every_tier_has_whole_texts_in_every_language()
        {
            var t = new Texts(new FileContentSource());
            foreach (var lang in Texts.Languages)
            {
                t.SetLang(lang);
                foreach (var p in Kit.Data.Heroes.Values.SelectMany(h => h.Powers))
                    for (int tier = 0; tier <= 4; tier++)
                    {
                        string text = HudModel.PowerEffect(E.Upgraded(p, tier), t);
                        Assert.False(text.Contains("⟦"), $"{lang} {p.Id} {tier}: {text}");
                        if (tier == 4) Assert.Contains("★", text);
                    }
                Assert.DoesNotContain("⟦", t.T("blessing.title") + t.T("blessing.text") + t.T("shop.upgrades") + t.T("shop.upgrades.how"));
            }
            Assert.Equal(" ++", HudModel.TierTag(2, false));
            Assert.Equal(" ★", HudModel.TierTag(4, true));
            Assert.Equal("", HudModel.TierTag(0, false));
        }
    }
}
