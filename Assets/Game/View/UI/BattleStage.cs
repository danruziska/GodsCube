using System;
using UnityEngine;
using UnityEngine.UI;
using Cubo.App;
using Cubo.Domain;

namespace Cubo.View
{
    // O palco no alto da tela da batalha: o cenário (bosque, castelo, montanhas...), os três heróis do jogador
    // à esquerda e os três do rival à direita. Por enquanto é só enfeite: mostra quem está na luta.
    // Fase 2: as animações (grupo, carta, poder) vão pegar a figura de cada herói por AvatarOf.
    public sealed class BattleStage : MonoBehaviour
    {
        [Serializable]
        public struct Avatar
        {
            public string heroId;      // o id do heroes.json: rei, cavaleiro, bispo, joker, rainha, torre, peao...
            public Sprite sprite;      // a figura de corpo inteiro, de pé, olhando para a DIREITA
            [Tooltip("Opcional: o rosto pequeno, para o painel de poderes. Vazio: usa a figura de corpo inteiro.")]
            public Sprite face;
        }

        [Header("Cenário")]
        [SerializeField] private Image environment;
        [Tooltip("Os cenários possíveis. Cada batalha sorteia um. Vazio: fica a imagem que já está no Environment.")]
        [SerializeField] private Sprite[] environments = new Sprite[0];

        [Header("Os lugares dos heróis (da frente para trás)")]
        [Tooltip("Três Images, uma por herói do jogador, na ordem do time.")]
        [SerializeField] private Image[] playerSlots = new Image[3];
        [Tooltip("Três Images, uma por herói do rival, na ordem do time.")]
        [SerializeField] private Image[] enemySlots = new Image[3];
        [Tooltip("Opcional: a área de toque de cada rival (uma Image transparente com Raycast Target, embaixo da barra). " +
                 "Vazio: o toque vale na figura inteira do rival.")]
        [SerializeField] private Graphic[] enemyTapAreas = new Graphic[3];
        [Tooltip("Vira as figuras do rival para olharem para a esquerda (para o jogador).")]
        [SerializeField] private bool flipEnemies = true;

        [Header("3 contra 3: as barras de vida em cima das cabeças")]
        [Tooltip("Opcional: três barras, uma em cima de cada herói do jogador, na ordem do time.")]
        [SerializeField] private HeroHpBar[] playerBars = new HeroHpBar[3];
        [Tooltip("Opcional: três barras, uma em cima de cada herói do rival, na ordem do time.")]
        [SerializeField] private HeroHpBar[] enemyBars = new HeroHpBar[3];
        [Header("Efeitos nas barras (veneno, fogo e os que vierem)")]
        [Tooltip("O prefab StatusIcon: a barra cria uma cópia por efeito ativo, na fila Status dela.")]
        [SerializeField] private StatusIcon statusPrefab;
        [Tooltip("A imagem de cada efeito, pelo id: poison, burn. Um efeito sem imagem aqui aparece vazio.")]
        [SerializeField] private StatusArt[] statusArt = new StatusArt[0];

        [Serializable]
        public struct StatusArt
        {
            public string id;          // o id do efeito: poison, burn
            public Sprite sprite;
        }

        [Tooltip("A transparência da figura de um herói caído (0 = some, 1 = igual aos outros).")]
        [Range(0f, 1f)] [SerializeField] private float fallenAlpha = 0.3f;

        // Tocou numa figura do rival: o índice dele no time (0, 1, 2). A Image precisa de Raycast Target ligado.
        public event Action<int> EnemyTapped;

        // Tocou num ícone de efeito em cima de uma barra (do jogador ou do rival): o texto do balão e onde ele abre.
        public event Action<string, RectTransform> StatusTapped;

        [Header("As figuras")]
        [SerializeField] private Avatar[] avatars = new Avatar[0];
        [Tooltip("Herói sem figura na lista acima: aparece um bloco desta cor, para ver o lugar dele.")]
        [SerializeField] private Color placeholder = new Color(1f, 1f, 1f, 0.25f);

        // Uma batalha nova: sorteia o cenário e põe cada herói no seu lugar.
        public void Show(BattleState state, int seed)
        {
            if (environment != null && environments != null && environments.Length > 0)
            {
                var pick = environments[UiMath.PickIndex(seed, environments.Length)];
                if (pick != null) environment.sprite = pick;
            }
            Fill(playerSlots, state.Teams[(int)Side.Player], false);
            Fill(enemySlots, state.Teams[(int)Side.Enemy], flipEnemies);
            Unflip(enemySlots, enemyBars, flipEnemies);
            WireTaps();
        }

        // Uma barra filha de uma figura virada também viraria (texto ao contrário, barra esvaziando para o outro lado):
        // ela recebe a escala X oposta, para ficar sempre de frente.
        private static void Unflip(Image[] slots, HeroHpBar[] bars, bool flip)
        {
            if (slots == null || bars == null) return;
            for (int i = 0; i < bars.Length && i < slots.Length; i++)
            {
                if (bars[i] == null || slots[i] == null || !bars[i].transform.IsChildOf(slots[i].transform)) continue;
                var s = bars[i].transform.localScale;
                bars[i].transform.localScale = new Vector3(flip ? -Mathf.Abs(s.x) : Mathf.Abs(s.x), s.y, s.z);
            }
        }

        private bool _tapsWired;

        private void WireTaps()
        {
            if (_tapsWired || enemySlots == null) return;
            _tapsWired = true;
            foreach (var bars in new[] { playerBars, enemyBars })
                if (bars != null)
                    foreach (var b in bars)
                        if (b != null) b.StatusTapped += icon => StatusTapped?.Invoke(icon.Detail, icon.Rect);
            for (int i = 0; i < enemySlots.Length; i++)
            {
                Graphic area = enemyTapAreas != null && i < enemyTapAreas.Length && enemyTapAreas[i] != null ? enemyTapAreas[i] : enemySlots[i];
                if (area == null) continue;
                var tap = area.GetComponent<StageTap>();
                if (tap == null) tap = area.gameObject.AddComponent<StageTap>();     // não use ?? com componentes: o "nulo falso" da Unity passa
                tap.Index = i;
                tap.Tapped += k => EnemyTapped?.Invoke(k);
            }
        }

        // O estado de cada herói depois de um lote: a vida, quem está de pé e quem é alvo.
        public void ShowTeams(FighterView[] players, FighterView[] enemies)
        {
            ShowTeam(playerSlots, playerBars, players);
            ShowTeam(enemySlots, enemyBars, enemies);
        }

        private void ShowTeam(Image[] slots, HeroHpBar[] bars, FighterView[] team)
        {
            if (team == null) return;
            for (int i = 0; i < team.Length; i++)
            {
                var f = team[i];
                if (bars != null && i < bars.Length && bars[i] != null)
                {
                    bars[i].Show(f.Hp, f.Max, f.Alive, f.Targeted);
                    bars[i].ShowStatuses(f.Alive ? f.Statuses : null, statusPrefab, StatusSprite);
                }
                if (slots != null && i < slots.Length && slots[i] != null)
                {
                    var c = slots[i].color;
                    slots[i].color = new Color(c.r, c.g, c.b, f.Alive ? 1f : fallenAlpha);
                }
            }
        }

        // A vida de um herói mudando no meio da apresentação: a barra corre até o valor novo.
        public void MoveHp(Side side, int hero, int hp, int max)
        {
            var bars = side == Side.Player ? playerBars : enemyBars;
            if (bars != null && hero >= 0 && hero < bars.Length && bars[hero] != null) bars[hero].MoveTo(hp, max);
            if (hp <= 0)
            {
                var slots = side == Side.Player ? playerSlots : enemySlots;
                if (slots != null && hero >= 0 && hero < slots.Length && slots[hero] != null)
                {
                    var c = slots[hero].color;
                    slots[hero].color = new Color(c.r, c.g, c.b, fallenAlpha);
                }
            }
        }

        // Quem `side` está atacando: a marca de alvo aparece em cima do herói do outro lado.
        public void MarkTarget(Side side, int hero)
        {
            var bars = side == Side.Player ? enemyBars : playerBars;
            if (bars == null) return;
            for (int i = 0; i < bars.Length; i++) if (bars[i] != null) bars[i].SetTarget(i == hero);
        }

        private Sprite StatusSprite(string id)
        {
            if (statusArt == null) return null;
            foreach (var a in statusArt) if (a.id == id) return a.sprite;
            return null;
        }

        // A figura de um herói na tela (para as animações da fase 2); null se não houver.
        public RectTransform AvatarOf(Side side, int hero)
        {
            var slots = side == Side.Player ? playerSlots : enemySlots;
            return slots != null && hero >= 0 && hero < slots.Length && slots[hero] != null ? slots[hero].rectTransform : null;
        }

        private void Fill(Image[] slots, HeroDef[] team, bool flip)
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Length; i++)
            {
                var img = slots[i];
                if (img == null) continue;
                var hero = team != null && i < team.Length ? team[i] : null;
                img.gameObject.SetActive(hero != null);
                if (hero == null) continue;
                var sprite = SpriteOf(hero.Id);
                img.sprite = sprite;
                img.color = sprite != null ? Color.white : placeholder;
                img.preserveAspect = true;
                var s = img.rectTransform.localScale;
                img.rectTransform.localScale = new Vector3(flip ? -Mathf.Abs(s.x) : Mathf.Abs(s.x), s.y, s.z);
            }
        }

        // O rosto do herói para o painel de poderes (ou a figura, sem rosto); null se o herói não está na lista.
        public Sprite FaceOf(string heroId)
        {
            if (avatars == null) return null;
            foreach (var a in avatars) if (a.heroId == heroId) return a.face != null ? a.face : a.sprite;
            return null;
        }

        private Sprite SpriteOf(string heroId)
        {
            if (avatars == null) return null;
            foreach (var a in avatars) if (a.heroId == heroId) return a.sprite;
            return null;
        }

        public string MissingReference()
        {
            if (playerSlots == null || playerSlots.Length != 3) return "playerSlots (precisa de 3)";
            if (enemySlots == null || enemySlots.Length != 3) return "enemySlots (precisa de 3)";
            for (int i = 0; i < 3; i++)
            {
                if (playerSlots[i] == null) return "playerSlots, elemento " + i;
                if (enemySlots[i] == null) return "enemySlots, elemento " + i;
            }
            return null;
        }
    }
}
