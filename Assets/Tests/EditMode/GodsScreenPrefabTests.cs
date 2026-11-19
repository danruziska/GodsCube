using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Cubo.App;
using Cubo.View;

namespace Cubo.Tests
{
    // Confere os prefabs GodsScreen e BottomNav montados no editor e o que eles fazem com um HeroesView de mentira.
    // A regra (quem entra no time, a ordem) é testada no dotnet; aqui só se vê que a tela mostra e repassa.
    public class GodsScreenPrefabTests
    {
        public const string GodsPath = "Assets/Game/View/Prefabs/UI/GodsScreen.prefab";
        public const string TabsPath = "Assets/Game/View/Prefabs/UI/BottomNav.prefab";

        private GameObject _copy;

        [TearDown]
        public void TearDown()
        {
            if (_copy != null) Object.DestroyImmediate(_copy);
        }

        private static GameObject Load(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, "falta o prefab " + path);
            return prefab;
        }

        private GodsScreen NewGods()
        {
            _copy = Object.Instantiate(Load(GodsPath));
            var screen = _copy.GetComponent<GodsScreen>();
            Assert.IsNotNull(screen, "o GodsScreen não tem o componente Gods Screen na raiz");
            return screen;
        }

        private TabBar NewTabs()
        {
            _copy = Object.Instantiate(Load(TabsPath));
            var tabs = _copy.GetComponentInChildren<TabBar>();
            Assert.IsNotNull(tabs, "o BottomNav não tem um Tab Bar");
            return tabs;
        }

        // Seis heróis, dois no time (g0 e g1), g1 em destaque, o terceiro espaço vazio.
        private static HeroesView FakeView()
        {
            var v = new HeroesView { TeamLabel = "Time", AbilitiesLabel = "Habilidades", PassiveLabel = "Passiva", EmptySlot = "Arraste um deus", Hint = "Falta escolher 1 deus." };
            for (int i = 0; i < 6; i++)
                v.Heroes.Add(new HeroCardView { Id = "g" + i, Name = "Deus " + i, Tag = "Papel", Colors = new[] { i % 6, (i + 1) % 6 }, InTeam = i < 2, Selected = i == 1 });
            v.Team[0] = "g0";
            v.Team[1] = "g1";
            v.Detail = new HeroDetailView
            {
                Id = "g1", Name = "Deus 1", Tag = "Papel", Colors = new[] { 0, 1 }, PassiveName = "Passiva", PassiveText = "Faz algo.", HasPassive = true,
                Abilities = new[]
                {
                    new AbilityView { Color = 0, Cost = 9, Name = "Golpe", Domain = "Sol", Effect = "Causa 10 de dano." },
                    new AbilityView { Color = 1, Cost = 11, Name = "Cura", Domain = "Mar", Effect = "Recupera 12 de vida." }
                }
            };
            return v;
        }

        [Test]
        public void Every_part_of_the_gods_screen_is_wired()
        {
            var missing = Load(GodsPath).GetComponent<GodsScreen>().MissingReference();
            Assert.IsNull(missing, "falta ligar no Inspector do GodsScreen: " + missing);
        }

        [Test]
        public void Gods_screen_and_tabs_are_canvases_between_the_lists_and_the_top_of_the_stack()
        {
            var screens = Load(ScreensPrefabTests.ScreensPath).GetComponent<Canvas>().sortingOrder;
            var gods = Load(GodsPath);
            var tabs = Load(TabsPath);
            Assert.Greater(gods.GetComponent<Canvas>().sortingOrder, screens, "Sort Order do GodsScreen tem de ser maior que o do Screens");
            Assert.Greater(tabs.GetComponent<Canvas>().sortingOrder, gods.GetComponent<Canvas>().sortingOrder, "Sort Order do BottomNav tem de ser maior que o do GodsScreen");
            foreach (var root in new[] { gods, tabs })
            {
                var scaler = root.GetComponent<CanvasScaler>();
                Assert.IsNotNull(scaler, root.name + ": precisa de um Canvas Scaler");
                Assert.AreEqual(new Vector2(1080f, 1920f), scaler.referenceResolution, root.name + ": Reference Resolution 1080 x 1920");
                Assert.AreEqual(0f, scaler.matchWidthOrHeight, 0.01f, root.name + ": Match 0 (pela largura), para as três colunas caberem em qualquer aparelho");
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, root.GetComponent<Canvas>().renderMode, root.name + ": Screen Space - Overlay");
            }
        }

        [Test]
        public void Show_draws_every_god_the_team_and_the_detail()
        {
            var screen = NewGods();
            screen.Show(FakeView());
            Assert.IsTrue(screen.Visible);
            Assert.AreEqual(6, screen.CardCount);
            Assert.AreEqual("Deus 0", screen.Card(0).Label);
            Assert.IsTrue(screen.Card(0).InTeam);
            Assert.IsFalse(screen.Card(2).InTeam);
            Assert.IsTrue(screen.Card(1).Selected);
            Assert.IsFalse(screen.Card(0).Selected);
            Assert.AreEqual("Deus 0", screen.Slot(0).Label);
            Assert.AreEqual("Deus 1", screen.Slot(1).Label);
            Assert.AreEqual("Arraste um deus", screen.Slot(2).Label, "o espaço vazio mostra a dica");
            Assert.IsNull(screen.Slot(2).GodId);

            var smaller = FakeView();
            smaller.Heroes.RemoveRange(4, 2);
            screen.Show(smaller);
            Assert.AreEqual(4, screen.CardCount, "os cartões que sobram ficam desligados, não destruídos");
            screen.Hide();
            Assert.IsFalse(screen.Visible);
        }

        [Test]
        public void Taps_and_drops_reach_the_listeners()
        {
            var screen = NewGods();
            screen.Show(FakeView());
            string selected = null, placed = null;
            int slot = -1;
            screen.GodSelected += id => selected = id;
            screen.GodPlaced += (id, s) => { placed = id; slot = s; };

            screen.Card(3).Click();
            Assert.AreEqual("g3", selected);
            screen.Slot(1).Drop("g4");
            Assert.AreEqual("g4", placed);
            Assert.AreEqual(1, slot, "o Index do espaço é o da ordem em Slots");
            selected = null;
            screen.Slot(0).Click();
            Assert.AreEqual("g0", selected, "tocar num espaço ocupado mostra o deus");
            selected = null;
            screen.Slot(2).Click();
            Assert.IsNull(selected, "tocar num espaço vazio não faz nada");
            placed = null;
            screen.Slot(2).Drop(null);
            Assert.IsNull(placed, "soltar sem deus não faz nada");
        }

        [Test]
        public void Every_part_of_the_tabs_prefab_is_wired()
        {
            var tabs = Load(TabsPath).GetComponentInChildren<TabBar>();
            Assert.IsNotNull(tabs, "o BottomNav não tem um Tab Bar");
            Assert.AreEqual(3, tabs.Tabs.Count, "três abas, nesta ordem: Heróis, Batalha, Equipamento");
            Assert.IsNull(tabs.MissingArt(), "falta arte no Tab Bar: " + tabs.MissingArt());
        }

        [Test]
        public void Tabs_take_labels_lock_and_selection_before_being_built()
        {
            var tabs = NewTabs();
            tabs.SetLabel(1, "Batalha");
            Assert.AreEqual("Batalha", tabs.Tabs[1].Label);
            tabs.SetLocked(1, true);
            Assert.IsTrue(tabs.IsLocked(1), "sem time completo, a Batalha fica travada");
            tabs.SetLocked(1, false);
            Assert.IsFalse(tabs.IsLocked(1));
            tabs.SetIndex(2);
            Assert.AreEqual(2, tabs.SelectedIndex, "antes de montar os botões, a aba pedida vira a inicial");

            tabs.SetVisible(true);
            Assert.IsTrue(tabs.Visible);
            tabs.SetVisible(false);
            Assert.IsFalse(tabs.Visible);
        }
    }
}
