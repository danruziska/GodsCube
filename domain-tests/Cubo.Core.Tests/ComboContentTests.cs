using System.Collections.Generic;
using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Cubo.Core.Tests
{
    // Os efeitos de combo do motor (para as melhorias futuras), a Runa do Fluxo, o combo das elites e do chefe e
    // o recorde de combo da coleção.
    public class ComboContentTests
    {
        private static BattleEngine E => Kit.Engine;
        private static readonly CollectionService Cols = new CollectionService(Kit.Data);
        private static readonly RunService Runs = new RunService(Kit.Data, Cols, Kit.Engine);
        private static readonly SaveCodec Codec = new SaveCodec(Kit.Data, Cols);

        // ---------------------------------------------------------------- combo e focus

        [Fact]
        public void A_combo_effect_raises_the_chance_for_its_turns_on_the_player_side_only()
        {
            var st = Kit.New(1);
            E.AddFx(st, "combo", 0.3, 2, true);
            Assert.Equal(0.3, E.ComboChance(st, Side.Player), 6);
            E.TickFx(st, Side.Player);
            E.TickFx(st, Side.Enemy);                                   // o turno do rival não gasta o efeito
            Assert.Equal(0.3, E.ComboChance(st, Side.Player), 6);
            E.TickFx(st, Side.Player);
            Assert.Equal(0, E.ComboChance(st, Side.Player), 6);
        }

        [Fact]
        public void Focus_makes_new_pieces_come_only_in_the_most_common_front_colors()
        {
            for (uint seed = 1; seed <= 40; seed++)
            {
                var st = Kit.New(seed);
                st.Combo[0] = 0.5;                                      // a cópia de vizinha também respeita as cores
                E.AddFx(st, "focus", 3, 1, true);
                var g = Geometry.For(st.N);
                var cells = new[] { Kit.Cell(st, 0, 4), Kit.Cell(st, 1, 4), Kit.Cell(st, 2, 4), Kit.Cell(st, 3, 4) };
                var palette = E.FocusPalette(st, g, Side.Player, new HashSet<int>(cells));
                Assert.Equal(3, palette.Count);
                var fills = E.RefillCells(st, g, cells, new Mulberry32(seed), Side.Player);
                Assert.All(fills, c => Assert.Contains(c, palette));
            }
        }

        [Fact]
        public void The_focus_palette_is_the_most_common_colors_with_ties_to_the_lower_number()
        {
            var st = Kit.New(1);
            Kit.Front(st, "00001", "11122", "22333", "44450", "51234");    // 0:6, 1:5, 2:5, 3:4, 4:4, 5:2
            E.AddFx(st, "focus", 3, 1, true);
            var palette = E.FocusPalette(st, Geometry.For(st.N), Side.Player, new HashSet<int>());
            Assert.Equal(new[] { 0, 1, 2 }, palette);
        }

        [Fact]
        public void Focus_lasts_only_its_turn()
        {
            var st = Kit.New(1);
            E.AddFx(st, "focus", 3, 1, true);
            var g = Geometry.For(st.N);
            Assert.NotNull(E.FocusPalette(st, g, Side.Player, new HashSet<int>()));
            Assert.Null(E.FocusPalette(st, g, Side.Enemy, new HashSet<int>()));
            E.TickFx(st, Side.Player);
            Assert.Null(E.FocusPalette(st, g, Side.Player, new HashSet<int>()));
        }

        // ---------------------------------------------------------------- dano e mana por elo

        // Duas batalhas iguais (mesma semente), uma com o efeito: o sorteio anda igual, então a cascata é a mesma.
        private static (FireResult plain, FireResult with, BattleState a, BattleState b) Twin(string fx, double v)
        {
            for (uint seed = 1; seed <= 300; seed++)
            {
                var a = Kit.New(seed); var b = Kit.New(seed);
                foreach (var st in new[] { a, b })
                {
                    st.Combo[0] = 0.6;
                    for (int h = 0; h < BattleEngine.Heroes; h++) st.HeroHp[1][h] = st.HeroMax[1][h] = 100000;   // ninguém cai no meio da cascata
                    BattleEngine.SyncHp(st, Side.Enemy);
                    for (int s = 0; s < BattleEngine.Slots; s++) st.Mana[0][s] = 0;
                }
                E.AddFx(b, fx, v, null, true);
                int cell = E.FindGroups(a)[0][0];
                var ra = E.Fire(a, Side.Player, cell, new Mulberry32(seed));
                var rb = E.Fire(b, Side.Player, cell, new Mulberry32(seed));
                if (ra.Chain.Count >= 2) return (ra, rb, a, b);
            }
            Assert.Fail("nenhuma semente com cascata de 2 elos");
            return default;
        }

        [Fact]
        public void Chain_damage_makes_each_link_hit_harder_than_the_one_before()
        {
            var (plain, with, _, _) = Twin("chainDmg", 0.25);
            Assert.Equal(plain.Damage, with.Damage);                    // o grupo tocado não muda
            Assert.Equal(plain.Chain.Count, with.Chain.Count);
            for (int k = 0; k < plain.Chain.Count; k++)
                Assert.Equal(JsMath.RoundInt(plain.Chain[k].Damage * (1 + 0.25 * (k + 1))), with.Chain[k].Damage);
        }

        [Fact]
        public void Chain_mana_gives_mana_on_every_power_for_each_chained_group()
        {
            var (plain, _, a, b) = Twin("chainMana", 1);
            int links = plain.Chain.Count;
            for (int s = 0; s < BattleEngine.Slots; s++)
                if (b.Mana[0][s] < Kit.Data.Rules.ManaCap)
                    Assert.Equal(a.Mana[0][s] + links, b.Mana[0][s]);
        }

        // ---------------------------------------------------------------- Runa do Fluxo

        [Fact]
        public void The_flow_rune_raises_the_combo_chance_in_the_next_battle_and_is_spent()
        {
            var run = Runs.Start(Kit.Royal);
            run.Runes = new List<string> { "fluxo" };
            var st = Kit.New(1);
            var used = Runs.Boost(run, st);
            Assert.Equal(new[] { "fluxo" }, used.ToArray());
            Assert.Empty(run.Runes);
            Assert.Equal(0.2, E.ComboChance(st, Side.Player), 6);
            Assert.Equal(0, E.ComboChance(st, Side.Enemy), 6);
        }

        // ---------------------------------------------------------------- elites e chefe

        [Fact]
        public void Elites_and_the_boss_bring_their_own_combo_chance()
        {
            var r = Kit.Data.Run;
            var run = Runs.Start(Kit.Royal);
            run.Floor = 1; run.Elite = false;
            Assert.Null(Runs.Opts(run).ComboEnemy);
            run.Elite = true;
            Assert.Equal(r.EliteCombo, Runs.Opts(run).ComboEnemy);
            run.Floor = r.Floors; run.Elite = false;
            Assert.Equal(r.BossCombo, Runs.Opts(run).ComboEnemy);
            Assert.True(r.EliteCombo > 0 && r.BossCombo > r.EliteCombo);

            var st = E.NewState(new BattleOptions { Size = 5, Mode = "facil", ComboEnemy = r.BossCombo }, Kit.Royal, new Mulberry32(1));
            Assert.Equal(r.BossCombo, E.ComboChance(st, Side.Enemy), 6);
            Assert.Equal(0, E.ComboChance(st, Side.Player), 6);
        }

        [Fact]
        public void The_rival_combo_chance_is_saved_with_the_battle()
        {
            var rec = BattleRecordTests.Rec(5);
            rec.ComboEnemy = 0.2;
            var back = Codec.RecordFrom(JToken.Parse(Codec.ToJson(rec, new List<Command>()).ToString()));
            Assert.Equal(0.2, back.ComboEnemy);
            rec.ComboEnemy = null;
            back = Codec.RecordFrom(JToken.Parse(Codec.ToJson(rec, new List<Command>()).ToString()));
            Assert.Null(back.ComboEnemy);
            var old = (JObject)Codec.ToJson(rec, new List<Command>());
            old.Remove("comboEnemy");
            Assert.Null(Codec.RecordFrom(old).ComboEnemy);               // gravações de antes: sem combo
        }

        // ---------------------------------------------------------------- recorde de combo

        [Fact]
        public void The_best_combo_is_kept_in_the_collection()
        {
            var col = Cols.NewCollection();
            col.BestCombo = 7;
            var back = Codec.CollectionFrom(JToken.Parse(Codec.ToJson(col).ToString()));
            Assert.Equal(7, back.BestCombo);
        }

        [Fact]
        public void The_session_counts_the_longest_player_combo()
        {
            for (uint seed = 5000; seed < 5300; seed++)
            {
                var (s, log) = Kit.Session(seed);
                s.State.Combo[0] = 0.6;
                Assert.Equal(0, s.BestCombo);
                Assert.True(s.TryFire(E.FindGroups(s.State)[0][0]));
                int links = log.OfType<GroupFired>().Where(f => f.Side == Side.Player).Max(f => f.Link);
                if (links == 0) { Assert.Equal(0, s.BestCombo); continue; }
                Assert.Equal(links + 1, s.BestCombo);
                return;
            }
            Assert.Fail("nenhuma semente com cascata");
        }

        // Uma batalha com uma cor só: a cascata derruba o rival; no fim, a coleção guarda o recorde.
        [Fact]
        public void Finishing_a_battle_records_the_best_combo()
        {
            var f = new GameFlow(Kit.Data, new Texts(new FileContentSource()), new MemoryFileStore(), 32);
            f.Load();
            f.StartRun();
            Assert.Equal(Stage.Battle, f.Stage);
            Assert.True(f.Session.TryTarget(0)); f.Session.PresentationDone();
            f.Session.State.NumColors = 1;
            Kit.FillFront(f.Session.State);
            Assert.True(f.Session.TryFire(E.FindGroups(f.Session.State)[0][0]));
            Assert.Equal(Side.Player, f.Session.State.Winner);
            Assert.True(f.Session.BestCombo >= 2);                      // a frente inteira dispara: poucos elos já derrubam o rival
            f.BattleFinished();
            Assert.Equal(f.Session.BestCombo, f.Col.BestCombo);
        }
    }
}
