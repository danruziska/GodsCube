using System.Linq;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    // O rival (Fase 10): a face que ele entrega, os poderes que ele usa e batalhas inteiras de IA contra IA.
    public class AiTests
    {
        private static readonly BattleEngine E = Kit.Engine;

        private static BattleState AtHand(uint seed)
        {
            var st = Kit.New(seed);
            E.Fire(st, Side.Player, E.FindGroups(st)[0][0], new Mulberry32(seed));
            return st;
        }

        [Fact]
        public void The_relentless_rival_hands_the_face_with_the_weakest_group_and_never_a_forbidden_one()
        {
            for (uint seed = 1; seed <= 60; seed++)
            {
                var st = AtHand(seed);
                st.LastFace[1] = st.FaceId[(int)(seed % 4)];                // uma proibida qualquer
                var opts = E.HandOptions(st, Side.Player);
                int face = E.ChooseHand(st, Side.Player, "implacavel", new Mulberry32(seed));
                Assert.Contains(face, opts);
                int best = opts.Min(f => E.BestAfter(st, f, Side.Enemy));
                Assert.Equal(best, E.BestAfter(st, face, Side.Enemy));
            }
        }

        [Fact]
        public void A_distracted_rival_still_hands_a_legal_face()
        {
            for (uint seed = 1; seed <= 60; seed++)
            {
                var st = AtHand(seed);
                int face = E.ChooseHand(st, Side.Player, "distraido", new Mulberry32(seed));
                Assert.True(E.CanHand(st, Side.Player, face));
            }
        }

        [Fact]
        public void BestAfter_looks_at_a_turned_copy_and_leaves_the_state_alone()
        {
            var st = AtHand(3);
            var colors = (sbyte[])st.Colors.Clone();
            var ids = (int[])st.FaceId.Clone();
            for (int f = 0; f < 4; f++) E.BestAfter(st, f, Side.Enemy);
            Assert.Equal(colors, st.Colors);
            Assert.Equal(ids, st.FaceId);
            Assert.Equal(0, E.BestAfter(st, Faces.Back, Side.Enemy));
        }

        [Fact]
        public void The_rival_only_casts_powers_that_help_now()
        {
            var st = Kit.New(4);
            st.Turn = Side.Enemy;
            Kit.FillMana(st, Side.Enemy);
            // rival: Rainha (dano com crítico, cura), Torre (mais um grupo, dano), Peão (+crítico, dano)
            var casts = E.EnemyCasts(st);
            Assert.DoesNotContain(1, casts);                        // vida cheia: não cura
            Assert.Contains(4, casts);                              // +10% de crítico
            st.HeroHp[1][2] -= 30; BattleEngine.SyncHp(st, Side.Enemy);   // um herói do rival ferido
            Assert.Contains(1, E.EnemyCasts(st));
            E.AddFx(st, Side.Enemy, "crit", 1, null, true);         // já no teto
            Assert.DoesNotContain(4, E.EnemyCasts(st));
            st.Silenced[1] = true;
            Assert.Empty(E.EnemyCasts(st));
        }

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void Whole_ai_battles_always_end_with_a_winner(int size)
        {
            for (uint seed = 1; seed <= 12; seed++)
            {
                var st = Kit.PlayOut(seed, null, size);
                Assert.NotNull(st.Winner);
                Assert.InRange(st.Round, 2, 60);
            }
        }

        // Com o mesmo time dos dois lados e a mesma vida, quem joga depois recebe 1 de mana em cada poder (2 por
        // herói): em 400 batalhas, quem começa vence perto da metade.
        [Fact]
        public void In_a_mirror_match_the_second_players_mana_keeps_the_start_fair()
        {
            int first = 0, n = 400;
            for (uint seed = 1; seed <= n; seed++)
            {
                var rng = new Mulberry32(seed);
                var st = E.NewState(new BattleOptions { Size = 5, Mode = "facil" }, Kit.Royal, rng);
                st.Teams[1] = Kit.Data.ResolveTeam(Kit.Royal);
                st.Hp[1] = st.MaxHp[1] = st.Hp[0];
                for (int t = 0; t < 400 && st.Winner == null; t++)
                {
                    var side = st.Turn;
                    for (int g = 0; g < 12 && st.Winner == null && st.Phase == Phase.Match; g++)
                    {
                        var m = E.ChooseMove(st, side, "atento", rng);
                        E.Fire(st, side, m.Cell, rng);
                    }
                    if (st.Winner == null) E.HandOver(st, side, E.ChooseHand(st, side, "atento", rng), rng);
                }
                if (st.Winner == Side.Player) first++;
            }
            Assert.InRange(first / (double)n, 0.40, 0.60);
        }
    }
}