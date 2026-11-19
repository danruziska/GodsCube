using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    public class GameFlowTests
    {
        private static GameFlow Make(MemoryFileStore store = null, uint seed = 777)
        {
            var f = new GameFlow(Kit.Data, new Texts(new FileContentSource()), store ?? new MemoryFileStore(), seed);
            f.Load();
            return f;
        }

        private static void Click(GameFlow f, string id)
        {
            var b = f.Screen().Button(id);
            Assert.True(b != null, "botão ausente: " + id + " (tela " + f.Stage + ")");
            Assert.True(b.Enabled, "botão desligado: " + id);
            b.Click();
        }

        // Joga alguns comandos legais (grupos e entregas), sem se importar com o resultado: só precisa haver o que rejogar.
        private static int Play(BattleSession s, int max)
        {
            var bot = new Mulberry32(5);
            int n = 0;
            while (n < max && s.State.Winner == null && s.IsPlayerTurn)
            {
                if (s.NeedsTarget)
                {
                    if (!s.TryTarget(Kit.Engine.ChooseTarget(s.State, Side.Player, "atento", bot))) break;
                }
                else if (s.IsMatching)
                {
                    var m = Kit.Engine.ChooseMove(s.State, Side.Player, "atento", bot);
                    if (m == null || !s.TryFire(m.Cell)) break;
                }
                else if (!s.TryHand(Kit.Engine.ChooseHand(s.State, Side.Player, "atento", bot))) break;
                s.PresentationDone(); n++;
            }
            return n;
        }

        // Joga a batalha de verdade com a IA no lugar do jogador (grupos e entregas), até alguém vencer. Devolve se o jogador venceu.
        private static bool PlayToEnd(GameFlow f)
        {
            var bot = new Mulberry32(1);
            var s = f.Session;
            for (int i = 0; i < 300 && s.State.Winner == null; i++)
            {
                if (!s.IsPlayerTurn) return false;
                if (!Kit.PlayTurn(s, bot)) return false;
            }
            return s.State.Winner == Side.Player;
        }

        private static void Win(GameFlow f) { f.Session.State.Winner = Side.Player; f.BattleFinished(); }

        [Fact]
        public void First_launch_opens_the_prep_screen_with_the_default_team()
        {
            var f = Make();
            Assert.Equal(Stage.Prep, f.Stage);
            Assert.True(f.PrepReady);
            Assert.Equal(Kit.Data.DefaultTeam, f.PrepTeam.ToArray());
        }

        [Fact]
        public void Choosing_a_fourth_hero_is_refused_and_a_hint_explains_why()
        {
            var f = Make();
            var free = Kit.Data.HeroIds.First(id => !f.PrepTeam.Contains(id));
            f.ToggleHero(free);
            Assert.Equal(3, f.PrepTeam.Count);
            Assert.Equal(f.Texts.T("prep.full"), f.HeroesScreen().Hint);
        }

        [Fact]
        public void Removing_a_hero_disables_start()
        {
            var f = Make();
            f.RemoveHero(f.PrepTeam[0]);
            Assert.False(f.PrepReady);
            f.StartRun();
            Assert.Equal(Stage.Prep, f.Stage);
        }

        [Fact]
        public void Start_creates_a_battle_and_announces_it()
        {
            var f = Make();
            BattleSession got = null;
            f.BattleCreated += s => got = s;
            f.StartRun();
            Assert.Equal(Stage.Battle, f.Stage);
            Assert.Same(f.Session, got);
            Assert.Null(f.Screen());
            Assert.Equal(1, f.Run.Floor);
        }

        [Fact]
        public void Winning_a_floor_pays_and_leads_to_the_shop_or_next_battle()
        {
            var f = Make();
            f.StartRun();
            Win(f);
            Assert.Equal(Kit.Data.Run.GoldBase + Kit.Data.Run.GoldPerFloor, f.Run.Gold);
            Assert.Contains(f.Stage, new[] { Stage.Blessing, Stage.Event, Stage.Shop, Stage.Door, Stage.Battle });
            Assert.Equal(2, f.Run.Floor);
        }

        [Fact]
        public void Losing_ends_the_run_and_again_returns_to_the_prep()
        {
            var f = Make();
            f.StartRun();
            f.Session.State.Winner = Side.Enemy; f.BattleFinished();
            Assert.Equal(Stage.End, f.Stage);
            Click(f, "again");
            Assert.Equal(Stage.Prep, f.Stage);
            Assert.Null(f.Run);
        }

        [Fact]
        public void A_whole_run_to_the_boss_ends_in_victory_and_counts_a_win()
        {
            var f = Make(null, 4242);
            f.StartRun();
            int guard = 0;
            while (f.Stage != Stage.End && guard++ < 200)
            {
                switch (f.Stage)
                {
                    case Stage.Battle: Win(f); break;
                    case Stage.Blessing: f.Screen().Buttons.First().Click(); break;
                    case Stage.Door: f.PickDoor(false); break;
                    case Stage.Event:
                        var opt = f.Screen().Buttons.First(b => b.Enabled);
                        opt.Click(); break;
                    case Stage.Shop: f.ShopLeave(); break;
                }
            }
            Assert.Equal(Stage.End, f.Stage);
            Assert.Equal(1, f.Col.Wins);
            Assert.Equal(Kit.Data.Run.Floors, f.Run.Floor);
        }

        [Fact]
        public void A_carried_poison_item_shows_up_as_an_upgrade_in_blessings_and_shops()
        {
            bool seen = false;
            for (uint seed = 1; seed <= 12 && !seen; seed++)
            {
                var f = Make(null, seed);
                f.SelectEquipHero("rei"); f.FocusEquip("presa"); f.PressEquip();
                f.StartRun();
                Assert.Equal("presa", f.Run.Equip["rei"]);
                int guard = 0;
                while (f.Stage != Stage.End && guard++ < 200)
                {
                    if (f.Stage == Stage.Blessing || f.Stage == Stage.Shop)
                    {
                        var text = string.Join("\n", f.Screen().Items.Select(i => i.Text));      // monta o texto sem quebrar
                        if (text.Contains("Presa de Víbora")) seen = true;
                    }
                    switch (f.Stage)
                    {
                        case Stage.Battle: Win(f); break;
                        case Stage.Blessing:
                            var pick = f.Screen().Buttons.FirstOrDefault(b => b.Id == "bless:presa") ?? f.Screen().Buttons.First();
                            pick.Click(); break;
                        case Stage.Door: f.PickDoor(false); break;
                        case Stage.Event: f.Screen().Buttons.First(b => b.Enabled).Click(); break;
                        case Stage.Shop: f.ShopLeave(); break;
                    }
                }
                if (seen) Assert.True(f.Run.Tiers.ContainsKey("presa") || f.Stage == Stage.End);
            }
            Assert.True(seen);
        }

        [Fact]
        public void The_whole_flow_is_deterministic_for_a_seed()
        {
            string Trace(uint seed)
            {
                var f = Make(null, seed); f.StartRun();
                var sb = new System.Text.StringBuilder(); int g = 0;
                while (f.Stage != Stage.End && g++ < 100)
                {
                    sb.Append(f.Stage).Append(f.Record?.Seed).Append(';');
                    if (f.Stage == Stage.Battle) Win(f);
                    else if (f.Stage == Stage.Blessing) f.Screen().Buttons.First().Click();
                    else if (f.Stage == Stage.Door) f.PickDoor(false);
                    else if (f.Stage == Stage.Event) f.Screen().Buttons.First(b => b.Enabled).Click();
                    else if (f.Stage == Stage.Shop) f.ShopLeave();
                }
                return sb.ToString();
            }
            Assert.Equal(Trace(9), Trace(9));
            Assert.NotEqual(Trace(9), Trace(10));
        }

        // ------------------------------------------------------------ salvamento

        [Fact]
        public void Settings_and_language_survive_a_restart()
        {
            var store = new MemoryFileStore();
            var f = Make(store);
            f.CycleLanguage();
            var again = Make(store);
            Assert.Equal("en", again.Texts.Lang);
            Assert.Equal("en", again.Settings.Lang);
        }

        [Fact]
        public void Every_screen_comes_back_after_a_restart() => Every_screen_comes_back(30);   // uma corrida em que o bot vence a primeira batalha

        private static void Every_screen_comes_back(uint Seed)
        {
            var store = new MemoryFileStore();
            var f = Make(store, Seed);
            f.StartRun();
            int g = 0;
            var seen = new System.Collections.Generic.HashSet<Stage>();
            while (f.Stage != Stage.End && g++ < 60)
            {
                var before = f.Stage;
                seen.Add(before);
                f.SaveRun();
                var again = Make(store, Seed);
                Assert.Equal(before, again.Stage);
                if (before != Stage.Battle) Assert.Equal(f.Screen().Items.Count, again.Screen().Items.Count);
                Assert.Equal(f.Run.Floor, again.Run.Floor);
                Assert.Equal(f.Run.Gold, again.Run.Gold);
                if (f.Stage == Stage.Battle) { PlayToEnd(f); if (f.Session.State.Winner == null) break; f.BattleFinished(); }
                else if (f.Stage == Stage.Blessing) f.Screen().Buttons.First().Click();
                else if (f.Stage == Stage.Door) f.PickDoor(false);
                else if (f.Stage == Stage.Event) f.Screen().Buttons.First(b => b.Enabled).Click();
                else if (f.Stage == Stage.Shop) f.ShopLeave();
            }
            Assert.True(seen.Contains(Stage.Battle) && seen.Contains(Stage.Blessing) && seen.Contains(Stage.Shop), "telas vistas: " + string.Join(",", seen));
        }

        [Fact]
        public void A_battle_in_progress_is_rebuilt_exactly_by_replaying_the_commands()
        {
            var store = new MemoryFileStore();
            var f = Make(store, 55);
            f.StartRun();
            Assert.True(Play(f.Session, 3) > 0);
            f.SaveRun();
            var again = Make(store, 55);
            Assert.Equal(Stage.Battle, again.Stage);
            Assert.Equal(f.Session.Commands.Count, again.Session.Commands.Count);
            Assert.Equal(f.Session.State.Hp, again.Session.State.Hp);
            Assert.Equal(f.Session.State.Colors, again.Session.State.Colors);
        }


        [Fact]
        public void The_player_can_keep_playing_after_the_battle_is_restored()
        {
            var store = new MemoryFileStore();
            var f = Make(store, 56);
            f.StartRun();
            Play(f.Session, 2);
            f.SaveRun();
            var again = Make(store, 56);
            var n = again.Session.Commands.Count;
            Assert.True(again.Session.IsPlayerTurn);
            Assert.Equal(1, Play(again.Session, 1));
            Assert.Equal(n + 1, again.Session.Commands.Count);
        }

        [Theory]
        [InlineData("")]
        [InlineData("não é json")]
        [InlineData("{}")]
        [InlineData("[1,2,3]")]
        [InlineData("{\"v\":1,\"stage\":\"Battle\",\"run\":{}}")]
        [InlineData("{\"v\":99,\"stage\":\"Shop\"}")]
        public void A_broken_run_file_falls_back_to_the_prep_without_throwing(string text)
        {
            var store = new MemoryFileStore();
            store.Files[GameFlow.RunFile] = text;
            var f = Make(store);
            Assert.Equal(Stage.Prep, f.Stage);
        }

        [Fact]
        public void A_tampered_command_list_is_refused_and_the_run_restarts()
        {
            var store = new MemoryFileStore();
            var f = Make(store, 57);
            f.StartRun();
            Play(f.Session, 2);                                   // escolhe o rival e dispara
            f.SaveRun();
            Assert.Contains("\"F:", store.Files[GameFlow.RunFile]);
            store.Files[GameFlow.RunFile] = store.Files[GameFlow.RunFile].Replace("\"F:", "\"X:");
            Assert.Equal(Stage.Prep, Make(store, 57).Stage);
        }

        // Uma batalha gravada antes de o giro sair do jogo tem comandos de giro ("S:"): não dá para refazer, e a
        // corrida recomeça em vez de abrir um tabuleiro diferente do que o jogador via.
        [Fact]
        public void A_saved_battle_with_an_old_spin_command_is_refused()
        {
            var store = new MemoryFileStore();
            var f = Make(store, 57);
            f.StartRun();
            Play(f.Session, 2);                                   // escolhe o rival e dispara
            f.SaveRun();
            Assert.Contains("\"F:", store.Files[GameFlow.RunFile]);
            store.Files[GameFlow.RunFile] = store.Files[GameFlow.RunFile].Replace("\"F:", "\"S:0:0:1\",\"F:");
            Assert.Equal(Stage.Prep, Make(store, 57).Stage);
        }

        [Fact]
        public void A_broken_collection_file_gives_a_fresh_collection()
        {
            var store = new MemoryFileStore();
            store.Files[GameFlow.CollectionFile] = "{{{{";
            var f = Make(store);
            Assert.True(Kit.Data.ValidTeam(f.Col.Team));
            Assert.True(f.PrepReady);
        }


        // Uma corrida inteira em cada idioma: nenhuma tela pode mostrar a marca de texto que falta (⟦chave⟧).
        [Theory]
        [InlineData("pt")]
        [InlineData("en")]
        [InlineData("es")]
        public void No_screen_of_a_whole_run_shows_a_missing_text(string lang)
        {
            var f = Make(null, 2024);
            while (f.Texts.Lang != lang) f.CycleLanguage();
            int seen = 0, g = 0;
            while (g++ < 200)
            {
                var sc = f.Screen();
                if (sc != null)
                {
                    seen++;
                    Assert.DoesNotContain("⟦", sc.Title);
                    foreach (var it in sc.Items) Assert.DoesNotContain("⟦", it.Text);
                }
                if (f.Stage == Stage.Prep) f.StartRun();
                else if (f.Stage == Stage.Battle) Win(f);
                else if (f.Stage == Stage.Blessing) f.Screen().Buttons.First().Click();
                else if (f.Stage == Stage.Door) f.PickDoor(g % 2 == 0);
                else if (f.Stage == Stage.Event) f.Screen().Buttons.First(b => b.Enabled).Click();
                else if (f.Stage == Stage.Shop) f.ShopLeave();
                else break;
            }
            Assert.True(seen > 8, "poucas telas vistas: " + seen);
        }
    }
}