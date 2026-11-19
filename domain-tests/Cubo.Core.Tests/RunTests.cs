using System.Collections.Generic;
using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    // A coleção (time e recordes) e a corrida: andares, ouro, runas, loja e eventos.
    public class RunTests
    {
        private static readonly GameData Data = Kit.Data;
        private static readonly CollectionService Cols = new CollectionService(Data);
        private static readonly RunService Runs = new RunService(Data, Cols, Kit.Engine);

        [Fact] public void A_new_collection_has_the_default_team_and_no_records()
        {
            var col = Cols.NewCollection();
            Assert.Equal(new[] { "rei", "cavaleiro", "bispo" }, col.Team);
            Assert.Equal(0, col.Wins);
            Assert.Equal(0, col.BestCombo);
        }

        [Fact] public void Clean_drops_what_is_invalid()
        {
            var raw = new Collection { Team = new[] { "rei", "rei", "bispo" }, Wins = -3, BestCombo = 5000 };
            raw.HeroWins["naoexiste"] = 3;
            raw.HeroWins["joker"] = 2;
            var col = Cols.Clean(raw);
            Assert.Equal(Data.DefaultTeam, col.Team);
            Assert.Equal(0, col.Wins);
            Assert.Equal(999, col.BestCombo);
            Assert.False(col.HeroWins.ContainsKey("naoexiste"));
            Assert.Equal(2, col.HeroWins["joker"]);
        }

        [Fact] public void Winning_a_run_counts_for_each_hero()
        {
            var col = Cols.NewCollection();
            Cols.Win(col, Kit.Tricky);
            Assert.Equal(1, col.Wins);
            foreach (var id in Kit.Tricky) Assert.Equal(1, col.HeroWins[id]);
            Assert.False(col.HeroWins.ContainsKey("rei"));
        }

        [Fact] public void A_run_starts_on_floor_one_with_the_team_or_the_default()
        {
            var run = Runs.Start(Kit.Tricky);
            Assert.Equal(1, run.Floor);
            Assert.Equal(Kit.Tricky, run.Team);
            Assert.Equal(Data.DefaultTeam, Runs.Start(new[] { "rei", "rei", "bispo" }).Team);
        }

        [Fact] public void Winning_a_floor_heals_pays_gold_and_moves_up()
        {
            var run = Runs.Start(Kit.Royal);
            run.Elite = true;
            var st = Kit.New(1);
            st.HeroHp[0] = new[] { 4, 0, 6 }; BattleEngine.SyncHp(st, Side.Player);   // o segundo caiu
            Runs.Advance(run, st);
            Assert.Equal(2, run.Floor);
            Assert.False(run.Elite);
            Assert.Equal(Runs.Gold(1, true), run.Gold);
            Assert.Equal(new[] { 4, 0, 6 }, run.HeroHp);               // cada herói como terminou: nada de cura sozinha
            Assert.Equal(10, run.Hp);
            Assert.Equal(st.MaxHp[0], run.MaxHp);
            Assert.Equal(st.HeroMax[0], run.HeroMax);
        }

        [Fact]
        public void The_next_battle_starts_each_hero_where_the_last_one_ended_and_the_fallen_stay_down()
        {
            var run = Runs.Start(Kit.Royal);
            var st = Kit.New(1);
            st.HeroHp[0] = new[] { 4, 0, 6 }; BattleEngine.SyncHp(st, Side.Player);
            Runs.Advance(run, st);
            var next = Kit.Engine.NewState(Runs.Opts(run), run.Team, new Mulberry32(2));
            Assert.Equal(new[] { 4, 0, 6 }, next.HeroHp[0]);
            Assert.False(BattleEngine.Alive(next, Side.Player, 1));
            Assert.Equal(10, next.Hp[0]);
        }

        [Fact]
        public void A_run_saved_with_only_the_team_total_gets_each_heros_health_back()
        {
            var run = Runs.Start(Kit.Royal);
            run.Hp = 60;
            Runs.EnsureHeroHp(run, 6, "facil");
            Assert.Equal(3, run.HeroHp.Length);
            Assert.Equal(60, run.HeroHp.Sum());
            Assert.All(run.HeroHp, h => Assert.True(h > 0));
            Assert.Equal(run.HeroMax.Sum(), run.MaxHp);
        }

                [Fact]
        public void The_potion_heals_each_hero_once_per_shop_and_never_past_the_max()
        {
            var run = Runs.Start(Kit.Royal);
            var st = Kit.New(1);
            st.HeroHp[0] = new[] { 10, 0, 10 }; BattleEngine.SyncHp(st, Side.Player);   // um caído
            Runs.Advance(run, st);
            run.Gold = 100;
            run.Shop = Runs.NewShop(run, new Mulberry32(1));
            int per = Data.Run.PotionHp;
            Assert.True(per > 0);
            Assert.True(Runs.BuyPotion(run));
            Assert.Equal(new[] { 10 + per, per, 10 + per }, run.HeroHp);   // o caído também volta
            Assert.Equal(20 + 3 * per, run.Hp);
            Assert.Equal(100 - Data.Run.PotionPrice, run.Gold);
            Assert.False(Runs.BuyPotion(run));                          // uma por loja

            run.Shop = Runs.NewShop(run, new Mulberry32(2));
            run.HeroHp = new[] { run.HeroMax[0] - 5, run.HeroMax[1], run.HeroMax[2] }; RunService.SyncRunHp(run);
            Assert.True(Runs.BuyPotion(run));
            Assert.Equal(run.HeroMax, run.HeroHp);                      // não passa do máximo de ninguém
            run.Shop = Runs.NewShop(run, new Mulberry32(3));
            Assert.False(Runs.CanBuyPotion(run));                       // vida cheia: nada a comprar
            run.HeroHp = new[] { 1, 1, 1 }; RunService.SyncRunHp(run); run.Gold = Data.Run.PotionPrice - 1;
            Assert.False(Runs.CanBuyPotion(run));                       // sem ouro
        }

        [Fact] public void Battle_runes_are_spent_at_the_start()
        {
            var run = Runs.Start(Kit.Royal);
            run.Runes = new List<string> { "folego", "vida" };
            var st = Kit.New(1);
            st.HeroHp[0][0] -= 30; BattleEngine.SyncHp(st, Side.Player);
            var used = Runs.Boost(run, st);
            Assert.Equal(new[] { "folego", "vida" }, used.ToArray());
            Assert.Empty(run.Runes);
            Assert.All(st.Mana[0], m => Assert.Equal(4, m));
            Assert.Equal(st.MaxHp[0] - 10, st.Hp[0]);
        }

        [Fact] public void The_shop_sells_three_different_runes()
        {
            for (uint seed = 1; seed <= 50; seed++)
            {
                var run = Runs.Start(Kit.Royal);
                run.Floor = 2;
                var shop = Runs.NewShop(run, new Mulberry32(seed));
                Assert.Equal(3, shop.Runes.Distinct().Count());
                Assert.All(shop.Runes, id => Assert.True(Data.RuneById.ContainsKey(id)));
            }
        }

        [Fact] public void Buying_a_rune_costs_gold_and_fills_a_slot()
        {
            var run = Runs.Start(Kit.Royal);
            run.Gold = 100;
            run.Shop = Runs.NewShop(run, new Mulberry32(3));
            var id = run.Shop.Runes[0];
            Assert.True(Runs.BuyRune(run, id));
            Assert.Equal(100 - Data.RuneById[id].Price, run.Gold);
            Assert.Contains(id, run.Runes);
            Assert.False(Runs.BuyRune(run, id));                       // já vendida
        }

            }
}
