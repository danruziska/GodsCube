using System.Collections.Generic;
using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Cubo.Core.Tests
{
    // Os equipamentos: cada herói leva um, escolhido na preparação, e vale a corrida inteira.
    public class EquipmentTests
    {
        private static BattleEngine E => Kit.Engine;
        private static readonly CollectionService Cols = new CollectionService(Kit.Data);
        private static readonly RunService Runs = new RunService(Kit.Data, Cols, Kit.Engine);
        private static readonly SaveCodec Codec = new SaveCodec(Kit.Data, Cols);

        private static BattleState With(string hero, string item, Dictionary<string, int> tiers = null) =>
            E.NewState(new BattleOptions { Size = 5, Mode = "facil", Tiers = tiers, Equip = new Dictionary<string, string> { { hero, item } } },
                       Kit.Royal, new Mulberry32(3));

        private static BattleState Bare() => E.NewState(new BattleOptions { Size = 5, Mode = "facil" }, Kit.Royal, new Mulberry32(3));

        // ---------------------------------------------------------------- o conteúdo

        [Fact]
        public void The_items_load_with_known_kinds()
        {
            var kinds = new[] { EquipKinds.Amount, EquipKinds.Mana, EquipKinds.Cost, EquipKinds.MaxHp, EquipKinds.Ward,
                                EquipKinds.Combo, EquipKinds.ChainDmg, EquipKinds.Gold,
                                EquipKinds.Poison, EquipKinds.Burn };
            Assert.Equal(10, Kit.Data.Equipment.Count);
            Assert.Equal(10, Kit.Data.Equipment.Select(e => e.Id).Distinct().Count());
            Assert.All(Kit.Data.Equipment, e => Assert.Contains(e.Kind, kinds));
        }

        [Fact]
        public void Every_item_has_a_name_and_a_text_in_every_language()
        {
            var t = new Texts(new FileContentSource());
            var f = new GameFlow(Kit.Data, t, new MemoryFileStore(), 1);
            foreach (var lang in Texts.Languages)
            {
                t.SetLang(lang);
                foreach (var e in Kit.Data.Equipment)
                    Assert.DoesNotContain("⟦", t.Name("equip." + e.Id) + f.EquipText(e.Id));
                foreach (var k in new[] { "equip.title", "equip.how", "equip.noteam", "equip.none", "heroes.tab.equip" })
                    Assert.DoesNotContain("⟦", t.T(k));
            }
        }

        // ---------------------------------------------------------------- na batalha

        [Fact]
        public void The_blade_adds_to_the_heroes_damage_and_heal_powers_only()
        {
            var st = With("cavaleiro", "lamina");
            Assert.Equal(13, st.Power(Side.Player, 2).Amount);                // Lança: 10 + 3
            Assert.Equal(0, st.Power(Side.Player, 3).Amount);                 // Grito de guerra não tem quantidade
            Assert.Equal(12, st.Power(Side.Player, 1).Amount);                // a cura do Rei não muda
            Assert.Equal(10, Kit.Data.Heroes["cavaleiro"].Powers[0].Amount);  // a tabela não muda
            var up = With("cavaleiro", "lamina", new Dictionary<string, int> { { "cavaleiro.0", 1 } });
            Assert.Equal(16, up.Power(Side.Player, 2).Amount);                // soma com a melhoria +
        }

        [Fact]
        public void The_ring_fills_the_heroes_powers_at_the_start()
        {
            var st = With("bispo", "anel");
            Assert.Equal(new[] { 0, 0, 0, 0, 4, 4 }, st.Mana[0]);
        }

        [Fact]
        public void The_scepter_makes_the_heroes_powers_cheaper()
        {
            var bare = Bare(); var st = With("bispo", "cetro");
            Assert.Equal(E.CostOf(bare, Side.Player, 4) - 1, E.CostOf(st, Side.Player, 4));
            Assert.Equal(E.CostOf(bare, Side.Player, 2), E.CostOf(st, Side.Player, 2));
        }

        [Fact]
        public void The_helm_raises_max_health()
        {
            var bare = Bare(); var st = With("rei", "elmo");
            Assert.Equal(bare.MaxHp[0] + 15, st.MaxHp[0]);
            Assert.Equal(st.MaxHp[0], st.Hp[0]);
        }

        [Fact]
        public void Buckler_clover_and_chain_last_the_whole_battle()
        {
            var ward = With("rei", "broquel");
            Assert.Equal(0.1, E.FxSum(ward, "ward"), 6);
            var clover = With("rei", "trevo");
            Assert.Equal(0.05, E.ComboChance(clover, Side.Player), 6);
            for (int k = 0; k < 5; k++) { E.TickFx(clover, Side.Player); E.TickFx(clover, Side.Enemy); }
            Assert.Equal(0.05, E.ComboChance(clover, Side.Player), 6);
            Assert.Equal(0.1, E.FxSum(With("rei", "corrente"), "chainDmg"), 6);
        }

        [Fact]
        public void An_item_on_a_hero_outside_the_team_does_nothing()
        {
            var st = With("joker", "elmo");
            Assert.Equal(Bare().MaxHp[0], st.MaxHp[0]);
        }

        // ---------------------------------------------------------------- na corrida

        [Fact]
        public void Clean_equip_keeps_one_item_per_hero_and_one_hero_per_item()
        {
            var raw = new Dictionary<string, string> { { "rei", "elmo" }, { "cavaleiro", "elmo" }, { "bispo", "naoexiste" }, { "ninguem", "anel" }, { "joker", "anel" } };
            Assert.Equal(new Dictionary<string, string> { { "rei", "elmo" }, { "joker", "anel" } }, Kit.Data.CleanEquip(raw));
            Assert.Equal(new Dictionary<string, string> { { "rei", "elmo" } }, Kit.Data.CleanEquip(raw, Kit.Royal));
        }

        [Fact]
        public void A_run_takes_the_items_of_its_team_only()
        {
            var run = Runs.Start(Kit.Royal, new Dictionary<string, string> { { "rei", "elmo" }, { "joker", "anel" } });
            Assert.Equal(new Dictionary<string, string> { { "rei", "elmo" } }, run.Equip);
            Assert.Equal("elmo", Runs.Opts(run).Equip["rei"]);
        }

        [Fact]
        public void The_purse_pays_more_gold_per_floor()
        {
            var run = Runs.Start(Kit.Royal, new Dictionary<string, string> { { "bispo", "bolsa" } });
            Runs.Advance(run, Kit.New(1));
            Assert.Equal(Runs.Gold(1, false, 0.3), run.Gold);
            Assert.True(run.Gold > Runs.Gold(1, false));
        }

        [Fact]
        public void Items_are_saved_with_the_collection_the_run_and_the_battle()
        {
            var col = Cols.NewCollection();
            col.Equip["cavaleiro"] = "lamina"; col.Equip["joker"] = "trevo";
            Assert.Equal(col.Equip, Codec.CollectionFrom(JToken.Parse(Codec.ToJson(col).ToString())).Equip);

            var run = Runs.Start(Kit.Royal, col.Equip);
            Assert.Equal(run.Equip, Codec.RunFrom(JToken.Parse(Codec.ToJson(run).ToString())).Equip);

            var rec = BattleRecordTests.Rec(5);
            rec.Equip = new Dictionary<string, string> { { "cavaleiro", "lamina" } };
            var back = Codec.RecordFrom(JToken.Parse(Codec.ToJson(rec, new List<Command>()).ToString()));
            Assert.Equal(rec.Equip, back.Equip);
            var s = BattleBuilder.Replay(Kit.Engine, Runs, back);
            Assert.Equal(13, s.State.Power(Side.Player, 2).Amount);
        }

        // ---------------------------------------------------------------- a aba Arsenal

        [Fact]
        public void The_armory_tab_equips_the_chosen_hero_and_moves_items_between_heroes()
        {
            var store = new MemoryFileStore();
            var f = new GameFlow(Kit.Data, new Texts(new FileContentSource()), store, 7);
            f.Load();
            var screen = f.Screen();
            Assert.Equal(3, screen.Buttons.Count(b => b.Id.StartsWith("equiphero:")));
            Assert.Equal(10, screen.Buttons.Count(b => b.Id.StartsWith("equip:")));
            Assert.True(screen.Button("equiphero:rei").On);                      // começa no primeiro do time

            f.Screen().Button("equip:elmo").Click();
            Assert.Equal("elmo", f.Col.Equip["rei"]);
            Assert.True(f.Screen().Button("equip:elmo").On);

            f.Screen().Button("equip:anel").Click();                            // troca: um por herói
            Assert.Equal("anel", f.Col.Equip["rei"]);

            f.Screen().Button("equiphero:bispo").Click();
            f.Screen().Button("equip:anel").Click();                            // o Bispo toma o Anel do Rei
            Assert.Equal("anel", f.Col.Equip["bispo"]);
            Assert.False(f.Col.Equip.ContainsKey("rei"));

            f.Screen().Button("equip:anel").Click();                            // tocar de novo tira
            Assert.Empty(f.Col.Equip);

            f.Screen().Button("equip:elmo").Click();
            var again = new GameFlow(Kit.Data, new Texts(new FileContentSource()), store, 7);
            again.Load();
            Assert.Equal("elmo", again.Col.Equip["bispo"]);                     // a coleção guardou

            again.StartRun();
            Assert.Equal("elmo", again.Run.Equip["bispo"]);
            Assert.Equal("elmo", again.Session.State.Equip[2].Id);
        }

        // A tela desenhada: tocar num equipamento só mostra; o botão equipa, pega de outro herói ou tira.
        [Fact]
        public void The_drawn_armory_shows_first_and_the_button_equips()
        {
            var f = new GameFlow(Kit.Data, new Texts(new FileContentSource()), new MemoryFileStore(), 7);
            f.Load();
            var v = f.EquipScreen();
            Assert.Equal(new[] { "rei", "cavaleiro", "bispo" }, v.Heroes.Select(h => h.Id));
            Assert.True(v.Heroes[0].Selected);
            Assert.Equal(10, v.Items.Count);
            Assert.Equal("lamina", v.DetailId);                                  // nada equipado: o primeiro
            Assert.True(v.CanAct);
            Assert.Equal(f.Texts.T("heroes.tab.equip"), v.Title);               // a placa do Arsenal usa o nome da aba
            Assert.Equal(f.Texts.T("heroes.team"), v.TeamTitle);

            f.FocusEquip("elmo");
            Assert.Empty(f.Col.Equip);                                           // só mostrou
            v = f.EquipScreen();
            Assert.Equal("elmo", v.DetailId);
            Assert.True(v.Items.Single(i => i.Id == "elmo").Focused);
            Assert.Equal(f.Texts.T("equip.btn.equip"), v.ActionText);

            f.PressEquip();
            Assert.Equal("elmo", f.Col.Equip["rei"]);
            v = f.EquipScreen();
            Assert.True(v.Items.Single(i => i.Id == "elmo").OnSelected);
            Assert.Equal("elmo", v.Heroes[0].ItemId);
            Assert.Equal(f.Texts.T("equip.btn.remove"), v.ActionText);

            f.SelectEquipHero("bispo");                                         // o Bispo não leva nada: o foco fica no Elmo
            v = f.EquipScreen();
            Assert.Equal("elmo", v.DetailId);
            Assert.Equal("rei", v.Items.Single(i => i.Id == "elmo").Owner);
            Assert.False(v.Items.Single(i => i.Id == "elmo").OnSelected);
            Assert.Equal(f.Texts.T("equip.btn.take"), v.ActionText);
            Assert.NotEqual("", v.DetailOwner);

            f.PressEquip();                                                     // o Bispo pega o Elmo do Rei
            Assert.Equal("elmo", f.Col.Equip["bispo"]);
            Assert.False(f.Col.Equip.ContainsKey("rei"));

            f.SelectEquipHero("rei");
            f.FocusEquip("anel");
            f.PressEquip();
            f.SelectEquipHero("bispo");
            Assert.Equal("elmo", f.EquipScreen().DetailId);                     // escolher um herói mostra o que ele leva
            f.PressEquip();                                                     // e o botão tira
            Assert.False(f.Col.Equip.ContainsKey("bispo"));
            Assert.Equal("anel", f.Col.Equip["rei"]);
        }

        [Fact]
        public void The_button_texts_exist_in_every_language()
        {
            var t = new Texts(new FileContentSource());
            foreach (var lang in Texts.Languages)
            {
                t.SetLang(lang);
                foreach (var k in new[] { "equip.btn.equip", "equip.btn.remove", "equip.btn.take", "heroes.tab.equip", "heroes.team" })
                    Assert.DoesNotContain("⟦", t.T(k));
            }
        }
    }
}
