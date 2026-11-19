using System.Collections.Generic;
using System.Linq;
using Cubo.App;
using Cubo.Data;
using Cubo.Domain;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Cubo.Core.Tests
{
    public class TextsTests
    {
        private static readonly FileContentSource Src = new FileContentSource();
        private static readonly GameData Data = ContentLoader.Load(Src);
        private static readonly BattleEngine Engine = new BattleEngine(Data);

        private static Texts Make(string lang = "pt") { var t = new Texts(Src); t.SetLang(lang); return t; }

        [Fact]
        public void The_three_languages_have_exactly_the_same_keys()
        {
            var t = Make();
            var pt = t.UiKeys("pt").OrderBy(k => k).ToList();
            Assert.True(pt.Count > 200);
            foreach (var lang in new[] { "en", "es" })
            {
                var other = t.UiKeys(lang).OrderBy(k => k).ToList();
                Assert.True(pt.SequenceEqual(other),
                    $"{lang} difere de pt: faltam " + string.Join(", ", pt.Except(other).Take(5)) + "; sobram " + string.Join(", ", other.Except(pt).Take(5)));
            }
        }

        [Fact]
        public void Every_proper_name_exists_in_all_three_languages()
        {
            var t = Make();
            foreach (var key in t.NameKeys)
                foreach (var lang in Texts.Languages)
                    Assert.True(t.HasName(key, lang), $"{key} sem {lang}");
        }

        [Fact]
        public void Every_hero_power_passive_card_rune_and_domain_has_a_name()
        {
            var t = Make();
            var keys = new HashSet<string>(t.NameKeys);
            foreach (var hero in Data.Heroes.Values.Concat(Data.EnemyTeam))
            {
                Assert.Contains("hero." + hero.Id, keys);
                Assert.Contains("herotag." + hero.Id, keys);
                foreach (var p in hero.Powers) Assert.Contains("power." + p.Id, keys);
                if (hero.Passive != null) Assert.Contains("passive." + hero.Passive, keys);
            }
            foreach (var r in Data.Runes) Assert.Contains("rune." + r.Id, keys);
            foreach (var d in Texts.DomainIds) Assert.Contains("domain." + d, keys);
            Assert.DoesNotContain(keys, k => k.StartsWith("god.") || k.StartsWith("civ."));
        }

        [Fact]
        public void Domain_ids_match_the_content_files()
        {
            var ids = JArray.Parse(Src.Read("domains")).Select(x => (string)x["id"]).ToArray();
            Assert.Equal(ids, Texts.DomainIds);
        }

        [Fact]
        public void T_fills_placeholders_and_marks_what_is_missing()
        {
            var t = Make();
            Assert.Equal("Você caiu no andar 2 de 6.", t.T("run.lose.text", ("f", 2), ("n", 6)));
            Assert.Equal("⟦nao.existe⟧", t.T("nao.existe"));
            Assert.Equal("⟦nao.existe⟧", t.Name("nao.existe"));
        }

        [Fact]
        public void Switching_language_changes_texts_and_raises_Changed()
        {
            var t = Make();
            int fired = 0; t.Changed += () => fired++;
            Assert.Equal("Tirar", t.T("equip.btn.remove"));
            t.SetLang("en"); Assert.Equal("Remove", t.T("equip.btn.remove")); Assert.Equal("Sun", t.Domain(0));
            t.SetLang("es"); Assert.Equal("Quitar", t.T("equip.btn.remove"));
            t.SetLang("xx"); Assert.Equal("Tirar", t.T("equip.btn.remove"));      // idioma desconhecido volta ao português
            t.SetLang("pt");                                              // já era pt: não dispara
            Assert.Equal(3, fired);
        }

        [Theory]
        [InlineData("pt-BR", "pt")]
        [InlineData("PT", "pt")]
        [InlineData("es-MX", "es")]
        [InlineData("en-US", "en")]
        [InlineData("fr", "en")]
        [InlineData(null, "pt")]
        public void Detect_maps_system_language(string code, string expected) => Assert.Equal(expected, Texts.Detect(code));

        // Os textos das passivas levam os números de rules.json.
        [Fact]
        public void Passive_texts_carry_the_numbers_of_the_rules()
        {
            var t = Make();
            Assert.Equal("Os poderes do Rei custam 1 de mana a menos.", HeroesModel.PassiveText(Data, t, Passives.Decree));
            Assert.Equal("Grupos de 4 ou mais peças causam +15% de dano.", HeroesModel.PassiveText(Data, t, Passives.Charge));
            t.SetLang("en");
            Assert.Equal("Each group gives +1 mana to the emptiest power on the team.", HeroesModel.PassiveText(Data, t, Passives.Communion));
            t.SetLang("es");
            Assert.Equal("Puede entregar la cara prohibida.", HeroesModel.PassiveText(Data, t, Passives.Trick));
        }

        // The game is played with a mouse on Windows: no text tells the player to tap or use a finger.
        [Theory]
        [InlineData("en", @"\b(tap|taps|tapped|tapping|touch|finger|swipe)\b")]
        [InlineData("pt", @"\b(toque|toques|tocar|tocado|tocados|tocada|toca|dedo|deslize)\b")]
        [InlineData("es", @"\b(toca|tocar|tocado|tocan|toque|dedo|desliza)\b")]
        public void No_text_talks_about_touch(string lang, string touchWords)
        {
            var t = Make(lang);
            var re = new System.Text.RegularExpressions.Regex(touchWords, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (var key in t.UiKeys(lang))
            {
                string text = t.T(key);
                Assert.False(re.IsMatch(text), $"{lang} {key}: \"{text}\"");
            }
        }

        [Fact]
        public void Texts_tell_the_player_to_click()
        {
            Assert.Equal("Your turn · click a group on the front", Make("en").T("turn.can"));
            Assert.Equal("Sua vez · clique num grupo da frente", Make("pt").T("turn.can"));
        }
    }
}