using System.Collections.Generic;
using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    public class BattleSessionTests
    {
        private static readonly BattleEngine E = Kit.Engine;

        // Uma jogada que o motor aceita: um grupo da frente.
        private static MoveEval Pick(BattleSession s) => E.ChooseMove(s.State, Side.Player, "atento", new Mulberry32(1));

        private static void FireOnce(BattleSession s)
        {
            var pick = Pick(s);
            Assert.True(s.TryFire(pick.Cell));
            s.PresentationDone();
        }

        [Fact]
        public void Commands_are_refused_while_busy()
        {
            var (s, log) = Kit.Session();
            var pick = Pick(s);
            Assert.True(s.TryFire(pick.Cell));
            Assert.True(s.Busy);
            Assert.False(s.TryHand(0));
            Assert.False(s.TryFire(pick.Cell));
            s.PresentationDone();
            Assert.False(s.Busy);
        }

        // Disparar não passa a vez: a sessão conta o grupo e avisa que é hora de entregar uma face.
        [Fact]
        public void Firing_the_last_group_reports_it_and_opens_the_hand_phase()
        {
            var (s, log) = Kit.Session();
            FireOnce(s);
            Assert.True(s.IsHanding);
            Assert.False(s.IsMatching);
            Assert.Equal(Side.Player, s.State.Turn);
            Assert.IsType<HandPhase>(log.Last());
            var fired = log.OfType<GroupFired>().Single(g => g.Link == 0);    // o tocado (depois dele pode vir cascata)
            Assert.Equal(Side.Player, fired.Side);
            Assert.True(fired.Damage > 0 && fired.Cells.Length >= 3);
            Assert.Empty(log.OfType<TurnChanged>());
            Assert.Equal(new List<int> { 0, 1, 2, 3 }, s.HandFaces);
            Assert.Equal(-1, s.BannedFace);
            Assert.Null(s.GroupPreview(fired.Cells[0]));                  // já não dá para disparar
        }

        // Entregar: o cubo gira, a vez passa, o rival joga o turno inteiro (giro, grupo, entrega) e a vez volta.
        [Fact]
        public void Handing_a_face_plays_the_whole_rival_turn_in_one_batch_in_order()
        {
            var (s, log) = Kit.Session();
            FireOnce(s);
            log.Clear();
            Assert.False(s.TryHand(Faces.Back));
            Assert.True(s.TryHand(2));
            var turned = log.OfType<CubeTurned>().ToList();
            Assert.Equal(new[] { Side.Player, Side.Enemy }, turned.Select(t => t.Side).ToArray());
            Assert.Equal(2, turned[0].Face);
            Assert.Equal(-1, turned[0].Move.Slice);
            int iTurnToEnemy = log.FindIndex(e => e is TurnChanged t && t.Side == Side.Enemy);
            int iEnemyFire = log.FindIndex(e => e is GroupFired f && f.Side == Side.Enemy);
            int iEnemyHand = log.FindIndex(e => e is CubeTurned c && c.Side == Side.Enemy);
            Assert.True(log.IndexOf(turned[0]) < iTurnToEnemy && iTurnToEnemy < iEnemyFire && iEnemyFire < iEnemyHand,
                string.Join(" ", log.Select(e => e.GetType().Name)));
            var back = (TurnChanged)log.Last(e => e is TurnChanged);
            Assert.Equal(Side.Player, back.Side);
            Assert.Equal(2, back.Round);
            Assert.Same(back, log[log.Count - 2]);                        // depois da troca de turno...
            Assert.Equal(Side.Enemy, Assert.IsType<TargetChanged>(log.Last()).Side);   // ...o rival anuncia o próximo alvo
            Assert.False(s.IsMatching);                                   // ocupada até a tela tocar o lote
            s.PresentationDone();
            Assert.True(s.IsMatching);
            Assert.NotEmpty(E.FindGroups(s.State));                       // a garantia do jogador
            Assert.True(Faces.IsNeighbor(s.BannedFace));                   // a face que o rival jogou está proibida
            Assert.DoesNotContain(s.BannedFace, E.HandOptions(s.State, Side.Player));
            foreach (var h in log.OfType<HpChanged>()) Assert.NotEqual(h.From, h.To);
        }

        // O tabuleiro do CubeTurned é o girado, antes da garantia; a garantia vem depois, num CellsRefilled.
        [Fact]
        public void The_cube_turn_event_carries_the_turned_board_before_the_guarantee()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                var (s, log) = Kit.Session(seed);
                FireOnce(s);
                var before = (sbyte[])s.State.Colors.Clone();
                log.Clear();
                Assert.True(s.TryHand(0));
                var turn = (CubeTurned)log[0];
                Assert.Equal(BattleEngine.Apply(Geometry.For(5), before, turn.Move), turn.Board);
                if (log[1] is CellsRefilled r) Assert.Empty(r.Cells);
            }
        }

        [Fact]
        public void Casting_a_damage_power_costs_mana_hits_the_rival_and_keeps_the_turn()
        {
            var (s, log) = Kit.Session();
            var st = s.State;
            int slot = 2;                                                  // Lança do Cavaleiro
            Assert.False(s.TryCast(slot));                                 // sem mana
            int cost = E.CostOf(st, Side.Player, slot);
            st.Mana[0][slot] = cost + 3;
            int hp = st.Hp[1];
            Assert.True(s.TryCast(slot));
            var cast = log.OfType<PowerCast>().Single();
            Assert.Equal((Side.Player, slot, "damage"), (cast.Side, cast.Slot, cast.Kind));
            Assert.True(cast.Amount > 0 && st.Hp[1] == hp - cast.Amount);
            double el = E.ElementMult(st, Side.Player, 2, 0);              // a Lança é de fogo, contra o rival 0
            Assert.Equal(JsMath.RoundInt((cast.Crit ? 20 : 10) * el), cast.Amount);
            Assert.Contains(new HpChanged(Side.Enemy, hp, st.Hp[1]), log.OfType<HpChanged>());
            Assert.Equal(3, st.Mana[0][slot]);
            Assert.Equal(Side.Player, st.Turn);                            // poder não gasta a vez
            s.PresentationDone();
            Assert.False(s.CanCast(slot));
        }

        [Fact]
        public void Powers_wait_for_the_next_turn_once_the_group_is_fired()
        {
            var (s, log) = Kit.Session();
            Kit.FillMana(s.State, Side.Player);
            FireOnce(s);
            Assert.False(s.CanCast(2));
        }


        [Fact]
        public void The_rival_uses_its_powers_before_its_group()
        {
            var (s, log) = Kit.Session();
            Kit.FillMana(s.State, Side.Enemy);
            FireOnce(s);
            Assert.True(s.TryHand(0));
            int iTurn = log.FindIndex(e => e is TurnChanged t && t.Side == Side.Enemy);
            int iCast = log.FindIndex(e => e is PowerCast c && c.Side == Side.Enemy);
            int iFire = log.FindIndex(e => e is GroupFired f && f.Side == Side.Enemy);
            Assert.True(iTurn >= 0 && iTurn < iCast && iCast < iFire, $"ordem: turno {iTurn}, poder {iCast}, disparo {iFire}");
        }

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void A_whole_match_ends_with_one_BattleEnded_and_then_refuses_input(int size)
        {
            var (s, log) = Kit.Session(777, size);
            var bot = new Mulberry32(9);
            for (int turn = 0; turn < 300 && s.State.Winner == null; turn++) Assert.True(Kit.PlayTurn(s, bot));
            Assert.NotNull(s.State.Winner);
            Assert.Single(log.OfType<BattleEnded>());
            Assert.IsType<BattleEnded>(log.Last());
            Assert.False(s.TryFire(Kit.Cell(s.State, 0, 0)));
            Assert.False(s.TryHand(0));
        }
    }
}