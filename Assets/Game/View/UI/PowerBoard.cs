using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cubo.App;
using Cubo.Domain;

namespace Cubo.View
{
    // O painel de poderes no alto da tela: os 6 do jogador à esquerda e os 6 do rival à direita
    // (cada herói com os seus dois). Tocar num poder abre o detalhe; nos do jogador prontos, o detalhe traz Usar.
    // Ver os poderes do rival é o que dá sentido a escolher a face entregue.
    public sealed class PowerBoard : MonoBehaviour
    {
        [Tooltip("As 6 linhas do jogador, na ordem dos poderes: herói 1 (poder 1, poder 2), herói 2, herói 3.")]
        [SerializeField] private PowerRow[] playerRows = new PowerRow[BattleEngine.Slots];
        [Tooltip("As 6 linhas do rival, na mesma ordem.")]
        [SerializeField] private PowerRow[] enemyRows = new PowerRow[BattleEngine.Slots];
        [SerializeField] private PowerTip tip;

        [Header("Os rostos dos heróis (um por linha, antes dos seus dois poderes)")]
        [Tooltip("3 Images: o rosto do herói 1, 2 e 3 do jogador.")]
        [SerializeField] private Image[] playerFaces = new Image[3];
        [Tooltip("3 Images: o rosto do herói 1, 2 e 3 do rival.")]
        [SerializeField] private Image[] enemyFaces = new Image[3];
        [Tooltip("Vira os rostos do rival para olharem para a esquerda, como as figuras do palco.")]
        [SerializeField] private bool flipEnemyFaces = true;

        [Header("O rival escolhido como alvo (3 contra 3)")]
        [Tooltip("Opcional: 3 molduras douradas, uma por linha do rival (rosto + dois poderes), na ordem do time. "
               + "Só a do rival que o jogador escolheu fica acesa.")]
        [SerializeField] private GameObject[] enemyTargetFrames = new GameObject[3];

        public event Action<int> UseRequested;      // o jogador tocou Usar no poder deste espaço

        private PowerView[] _player, _enemy;
        private Func<int, Sprite> _iconOf;
        private Func<int, Color> _colorOf;
        private Func<string, Sprite> _faceOf;
        private Side _tipSide;
        private int _tipSlot = -1;
        private bool _wired;

        public IReadOnlyList<PowerRow> PlayerRows => playerRows;
        public IReadOnlyList<PowerRow> EnemyRows => enemyRows;

        public void Init(Func<int, Sprite> iconOf, Func<int, Color> colorOf, Func<string, Sprite> faceOf = null)
        {
            _iconOf = iconOf;
            _colorOf = colorOf;
            _faceOf = faceOf;
            if (_wired) return;
            _wired = true;
            for (int i = 0; i < playerRows.Length; i++)
            {
                int slot = i;
                playerRows[i].Init();
                playerRows[i].Tapped += () => Toggle(Side.Player, slot);
            }
            for (int i = 0; i < enemyRows.Length; i++)
            {
                int slot = i;
                enemyRows[i].Init();
                enemyRows[i].Tapped += () => Toggle(Side.Enemy, slot);
            }
            tip.Init();
            tip.Used += () =>
            {
                int slot = _tipSlot;
                HideTip();
                if (slot >= 0) UseRequested?.Invoke(slot);
            };
            tip.Closed += () => { _tipSlot = -1; _infoSource = null; };
        }

        // O estado novo (fim de lote, troca de idioma): pinta as 12 linhas e atualiza o balão aberto.
        public void Show(PowerView[] player, PowerView[] enemy)
        {
            _player = player;
            _enemy = enemy;
            Paint(playerRows, player);
            Paint(enemyRows, enemy);
            PaintFaces(playerFaces, player, false);
            PaintFaces(enemyFaces, enemy, flipEnemyFaces);
            if (_tipSlot >= 0 && tip.IsOpen)
            {
                var p = ViewOf(_tipSide, _tipSlot);
                if (p != null) tip.UpdateBody(p.Detail, CanUse(_tipSide, p));
            }
        }

        // A mana de um lado mudou no meio do lote: só as barras andam.
        public void SetMana(Side side, int[] mana)
        {
            var rows = side == Side.Player ? playerRows : enemyRows;
            var views = side == Side.Player ? _player : _enemy;
            if (views == null || mana == null) return;
            for (int i = 0; i < rows.Length && i < mana.Length && i < views.Length; i++)
                rows[i].SetFill(UiMath.ManaFill(mana[i], views[i].Cost));
        }

        // O rival que o jogador vai atacar (0, 1, 2), ou -1 sem alvo: acende só a moldura dele.
        public void MarkEnemyTarget(int hero)
        {
            if (enemyTargetFrames == null) return;
            for (int i = 0; i < enemyTargetFrames.Length; i++)
                if (enemyTargetFrames[i] != null) enemyTargetFrames[i].SetActive(i == hero);
        }

        // O mesmo balão dos poderes, só com texto (sem Usar), aberto debaixo de `source`: o detalhe de um efeito
        // (veneno, fogo). Tocar de novo na mesma coisa fecha.
        public void ShowInfo(string body, RectTransform source)
        {
            if (tip.IsOpen && _infoSource == source) { HideTip(); return; }
            _tipSlot = -1;
            _infoSource = source;
            tip.Show(body, source, false);
        }

        private RectTransform _infoSource;

        public void HideTip()
        {
            _tipSlot = -1;
            _infoSource = null;
            tip.Hide();
        }

        private void Toggle(Side side, int slot)
        {
            if (tip.IsOpen && _tipSide == side && _tipSlot == slot) { HideTip(); return; }
            var p = ViewOf(side, slot);
            if (p == null) return;
            _tipSide = side;
            _tipSlot = slot;
            _infoSource = null;
            var rows = side == Side.Player ? playerRows : enemyRows;
            tip.Show(p.Detail, rows[slot].Rect, CanUse(side, p));
        }

        private static bool CanUse(Side side, PowerView p) => side == Side.Player && p.Enabled;

        private PowerView ViewOf(Side side, int slot)
        {
            var views = side == Side.Player ? _player : _enemy;
            return views != null && slot >= 0 && slot < views.Length ? views[slot] : null;
        }

        private void Paint(PowerRow[] rows, PowerView[] views)
        {
            if (views == null) return;
            for (int i = 0; i < rows.Length && i < views.Length; i++)
            {
                var p = views[i];
                rows[i].Show(_iconOf?.Invoke(p.Color), _colorOf != null ? _colorOf(p.Color) : Color.white, p.Fill, p.Enabled);
            }
        }

        // Um rosto por herói: o herói h tem os poderes 2h e 2h+1.
        private void PaintFaces(Image[] faces, PowerView[] views, bool flip)
        {
            if (faces == null || views == null) return;
            for (int h = 0; h < faces.Length; h++)
            {
                var img = faces[h];
                if (img == null) continue;
                int slot = 2 * h;
                var sprite = slot < views.Length && _faceOf != null ? _faceOf(views[slot].HeroId) : null;
                img.sprite = sprite;
                img.enabled = sprite != null;
                img.preserveAspect = true;
                var s = img.rectTransform.localScale;
                img.rectTransform.localScale = new Vector3(flip ? -Mathf.Abs(s.x) : Mathf.Abs(s.x), s.y, s.z);
            }
        }

        public string MissingReference()
        {
            if (playerRows == null || playerRows.Length != BattleEngine.Slots) return "playerRows (precisa de 6)";
            if (enemyRows == null || enemyRows.Length != BattleEngine.Slots) return "enemyRows (precisa de 6)";
            for (int i = 0; i < BattleEngine.Slots; i++)
            {
                if (playerRows[i] == null) return "playerRows[" + i + "]";
                if (playerRows[i].MissingReference() is string a) return "playerRows[" + i + "]." + a;
                if (enemyRows[i] == null) return "enemyRows[" + i + "]";
                if (enemyRows[i].MissingReference() is string b) return "enemyRows[" + i + "]." + b;
            }
            // a mesma linha arrastada duas vezes (por exemplo nas duas listas): uma das duas nunca seria pintada
            var seen = new HashSet<PowerRow>();
            for (int i = 0; i < BattleEngine.Slots; i++)
            {
                if (!seen.Add(playerRows[i])) return "playerRows[" + i + "] repete outra linha";
            }
            for (int i = 0; i < BattleEngine.Slots; i++)
            {
                if (!seen.Add(enemyRows[i])) return "enemyRows[" + i + "] repete uma linha (é do jogador ou já está na lista)";
            }
            if (tip == null) return "tip";
            return tip.MissingReference() is string t ? "tip." + t : null;
        }
    }
}
