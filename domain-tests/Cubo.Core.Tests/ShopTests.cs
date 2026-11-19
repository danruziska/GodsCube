using System.Collections.Generic;
using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    // A loja: a promoção, a troca das ofertas e a tela desenhada (ShopView) com foco, compra e a fala do vendedor.
    public class ShopTests
    {
        private static readonly GameData Data = Kit.Data;
        private static readonly RunService Runs = new RunService(Data, new CollectionService(Data), Kit.Engine);

        private static RunState RunWithShop(uint seed = 1, int gold = 200)
        {
            var run = Runs.Start(Kit.Royal);
            var st = Kit.New(1); st.HeroHp[0] = new[] { 10, 10, 10 }; BattleEngine.SyncHp(st, Side.Player);
            Runs.Advance(run, st);
            run.Gold = gold;
            run.Shop = Runs.NewShop(run, new Mulberry32(seed));
            return run;
        }

        [Fact]
        public void Every_shop_has_one_offer_on_sale_at_half_price()
        {
            var run = RunWithShop();
            var sale = run.Shop.Sale;
            Assert.NotNull(sale);
            Assert.Contains(sale, run.Shop.Runes.Concat(run.Shop.Upgrades));
            int full = Runs.FullPrice(run, sale);
            int price = Data.RuneById.ContainsKey(sale) ? Runs.RunePrice(run, sale) : Runs.UpgradePrice(run, sale);
            Assert.Equal(JsMath.RoundInt(full * 0.5), price);
            var other = run.Shop.Runes.First(id => id != sale);
            Assert.Equal(Data.RuneById[other].Price, Runs.RunePrice(run, other));
        }

        [Fact]
        public void Buying_the_rune_on_sale_charges_the_sale_price()
        {
            for (uint seed = 1; seed < 50; seed++)
            {
                var run = RunWithShop(seed);
                if (!Data.RuneById.ContainsKey(run.Shop.Sale)) continue;
                int price = Runs.RunePrice(run, run.Shop.Sale);
                Assert.True(Runs.BuyRune(run, run.Shop.Sale));
                Assert.Equal(200 - price, run.Gold);
                return;
            }
            Assert.Fail("nenhuma loja com runa em promoção");
        }

        [Fact]
        public void Restocking_redraws_the_offers_keeps_the_potion_and_costs_more_each_time()
        {
            var run = RunWithShop();
            Assert.True(Runs.BuyPotion(run));
            var rune = run.Shop.Runes[0];
            Assert.True(Runs.BuyRune(run, rune));
            int gold = run.Gold;
            Assert.Equal(Data.Run.RerollBase, Runs.RerollPrice(run));
            Assert.True(Runs.Reroll(run, new Mulberry32(7)));
            Assert.Equal(gold - Data.Run.RerollBase, run.Gold);
            Assert.Equal(1, run.Shop.Rerolls);
            Assert.Equal(new[] { RunService.Potion }, run.Shop.Sold);              // a poção continua comprada
            Assert.Equal(Data.Run.RerollBase + Data.Run.RerollStep, Runs.RerollPrice(run));
            run.Gold = Runs.RerollPrice(run) - 1;
            Assert.False(Runs.Reroll(run, new Mulberry32(8)));
        }

        [Fact]
        public void The_shop_round_trips_its_restocks_and_sale_through_the_save()
        {
            var run = RunWithShop();
            Runs.Reroll(run, new Mulberry32(3));
            var codec = new SaveCodec(Data, new CollectionService(Data));
            var back = codec.RunFrom(codec.ToJson(run));
            Assert.Equal(run.Shop.Rerolls, back.Shop.Rerolls);
            Assert.Equal(run.Shop.Sale, back.Shop.Sale);
            Assert.Equal(run.MaxHp, back.MaxHp);
            Assert.Equal(run.HeroHp, back.HeroHp);
            Assert.Equal(run.HeroMax, back.HeroMax);
        }

        // Uma corrida até a primeira loja, pelo fluxo de verdade.
        private static GameFlow AtShop(uint seed = 21)
        {
            var f = new GameFlow(Data, new Texts(new FileContentSource()), new MemoryFileStore(), seed);
            f.Load();
            f.Texts.SetLang("en");
            f.StartRun();
            for (int guard = 0; guard < 50 && f.Stage != Stage.Shop; guard++)
            {
                if (f.Stage == Stage.Battle) { f.Session.State.Winner = Side.Player; f.BattleFinished(); }
                else if (f.Stage == Stage.Blessing) f.Screen().Buttons.First().Click();
                else if (f.Stage == Stage.Event) f.Screen().Buttons.First(b => b.Enabled).Click();
                else if (f.Stage == Stage.Door) f.PickDoor(false);
            }
            Assert.Equal(Stage.Shop, f.Stage);
            return f;
        }

        [Fact]
        public void The_drawn_shop_lists_shelves_focuses_the_first_offer_and_greets()
        {
            var f = AtShop();
            var v = f.ShopScreen();
            Assert.Equal("Shop", v.Title);
            Assert.Equal(f.Run.Gold.ToString(), v.Gold);
            Assert.Equal(Data.Run.ShopRunes, v.Runes.Count);
            Assert.Equal(new[] { "potion", "reroll" }, v.Services.Select(o => o.Id));
            Assert.Single(v.All.Where(o => o.Focused));
            Assert.Equal(v.All.First(o => !o.Sold).Id, v.DetailId);
            Assert.Single(v.All.Where(o => o.OnSale));
            Assert.False(string.IsNullOrEmpty(v.Line));
            Assert.Equal(Data.Run.RuneSlots, v.Bag.Count);
            Assert.All(v.Upgrades, u => Assert.True(u.HeroId != null || u.IconId != null));
        }

        [Fact]
        public void Focusing_and_buying_a_rune_updates_gold_bag_sold_state_and_the_merchant()
        {
            var f = AtShop();
            f.Run.Gold = 200;
            var rune = f.ShopScreen().Runes.First();
            f.FocusShop(rune.Id);
            var v = f.ShopScreen();
            Assert.Equal(rune.Id, v.DetailId);
            Assert.True(v.CanBuy);
            Assert.StartsWith("Buy · ", v.ActionText);
            string greet = v.Line;
            f.ShopBuyFocused();
            v = f.ShopScreen();
            Assert.True(v.Runes.First(o => o.Id == rune.Id).Sold);
            Assert.Equal((200 - rune.Price).ToString(), v.Gold);
            Assert.Equal(rune.IconId, v.Bag[0]);
            Assert.Equal("Sold", v.ActionText);
            Assert.NotEqual(greet, v.Line);                                         // o vendedor agradece
        }

        [Fact]
        public void Without_gold_the_button_says_why_and_the_merchant_complains()
        {
            var f = AtShop();
            f.Run.Gold = 0;
            var rune = f.ShopScreen().Runes.First();
            f.FocusShop(rune.Id);
            var v = f.ShopScreen();
            Assert.False(v.CanBuy);
            Assert.Equal("Not enough gold", v.ActionText);
            Assert.Equal("Come back with more coin.", v.Line);
            Assert.False(v.Runes.First().Affordable);
        }

        [Fact]
        public void The_restock_offer_redraws_and_stays_focused()
        {
            var f = AtShop();
            f.Run.Gold = 100;
            f.FocusShop("reroll");
            var before = f.ShopScreen();
            Assert.StartsWith("Restock · ", before.ActionText);
            f.ShopBuyFocused();
            var v = f.ShopScreen();
            Assert.Equal("reroll", v.DetailId);
            Assert.Equal((100 - before.DetailPrice).ToString(), v.Gold);
            Assert.Equal(Data.Run.RerollBase + Data.Run.RerollStep, v.DetailPrice);
            Assert.Equal("Fresh stock, straight from the back.", v.Line);
        }
    }
}
