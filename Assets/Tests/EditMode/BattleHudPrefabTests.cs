using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Cubo.App;
using Cubo.Domain;
using Cubo.View;

namespace Cubo.Tests
{
    // Confere o prefab da interface montado no editor. Um campo esquecido no Inspector aparece aqui,
    // com o caminho do campo, e não como NullReferenceException no meio de uma partida.
    public class BattleHudPrefabTests
    {
        public const string HudPath = "Assets/Game/View/Prefabs/UI/BattleHud.prefab";

        private static GameObject LoadHud()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
            Assert.IsNotNull(prefab, "falta o prefab " + HudPath);
            Assert.IsNotNull(prefab.GetComponent<BattleUi>(), "o BattleHud não tem o componente BattleUi na raiz");
            return prefab;
        }

        [Test]
        public void Every_part_of_the_hud_is_wired()
        {
            var missing = LoadHud().GetComponent<BattleUi>().MissingReference();
            Assert.IsNull(missing, "falta ligar no Inspector do BattleHud: " + missing);
        }

        [Test]
        public void Twelve_power_rows_all_different()
        {
            var ui = LoadHud().GetComponent<BattleUi>();
            Assert.IsNotNull(ui.PowerBoard, "falta o Power Board");
            var rows = ui.PowerBoard.PlayerRows.Concat(ui.PowerBoard.EnemyRows).ToList();
            Assert.AreEqual(2 * BattleEngine.Slots, rows.Count, "Player Rows e Enemy Rows precisam de 6 linhas cada");
            Assert.AreEqual(rows.Count, rows.Distinct().Count(), "a mesma PowerRow foi arrastada para dois lugares");
        }

        // A entrega é pelo balão (Confirmar e Cancelar); a vista volta para a sua face pelo botão de casa.
        [Test]
        public void The_hand_popup_and_the_home_button_are_wired()
        {
            var ui = LoadHud().GetComponent<BattleUi>();
            Assert.IsNotNull(ui.HomeButton, "falta o Home Button");
            Assert.IsNotNull(ui.HandPopup, "falta o Hand Popup");
            Assert.IsNull(ui.HandPopup.MissingReference(), "falta ligar no Hand Popup: " + ui.HandPopup.MissingReference());
        }

        [Test]
        public void Canvas_scales_from_a_1080_by_1920_portrait_reference()
        {
            var hud = LoadHud();
            var canvas = hud.GetComponent<Canvas>();
            Assert.IsNotNull(canvas, "a raiz do BattleHud precisa de um Canvas");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode, "Render Mode: Screen Space - Overlay");
            var scaler = hud.GetComponent<CanvasScaler>();
            Assert.IsNotNull(scaler, "a raiz do BattleHud precisa de um Canvas Scaler");
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode, "UI Scale Mode: Scale With Screen Size");
            Assert.AreEqual(1080f, scaler.referenceResolution.x, 0.5f, "Reference Resolution X");
            Assert.AreEqual(1920f, scaler.referenceResolution.y, 0.5f, "Reference Resolution Y");
            Assert.AreEqual(0.5f, scaler.matchWidthOrHeight, 0.01f, "Match");
            Assert.IsNotNull(hud.GetComponent<GraphicRaycaster>(), "sem Graphic Raycaster os botões não recebem toque");
        }

    }
}