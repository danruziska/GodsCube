using System.Linq;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    // As regras da frente: grupos (manchas de 2 ou mais peças iguais que se tocam) só na face da frente, o ganho de
    // um grupo, a garantia, o disparo que leva à entrega e a entrega que gira o cubo inteiro.
    public class FrontRulesTests
    {
        private static readonly BattleEngine E = Kit.Engine;

        private static BattleState Blank(int size = 5, string[] team = null)
        {
            var st = Kit.New(1, size, team);
            Kit.NoGroups(st);
            return st;
        }

        // ---------------------------------------------------------------- grupos

        [Fact]
        public void Only_lines_on_the_front_make_groups()
        {
            var st = Blank();
            var g = Geometry.For(st.N);
            for (int x = 0; x < 3; x++) st.Colors[2 * g.NN + x * g.N] = 5;     // três iguais em fila na face de cima
            Assert.Empty(E.FindGroups(st));
            Kit.Front(st, "555..");
            var groups = E.FindGroups(st);
            Assert.Single(groups);
            Assert.Equal(new[] { Kit.Cell(st, 0, 4), Kit.Cell(st, 1, 4), Kit.Cell(st, 2, 4) }.OrderBy(c => c).ToArray(), groups[0]);
        }

        [Fact]
        public void Touching_lines_of_the_same_color_merge_into_an_L_a_T_or_a_cross()
        {
            var st = Blank();
            Kit.Front(st, "5....", "5....", "555..");       // L de 5 peças
            Assert.Single(E.FindGroups(st));
            Assert.Equal(5, E.FindGroups(st)[0].Length);
            st = Blank();
            Kit.Front(st, ".5...", "555..", ".5...");       // cruz de 5
            Assert.Equal(5, E.FindGroups(st)[0].Length);
        }

        [Fact]
        public void Patches_of_any_shape_are_groups_but_diagonals_and_single_pieces_are_not()
        {
            var st = Blank();
            Kit.Front(st, "55...", ".5...");                // curva de 3: é grupo
            Assert.Equal(3, E.FindGroups(st).Single().Length);
            st = Blank();
            Kit.Front(st, "55...");                         // um par já é grupo
            Assert.Equal(2, E.FindGroups(st).Single().Length);
            st = Blank();
            Kit.Front(st, "5....", ".5...", "..5..");       // diagonal: não é
            Assert.Empty(E.FindGroups(st));
            Kit.Front(st, "555..");
            Assert.Null(E.GroupAt(st, 0));                  // peça da face 0
            Assert.NotNull(E.GroupAt(st, Kit.Cell(st, 1, 4)));
            Assert.Null(E.GroupAt(st, Kit.Cell(st, 4, 0)));
        }

        // ---------------------------------------------------------------- o que um grupo rende

        // Time padrão: Rei (azul, amarelo), Cavaleiro (vermelho, verde), Bispo (roxo, azul). O azul (1) enche os
        // poderes 0 (Rei) e 5 (Bispo).
        [Fact]
        public void A_group_pays_one_damage_per_piece_plus_the_size_bonus_and_mana_to_every_power_of_its_color()
        {
            var st = Blank();
            Kit.Front(st, "11120", "30303", "03030", "30303", "03030");
            var t = E.GroupGain(st, st.Colors, E.GroupAt(st, Kit.Cell(st, 0, 4)), Side.Player);
            Assert.Equal(3, t.Gain[0].Value);                               // 3 peças, o bônus só começa na 4ª
            Assert.Equal(new[] { 3, 0, 0, 0, 0, 3 }, t.Gain[0].Mana);
            Assert.Equal(0, t.Gain[1].Value);
            Assert.Equal(new int[6], t.Gain[1].Mana);
            st = Blank();
            Kit.Front(st, "55555", "30303", "03030", "30303", "03030");             // azul-claro: nenhum herói usa, então só dano
            t = E.GroupGain(st, st.Colors, E.GroupAt(st, Kit.Cell(st, 0, 4)), Side.Player);
            double air = E.ElementMult(st, Side.Player, 5, 0);                      // o ar contra o rival 0 (elementos)
            Assert.Equal(JsMath.RoundInt(JsMath.RoundInt((5 + 2 * 2) * 1.15) * air), t.Gain[0].Value);   // 5 peças + bônus 4, com a Investida
            st = Blank();
            Kit.Front(st, "11020", "30303", "03030", "30303", "03030");             // um par: não ataca, só enche a mana
            var pair = E.GroupGain(st, st.Colors, E.GroupAt(st, Kit.Cell(st, 0, 4)), Side.Player);
            Assert.Equal(0, pair.Gain[0].Value);
            Assert.Equal(new[] { 2, 0, 0, 0, 0, 2 }, pair.Gain[0].Mana);
            Assert.Equal(new int[6], t.Gain[0].Mana);
        }

        [Fact]
        public void The_charge_passive_adds_15_percent_to_groups_of_four_or_more()
        {
            var st = Blank();
            Kit.Front(st, "22220", "30303", "03030", "30303", "03030");
            var t = E.GroupGain(st, st.Colors, E.GroupAt(st, Kit.Cell(st, 0, 4)), Side.Player);
            Assert.True(t.Charge);
            double fire = E.ElementMult(st, Side.Player, 2, 0);                     // o fogo contra o rival 0 (elementos)
            Assert.Equal(JsMath.RoundInt(JsMath.RoundInt((4 + 2) * 1.15) * fire), t.Gain[0].Value);
            st = Blank(5, new[] { "rei", "joker", "bispo" });   // sem o Cavaleiro
            Kit.Front(st, "22220", "30303", "03030", "30303", "03030");
            t = E.GroupGain(st, st.Colors, E.GroupAt(st, Kit.Cell(st, 0, 4)), Side.Player);
            Assert.False(t.Charge);
            Assert.Equal(JsMath.RoundInt(6 * fire), t.Gain[0].Value);
            Kit.Front(st, "22204", "30303", "03030", "30303", "03030");
            Assert.False(E.GroupGain(st, st.Colors, E.GroupAt(st, Kit.Cell(st, 0, 4)), Side.Player).Charge);
        }

        // Um par dispara (some, enche a mana, as peças caem), mas não tira vida, nunca é crítico e não gasta o
        // "próximo ataque crítico", que fica para o próximo ataque de verdade.
        [Fact]
        public void A_pair_fills_mana_without_damage_and_keeps_the_next_crit()
        {
            var st = Blank();
            Kit.Front(st, "11020", "30303", "03030", "30303", "03030");
            st.Fx.SetNextCrit(true);
            int hp = st.Hp[1];
            var r = E.Fire(st, Side.Player, Kit.Cell(st, 0, 4), new Mulberry32(1));
            Assert.Equal(2, r.Cells.Length);
            Assert.False(r.Crit);
            Assert.Equal(0, r.Damage);
            Assert.Equal(hp - r.Chain.Sum(l => l.Damage), st.Hp[1]);
            Assert.Equal(2, st.Mana[0][0]);
            Assert.True(st.Fx.NextCrit == true || r.Chain.Count > 0);
        }

        [Fact]
        public void A_critical_hit_doubles_the_damage_but_not_the_mana()
        {
            var st = Blank();
            Kit.Front(st, "111..");
            var plain = E.GroupGain(st, st.Colors, E.FindGroups(st)[0], Side.Player);
            var crit = E.GroupGain(st, st.Colors, E.FindGroups(st)[0], Side.Player, true);
            Assert.True(crit.Crit);
            Assert.Equal(plain.Gain[0].Value * 2, crit.Gain[0].Value);
            Assert.Equal(plain.Gain[0].Mana, crit.Gain[0].Mana);
        }

        // ---------------------------------------------------------------- começo

        [Fact]
        public void A_new_battle_starts_with_a_group_on_the_front_and_the_second_player_mana()
        {
            for (uint seed = 1; seed <= 40; seed++)
            {
                var st = Kit.New(seed);
                Assert.NotEmpty(E.FindGroups(st));
                Assert.Equal(Side.Player, st.Turn);
                Assert.Equal(Phase.Match, st.Phase);
                Assert.Equal(new int[6], st.Mana[0]);
                Assert.All(st.Mana[1], m => Assert.Equal(Kit.Data.Rules.SecondMana, m));
                Assert.Equal(0.05, E.CritChance(st, Side.Player));
                Assert.Equal(0.05, E.CritChance(st, Side.Enemy));
                Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, st.FaceId);
            }
        }

        [Theory]
        [InlineData("facil", 4)]
        [InlineData("facil", 6)]
        [InlineData("dificil", 5)]
        [InlineData("dificil", 8)]
        public void Starting_health_comes_from_the_table_of_the_mode_and_the_rival_has_ten_percent_more(string mode, int size)
        {
            var r = Kit.Data.Rules;
            var st = Kit.New(1, size, null, mode);
            int hp = mode == "facil" ? r.HpEasy[size] : JsMath.RoundInt(r.HpHard[size] / 5.0) * 5;
            Assert.Equal(hp, st.Hp[0]);
            Assert.Equal(JsMath.RoundInt(hp * 1.1 / 5) * 5, st.Hp[1]);
            Assert.Equal(st.Hp, st.MaxHp);
        }

        // ---------------------------------------------------------------- a garantia

        [Fact]
        public void The_guarantee_recolors_one_front_piece_when_the_front_has_no_group()
        {
            var st = Blank();
            Kit.Front(st, "03030", "30303", "03030", "30303", "03030");             // nenhuma peça encosta numa igual
            var g = Geometry.For(st.N);
            Assert.Empty(E.FindGroups(st));
            var before = (sbyte[])st.Colors.Clone();
            var edits = E.Assist(st, g, new Mulberry32(3));
            Assert.Single(edits);
            Assert.True(BattleEngine.OnFront(g, edits[0].Cell));
            Assert.NotEmpty(E.FindGroups(st));
            Assert.Equal(1, Enumerable.Range(0, st.Colors.Length).Count(i => st.Colors[i] != before[i]));
            Assert.Empty(E.Assist(st, g, new Mulberry32(3)));     // já tem grupo: nada muda
        }

        // ---------------------------------------------------------------- sem giro

        // O giro saiu do jogo: o que o lado tem para fazer é só o que já está na frente. A IA escolhe um desses
        // grupos e a garantia sempre deixa pelo menos um.
        [Fact]
        public void The_ai_picks_one_of_the_groups_already_on_the_front()
        {
            var st = Kit.New(7);
            var front = E.FindGroups(st).SelectMany(g => g).ToHashSet();
            var m = E.ChooseMove(st, Side.Player, "implacavel", new Mulberry32(3));
            Assert.NotNull(m);
            Assert.Contains(m.Cell, front);
            Assert.Equal(E.FindGroups(st).Count, E.EvaluateMoves(st, Geometry.For(st.N), Side.Player).Count);
        }

        [Fact]
        public void Without_a_group_on_the_front_the_guarantee_makes_one()
        {
            var st = Blank();
            Kit.Front(st, "01234", "12340", "23401", "34012", "40123");    // nenhum trio
            Assert.Empty(E.FindGroups(st));
            E.Assist(st, Geometry.For(st.N), new Mulberry32(1));
            Assert.NotEmpty(E.FindGroups(st));
        }

        // ---------------------------------------------------------------- o disparo e a entrega

        [Fact]
        public void Firing_the_last_group_of_the_turn_leads_to_the_hand_phase_without_passing_the_turn()
        {
            var st = Kit.New(11);
            int cell = E.FindGroups(st)[0][0];
            var r = E.Fire(st, Side.Player, cell, new Mulberry32(1));
            Assert.NotNull(r);
            Assert.Equal(Phase.Hand, st.Phase);
            Assert.Equal(Side.Player, st.Turn);
            Assert.Equal(0, st.MovesLeft);
            Assert.Null(E.Fire(st, Side.Player, E.FindGroups(st).Count > 0 ? E.FindGroups(st)[0][0] : cell, new Mulberry32(1)));
            Assert.Equal(st.Hp[1], st.MaxHp[1] - r.Damage - r.Chain.Sum(l => l.Damage));   // o tocado e a cascata
        }

        [Fact]
        public void With_an_extra_group_the_same_side_fires_again_and_the_guarantee_refills_the_front()
        {
            var st = Kit.New(12);
            st.MovesLeft = 2;
            E.Fire(st, Side.Player, E.FindGroups(st)[0][0], new Mulberry32(1));
            Assert.Equal(Phase.Match, st.Phase);
            Assert.Equal(1, st.MovesLeft);
            Assert.NotEmpty(E.FindGroups(st));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Handing_a_face_turns_the_whole_cube_so_that_face_is_the_rivals_front(int face)
        {
            var st = Kit.New(13);
            var g = Geometry.For(st.N);
            for (int f = 0; f < 6; f++) for (int i = f * g.NN; i < (f + 1) * g.NN; i++) st.Colors[i] = (sbyte)f;   // uma cor por face
            Kit.Front(st, "555..");             // a frente precisa de um grupo para disparar
            E.Fire(st, Side.Player, Kit.Cell(st, 0, 4), new Mulberry32(1));
            for (int f = 0; f < 4; f++) for (int i = f * g.NN; i < (f + 1) * g.NN; i++) st.Colors[i] = (sbyte)f;
            int frontId = st.FaceId[Faces.Front];
            var r = E.HandOver(st, Side.Player, face, new Mulberry32(1));
            Assert.NotNull(r);
            Assert.Equal(-1, r.Move.Slice);
            Assert.Equal(face, st.FaceId[Faces.Front]);                               // o id da face entregue está na frente
            Assert.Equal(frontId, st.LastFace[0]);
            int same = Enumerable.Range(Faces.Front * g.NN, g.NN).Count(i => st.Colors[i] == face);
            Assert.True(same >= g.NN - 1, "a frente nova é a face entregue (só a garantia pode trocar uma peça)");
            Assert.Equal(Side.Enemy, st.Turn);
            Assert.Equal(Phase.Match, st.Phase);
            Assert.Equal(1, st.Round);
            Assert.NotEmpty(E.FindGroups(st));                                         // a garantia do rival
        }

        [Fact]
        public void The_back_is_never_offered_and_the_face_the_rival_just_played_is_forbidden()
        {
            var st = Kit.New(14);
            var rng = new Mulberry32(2);
            E.Fire(st, Side.Player, E.FindGroups(st)[0][0], rng);
            Assert.Equal(new[] { 0, 1, 2, 3 }, E.HandOptions(st, Side.Player).ToArray());         // no começo não há proibida
            Assert.False(E.CanHand(st, Side.Player, Faces.Back));
            Assert.False(E.CanHand(st, Side.Player, Faces.Front));
            int played = st.FaceId[Faces.Front];
            E.HandOver(st, Side.Player, 2, rng);
            // o rival não pode devolver a face em que o jogador acabou de jogar
            Assert.Equal(played, E.BanFor(st, Side.Enemy));
            int bannedPhysical = BattleEngine.FaceOf(st, played);
            Assert.True(Faces.IsNeighbor(bannedPhysical));
            E.Fire(st, Side.Enemy, E.FindGroups(st)[0][0], rng);
            var opts = E.HandOptions(st, Side.Enemy);
            Assert.Equal(3, opts.Count);
            Assert.DoesNotContain(bannedPhysical, opts);
            Assert.False(E.CanHand(st, Side.Enemy, bannedPhysical));
            int enemyPlayed = st.FaceId[Faces.Front];
            E.HandOver(st, Side.Enemy, opts[0], rng);
            Assert.Equal(enemyPlayed, E.BanFor(st, Side.Player));
            Assert.Equal(2, st.Round);
        }

        // O Decreto real (passiva do Rei): os poderes dele custam 1 a menos; os dos outros heróis não mudam.
        [Fact]
        public void Royal_decree_makes_the_kings_powers_cheaper()
        {
            var st = Kit.New(1);                                   // Rei, Cavaleiro, Bispo
            var d = Kit.Data.Rules.DecreeDiscount;
            Assert.Equal(System.Math.Max(1, st.Power(Side.Player, 0).Cost - d), E.CostOf(st, Side.Player, 0));
            Assert.Equal(System.Math.Max(1, st.Power(Side.Player, 1).Cost - d), E.CostOf(st, Side.Player, 1));
            Assert.Equal(st.Power(Side.Player, 2).Cost, E.CostOf(st, Side.Player, 2));
        }

        [Fact]
        public void Trickery_lifts_the_forbidden_face()
        {
            var st = Kit.New(15, 5, Kit.Tricky);                 // o Joker tem a Trapaça
            st.LastFace[1] = st.FaceId[0];
            Assert.Null(E.BanFor(st, Side.Player));
            st = Kit.New(15);
            st.LastFace[1] = st.FaceId[0];
            Assert.Equal(st.FaceId[0], E.BanFor(st, Side.Player));
        }

        [Fact]
        public void Handing_over_ends_the_turn_effects_of_the_side_that_played()
        {
            var st = Kit.New(16);
            var rng = new Mulberry32(4);
            E.AddFx(st, "dmg", 0.4, 1, true);
            st.Silenced[1] = true;
            E.Fire(st, Side.Player, E.FindGroups(st)[0][0], rng);
            E.HandOver(st, Side.Player, 0, rng);
            Assert.Empty(st.Fx.List("dmg"));            // durava 1 turno do jogador
            Assert.True(st.Silenced[1]);                  // vale no turno do rival, que começa agora
            E.Fire(st, Side.Enemy, E.FindGroups(st)[0][0], rng);
            E.HandOver(st, Side.Enemy, E.HandOptions(st, Side.Enemy)[0], rng);
            Assert.False(st.Silenced[1]);
        }

        // Cada face física sai da frente para uma vizinha e volta: quatro entregas para o mesmo lado devolvem o cubo.
        [Fact]
        public void Four_quarter_turns_on_the_same_axis_bring_the_cube_back()
        {
            var st = Kit.New(17);
            var colors = (sbyte[])st.Colors.Clone();
            var m = BattleEngine.TurnToFront(st.N, 0);
            for (int k = 0; k < 4; k++) BattleEngine.TurnWhole(st, m);
            Assert.Equal(colors, st.Colors);
            Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, st.FaceId);
            Assert.Null(BattleEngine.TurnToFront(st.N, Faces.Back));
            Assert.Null(BattleEngine.TurnToFront(st.N, Faces.Front));
        }
    }
}