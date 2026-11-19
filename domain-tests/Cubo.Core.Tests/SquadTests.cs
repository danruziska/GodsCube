using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    // 3 contra 3: cada herói com a sua vida, o alvo escolhido a cada turno e os elementos.
    public class SquadTests
    {
        private static BattleEngine E => Kit.Engine;

        [Fact]
        public void Each_side_splits_its_health_across_three_heroes()
        {
            var st = Kit.New(1);
            for (int s = 0; s < 2; s++)
            {
                Assert.Equal(st.Hp[s], st.HeroHp[s].Sum());
                Assert.Equal(st.MaxHp[s], st.HeroMax[s].Sum());
                Assert.True(st.HeroMax[s].Max() - st.HeroMax[s].Min() <= 1);
            }
        }

        [Fact]
        public void Damage_goes_only_to_the_chosen_target()
        {
            var st = Kit.New(2);
            Kit.NoGroups(st);
            Kit.Front(st, "55500", "30303", "03030", "30303", "03030");   // um trio de ar: pouco dano, ninguém cai
            Assert.True(E.SetTarget(st, Side.Player, 1));
            var before = (int[])st.HeroHp[1].Clone();
            var r = E.Fire(st, Side.Player, Kit.Cell(st, 0, 4), new Mulberry32(1));
            Assert.Equal(1, r.Target);
            Assert.Equal(before[0], st.HeroHp[1][0]);
            Assert.Equal(before[2], st.HeroHp[1][2]);
            Assert.True(st.HeroHp[1][1] < before[1]);
        }

        [Fact]
        public void A_fallen_target_is_cleared_and_the_side_loses_only_when_all_three_fall()
        {
            var st = Kit.New(3);
            st.HeroHp[1][0] = 1; st.HeroHp[1][1] = 1; BattleEngine.SyncHp(st, Side.Enemy);
            E.SetTarget(st, Side.Player, 0);
            Assert.Equal(1, E.Hit(st, Side.Player, 50, out int h));
            Assert.Equal(0, h);
            Assert.Equal(-1, st.Target[0]);                         // caiu: escolher de novo
            Assert.False(E.SetTarget(st, Side.Player, 0));          // não dá para mirar num caído
            Assert.Null(E.CheckWinner(st, Side.Player));
            E.Hit(st, Side.Player, 50, out h);                      // sem alvo: o primeiro de pé
            Assert.Equal(1, h);
            Assert.Null(E.CheckWinner(st, Side.Player));
            E.Hit(st, Side.Player, 999, out h);
            Assert.Equal(Side.Player, E.CheckWinner(st, Side.Player));
        }

        [Fact]
        public void A_fallen_hero_cannot_cast_and_his_passive_stops()
        {
            var st = Kit.New(4);                                     // Rei, Cavaleiro (Investida), Bispo
            Kit.FillMana(st, Side.Player);
            Assert.True(E.CanCast(st, Side.Player, 2));
            Assert.True(E.Has(st, Side.Player, Passives.Charge));
            st.HeroHp[0][1] = 0; BattleEngine.SyncHp(st, Side.Player);
            Assert.False(E.CanCast(st, Side.Player, 2));
            Assert.False(E.CanCast(st, Side.Player, 3));
            Assert.False(E.Has(st, Side.Player, Passives.Charge));
            Assert.DoesNotContain(2, E.SlotsOfColor(st, Side.Player, 2));
        }

        [Fact]
        public void Healing_goes_to_the_most_wounded_living_ally()
        {
            var st = Kit.New(5);
            st.HeroHp[0][0] -= 5; st.HeroHp[0][2] -= 20; BattleEngine.SyncHp(st, Side.Player);
            Assert.Equal(12, E.HealOne(st, Side.Player, 12, out int h));
            Assert.Equal(2, h);
        }

        // Os elementos: água > fogo > folha > ar > terra > raio > água. Cada cor do alvo que a cor do ataque vence:
        // +50%; cada cor do alvo que vence a do ataque: -25%.
        [Fact]
        public void Elements_follow_the_cycle()
        {
            Assert.True(E.Beats(1, 2) && E.Beats(2, 4) && E.Beats(4, 5) && E.Beats(5, 3) && E.Beats(3, 0) && E.Beats(0, 1));
            for (int c = 0; c < 6; c++)
                Assert.Equal(1, Enumerable.Range(0, 6).Count(o => E.Beats(c, o)));     // cada cor vence uma só
            var st = Kit.New(6);
            // rival: Rainha (terra, folha), Torre (fogo, raio), Peão (folha, água)
            Assert.Equal(1.5, E.ElementMult(st, Side.Player, 2, 0));    // fogo vence a folha da Rainha
            Assert.Equal(0.75, E.ElementMult(st, Side.Player, 0, 0));   // a terra da Rainha vence o raio
            Assert.Equal(1.25, E.ElementMult(st, Side.Player, 1, 1));   // água vence o fogo da Torre (+50%), o raio dela vence a água (-25%)
            Assert.Equal(1.0, E.ElementMult(st, Side.Player, 5, 2) + 0.25);   // ar: a folha do Peão vence o ar (-25%)
        }

        [Fact]
        public void The_player_must_pick_a_rival_each_turn_before_firing()
        {
            var rng = new Mulberry32(9);
            var st = Kit.Engine.NewState(new BattleOptions { Size = 5, Mode = "facil" }, Kit.Royal, rng);
            var s = new BattleSession(Kit.Engine, st, rng, "atento");
            Assert.True(s.NeedsTarget);
            int cell = E.FindGroups(st)[0][0];
            Assert.False(s.TryFire(cell));
            Assert.False(s.TryTarget(7));
            Assert.True(s.TryTarget(2)); s.PresentationDone();
            Assert.False(s.NeedsTarget);
            Assert.True(s.TryFire(cell)); s.PresentationDone();
            if (st.Winner == null && s.IsHanding)
            {
                Assert.True(s.TryHand(s.HandFaces[0])); s.PresentationDone();
                if (st.Winner == null) Assert.True(s.NeedsTarget);   // turno novo: escolher de novo
            }
        }

        [Fact]
        public void The_rival_picks_its_own_target_and_reports_it()
        {
            var (s, log) = Kit.Session(31);
            var bot = new Mulberry32(2);
            Assert.True(Kit.PlayTurn(s, bot));
            if (s.State.Winner != null) return;
            var t = log.OfType<TargetChanged>().Where(e => e.Side == Side.Enemy).ToList();
            Assert.NotEmpty(t);
            Assert.InRange(t[0].Hero, 0, 2);
            Assert.Contains(log, e => e is HeroHpChanged h && h.Side == Side.Player);
        }

        // O rival anuncia um alvo só, antes: já no começo da batalha e de novo no fim de cada turno dele. No turno dele,
        // todo o dano (grupos e poderes) vai para esse herói; só troca se ele cair no meio do turno.
        [Fact]
        public void The_rival_announces_one_target_and_keeps_it()
        {
            var (s, log) = Kit.Session(41);
            Assert.True(BattleEngine.HasTarget(s.State, Side.Enemy));                  // aviso desde o começo
            int announced = s.State.Target[1];
            var bot = new Mulberry32(2);
            Assert.True(Kit.PlayTurn(s, bot));
            if (s.State.Winner != null) return;
            int hand = log.FindIndex(x => x is CubeTurned c && c.Side == Side.Enemy);
            if (BattleEngine.Alive(s.State, Side.Player, announced))
            {
                Assert.DoesNotContain(log.Take(hand), e => e is TargetChanged t && t.Side == Side.Enemy);   // não trocou
                foreach (var h in log.Take(hand).OfType<HeroHpChanged>().Where(e => e.Side == Side.Player && e.To < e.From))
                    Assert.Equal(announced, h.Hero);                                // todo o dano foi no anunciado
            }
            Assert.Equal(Side.Enemy, Assert.IsType<TargetChanged>(log.Last()).Side);  // o próximo, anunciado no fim do turno
            Assert.True(s.NeedsTarget);                                              // o jogador ainda escolhe o dele
        }

        [Fact]
        public void The_announced_target_prefers_heroes_the_rival_colours_hurt_and_the_wounded()
        {
            var st = Kit.New(7);
            int fresh = Kit.Engine.IntentTarget(st, Side.Enemy, "atento");
            Assert.InRange(fresh, 0, 2);
            int other = (fresh + 1) % 3;
            st.HeroHp[0][other] = 1; BattleEngine.SyncHp(st, Side.Player);           // quase caindo: vira o alvo
            Assert.Equal(other, Kit.Engine.IntentTarget(st, Side.Enemy, "atento"));
            Assert.Equal(0, Kit.Engine.IntentTarget(st, Side.Enemy, "distraido"));
        }

        [Fact]
        public void A_saved_battle_with_target_choices_replays()
        {
            var runs = new RunService(Kit.Data, new CollectionService(Kit.Data), Kit.Engine);
            var rec = BattleRecordTests.Rec(4321);
            var live = BattleBuilder.Create(Kit.Engine, runs, rec);
            BattleRecordTests.PlaySome(live, 3);
            Assert.Contains(live.Commands, c => c.Kind == 'T');
            rec.Commands = live.Commands.ToList();
            var again = BattleBuilder.Replay(Kit.Engine, runs, rec);
            Assert.NotNull(again);
            Assert.Equal(live.State.HeroHp, again.State.HeroHp);
        }
    }
}
