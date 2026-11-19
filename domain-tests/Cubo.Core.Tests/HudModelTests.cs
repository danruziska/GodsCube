using System.Collections.Generic;
using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    public class HudModelTests
    {
        private static readonly BattleEngine E = Kit.Engine;

        private static (BattleSession s, Texts t) Make(uint seed = 5000, int size = 5, string[] team = null)
        {
            var (s, _) = Kit.Session(seed, size, team);
            return (s, new Texts(new FileContentSource()));
        }

        private static IEnumerable<string> AllStrings(HudView v)
        {
            foreach (var p in v.Powers) { yield return p.HeroName; yield return p.Name; yield return p.Domain; yield return p.Effect; yield return p.Detail; }
            foreach (var p in v.Pills) yield return p;
            yield return v.TurnText; yield return v.ComboPlayer; yield return v.ComboEnemy;
            yield return v.CritPlayer; yield return v.CritEnemy; yield return v.HandText;
        }

        [Fact]
        public void A_new_battle_shows_six_empty_powers_and_five_percent_crit()
        {
            var (s, t) = Make();
            var v = HudModel.Build(s, t);
            Assert.Equal(6, v.Powers.Length);
            Assert.True(v.Powers.All(p => p.Fill == 0f && !p.Enabled && p.Cost > 0));
            Assert.Empty(v.Pills);
            Assert.Equal("Rei", v.Powers[0].HeroName);
            Assert.Equal("Ordem real", v.Powers[0].Name);
            Assert.Equal("Crítico 5%", v.CritPlayer);
            Assert.Equal("Crítico 5%", v.CritEnemy);
            Assert.False(v.Over);
            Assert.False(v.Handing);
            Assert.Equal("", v.HandText);
        }

        [Fact]
        public void Fill_follows_mana_and_Enabled_follows_the_session()
        {
            var (s, t) = Make();
            var st = s.State;
            st.Mana[0][0] = E.CostOf(st, Side.Player, 0) / 2;
            st.Mana[0][1] = E.CostOf(st, Side.Player, 1);
            st.Mana[0][2] = 999;
            var v = HudModel.Build(s, t);
            Assert.Equal(st.Mana[0][0] / (float)v.Powers[0].Cost, v.Powers[0].Fill);
            Assert.True(v.Powers[0].Fill > 0f && v.Powers[0].Fill < 1f);
            Assert.Equal(1f, v.Powers[1].Fill);
            Assert.Equal(1f, v.Powers[2].Fill);
            for (int i = 0; i < 6; i++) Assert.Equal(s.CanCast(i), v.Powers[i].Enabled);
            Assert.False(v.Powers[0].Enabled);
            Assert.True(v.Powers[1].Enabled && v.Powers[2].Enabled);
        }

        [Fact]
        public void Power_texts_tell_the_crit_chance_and_the_crit_powers()
        {
            var (s, t) = Make(1, 5, Kit.Tricky);                    // Joker, Cavaleiro, Bispo
            var v = HudModel.Build(s, t);
            Assert.Equal("Seu próximo ataque é crítico", v.Powers[0].Effect);
            Assert.Equal("10 de dano no rival (30% de chance de crítico)", v.Powers[2].Effect);
            Assert.Equal("+10% de chance de crítico até o fim da batalha", v.Powers[3].Effect);
            s.State.Fx.SetNextCrit(true);
            E.AddFx(s.State, "crit", 0.15, null, true);
            Assert.Equal("Crítico 20% · próximo garantido", HudModel.Build(s, t).CritPlayer);
        }


        [Fact]
        public void Switching_language_rewrites_every_text()
        {
            var (s, t) = Make();
            var pt = AllStrings(HudModel.Build(s, t)).ToList();
            t.SetLang("en");
            var en = AllStrings(HudModel.Build(s, t)).ToList();
            Assert.Equal("Sun", t.Domain(0));
            t.SetLang("es");
            var es = AllStrings(HudModel.Build(s, t)).ToList();
            Assert.Equal(pt.Count, en.Count);
            Assert.NotEqual(pt, en);
            Assert.NotEqual(en, es);
        }

        [Fact]
        public void Pills_list_the_active_effects_and_the_rivals_locks()
        {
            var (s, t) = Make();
            var st = s.State;
            E.AddFx(st, "dmg", 0.4, 2, true);
            E.AddFx(st, "ward", 0.35, null, true);
            E.AddFx(st, "dmg", -0.5, 1, true);
            E.AddFx(st, "echo", 1, null, false);
            E.AddFx(st, "echo", 2, null, false);
            E.AddFx(st, "crit", 0.3, 1, true);
            st.Fx.SetNextCrit(true);
            st.Silenced[1] = true;
            Assert.Equal(new[] { "Proteção −35%", "Dano +40% · 2", "Dano −50% · 1", "Crítico +30% · 1", "Eco de mana +3",
                                 "Próximo ataque crítico", "Rival sem poderes" },
                HudModel.Build(s, t).Pills.ToArray());
        }

        [Fact]
        public void Turn_text_follows_the_state_and_the_hand_phase_names_the_forbidden_face()
        {
            var (s, t) = Make();
            Assert.Equal(t.T("turn.can"), HudModel.TurnText(s, t));
            s.State.MovesLeft = 2;
            Assert.Equal(t.T("turn.can") + t.T("turn.shots", ("n", 2)), HudModel.TurnText(s, t));
            s.State.MovesLeft = 1;
            Assert.True(s.TryFire(E.FindGroups(s.State)[0][0])); s.PresentationDone();
            var v = HudModel.Build(s, t);
            Assert.True(v.Handing);
            Assert.Equal("Escolha a face do rival", v.TurnText);
            Assert.Equal(t.T("hand.how"), v.HandText);
            s.State.LastFace[1] = s.State.FaceId[2];
            Assert.Equal(t.T("hand.how") + " Proibida: Cima.", HudModel.Build(s, t).HandText);
            s.State.Winner = Side.Player;
            Assert.Equal(t.T("turn.over"), HudModel.TurnText(s, t));
        }

        // Uma batalha inteira, nos três idiomas: nenhum texto da interface pode sair como ⟦chave⟧.
        [Fact]
        public void No_text_is_ever_missing_during_a_whole_battle_in_any_language()
        {
            var (s, t) = Make(4242, 5, Kit.Tricky);
            var bot = new Mulberry32(4242);
            Kit.FillMana(s.State, Side.Player);
            for (int turn = 0; turn < 300 && s.State.Winner == null; turn++)
            {
                foreach (var lang in Texts.Languages)
                {
                    t.SetLang(lang);
                    foreach (var text in AllStrings(HudModel.Build(s, t)))
                        Assert.False(text != null && text.Contains("⟦"), $"{lang}, turno {turn}: texto faltando: {text}");
                }
                if (s.NeedsTarget) { s.TryTarget(E.ChooseTarget(s.State, Side.Player, "atento", bot)); s.PresentationDone(); }
                else if (s.IsMatching)
                {
                    var m = E.ChooseMove(s.State, Side.Player, "atento", bot);
                    s.TryFire(m.Cell); s.PresentationDone();
                }
                else if (s.IsHanding) { s.TryHand(E.ChooseHand(s.State, Side.Player, "atento", bot)); s.PresentationDone(); }
            }
            Assert.NotNull(s.State.Winner);
        }

    }
}