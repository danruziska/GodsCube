using System;
using System.Collections.Generic;
using System.Linq;
using Cubo.Domain;

namespace Cubo.App
{
    // A batalha vista de fora: a tela pede (TryTarget, TryFire, TryHand, TryCast) e recebe eventos. É a
    // única classe que chama as funções do domínio que mudam o estado. Não conhece Unity.
    public sealed class BattleSession
    {
        private readonly BattleEngine _engine;
        private readonly Mulberry32 _rng;
        private readonly string _aiLevel;

        public BattleState State { get; }
        public BattleEngine Engine => _engine;        // só para perguntas (custos, textos); quem muda o estado é a sessão
        public bool Busy { get; private set; }        // verdadeiro entre um comando aceito e PresentationDone()
        public List<Command> Commands { get; } = new List<Command>();   // o que o jogador fez, para refazer a batalha (salvamento)

        // Cada comando aceito publica um lote de eventos. Quem escuta toca o lote e chama PresentationDone().
        public event Action<IReadOnlyList<IBattleEvent>> Played;

        public BattleSession(BattleEngine engine, BattleState state, Mulberry32 rng, string aiLevel)
        {
            _engine = engine; State = state; _rng = rng; _aiLevel = aiLevel;
            AnnounceEnemyTarget();                 // o rival já começa a batalha mostrando quem vai atacar
        }

        // O rival trava o único herói do jogador que vai atacar no próximo turno dele (a marca de aviso). Sem sorteio.
        private TargetChanged AnnounceEnemyTarget()
        {
            if (State.Winner != null) return null;
            int h = _engine.IntentTarget(State, Side.Enemy, _aiLevel);
            if (h < 0 || !_engine.SetTarget(State, Side.Enemy, h)) return null;
            return new TargetChanged(Side.Enemy, h);
        }

        public bool IsPlayerTurn => !Busy && State.Winner == null && State.Turn == Side.Player;
        public bool IsMatching => IsPlayerTurn && State.Phase == Phase.Match;    // antes do grupo
        public bool IsHanding => IsPlayerTurn && State.Phase == Phase.Hand;      // escolhendo a face do rival
        // Logo depois da entrega, com veneno ou fogo armado: o jogador escolhe a área na face que entregou.
        public bool IsZoning => !Busy && BattleEngine.AwaitingZone(State, Side.Player);
        public string ZoneKind => IsZoning ? State.Pending[0].Kind : null;

        // ---- perguntas (não mudam nada) ----

        // 3 contra 3: em cada turno o jogador escolhe um rival antes do grupo (e antes dos poderes que causam dano).
        public bool HasTarget => BattleEngine.HasTarget(State, Side.Player);
        public bool NeedsTarget => IsMatching && !HasTarget;
        public bool CanTarget(int hero) => IsMatching && BattleEngine.Alive(State, Side.Enemy, hero);

        public bool CanCast(int slot) => IsPlayerTurn && slot >= 0 && slot < BattleEngine.Slots && _engine.CanCast(State, Side.Player, slot)
                                         && (HasTarget || !Hits(State.Power(Side.Player, slot).Kind));

        private static bool Hits(string kind) => kind == "damage" || kind == "purge";

        // O grupo que um toque nesta peça dispararia (para destacar enquanto o dedo está em cima), ou null.
        public int[] GroupPreview(int cell) => IsMatching ? _engine.GroupAt(State, cell) : null;

        // A área que um dedo nesta casa escolheria (o dedo é o centro), ou null fora da escolha da área.
        public int[] ZonePreview(int cell) => IsZoning ? _engine.HazardArea(State.N, cell, _engine.AreaOf(State.Pending[0])) : null;

        // As faces (físicas) que o jogador pode entregar agora; vazia fora da fase de entrega.
        public List<int> HandFaces => IsHanding ? _engine.HandOptions(State, Side.Player) : new List<int>();
        public bool CanHand(int face) => IsHanding && _engine.CanHand(State, Side.Player, face);

        // A face física proibida para o jogador (a que o rival acabou de jogar), ou -1.
        public int BannedFace
        {
            get
            {
                int? id = _engine.BanFor(State, Side.Player);
                return id == null ? -1 : BattleEngine.FaceOf(State, id.Value);
            }
        }

        // ---- comandos do jogador ----

        // Escolhe o rival que o jogador ataca neste turno (dá para trocar até disparar).
        public bool TryTarget(int hero)
        {
            if (!CanTarget(hero) || !_engine.SetTarget(State, Side.Player, hero)) return false;
            Commands.Add(new Command('T', hero));
            Publish(new List<IBattleEvent> { new TargetChanged(Side.Player, hero) });
            return true;
        }

        // Dispara o grupo da peça tocada (precisa de um rival escolhido). Se era o último grupo do turno, a fase passa
        // a ser a da entrega.
        public bool TryFire(int cell)
        {
            if (!IsMatching || !HasTarget) return false;
            var ev = new List<IBattleEvent>();
            if (!FireAndReport(Side.Player, cell, ev)) return false;
            Commands.Add(new Command('F', cell));
            Publish(ev);
            return true;
        }

        // Entrega a face física `face` (0 a 3): o cubo gira e o turno inteiro do rival já vem no mesmo lote.
        public bool TryHand(int face)
        {
            if (!CanHand(face)) return false;
            var ev = new List<IBattleEvent>();
            HandAndReport(Side.Player, face, ev);
            Commands.Add(new Command('H', face));
            if (!BattleEngine.AwaitingZone(State, Side.Player)) PlayEnemyTurns(ev);   // com área armada, o rival espera
            Publish(ev);
            return true;
        }

        // Põe o veneno (ou o fogo) armado com o centro na casa `cell` da face que acabou de ir para o rival; aí ele joga.
        public bool TryZone(int cell)
        {
            if (!IsZoning) return false;
            var ev = new List<IBattleEvent>();
            if (_engine.PlaceHazard(State, Side.Player, cell) == null) return false;
            ev.Add(new HazardsChanged(BattleEngine.HazardMap(State)));
            Commands.Add(new Command('Z', cell));
            PlayEnemyTurns(ev);
            Publish(ev);
            return true;
        }

        // Usa o poder `slot` (0 a 5: dois por herói). Não gasta a vez.
        public bool TryCast(int slot)
        {
            if (!CanCast(slot)) return false;
            var ev = new List<IBattleEvent>();
            CastAndReport(Side.Player, slot, ev);
            Commands.Add(new Command('C', slot));
            Publish(ev);
            return true;
        }


        public void PresentationDone() => Busy = false;

        // ---- por dentro ----

        private sbyte[] Board() => (sbyte[])State.Colors.Clone();

        private void Publish(List<IBattleEvent> ev)
        {
            Busy = true;
            Played?.Invoke(ev);
        }

        // Vida e mana antes de um comando, para contar só o que mudou.
        private sealed class Snap
        {
            public int[] Hp; public int[][] HeroHp, Mana;
            public static Snap Take(BattleState st) => new Snap
            {
                Hp = (int[])st.Hp.Clone(),
                HeroHp = BattleEngine.CloneHeroHp(st),
                Mana = new[] { (int[])st.Mana[0].Clone(), (int[])st.Mana[1].Clone() }
            };
            public static Snap Of(int[] hp, int[][] heroHp, int[][] mana) => new Snap { Hp = hp, HeroHp = heroHp, Mana = mana };
        }

        private void Diff(Snap before, Snap after, List<IBattleEvent> ev)
        {
            foreach (var sd in new[] { Side.Player, Side.Enemy })
            {
                int i = (int)sd;
                if (after.Hp[i] != before.Hp[i]) ev.Add(new HpChanged(sd, before.Hp[i], after.Hp[i]));
                for (int h = 0; h < BattleEngine.Heroes; h++)
                    if (after.HeroHp[i][h] != before.HeroHp[i][h])
                        ev.Add(new HeroHpChanged(sd, h, before.HeroHp[i][h], after.HeroHp[i][h], State.HeroMax[i][h]));
                if (!SameArray(after.Mana[i], before.Mana[i])) ev.Add(new ManaChanged(sd, (int[])after.Mana[i].Clone()));
            }
        }

        // Depois de um grupo (ou de um poder que varre peças): a vida e a mana até ali, o que caiu e entrou e, se
        // elas formaram grupos, cada elo da cascata na ordem (dispara, vida e mana, peças novas). Cada
        // CellsRefilled leva o tabuleiro daquele momento; a garantia só entra no último.
        private void ReportRefillAndChain(Side side, Snap snap, Drop drop, sbyte[] board, List<ChainLink> chain,
                                          List<Edit> retouch, List<IBattleEvent> ev)
        {
            var none = new List<Edit>();
            bool chained = chain != null && chain.Count > 0;
            Diff(snap, chained ? Snap.Of(chain[0].HpFrom, chain[0].HeroHpFrom, chain[0].ManaFrom) : Snap.Take(State), ev);
            ev.Add(new CellsRefilled(drop.New, chained ? none : retouch, chained ? board : Board(), drop.Falls));
            if (!chained) return;
            int opp = (int)side.Opp();
            for (int k = 0; k < chain.Count; k++)
            {
                var l = chain[k];
                bool last = k == chain.Count - 1;
                ev.Add(new GroupFired(side, l.Cells, l.Color, l.Crit, l.HpFrom[opp] - l.Hp[opp], k + 1));
                Diff(Snap.Of(l.HpFrom, l.HeroHpFrom, l.ManaFrom), last ? Snap.Take(State) : Snap.Of(l.Hp, l.HeroHp, l.Mana), ev);
                ev.Add(new CellsRefilled(l.Drop.New, last ? retouch : none, last ? Board() : l.Board, l.Drop.Falls));
            }
        }

        private void CastAndReport(Side side, int slot, List<IBattleEvent> ev)
        {
            var snap = Snap.Take(State);
            var r = _engine.Cast(State, side, slot, _rng);
            if (r == null) return;
            ev.Add(new PowerCast(side, slot, r.Kind, r.Amount, r.Cells.ToArray(), r.Crit));
            NoteCombo(side, r.Chain);
            if (r.Kind == "purge") ReportRefillAndChain(side, snap, r.Drop, r.Board, r.Chain, r.Retouch, ev);
            else Diff(snap, Snap.Take(State), ev);
            if (State.Winner != null) ev.Add(new BattleEnded(State.Winner.Value));
        }

        // O rival: antes de cada grupo usa os poderes que servem, escolhe o grupo com o mesmo gerador do jogo
        // e dispara; com mais de um grupo no turno (poder "extra"), repete. No fim entrega a face que deixa o
        // jogador com o pior grupo.
        private void PlayEnemyTurns(List<IBattleEvent> ev)
        {
            for (int guard = 0; guard < 40 && State.Winner == null && State.Turn == Side.Enemy; guard++)
            {
                if (State.Phase == Phase.Hand)
                {
                    int face = _engine.ChooseHand(State, Side.Enemy, _aiLevel, _rng);
                    HandAndReport(Side.Enemy, face, ev);
                    continue;
                }
                if (!BattleEngine.HasTarget(State, Side.Enemy))     // só se o anunciado caiu no meio do turno
                {
                    int target = _engine.ChooseTarget(State, Side.Enemy, _aiLevel, _rng);
                    if (target >= 0 && _engine.SetTarget(State, Side.Enemy, target)) ev.Add(new TargetChanged(Side.Enemy, target));
                }
                foreach (int slot in _engine.EnemyCasts(State))
                {
                    if (State.Winner != null) break;
                    if (_engine.CanCast(State, Side.Enemy, slot)) CastAndReport(Side.Enemy, slot, ev);
                }
                if (State.Winner != null) break;

                var choice = _engine.ChooseMove(State, Side.Enemy, _aiLevel, _rng);
                bool fired = false;
                if (choice != null) fired = FireAndReport(Side.Enemy, choice.Cell, ev);
                if (!fired) _engine.EndGroups(State, Side.Enemy);   // nunca deveria acontecer (a garantia sempre deixa um grupo)
            }
        }

        // O maior combo do jogador nesta batalha: quantos grupos dispararam numa cascata (o tocado conta; depois de
        // um poder que varre peças, o primeiro elo já é x2). 0 = nenhuma cascata ainda. Vai para a coleção no fim.
        public int BestCombo { get; private set; }

        private void NoteCombo(Side side, List<ChainLink> chain)
        {
            if (side == Side.Player && chain != null && chain.Count > 0) BestCombo = Math.Max(BestCombo, chain.Count + 1);
        }

        private bool FireAndReport(Side side, int cell, List<IBattleEvent> ev)
        {
            var snap = Snap.Take(State);
            var r = _engine.Fire(State, side, cell, _rng);
            if (r == null) return false;
            NoteCombo(side, r.Chain);

            int opp = (int)side.Opp();
            ev.Add(new GroupFired(side, r.Cells, r.Color, r.Crit, snap.Hp[opp] - r.Hp[opp]));
            ReportRefillAndChain(side, snap, r.Drop, r.Board, r.Chain, r.Retouch, ev);
            var struck = r.Struck.Concat(r.Chain.SelectMany(l => l.Struck)).ToList();
            foreach (var a in struck) ev.Add(new Afflicted(a.Side, a.Hero, a.Kind, a.Dmg, a.Turns));
            if (struck.Count > 0) ev.Add(new HazardsChanged(BattleEngine.HazardMap(State)));
            if (r.Armed && State.Pending[(int)side] is EquipDef item) ev.Add(new HazardArmed(side, item.Kind));
            if (State.Winner != null) ev.Add(new BattleEnded(State.Winner.Value));
            else if (State.Phase == Phase.Hand) ev.Add(new HandPhase(side));
            return true;
        }

        // A entrega: o cubo inteiro gira (a tela anima com o tabuleiro de antes da garantia), depois a garantia de
        // quem vai jogar, depois a troca de turno.
        private void HandAndReport(Side side, int face, List<IBattleEvent> ev)
        {
            var snap = Snap.Take(State);
            var r = _engine.HandOver(State, side, face, _rng);
            if (r == null) return;
            var turned = Board();
            foreach (var e in r.Retouch) turned[e.Cell] = (sbyte)e.From;
            ev.Add(new CubeTurned(side, r.Move, r.Face, turned));
            ev.Add(new HazardsChanged(BattleEngine.HazardMap(State)));      // as áreas giraram junto (e as velhas somem)
            if (r.Retouch.Count > 0) ev.Add(new CellsRefilled(new int[0], r.Retouch, Board()));
            ev.Add(new TurnChanged(State.Turn, State.Round));
            foreach (var t in r.Ticks) ev.Add(new DotTicked(t.Side, t.Hero, t.Kind, t.Dmg, t.Turns));
            if (r.Ticks.Count > 0) Diff(snap, Snap.Take(State), ev);
            if (State.Winner != null) { ev.Add(new BattleEnded(State.Winner.Value)); return; }
            if (r.Zone && State.Pending[(int)side] is EquipDef item) ev.Add(new ZonePhase(side, item.Kind));
            if (side == Side.Enemy && AnnounceEnemyTarget() is TargetChanged next) ev.Add(next);   // o aviso do próximo ataque
        }

        private static bool SameArray(int[] a, int[] b)
        {
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}