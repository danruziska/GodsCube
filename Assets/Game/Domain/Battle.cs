using System;
using System.Collections.Generic;
using System.Linq;

namespace Cubo.Domain
{
    // O turno: poderes, o grupo na frente (sem giro: só os grupos que já estão lá) e, por fim, a face que vai para o rival.
    public sealed partial class BattleEngine
    {
        public Side? CheckWinner(BattleState st, Side mover)
        {
            if (st.Winner != null) return st.Winner;
            bool pDead = st.Hp[0] <= 0, eDead = st.Hp[1] <= 0;
            if (pDead && eDead) st.Winner = mover;
            else if (eDead) st.Winner = Side.Player;
            else if (pDead) st.Winner = Side.Enemy;
            return st.Winner;
        }

        // É a vez de `side`, antes do grupo, com a batalha em andamento?
        public static bool InMatch(BattleState st, Side side) =>
            st.Winner == null && st.Turn == side && st.Phase == Phase.Match;

        // ---------------------------------------------------------------- o grupo

        // Dispara o grupo da peça tocada na frente: sorteio de crítico, dano, mana; as peças de cima caem e entram
        // novas no alto. Se o que caiu ou entrou forma grupos, eles disparam sozinhos, um por vez (a cascata, sem gastar a vez). Depois, com
        // mais um disparo no turno (poder ou carta), a garantia repõe um grupo na frente; senão a fase passa a ser
        // a da entrega.
        public FireResult Fire(BattleState st, Side side, int cell, Mulberry32 rng)
        {
            if (!InMatch(st, side)) return null;
            var g = Geometry.For(st.N);
            var cells = GroupOf(st.Colors, g, cell);
            if (cells == null) return null;

            var first = FireGroup(st, g, side, cells, 0, rng);
            bool armed = st.Pending[(int)side] == null;
            ArmHazard(st, side, cells.Length);
            armed = armed && st.Pending[(int)side] != null;
            var res = new FireResult
            {
                Side = side, Color = first.Color, Crit = first.Crit, Charge = first.Charge, Cells = first.Cells,
                Before = first.Before, Fills = first.Fills, Gain = first.Gain, Bonus = first.Bonus, Damage = first.Damage,
                Hp = first.Hp, HeroHp = first.HeroHp, Target = first.Target, Mana = first.Mana, Board = first.Board
            };
            res.Drop = first.Drop;
            res.Struck = first.Struck;
            res.Armed = armed;
            res.Chain = Cascade(st, g, side, first.Drop.Changed, rng);
            st.MovesLeft -= 1;
            CheckWinner(st, side);
            if (st.Winner == null)
            {
                if (st.MovesLeft > 0) res.Retouch = Assist(st, g, rng);
                else st.Phase = Phase.Hand;
            }
            return res;
        }

        // Trava de segurança da cascata: nunca chega perto disto num jogo normal; só evita travar o jogo se as
        // regras tiverem pouquíssimas cores (aí toda peça nova formaria grupo).
        public const int ChainSafetyStop = 100;

        // Um grupo dispara: sorteio de crítico, dano no rival, mana nos poderes da cor, as passivas de quem
        // dispara; as peças de cima caem e entram novas no alto (DropFront). Vale para o grupo tocado e para cada
        // elo da cascata. `link`: 0 é o grupo tocado; 1, 2... os elos (Avalanche e Rosário só valem para os elos).
        private ChainLink FireGroup(BattleState st, Geometry g, Side side, int[] cells, int link0, Mulberry32 rng)
        {
            var link = new ChainLink { Cells = cells, HpFrom = (int[])st.Hp.Clone(), HeroHpFrom = CloneHeroHp(st), ManaFrom = CloneMana(st) };
            bool crit = RollCrit(st, side, rng, cells.Length >= R.DamageMin);
            var t = GroupGain(st, st.Colors, cells, side, crit);
            link.Color = GroupColor(st.Colors, cells);
            link.Before = cells.Select(c => (int)st.Colors[c]).ToArray();
            var gn = t.Gain[(int)side];
            var fx = st.FxOf(side);
            if (link0 > 0)
            {
                // Avalanche: cada elo bate mais forte que o anterior (o 2º grupo x(1+v), o 3º x(1+2v)...)
                double chainDmg = FxSum(fx, "chainDmg");
                if (chainDmg > 0) gn.Value = JsMath.RoundInt(gn.Value * (1 + chainDmg * link0));
            }
            int me = (int)side;
            int dealt = Hit(st, side, gn.Value * R.DamagePerPiece, out int target);   // no único alvo de `side` neste turno
            link.Struck = Spring(st, side, cells);                                  // passou por uma área: veneno ou queimadura
            for (int s = 0; s < Slots; s++) st.Mana[me][s] = Math.Min(R.ManaCap, st.Mana[me][s] + gn.Mana[s]);
            if (side == Side.Player && st.Fx != null)
            {
                int echo = (int)FxSum(st, "echo");
                if (echo != 0)
                    for (int q = 0; q < Slots; q++) st.Mana[0][q] = Math.Min(R.ManaCap, st.Mana[0][q] + echo);
            }
            if (link0 > 0)
            {
                // Rosário: cada elo da cascata dá mana em todos os poderes
                int chainMana = (int)FxSum(fx, "chainMana");
                if (chainMana != 0)
                    for (int q = 0; q < Slots; q++) st.Mana[me][q] = Math.Min(R.ManaCap, st.Mana[me][q] + chainMana);
            }
            if (Has(st, side, Passives.Communion))
            {
                int slot = 0;                                  // o poder mais vazio do time (no empate, o primeiro)
                for (int q = 1; q < Slots; q++) if (st.Mana[me][q] < st.Mana[me][slot]) slot = q;
                st.Mana[me][slot] = Math.Min(R.ManaCap, st.Mana[me][slot] + R.CommunionMana);
            }

            link.Drop = DropFront(st, g, side, cells, rng);
            link.Fills = link.Drop.Fills;
            link.Crit = crit; link.Charge = t.Charge; link.Gain = t; link.Bonus = t.Bonus;
            link.Damage = dealt;
            link.Target = target;
            link.Hp = (int[])st.Hp.Clone(); link.HeroHp = CloneHeroHp(st); link.Mana = CloneMana(st);
            link.Board = (sbyte[])st.Colors.Clone();
            return link;
        }

        // A cascata: enquanto houver na frente uma mancha de CascadeMin ou mais peças que passe por uma casa que mudou
        // desde o toque (peça que caiu ou peça nova), ela dispara por `side`, e o que muda depois dela também conta.
        // As manchas menores ficam no tabuleiro como opções para o próximo toque. Para quando o rival cai.
        private List<ChainLink> Cascade(BattleState st, Geometry g, Side side, IEnumerable<int> refilled, Mulberry32 rng)
        {
            var links = new List<ChainLink>();
            var fresh = new HashSet<int>(refilled);
            int victim = (int)side.Opp();
            while (links.Count < ChainSafetyStop && st.Hp[victim] > 0)
            {
                int[] next = null;
                foreach (var grp in GroupsIn(st.Colors, g, Math.Max(R.MinGroup, R.CascadeMin)))
                    if (grp.Any(fresh.Contains)) { next = grp; break; }
                if (next == null) break;
                links.Add(FireGroup(st, g, side, next, links.Count + 1, rng));
                fresh.UnionWith(links[links.Count - 1].Drop.Changed);
            }
            return links;
        }

        // Encerra os grupos do turno sem disparar (rede de segurança da IA: a garantia sempre deixa um grupo).
        public void EndGroups(BattleState st, Side side)
        {
            if (!InMatch(st, side)) return;
            st.MovesLeft = 0;
            st.Phase = Phase.Hand;
        }

        // ---------------------------------------------------------------- a entrega

        // O id da face que `side` não pode entregar: a que o rival acabou de jogar. Null se não há proibida
        // (começo da batalha ou a passiva Trapaça).
        public int? BanFor(BattleState st, Side side)
        {
            if (Has(st, side, Passives.Trick)) return null;
            return st.LastFace[(int)side.Opp()];
        }

        // As faces vizinhas (físicas) que `side` pode entregar agora.
        public List<int> HandOptions(BattleState st, Side side)
        {
            var list = new List<int>();
            int? ban = BanFor(st, side);
            foreach (int f in Faces.Neighbors) if (st.FaceId[f] != ban) list.Add(f);
            return list;
        }

        public bool CanHand(BattleState st, Side side, int face) =>
            st.Winner == null && st.Turn == side && st.Phase == Phase.Hand && HandOptions(st, side).Contains(face);

        // Gira o cubo para que `face` vá para a frente do rival e passa a vez. Os efeitos que contam turnos
        // deste lado andam um turno; as travas que valiam para este turno caem. Quem vai jogar recebe a garantia.
        public HandResult HandOver(BattleState st, Side side, int face, Mulberry32 rng)
        {
            if (!CanHand(st, side, face)) return null;
            int played = st.FaceId[Faces.Front];
            var m = TurnToFront(st.N, face);
            TurnWhole(st, m);
            st.LastFace[(int)side] = played;
            bool zone = st.Pending[(int)side] != null && st.Winner == null;
            TickFx(st, side);
            st.Silenced[(int)side] = false;
            st.Turn = side.Opp();
            if (st.Turn == Side.Player) st.Target[0] = -1;   // o jogador escolhe de novo; o rival já anunciou o dele
            st.Phase = zone ? Phase.Zone : Phase.Match;
            st.MovesLeft = 1;
            if (st.Turn == Side.Player) { st.Round += 1; ExpireHazards(st); }
            var res = new HandResult { Side = side, Face = face, Move = m, Played = played, Received = st.FaceId[Faces.Front], Zone = zone };
            res.Ticks = TickDots(st, st.Turn);                      // veneno e queimadura de quem vai jogar
            CheckWinner(st, side);
            if (st.Winner != null) { st.Pending[(int)side] = null; return res; }
            res.Retouch = Assist(st, Geometry.For(st.N), rng);
            return res;
        }
    }
}