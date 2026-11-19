using System.Collections.Generic;
using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    // Veneno e fogo: o item arma com um grupo tocado de 4+, a área vai na face entregue, quem combinar nela pega o
    // efeito no herói que o outro lado ataca, e o efeito tira vida no começo de cada turno do lado dele.
    public class HazardTests
    {
        // As regras destes testes são as do jogo, mas com a área de 3x3 fixa: o rules.json pode trocar o tamanho
        // (para testar no aparelho) sem quebrar as contas daqui.
        private static readonly BattleEngine E = Area3();

        private static BattleEngine Area3()
        {
            var d = Cubo.Data.ContentLoader.Load(new FileContentSource());
            d.Rules.HazardArea = 3;
            return new BattleEngine(d);
        }
        private static Dictionary<string, string> Poison => new Dictionary<string, string> { { "rei", "presa" } };
        private static Dictionary<string, string> Fire => new Dictionary<string, string> { { "rei", "brasa" } };

        private static int Count(BattleState st, int kind) => BattleEngine.HazardMap(st).Count(k => k == kind);

        // Um tabuleiro sem grupos, com a cor 5 (que NoGroups não usa) onde a linha pede.
        private static BattleState Board(Dictionary<string, string> equip, params string[] rows)
        {
            var st = Kit.NewWith(equip);
            Kit.NoGroups(st);
            Kit.Front(st, rows);
            st.Target[0] = 0;
            return st;
        }

        // A área já posta pelo jogador com centro em (x, y), e o rival a jogar.
        private static BattleState Placed(Dictionary<string, string> equip, int x, int y)
        {
            var st = Kit.NewWith(equip);
            st.Pending[0] = st.Equip[0];
            st.Turn = Side.Enemy;
            st.Phase = Phase.Zone;
            Assert.NotNull(E.PlaceHazard(st, Side.Player, Kit.Cell(st, x, y)));
            return st;
        }

        [Fact]
        public void The_new_items_load_poison_longer_and_fire_stronger()
        {
            var p = Kit.Data.EquipById["presa"]; var f = Kit.Data.EquipById["brasa"];
            Assert.Equal(EquipKinds.Poison, p.Kind);
            Assert.Equal(EquipKinds.Burn, f.Kind);
            Assert.True(f.N > p.N);
            Assert.True(f.Turns < p.Turns);
            Assert.Equal(4, Kit.Data.Rules.HazardMin);
            Assert.InRange(Kit.Data.Rules.HazardArea, 1, 8);
            Assert.Equal(3, Kit.Data.Rules.HazardRounds);
        }

        [Fact]
        public void The_cube_is_six_by_six_by_default()
        {
            Assert.Equal(6, Kit.Data.Rules.DefaultSize);
            Assert.Equal(6, new BattleRecord().Size);
            Assert.Equal(6, E.NewState(new BattleOptions { Size = 0, Mode = "facil" }, Kit.Royal, new Mulberry32(1)).N);
        }

        [Fact]
        public void A_tapped_group_of_four_arms_the_item_and_three_does_not()
        {
            var four = Board(Poison, ".....", ".....", ".....", ".....", "5555.");
            var r = E.Fire(four, Side.Player, Kit.Cell(four, 0, 0), new Mulberry32(1));
            Assert.True(r.Armed);
            Assert.Equal(EquipKinds.Poison, four.Pending[0].Kind);

            var three = Board(Poison, ".....", ".....", ".....", ".....", "555..");
            Assert.False(E.Fire(three, Side.Player, Kit.Cell(three, 0, 0), new Mulberry32(1)).Armed);
            Assert.Null(three.Pending[0]);

            var plain = Board(null, ".....", ".....", ".....", ".....", "5555.");
            Assert.False(E.Fire(plain, Side.Player, Kit.Cell(plain, 0, 0), new Mulberry32(1)).Armed);
        }

        [Fact]
        public void After_the_handover_the_player_places_the_area_before_the_rival_plays()
        {
            var st = Board(Fire, ".....", ".....", ".....", ".....", "5555.");
            E.Fire(st, Side.Player, Kit.Cell(st, 0, 0), new Mulberry32(1));
            Assert.Equal(Phase.Hand, st.Phase);
            var face = E.HandOptions(st, Side.Player)[0];
            var h = E.HandOver(st, Side.Player, face, new Mulberry32(2));
            Assert.True(h.Zone);
            Assert.Equal(Side.Enemy, st.Turn);
            Assert.Equal(Phase.Zone, st.Phase);
            Assert.False(BattleEngine.InMatch(st, Side.Enemy));                     // o rival espera
            Assert.Null(E.PlaceHazard(st, Side.Player, 0));                         // fora da frente não vale
            var cells = E.PlaceHazard(st, Side.Player, Kit.Cell(st, 2, 2));
            Assert.Equal(9, cells.Length);
            Assert.Equal(9, Count(st, 2));                                          // fogo
            Assert.Null(st.Pending[0]);
            Assert.True(BattleEngine.InMatch(st, Side.Enemy));
        }

        [Fact]
        public void The_finger_is_the_centre_and_the_area_stays_inside_the_face()
        {
            var g5 = Geometry.For(5);
            Assert.Equal(new[] { 1, 2, 3 }.SelectMany(x => new[] { 1, 2, 3 }.Select(y => BattleEngine.FrontCell(g5, x, y))).OrderBy(c => c),
                         E.HazardArea(5, BattleEngine.FrontCell(g5, 2, 2)));
            var corner = E.HazardArea(5, BattleEngine.FrontCell(g5, 0, 4));          // canto: a área entra na face
            Assert.Equal(new[] { 0, 1, 2 }.SelectMany(x => new[] { 2, 3, 4 }.Select(y => BattleEngine.FrontCell(g5, x, y))).OrderBy(c => c), corner);
            var g6 = Geometry.For(6);
            Assert.Equal(9, E.HazardArea(6, BattleEngine.FrontCell(g6, 3, 4)).Length);
            Assert.Empty(E.HazardArea(5, 0));                                       // uma casa que não é da frente
        }

        [Fact]
        public void A_group_through_the_area_poisons_the_hero_the_other_side_attacks_and_the_area_goes()
        {
            var st = Placed(Poison, 1, 1);                                           // área de (0,0) a (2,2)
            Kit.NoGroups(st);
            Kit.Front(st, ".....", ".....", ".....", ".....", "55...");             // um par dentro da área
            st.Phase = Phase.Match;
            st.Target[0] = 2;                                                        // o jogador ataca o terceiro rival
            var r = E.Fire(st, Side.Enemy, Kit.Cell(st, 0, 0), new Mulberry32(1));
            var a = Assert.Single(r.Struck);
            Assert.Equal(Side.Enemy, a.Side);
            Assert.Equal(2, a.Hero);
            Assert.Equal(3, st.Dots[1][2][0].Turns);
            Assert.Equal(3, st.Dots[1][2][0].Dmg);
            Assert.Equal(0, Count(st, 1));                                           // a área pegou e sumiu
        }

        [Fact]
        public void A_group_outside_the_area_does_not_trigger_it()
        {
            var st = Placed(Poison, 1, 1);
            Kit.NoGroups(st);
            Kit.Front(st, "...55", ".....", ".....", ".....", ".....");
            st.Phase = Phase.Match;
            var r = E.Fire(st, Side.Enemy, Kit.Cell(st, 3, 4), new Mulberry32(1));
            Assert.Empty(r.Struck);
            Assert.Equal(9, Count(st, 1));                                           // a área continua lá
        }

        [Fact]
        public void The_effect_hurts_at_the_start_of_each_of_its_sides_turns_then_ends()
        {
            var st = Kit.New(3);
            st.Dots[1][0][1].Dmg = 5; st.Dots[1][0][1].Turns = 2;                  // o primeiro rival queimando
            int hp = st.HeroHp[1][0];
            for (int k = 0; k < 3; k++)
            {
                st.Turn = Side.Player; st.Phase = Phase.Hand;
                var h = E.HandOver(st, Side.Player, E.HandOptions(st, Side.Player)[0], new Mulberry32(9));
                int expected = hp - 5 * System.Math.Min(k + 1, 2);
                Assert.Equal(expected, st.HeroHp[1][0]);
                Assert.Equal(k < 2 ? 1 : 0, h.Ticks.Count);
            }
            Assert.False(st.Dots[1][0][1].Active);
        }

        [Fact]
        public void Getting_hit_again_renews_the_turns_and_keeps_the_stronger_damage()
        {
            var st = Placed(Poison, 1, 1);
            st.Dots[1][0][0].Dmg = 7; st.Dots[1][0][0].Turns = 1;
            Kit.NoGroups(st);
            Kit.Front(st, ".....", ".....", ".....", ".....", "55...");
            st.Phase = Phase.Match;
            st.Target[0] = 0;
            E.Fire(st, Side.Enemy, Kit.Cell(st, 0, 0), new Mulberry32(1));
            Assert.Equal(7, st.Dots[1][0][0].Dmg);
            Assert.Equal(3, st.Dots[1][0][0].Turns);
        }

        [Fact]
        public void The_hud_lists_the_effects_in_the_order_they_began_and_renewing_keeps_the_place()
        {
            // fogo primeiro, depois veneno: a tela mostra nessa ordem (não a ordem fixa veneno, fogo)
            var st = Placed(Fire, 1, 1);
            Kit.NoGroups(st);
            Kit.Front(st, ".....", ".....", ".....", ".....", "55...");
            st.Phase = Phase.Match; st.Target[0] = 0;
            E.Fire(st, Side.Enemy, Kit.Cell(st, 0, 0), new Mulberry32(1));
            st.Dots[1][0][0].Dmg = 3; st.Dots[1][0][0].Turns = 3; st.Dots[1][0][0].Since = ++st.DotClock;
            var (s, _) = (new BattleSession(E, st, new Mulberry32(1), "atento"), 0);
            var t = new Texts(new FileContentSource());
            t.SetLang("en");
            var kinds = HudModel.Build(s, t).EnemyTeam[0].Statuses.Select(x => x.Kind).ToArray();
            Assert.Equal(new[] { EquipKinds.Burn, EquipKinds.Poison }, kinds);
            Assert.Empty(HudModel.Build(s, t).EnemyTeam[1].Statuses);
            var burn = HudModel.Build(s, t).EnemyTeam[0].Statuses[0];
            Assert.Equal("Burn\nLoses 5 health at the start of each round.\n2 rounds left.", burn.Detail);
            st.Dots[1][0][1].Turns = 1;
            Assert.EndsWith("1 round left.", HudModel.Build(s, t).EnemyTeam[0].Statuses[0].Detail);
        }

        [Fact]
        public void A_hero_keeps_one_effect_and_a_different_one_takes_its_place()
        {
            var st = Placed(Poison, 1, 1);
            st.Dots[1][0][1].Dmg = 5; st.Dots[1][0][1].Turns = 2; st.Dots[1][0][1].Since = ++st.DotClock;   // já queimando
            Kit.NoGroups(st);
            Kit.Front(st, ".....", ".....", ".....", ".....", "55...");
            st.Phase = Phase.Match; st.Target[0] = 0;
            E.Fire(st, Side.Enemy, Kit.Cell(st, 0, 0), new Mulberry32(1));
            Assert.False(st.Dots[1][0][1].Active);                                  // o fogo saiu
            Assert.True(st.Dots[1][0][0].Active);                                   // o veneno entrou
            Assert.Equal(3, st.Dots[1][0][0].Dmg);
            var s = new BattleSession(E, st, new Mulberry32(1), "atento");
            Assert.Single(HudModel.Build(s, new Texts(new FileContentSource())).EnemyTeam[0].Statuses);
        }

        [Fact]
        public void Item_levels_grow_the_area_to_six_and_raise_damage_and_turns()
        {
            var k = Kit.Engine;                                                      // as regras do jogo: área de base 3
            Assert.Equal(6, k.MaxTier("presa"));
            Assert.Equal(6, k.MaxTier("brasa"));
            var p = k.UpgradedEquip(Kit.Data.EquipById["presa"], 6);
            Assert.Equal((5, 4, 6), (p.N, p.Turns, k.AreaOf(p)));
            var f = k.UpgradedEquip(Kit.Data.EquipById["brasa"], 6);
            Assert.Equal((9, 3, 6), (f.N, f.Turns, k.AreaOf(f)));
            Assert.Equal(4, k.AreaOf(k.UpgradedEquip(Kit.Data.EquipById["presa"], 1)));
            Assert.Same(Kit.Data.EquipById["presa"], k.UpgradedEquip(Kit.Data.EquipById["presa"], 0));
            Assert.Equal(3, Kit.Data.EquipById["presa"].N);                         // a tabela não muda
        }

        [Fact]
        public void A_battle_uses_the_items_level_and_the_area_never_leaves_the_face()
        {
            var k = Kit.Engine;
            var st = k.NewState(new BattleOptions { Size = 6, Mode = "facil", Equip = Poison,
                                                    Tiers = new Dictionary<string, int> { { "presa", 1 } } }, Kit.Royal, new Mulberry32(1));
            Assert.Equal(4, k.AreaOf(st.Equip[0]));
            st.Pending[0] = st.Equip[0]; st.Turn = Side.Enemy; st.Phase = Phase.Zone;
            Assert.Equal(16, k.PlaceHazard(st, Side.Player, Kit.Cell(st, 0, 0)).Length);

            var small = k.NewState(new BattleOptions { Size = 5, Mode = "facil", Equip = Poison,
                                                       Tiers = new Dictionary<string, int> { { "presa", 6 } } }, Kit.Royal, new Mulberry32(1));
            small.Pending[0] = small.Equip[0]; small.Turn = Side.Enemy; small.Phase = Phase.Zone;
            Assert.Equal(25, k.PlaceHazard(small, Side.Player, Kit.Cell(small, 2, 2)).Length);   // 6x6 num cubo 5x5: a face inteira
        }

        [Fact]
        public void The_run_offers_levels_only_for_a_carried_poison_or_fire_item()
        {
            var runs = new RunService(Kit.Data, new CollectionService(Kit.Data), Kit.Engine);
            var run = new RunState { Team = (string[])Kit.Royal.Clone(), Equip = new Dictionary<string, string> { { "rei", "presa" }, { "cavaleiro", "lamina" } } };
            Assert.Contains("presa", runs.TeamUpgrades(run));
            Assert.DoesNotContain("lamina", runs.TeamUpgrades(run));                // a Lâmina não tem níveis
            Assert.DoesNotContain("brasa", runs.TeamUpgrades(run));                 // ninguém leva
            run.Blessing = new List<string> { "presa" };
            Assert.True(runs.Bless(run, "presa"));
            Assert.Equal(1, runs.TierOf(run, "presa"));
            var next = runs.PreviewItem(run, "presa");                               // o 2º nível: +1 de dano, área ainda 4x4
            Assert.Equal((4, 4), (next.N, next.Area));
            run.Tiers["presa"] = 6;
            Assert.False(runs.CanUpgrade(run, "presa"));
        }

        [Fact]
        public void The_last_tick_can_win_the_battle()
        {
            var st = Kit.New(4);
            for (int h = 0; h < 3; h++) st.HeroHp[1][h] = h == 0 ? 2 : 0;
            BattleEngine.SyncHp(st, Side.Enemy);
            st.Dots[1][0][0].Dmg = 3; st.Dots[1][0][0].Turns = 3;
            st.Phase = Phase.Hand;
            E.HandOver(st, Side.Player, E.HandOptions(st, Side.Player)[0], new Mulberry32(1));
            Assert.Equal(Side.Player, st.Winner);
        }

        [Fact]
        public void The_area_turns_with_its_face_and_fades_after_three_rounds()
        {
            var st = Placed(Poison, 2, 2);
            int faceId = st.FaceId[Faces.Front];
            int until = st.Round + Kit.Data.Rules.HazardRounds;
            var g = Geometry.For(st.N);
            for (int guard = 0; guard < 20 && st.Round < until; guard++)
            {
                var side = st.Turn;
                st.Phase = Phase.Hand;
                E.HandOver(st, side, E.HandOptions(st, side)[0], new Mulberry32((uint)guard + 1));
                if (st.Round >= until) break;
                var cells = Enumerable.Range(0, g.Total).Where(c => st.HazardAt[c] != 0).ToList();
                Assert.Equal(9, cells.Count);
                Assert.All(cells, c => Assert.Equal(faceId, st.FaceId[g.CellFace[c]]));   // sempre na face em que foi posta
            }
            Assert.Equal(0, Count(st, 1));
            Assert.Empty(st.Hazards);
        }

        [Fact]
        public void The_rival_steers_around_the_area_when_it_can()
        {
            var st = Placed(Poison, 1, 1);
            Kit.NoGroups(st);
            Kit.Front(st, "....5", "....5", ".....", ".....", "55...");             // dois pares: um na área, outro fora
            st.Phase = Phase.Match;
            var m = E.ChooseMove(st, Side.Enemy, "implacavel", new Mulberry32(1));
            Assert.Contains(m.Cell, new[] { Kit.Cell(st, 4, 4), Kit.Cell(st, 4, 3) });
        }

        [Fact]
        public void The_session_waits_for_the_area_records_it_and_a_replay_gives_the_same_battle()
        {
            for (uint seed = 1; seed < 400; seed++)
            {
                var rec = new BattleRecord { Seed = seed, Team = (string[])Kit.Royal.Clone(), Size = 6, Equip = Poison };
                var runs = new RunService(Kit.Data, new CollectionService(Kit.Data), E);
                var s = BattleBuilder.Create(E, runs, rec);
                var g = Geometry.For(s.State.N);
                var big = E.EvaluateMoves(s.State, g, Side.Player).FirstOrDefault(m => m.Size >= 4);
                if (big == null) continue;
                var log = new List<IBattleEvent>();
                s.Played += ev => log.AddRange(ev);
                Assert.True(s.TryTarget(0)); s.PresentationDone();
                Assert.True(s.TryFire(big.Cell)); s.PresentationDone();
                if (!s.IsHanding) continue;                                          // a cascata acabou a batalha (não acontece)
                Assert.Contains(log, e => e is HazardArmed);
                Assert.True(s.TryHand(s.HandFaces[0])); s.PresentationDone();
                Assert.True(s.IsZoning);
                Assert.Equal(EquipKinds.Poison, s.ZoneKind);
                Assert.Contains(log, e => e is ZonePhase);
                Assert.Equal(Side.Enemy, s.State.Turn);                              // o rival ainda não jogou
                int center = BattleEngine.FrontCell(g, 2, 3);
                Assert.Equal(9, s.ZonePreview(center).Length);
                Assert.True(s.TryZone(center)); s.PresentationDone();
                Assert.Contains(s.Commands, c => c.Kind == 'Z' && c.A == center);
                Assert.True(s.State.Winner != null || s.State.Turn == Side.Player);  // o rival jogou

                rec.Commands = s.Commands.ToList();
                var again = BattleBuilder.Replay(E, runs, rec);
                Assert.NotNull(again);
                Assert.Equal(s.State.Colors, again.State.Colors);
                Assert.Equal(BattleEngine.HazardMap(s.State), BattleEngine.HazardMap(again.State));
                Assert.Equal(s.State.HeroHp[1], again.State.HeroHp[1]);
                return;
            }
            Assert.Fail("nenhuma semente com um grupo de 4+ no começo");
        }

        [Fact]
        public void Whole_battles_with_poison_and_fire_finish()
        {
            foreach (var equip in new[] { Poison, Fire })
                for (uint seed = 1; seed <= 6; seed++)
                {
                    var (s, _) = (BattleBuilder.Create(E, new RunService(Kit.Data, new CollectionService(Kit.Data), E),
                        new BattleRecord { Seed = seed, Team = (string[])Kit.Royal.Clone(), Size = 6, Equip = equip }), 0);
                    var bot = new Mulberry32(seed * 7);
                    for (int turn = 0; turn < 200 && s.State.Winner == null; turn++) Assert.True(Kit.PlayTurn(s, bot));
                    Assert.NotNull(s.State.Winner);
                }
        }
    }
}
