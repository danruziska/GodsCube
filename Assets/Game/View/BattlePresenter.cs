using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cubo.App;
using Cubo.Domain;

namespace Cubo.View
{
    // Toca na tela o que a BattleSession contou, evento por evento e na ordem. Não decide nada do jogo.
    // O que a tela mostra (cores, vida) vem só dos eventos; o estado do domínio já está no fim do lote.
    public sealed class BattlePresenter : MonoBehaviour
    {
        [SerializeField] private CubeView cube;
        [SerializeField] private CubeInput input;
        [Tooltip("Fase 10: a vista volta para a frente antes de o cubo inteiro girar.")]
        [SerializeField] private CubeOrbit orbit;
        [SerializeField] private float fireSeconds = 0.25f;
        [SerializeField] private float damageSeconds = 0.4f;
        [SerializeField] private float refillSeconds = 0.2f;
        [Tooltip("Quanto as peças levam para cair até o lugar novo, depois de um grupo.")]
        [SerializeField] private float fallSeconds = 0.22f;
        [SerializeField] private float turnSeconds = 0.5f;
        [SerializeField] private float enemyThinkSeconds = 0.3f;     // soma-se a turnSeconds: o rival "pensa" 0,8 s
        [SerializeField] private float wholeTurnSeconds = 0.45f;     // a entrega: o cubo inteiro gira um quarto de volta
        [SerializeField] private float homeSeconds = 0.25f;          // a vista volta para a frente antes disso

        private const float Banned = 0.95f;                          // a face proibida afunda um pouco
        private const float BannedBrightness = 0.45f;                // e fica apagada (encolher abria buracos entre as imagens)
        // A face escolhida brilha: uma luz dourada por cima dela, respirando como o brilho dos espaços do time
        // (alpha de 60% a 140% do da cor, ciclo de 1,2 s).
        private const float GlowSeconds = 1.2f, GlowSwing = 0.4f;

        private BattleSession _session;
        private Texts _texts;
        private readonly Queue<IReadOnlyList<IBattleEvent>> _queue = new Queue<IReadOnlyList<IBattleEvent>>();
        private readonly List<PieceView> _lit = new List<PieceView>();
        private readonly Dictionary<int, float> _marks = new Dictionary<int, float>();   // peça -> escala da marca de face
        private int _glowFace = -1;               // a face escolhida na entrega, ou -1
        private int[] _aim;                       // a área que o dedo mira (veneno ou fogo), ou null
        private int _aimKind;
        private float _aimStart;
        private float _glowStart;
        private bool _enemyTurn;
        private Vector3 _base = Vector3.one;      // a escala normal da peça (a do prefab)

        public event Action BatchStarted;          // a interface se desabilita enquanto toca
        public event Action BatchFinished;         // a interface relê o estado
        public event Action<Side, int[]> ManaShown; // a mana de um lado mudou neste ponto do lote (a barra enche na hora)
        public event Action<Side, int, int, int> HeroHpShown;   // 3 contra 3: lado, herói, vida nova, máximo
        public event Action<Side, int> TargetShown;              // lado, herói do outro lado que ele passou a atacar
        public string Banner { get; private set; } = "";   // o nome do poder ou da carta que acabou de entrar em cena

        public bool Playing { get; private set; }
        public bool Ended { get; private set; }
        public Side Winner { get; private set; }
        public Side Turn { get; private set; }
        public int Round { get; private set; }
        public readonly float[] Hp = new float[2];          // o que a tela mostra, não o estado
        public readonly int[] MaxHp = new int[2];

        public bool AwaitingRestart => Ended && !Playing;

        public void Attach(BattleSession session, Texts texts)
        {
            StopAllCoroutines();
            if (_session != null) _session.Played -= Enqueue;
            _queue.Clear(); _lit.Clear(); _marks.Clear(); _glowFace = -1; cube.HideFaceGlow();
            cube.SnapPiecesHome();
            Playing = false; Ended = false; _enemyTurn = false;
            _session = session; _texts = texts; Banner = "";
            _base = cube.Piece(0).transform.localScale;
            for (int i = 0; i < 2; i++) { Hp[i] = session.State.Hp[i]; MaxHp[i] = session.State.MaxHp[i]; }
            Turn = session.State.Turn; Round = session.State.Round;
            _aim = null;
            cube.ShowAim(null, 0, 1f);
            cube.ShowHazards(BattleEngine.HazardMap(session.State));      // as áreas de uma batalha retomada
            input.Locked = false;
            input.ViewLocked = false;
            session.Played += Enqueue;
            if (orbit == null) Debug.LogWarning("BattlePresenter: ligue o campo Orbit (o Cube Orbit do objeto Input); sem ele a vista não volta para a frente antes da entrega.", this);
        }

        private float ScaleOf(int cell) => _marks.TryGetValue(cell, out var k) ? k : 1f;

        // Destaque do grupo enquanto o dedo está em cima da peça.
        public void Highlight(int[] cells)
        {
            ClearHighlight();
            if (cells == null) return;
            foreach (int c in cells) { var p = cube.Piece(c); p.transform.localScale = _base * 1.12f; _lit.Add(p); }
        }

        public void ClearHighlight()
        {
            foreach (var p in _lit) p.transform.localScale = _base * ScaleOf(p.Cell);
            _lit.Clear();
        }

        // Na entrega: a face escolhida brilha e a proibida afunda apagada (-1 = nenhuma). Chamar de novo troca as marcas;
        // a mesma face escolhida continua o brilho de onde estava (a tela chama isto a cada atualização).
        public void MarkFaces(int selected, int banned)
        {
            int keepGlow = _glowFace;
            ClearMarks();
            var g = cube.Geo;
            if (g == null) return;
            if (banned >= 0 && banned <= 5)
                for (int c = banned * g.NN; c < (banned + 1) * g.NN; c++)
                {
                    _marks[c] = Banned;
                    var piece = cube.Piece(c);
                    piece.transform.localScale = _base * Banned;
                    piece.SetBrightness(BannedBrightness);
                }
            if (selected >= 0 && selected <= 5)
            {
                if (selected != keepGlow) _glowStart = Time.unscaledTime;
                _glowFace = selected;
                Glow();
            }
        }

        private void Update()
        {
            if (_glowFace >= 0) Glow();
            if (_aim != null) cube.ShowAim(_aim, _aimKind, 1f - GlowSwing + 2f * GlowSwing * UiMath.Pulse(Time.unscaledTime - _aimStart, GlowSeconds));
        }

        // Veneno e fogo: a área que o dedo está mirando (kind "poison" ou "burn"); null apaga a mira.
        public void Aim(int[] cells, string kind)
        {
            if (cells == null || cells.Length == 0) { ClearAim(); return; }
            if (_aim == null) _aimStart = Time.unscaledTime;
            _aim = cells;
            _aimKind = kind == EquipKinds.Burn ? 2 : 1;
            cube.ShowAim(_aim, _aimKind, 1f);
        }

        public void ClearAim()
        {
            _aim = null;
            cube.ShowAim(null, 0, 1f);
        }

        private void Glow()
        {
            float p = UiMath.Pulse(Time.unscaledTime - _glowStart, GlowSeconds);
            cube.ShowFaceGlow(_glowFace, 1f - GlowSwing + 2f * GlowSwing * p);
        }

        public void ClearMarks()
        {
            foreach (var c in _marks.Keys)
            {
                var piece = cube.Piece(c);
                piece.transform.localScale = _base;
                piece.SetBrightness(1f);
            }
            _marks.Clear();
            _glowFace = -1;
            cube.HideFaceGlow();
        }

        private void Enqueue(IReadOnlyList<IBattleEvent> batch)
        {
            ClearHighlight();
            ClearMarks();
            _queue.Enqueue(batch);
            if (!Playing) StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            Playing = true;
            input.Locked = true;
            BatchStarted?.Invoke();
            while (_queue.Count > 0)
            {
                _enemyTurn = false;
                foreach (var ev in _queue.Dequeue()) yield return Play(ev);
                _session.PresentationDone();
            }
            Playing = false;
            Banner = "";
            input.Locked = Ended;           // depois do fim, o toque é para recomeçar, não para jogar
            input.ViewLocked = Ended;
            BatchFinished?.Invoke();
        }

        public IEnumerator Play(IBattleEvent ev)
        {
            switch (ev)
            {
                case GroupFired g: return Fire(g);
                case HpChanged h: return TweenHp(h);
                case CellsRefilled r: return Refill(r);
                case CubeTurned c: return TurnCube(c);
                case TurnChanged t: return ChangeTurn(t);
                case PowerCast c: return Cast(c);
                case BattleEnded e: return End(e);
                case ManaChanged m: ManaShown?.Invoke(m.Side, m.Mana); return Nothing();
                case HeroHpChanged h: HeroHpShown?.Invoke(h.Side, h.Hero, h.To, h.Max); return Nothing();
                case TargetChanged tc: TargetShown?.Invoke(tc.Side, tc.Hero); return Nothing();
                case HazardsChanged hz: cube.ShowHazards(hz.Map); return Nothing();
                case HazardArmed a: return Say(_texts.T("hazard.armed." + a.Kind));
                case Afflicted a: return Say(_texts.T("hazard.hit." + a.Kind, ("hero", HeroName(a.Side, a.Hero))));
                case DotTicked d: return Say(_texts.T("dot." + d.Kind, ("n", d.Dmg), ("hero", HeroName(d.Side, d.Hero))));
                default: return Nothing();       // HandPhase: a interface relê o estado ao fim do lote
            }
        }

        private static IEnumerator Nothing() { yield break; }

        private string HeroName(Side side, int hero) => _texts.Name("hero." + _session.State.Hero(side, hero).Id);

        // Um aviso no letreiro (veneno pronto, herói envenenado, dano do veneno), com tempo para ler.
        private IEnumerator Say(string text)
        {
            Banner = text;
            yield return Wait(turnSeconds * 0.8f);
        }

        private IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) yield return null;
        }

        // O grupo pulsa; um crítico aparece no letreiro. Na cascata, o letreiro conta o combo (×2, ×3...).
        private IEnumerator Fire(GroupFired g)
        {
            string crit = g.Crit ? _texts.T("crit.banner") : null;
            if (g.Link > 0)
            {
                string combo = _texts.T("chain.banner", ("n", g.Link + 1));
                Banner = crit != null ? combo + " · " + crit : combo;
            }
            else if (crit != null) Banner = crit;
            yield return Pulse(g.Cells);
        }

        private IEnumerator Pulse(int[] cells)
        {
            for (float t = 0f; t < fireSeconds; t += Time.unscaledDeltaTime)
            {
                float k = 1f + 0.25f * Mathf.Sin(Mathf.PI * t / fireSeconds);
                foreach (int c in cells) cube.Piece(c).transform.localScale = _base * k;
                yield return null;
            }
            foreach (int c in cells) cube.Piece(c).transform.localScale = _base;
        }

        private IEnumerator TweenHp(HpChanged h)
        {
            int i = (int)h.Side;
            for (float t = 0f; t < damageSeconds; t += Time.unscaledDeltaTime)
            {
                Hp[i] = Mathf.Lerp(h.From, h.To, t / damageSeconds);
                yield return null;
            }
            Hp[i] = h.To;
        }

        // As peças de cima caem até o lugar novo (acelerando, como gravidade) e, ao mesmo tempo, as novas aparecem
        // pequenas no alto das colunas e crescem até o tamanho normal. Cada peça termina exatamente no seu lugar.
        private IEnumerator Refill(CellsRefilled r)
        {
            cube.Paint(r.Board);
            var falls = r.Falls ?? new List<Fall>();
            var from = new Vector3[falls.Count];
            var to = new Vector3[falls.Count];
            for (int i = 0; i < falls.Count; i++)
            {
                from[i] = CubeLayout.HomePosition(cube.Geo, falls[i].From);
                to[i] = CubeLayout.HomePosition(cube.Geo, falls[i].To);
                cube.Piece(falls[i].To).transform.localPosition = from[i];
            }
            float total = Mathf.Max(refillSeconds, falls.Count > 0 ? fallSeconds : 0f);
            for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(t / refillSeconds));
                foreach (int c in r.Cells) cube.Piece(c).transform.localScale = _base * k;
                float f = fallSeconds > 0f ? Mathf.Clamp01(t / fallSeconds) : 1f;
                for (int i = 0; i < falls.Count; i++)
                    cube.Piece(falls[i].To).transform.localPosition = Vector3.LerpUnclamped(from[i], to[i], f * f);
                yield return null;
            }
            for (int i = 0; i < falls.Count; i++) cube.Piece(falls[i].To).transform.localPosition = to[i];
            foreach (int c in r.Cells) cube.Piece(c).transform.localScale = _base;
        }

        // A entrega: a vista volta para a frente, o cubo inteiro gira um quarto de volta (a face escolhida chega à
        // frente) e as peças voltam ao lugar com as cores já giradas. O último quadro e o repintado são iguais.
        private IEnumerator TurnCube(CubeTurned c)
        {
            if (orbit != null) yield return orbit.GoHome(homeSeconds);
            var turn = cube.BeginTurn(c.Move);
            yield return turn.Animate(1f, wholeTurnSeconds);
            turn.End();
            cube.Paint(c.Board);
        }

        // O poder entra em cena: o nome aparece e, se varre peças, elas pulsam antes de serem repostas.
        private IEnumerator Cast(PowerCast c)
        {
            Banner = _texts.Name("power." + _session.State.Power(c.Side, c.Slot).Id) + (c.Crit ? " · " + _texts.T("crit.banner") : "");
            if (c.Cells.Length > 0) yield return Pulse(c.Cells);
            yield return Wait(turnSeconds * 0.8f);
        }

        // A carta entra em cena: o nome aparece (com o resultado da Aposta ou o crítico) e o tabuleiro é repintado.
        private IEnumerator ChangeTurn(TurnChanged t)
        {
            Turn = t.Side; Round = t.Round;
            _enemyTurn = t.Side == Side.Enemy;
            yield return Wait(turnSeconds + (_enemyTurn ? enemyThinkSeconds : 0f));
            if (!_enemyTurn && orbit != null) yield return orbit.GoHome(homeSeconds);   // cada turno seu começa olhando a frente
        }

        private IEnumerator End(BattleEnded e)
        {
            Winner = e.Winner; Ended = true;
            yield break;
        }

        public string MissingReference() => cube == null ? "cube" : input == null ? "input" : orbit == null ? "orbit" : null;
    }
}