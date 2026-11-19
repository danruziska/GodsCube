using System;
using System.Collections.Generic;
using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    // Os 3 eventos da corrida: o Ídolo (um pacto), o Acampamento (informação) e o Juramento (um desafio).
    public class EventTests
    {
        private static readonly GameData Data = Kit.Data;
        private static readonly RunService Runs = new RunService(Data, new CollectionService(Data), Kit.Engine);
        private static EventRules E => Data.Run.Event;

        // Uma corrida depois da 1ª batalha, com cada herói com 30 de vida.
        private static RunState AfterFirst(params int[] hp)
        {
            var run = Runs.Start(Kit.Royal);
            var st = Kit.New(1);
            st.HeroHp[0] = hp.Length == 3 ? hp : new[] { 30, 30, 30 };
            BattleEngine.SyncHp(st, Side.Player);
            Runs.Advance(run, st);
            return run;
        }

        private static BattleState Next(RunState run, uint seed = 5)
        {
            var o = Runs.Opts(run); o.Size = 6; o.Mode = "facil";
            return Kit.Engine.NewState(o, run.Team, new Mulberry32(seed));
        }

        [Fact]
        public void Each_run_plans_three_different_events_on_three_floors_between_battles()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                var run = Runs.Start(Kit.Royal);
                Runs.PlanEvents(run, new Mulberry32(seed));
                Assert.Equal(3, run.EventFloors.Count);
                Assert.Equal(3, run.EventFloors.Distinct().Count());
                Assert.All(run.EventFloors, f => Assert.InRange(f, 2, Data.Run.Floors));
                Assert.Equal(new[] { "acampamento", "idolo", "juramento" }, run.EventOrder.OrderBy(x => x));
                var seen = new List<string>();
                for (int f = 2; f <= Data.Run.Floors; f++)
                {
                    run.Floor = f;
                    var ev = Runs.EventFor(run);
                    Assert.Equal(run.EventFloors.Contains(f), ev != null);
                    if (ev != null) seen.Add(ev.Id);
                }
                Assert.Equal(3, seen.Distinct().Count());                          // nenhum repete
            }
        }

        [Fact]
        public void The_idols_blood_costs_max_health_on_the_chosen_hero_and_gives_crit_for_the_run()
        {
            var run = AfterFirst();
            int max1 = run.HeroMax[1], battleMax = Next(run).HeroMax[0][1];
            run.Event = new EventState { Id = "idolo", Variant = "rei" };
            Assert.NotNull(Runs.EventChoose(run, "blood:1", new Mulberry32(1)));
            Assert.Equal(max1 - E.IdolBlood, run.HeroMax[1]);
            var st = Next(run);
            Assert.Equal(battleMax - E.IdolBlood, st.HeroMax[0][1]);               // vale nas próximas batalhas
            Assert.Equal(Data.Rules.CritChance[0] + E.IdolCrit, st.Crit[0], 6);
            Runs.Advance(run, st);
            Assert.Equal(Data.Rules.CritChance[0] + E.IdolCrit, Next(run).Crit[0], 6);   // até o fim da corrida
        }

        [Fact]
        public void A_fallen_hero_cannot_give_blood()
        {
            var run = AfterFirst(30, 0, 30);
            run.Event = new EventState { Id = "idolo" };
            Assert.False(Runs.EventOptions(run).Single(o => o.Key == "blood:1").Ok);
            Assert.Null(Runs.EventChoose(run, "blood:1", new Mulberry32(1)));
        }

        [Fact]
        public void The_kings_wrath_angers_the_next_rival_and_makes_your_attacks_stronger_for_the_run()
        {
            var run = AfterFirst();
            double dmg = Runs.Opts(run).EnemyDmg;
            run.Event = new EventState { Id = "idolo", Variant = "rei" };
            Runs.EventChoose(run, "wrath", new Mulberry32(1));
            Assert.Equal(JsMath.Round2(dmg * E.IdolWrath), Runs.Opts(run).EnemyDmg, 2);
            var st = Next(run);
            int foe = BattleEngine.EffectiveTarget(st, Side.Player);
            Assert.Equal(JsMath.RoundInt(20 * (1 + E.IdolDmg)), Kit.Engine.Hit(st, Side.Player, 20, out _));
            Assert.True(foe >= 0);
            Runs.Advance(run, st);
            Assert.Equal(E.IdolDmg, Next(run).DmgBonus, 6);                     // até o fim da corrida
        }

        [Fact]
        public void The_queens_blood_fills_every_power_at_the_start_and_her_wrath_makes_powers_cheaper()
        {
            var run = AfterFirst();
            var before = Next(run);
            int cost = Kit.Engine.CostOf(before, Side.Player, 0);
            run.Event = new EventState { Id = "idolo", Variant = "rainha" };
            Runs.EventChoose(run, "blood:0", new Mulberry32(1));
            var st = Next(run);
            for (int k = 0; k < BattleEngine.Slots; k++)
                Assert.Equal(Math.Min(Data.Rules.ManaCap, before.Mana[0][k] + E.IdolMana), st.Mana[0][k]);
            run.Event = new EventState { Id = "idolo", Variant = "rainha" };
            Runs.EventChoose(run, "wrath", new Mulberry32(1));
            Assert.Equal(Math.Max(1, cost - E.IdolCost), Kit.Engine.CostOf(Next(run), Side.Player, 0));
        }

        [Fact]
        public void The_rooks_blood_shields_the_team_and_his_wrath_adds_max_health_to_every_hero()
        {
            var run = AfterFirst();
            var before = Next(run);
            run.Event = new EventState { Id = "idolo", Variant = "torre" };
            Runs.EventChoose(run, "blood:2", new Mulberry32(1));
            Assert.Equal(1 - E.IdolWard, Kit.Engine.WardMult(Next(run)), 6);
            int[] max = (int[])run.HeroMax.Clone(), hp = (int[])run.HeroHp.Clone();
            run.Event = new EventState { Id = "idolo", Variant = "torre" };
            Runs.EventChoose(run, "wrath", new Mulberry32(1));
            for (int i = 0; i < 3; i++) Assert.Equal(max[i] + E.IdolHp, run.HeroMax[i]);
            for (int i = 0; i < 3; i++) Assert.Equal(hp[i] + E.IdolHp, run.HeroHp[i]);
            var st = Next(run);
            Assert.Equal(before.HeroMax[0][0] + E.IdolHp, st.HeroMax[0][0]);
        }

        [Fact]
        public void Each_run_meets_one_of_the_three_idols_and_keeps_it_through_a_save()
        {
            var seen = new HashSet<string>();
            for (uint seed = 1; seed <= 40; seed++)
            {
                var run = Runs.Start(Kit.Royal);
                Runs.PlanEvents(run, new Mulberry32(seed));
                Assert.Contains(run.Idol, E.Idols);
                seen.Add(run.Idol);
                run.Floor = run.EventFloors[run.EventOrder.IndexOf("idolo")];
                var ev = Runs.EventFor(run);
                Assert.Equal(run.Idol, ev.Variant);
                run.Event = ev;
                var codec = new SaveCodec(Data, new CollectionService(Data));
                var back = codec.RunFrom(codec.ToJson(run));
                Assert.Equal(run.Idol, back.Idol);
                Assert.Equal(run.Idol, back.Event.Variant);
            }
            Assert.Equal(3, seen.Count);
        }

        [Fact]
        public void The_camp_ambush_weakens_only_the_next_rival()
        {
            var run = AfterFirst();
            int plain = Next(run).Hp[1];
            run.Event = new EventState { Id = "acampamento" };
            Runs.EventChoose(run, "ambush", new Mulberry32(1));
            var st = Next(run);
            Assert.InRange(st.Hp[1], (int)(plain * (1 - E.CampAmbush)) - 6, (int)(plain * (1 - E.CampAmbush)) + 6);
            Runs.Advance(run, st);
            Assert.Equal(1, run.NextEnemyHp);
        }

        [Fact]
        public void Marking_makes_the_strongest_rival_take_more_damage_in_the_next_battle_only()
        {
            var run = AfterFirst();
            run.Event = new EventState { Id = "acampamento" };
            Runs.EventChoose(run, "mark", new Mulberry32(1));
            var st = Next(run);
            Assert.True(st.Marked >= 0);
            for (int i = 0; i < 3; i++) Assert.True(st.HeroMax[1][st.Marked] >= st.HeroMax[1][i]);
            // o mesmo golpe tira 25% a mais do marcado do que tiraria sem a marca
            int hp = st.HeroHp[1][st.Marked];
            st.Target[0] = st.Marked;
            int dealt = Kit.Engine.Hit(st, Side.Player, 8, out int hero);
            Assert.Equal(st.Marked, hero);
            Assert.Equal(10, dealt);
            // e só o jogador: o rival não ganha nada com a marca
            Assert.Equal(-1, Next(AfterFirst()).Marked);
            Runs.Advance(run, st);
            Assert.Equal(0, run.NextMark);
            Assert.Equal(-1, Next(run).Marked);
        }

        [Fact]
        public void Raiding_gives_gold()
        {
            var run = AfterFirst();
            int gold = run.Gold;
            run.Event = new EventState { Id = "acampamento" };
            Runs.EventChoose(run, "raid", new Mulberry32(1));
            Assert.Equal(gold + E.CampRaid, run.Gold);
        }

        [Fact]
        public void A_vow_kept_pays_and_a_broken_one_does_not()
        {
            foreach (var (vow, breakIt) in new (string, System.Action<BattleState>)[]
            {
                ("swift", st => st.Round = E.VowRounds + 1),
                ("silent", st => st.PlayerCasts = 1),
                ("unbroken", st => { st.HeroHp[0][2] = 0; BattleEngine.SyncHp(st, Side.Player); })
            })
            {
                foreach (bool broken in new[] { false, true })
                {
                    var run = AfterFirst();
                    run.Event = new EventState { Id = "juramento" };
                    Runs.EventChoose(run, "vow:" + vow, new Mulberry32(1));
                    Assert.Equal(vow, run.Vow);
                    var st = Next(run);
                    Assert.Equal(vow, st.Vow);
                    st.Winner = Side.Player;
                    if (broken) breakIt(st);
                    int gold = run.Gold;
                    var r = Runs.CheckVow(run, st);
                    Assert.Equal(!broken, r.Kept);
                    Assert.Equal(gold + (broken ? 0 : E.VowGold), run.Gold);
                    Runs.Advance(run, st);
                    Assert.Null(run.Vow);                                           // o voto valia só para essa batalha
                }
            }
        }

        [Fact]
        public void A_hero_already_fallen_does_not_break_the_shield_vow()
        {
            var run = AfterFirst(30, 0, 30);
            run.Vow = "unbroken";
            var st = Next(run);
            Assert.False(st.StartAlive[1]);
            st.Winner = Side.Player;
            Assert.True(Runs.CheckVow(run, st).Kept);
        }

        [Fact]
        public void Casting_a_power_counts_for_the_silent_vow()
        {
            var st = Kit.New(2);
            st.Vow = "silent";
            Kit.FillMana(st, Side.Player);
            Assert.True(Kit.Engine.VowHolds(st));
            Kit.Engine.Cast(st, Side.Player, 0, new Mulberry32(1));
            Assert.False(Kit.Engine.VowHolds(st));
        }

        [Fact]
        public void The_run_keeps_event_plans_and_effects_through_the_save()
        {
            var run = AfterFirst();
            Runs.PlanEvents(run, new Mulberry32(3));
            run.BonusCrit = 0.1; run.BonusCombo = 0.15; run.MaxHpMod = new[] { 0, -10, 0 };
            run.NextEnemyHp = 0.8; run.NextMark = 0.25; run.Vow = "silent";
            var codec = new SaveCodec(Data, new CollectionService(Data));
            var back = codec.RunFrom(codec.ToJson(run));
            Assert.Equal(run.EventFloors, back.EventFloors);
            Assert.Equal(run.EventOrder, back.EventOrder);
            Assert.Equal((0.1, 0.15, 0.8, 0.25, "silent"), (back.BonusCrit, back.BonusCombo, back.NextEnemyHp, back.NextMark, back.Vow));
            Assert.Equal(new[] { 0, -10, 0 }, back.MaxHpMod);
        }

        // ---- a tela desenhada, pelo fluxo de verdade ----

        private static GameFlow AtEvent(string id)
        {
            var f = new GameFlow(Data, new Texts(new FileContentSource()), new MemoryFileStore(), 9);
            f.Load(); f.Texts.SetLang("en"); f.StartRun();
            f.Run.EventFloors = new List<int> { 2 }; f.Run.EventOrder = new List<string> { id }; f.Run.Idol = "rei";
            f.Session.State.Winner = Side.Player; f.BattleFinished();
            if (f.Stage == Stage.Blessing) f.Screen().Buttons.First().Click();
            Assert.Equal(Stage.Event, f.Stage);
            return f;
        }

        [Fact]
        public void The_idol_screen_lists_each_heros_blood_and_wrath_and_confirms_the_choice()
        {
            var f = AtEvent("idolo");
            var v = f.EventView();
            Assert.Equal("The King's Idol", v.Title);
            Assert.Equal(new[] { "blood:0", "blood:1", "blood:2", "wrath" }, v.Options.Select(o => o.Key));
            Assert.All(v.Options.Where(o => o.Group == "blood"), o => Assert.NotNull(o.HeroId));
            Assert.False(v.CanAct);
            Assert.Equal("Choose an option", v.ActionText);
            f.FocusEvent("wrath");
            v = f.EventView();
            Assert.True(v.CanAct);
            Assert.Equal("Make the bargain", v.ActionText);
            f.EventAct();
            v = f.EventView();
            Assert.True(v.Done);
            Assert.Contains("damage", v.Result);
            Assert.Equal("Continue", v.ActionText);
            f.EventAct();
            Assert.NotEqual(Stage.Event, f.Stage);
        }

        [Fact]
        public void The_camp_screen_shows_the_scouts_report_on_the_next_rival()
        {
            var f = AtEvent("acampamento");
            var v = f.EventView();
            Assert.Contains("Rival health", v.Info);
            Assert.Contains("strongest", v.Info);
            Assert.DoesNotContain("Fire", v.Info);
            Assert.Equal(new[] { "ambush", "mark", "raid" }, v.Options.Select(o => o.Key));
            f.EventLeave();
            Assert.NotEqual(Stage.Event, f.Stage);
        }

        [Fact]
        public void A_vow_shows_in_the_next_battles_pills_and_its_result_on_the_next_screen()
        {
            var f = AtEvent("juramento");
            f.FocusEvent("vow:silent");
            f.EventAct(); f.EventAct();
            for (int g = 0; g < 10 && f.Stage != Stage.Battle; g++)
            {
                if (f.Stage == Stage.Shop) f.ShopLeave();
                else if (f.Stage == Stage.Door) f.PickDoor(false);
                else if (f.Stage == Stage.Blessing) f.Screen().Buttons.First().Click();
            }
            Assert.Equal("silent", f.Session.State.Vow);
            Assert.Contains("Vow: no powers", HudModel.Pills(f.Session.Engine, f.Session.State, f.Texts));
            f.Session.State.Winner = Side.Player; f.BattleFinished();
            Assert.Equal("Vow kept! +80 gold.", f.Notice);
        }
    
        // ---- o atalho de teste: abrir qualquer tela de qualquer ponto ----

        [Theory]
        [InlineData("blessing", Stage.Blessing)]
        [InlineData("idolo:rei", Stage.Event)]
        [InlineData("idolo:rainha", Stage.Event)]
        [InlineData("idolo:torre", Stage.Event)]
        [InlineData("acampamento", Stage.Event)]
        [InlineData("juramento", Stage.Event)]
        [InlineData("shop", Stage.Shop)]
        [InlineData("win", Stage.End)]
        [InlineData("lose", Stage.End)]
        public void Preview_opens_any_screen_from_the_preparation(string screen, Stage stage)
        {
            var f = new GameFlow(Data, new Texts(new FileContentSource()), new MemoryFileStore(), 9);
            f.Load();
            f.Preview(screen);
            Assert.Equal(stage, f.Stage);
            Assert.NotNull(f.Run.HeroHp);
            if (stage == Stage.Event) Assert.Equal(screen, f.Run.Event.Id + (f.Run.Event.Variant != null ? ":" + f.Run.Event.Variant : ""));
        }

        [Fact]
        public void Preview_from_a_battle_and_leaving_goes_to_the_next_battle()
        {
            var f = new GameFlow(Data, new Texts(new FileContentSource()), new MemoryFileStore(), 9);
            f.Load(); f.StartRun();
            if (f.Stage == Stage.Door) f.PickDoor(false);
            Assert.Equal(Stage.Battle, f.Stage);
            f.Preview("shop");
            Assert.Equal(Stage.Shop, f.Stage);
            f.PreviewGold(500);
            f.PreviewHurt();
            Assert.Equal(0, f.Run.HeroHp[2]);
            f.ShopLeave();
            if (f.Stage == Stage.Door) f.PickDoor(false);
            Assert.Equal(Stage.Battle, f.Stage);
            Assert.NotNull(f.Session);
        }
}
}
