using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Cubo.App;
using Cubo.View;

namespace Cubo.Tests
{
    // Confere o prefab Screens montado no editor e o desenho das linhas, sem dar Play:
    // o teste cria uma cópia do prefab, mostra telas de mentira e conta as linhas.
    public class ScreensPrefabTests
    {
        public const string ScreensPath = "Assets/Game/View/Prefabs/UI/Screens.prefab";

        private GameObject _copy;

        [TearDown]
        public void TearDown()
        {
            if (_copy != null) Object.DestroyImmediate(_copy);
        }

        private static GameObject LoadScreens()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreensPath);
            Assert.IsNotNull(prefab, "falta o prefab " + ScreensPath);
            Assert.IsNotNull(prefab.GetComponent<ScreenUi>(), "o Screens não tem o componente Screen Ui na raiz");
            return prefab;
        }

        private ScreenUi NewCopy()
        {
            _copy = Object.Instantiate(LoadScreens());
            return _copy.GetComponent<ScreenUi>();
        }

        private static ScreenModel Model(string title, params ScreenItem[] items)
        {
            var m = new ScreenModel { Title = title };
            m.Items.AddRange(items);
            return m;
        }

        private static ScreenItem Heading(string text) => new ScreenItem { Kind = ItemKind.Heading, Text = text };
        private static ScreenItem Text(string text) => new ScreenItem { Kind = ItemKind.Text, Text = text };
        private static ScreenItem Button(string id, System.Action click, bool enabled = true) =>
            new ScreenItem { Kind = ItemKind.Button, Id = id, Text = id, Enabled = enabled, Click = click };

        [Test]
        public void Every_part_of_the_screens_prefab_is_wired()
        {
            var missing = LoadScreens().GetComponent<ScreenUi>().MissingReference();
            Assert.IsNull(missing, "falta ligar no Inspector do Screens: " + missing);
        }

        [Test]
        public void Screens_draws_above_the_battle_and_scales_like_it()
        {
            var screens = LoadScreens();
            var canvas = screens.GetComponent<Canvas>();
            Assert.IsNotNull(canvas, "a raiz do Screens precisa de um Canvas");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode, "Render Mode: Screen Space - Overlay");
            var hud = AssetDatabase.LoadAssetAtPath<GameObject>(BattleHudPrefabTests.HudPath);
            Assert.IsNotNull(hud, "falta o prefab " + BattleHudPrefabTests.HudPath);
            Assert.Greater(canvas.sortingOrder, hud.GetComponent<Canvas>().sortingOrder,
                "Sort Order do Screens tem de ser maior que o do BattleHud, senão a lista fica atrás da batalha");
            var scaler = screens.GetComponent<CanvasScaler>();
            Assert.IsNotNull(scaler, "a raiz do Screens precisa de um Canvas Scaler");
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode, "UI Scale Mode: Scale With Screen Size");
            Assert.AreEqual(new Vector2(1080f, 1920f), scaler.referenceResolution, "Reference Resolution 1080 x 1920");
            Assert.IsNotNull(screens.GetComponent<GraphicRaycaster>(), "sem Graphic Raycaster os botões não recebem toque");
        }

        [Test]
        public void The_list_scrolls_vertically_and_grows_with_its_rows()
        {
            var ui = LoadScreens().GetComponentInChildren<ScrollRect>(true);
            Assert.IsNotNull(ui, "falta o Scroll Rect da lista");
            Assert.IsTrue(ui.vertical, "Scroll Rect: Vertical marcado");
            Assert.IsFalse(ui.horizontal, "Scroll Rect: Horizontal desmarcado");
            Assert.IsNotNull(ui.viewport, "Scroll Rect: campo Viewport vazio");
            var content = ui.content;
            var group = content.GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(group, "o Content precisa de um Vertical Layout Group");
            Assert.IsTrue(group.childControlHeight, "Vertical Layout Group: Control Child Size > Height marcado (senão os textos se sobrepõem)");
            Assert.IsTrue(group.childControlWidth, "Vertical Layout Group: Control Child Size > Width marcado");
            Assert.IsFalse(group.childForceExpandHeight, "Vertical Layout Group: Child Force Expand > Height desmarcado");
            var fitter = content.GetComponent<ContentSizeFitter>();
            Assert.IsNotNull(fitter, "o Content precisa de um Content Size Fitter");
            Assert.AreEqual(ContentSizeFitter.FitMode.PreferredSize, fitter.verticalFit, "Content Size Fitter: Vertical Fit = Preferred Size");
            Assert.AreEqual(0, content.childCount, "o Content começa vazio: as linhas nascem dos prefabs ScreenHeading, ScreenText e ScreenButton");
        }

        [Test]
        public void Show_draws_one_row_per_item_in_order_and_reuses_rows()
        {
            var ui = NewCopy();
            int clicked = 0;
            ui.Show(Model("Loja", Heading("Cartas"), Text("Ouro 12"), Button("a", () => clicked = 1), Button("b", () => clicked = 2, enabled: false)), "pt");

            Assert.IsTrue(ui.Visible, "Show deixa a tela visível");
            Assert.AreEqual(4, ui.RowCount, "uma linha por item");
            var content = _copy.GetComponentInChildren<ScrollRect>(true).content;
            StringAssert.StartsWith("ScreenHeading", content.GetChild(0).name, "a primeira linha é uma cópia do prefab ScreenHeading");
            StringAssert.StartsWith("ScreenText", content.GetChild(1).name, "a segunda linha é uma cópia do prefab ScreenText");
            StringAssert.StartsWith("ScreenButton", content.GetChild(2).name, "a terceira linha é uma cópia do prefab ScreenButton");

            var second = content.GetChild(3).GetComponent<TextButton>();
            Assert.IsFalse(second.Interactable, "um botão do modelo com Enabled = false fica desligado");
            content.GetChild(2).GetComponent<TextButton>().Button.onClick.Invoke();
            Assert.AreEqual(1, clicked, "tocar no botão chama o Click do item");

            int children = content.childCount;
            ui.Show(Model("Porta", Button("c", () => clicked = 3)), "en");
            Assert.AreEqual(1, ui.RowCount, "só a linha da tela nova fica ligada");
            Assert.AreEqual(children, content.childCount, "trocar de tela reaproveita as linhas, não cria outras");
            content.GetChild(0).GetComponent<TextButton>().Button.onClick.Invoke();
            Assert.AreEqual(3, clicked, "o botão reaproveitado chama o Click da tela nova");

            ui.Hide();
            Assert.IsFalse(ui.Visible, "Hide esconde e deixa de bloquear toques");
        }
    }
}