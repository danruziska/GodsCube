using System;
using System.Collections.Generic;
using System.Linq;

namespace Cubo.Domain
{
    // Veneno e queimadura. Um herói com o item certo arma o efeito quando o lado dele TOCA um grupo de HazardMin peças
    // ou mais. Depois de entregar a face, o lado escolhe nela uma área de HazardArea x HazardArea casas, que brilha
    // (verde ou vermelha). Quem disparar um grupo (tocado ou da cascata, par ou maior) que passe por uma casa da área
    // leva o efeito no herói que o outro lado ataca, e a área some. Ninguém tocando, ela some depois de HazardRounds
    // rodadas. A face pode voltar para quem a pôs: a área machuca qualquer um dos dois lados.
    public sealed partial class BattleEngine
    {
        public static int DotIndex(string kind) => kind == EquipKinds.Burn ? 1 : 0;
        public static string DotKind(int index) => index == 1 ? EquipKinds.Burn : EquipKinds.Poison;

        private static void EnsureHazards(BattleState st)
        {
            if (st.HazardAt == null || st.HazardAt.Length != st.Colors.Length) st.HazardAt = new sbyte[st.Colors.Length];
        }

        // O item de veneno ou queimadura que `side` pode armar agora: o do primeiro herói vivo que leva um (ou null).
        // Por enquanto só o jogador tem itens.
        public static EquipDef HazardItem(BattleState st, Side side)
        {
            if (side != Side.Player || st.Equip == null) return null;
            for (int i = 0; i < st.Equip.Length && i < Heroes; i++)
                if (st.Equip[i] is EquipDef e && EquipKinds.IsHazard(e.Kind) && Alive(st, side, i)) return e;
            return null;
        }

        // O grupo tocado era grande o bastante: o item fica armado até a entrega.
        private void ArmHazard(BattleState st, Side side, int groupSize)
        {
            if (groupSize < R.HazardMin) return;
            var item = HazardItem(st, side);
            if (item != null) st.Pending[(int)side] = item;
        }

        // A área de lado HazardArea na frente com centro na casa `cell` (puxada para dentro da face se o centro estiver
        // perto da borda). Vazia se a casa não é da frente. Com área par, o centro fica no canto de cima-esquerda do meio.
        public int[] HazardArea(int n, int cell, int side = 0)
        {
            var g = Geometry.For(n);
            if (!OnFront(g, cell)) return new int[0];
            int size = Math.Max(1, Math.Min(side > 0 ? side : R.HazardArea, n));
            int r = cell - Faces.Front * g.NN, x = r / n, y = r % n;
            int half = (size - 1) / 2;
            int x0 = Math.Max(0, Math.Min(n - size, x - half)), y0 = Math.Max(0, Math.Min(n - size, y - (size - 1 - half)));
            var cells = new int[size * size];
            int k = 0;
            for (int i = 0; i < size; i++) for (int j = 0; j < size; j++) cells[k++] = FrontCell(g, x0 + i, y0 + j);
            Array.Sort(cells);
            return cells;
        }

        // Está esperando `side` pôr a área (logo depois da entrega dele)?
        public static bool AwaitingZone(BattleState st, Side side) =>
            st.Winner == null && st.Phase == Phase.Zone && st.Turn == side.Opp() && st.Pending[(int)side] != null;

        // `side` põe a área armada com centro na casa `cell` da frente (a face que acabou de entregar). Devolve as casas
        // da área, ou null se não dá. Uma área nova por cima de outra toma as casas dela. Depois disso o rival joga.
        public int[] PlaceHazard(BattleState st, Side side, int cell)
        {
            if (!AwaitingZone(st, side)) return null;
            var item = st.Pending[(int)side];
            var cells = HazardArea(st.N, cell, AreaOf(item));
            if (cells.Length == 0) return null;
            EnsureHazards(st);
            var hz = new Hazard
            {
                Id = NextHazardId(st), Kind = item.Kind, Dmg = item.N, Turns = Math.Max(1, item.Turns),
                Until = st.Round + R.HazardRounds, By = side
            };
            foreach (int c in cells) st.HazardAt[c] = hz.Id;
            st.Hazards.Add(hz);
            DropOrphans(st);
            st.Pending[(int)side] = null;
            st.Phase = Phase.Match;
            return cells;
        }

        private static sbyte NextHazardId(BattleState st)
        {
            for (sbyte id = 1; id < sbyte.MaxValue; id++)
                if (st.Hazards.All(h => h.Id != id)) return id;
            return 1;
        }

        // Uma área que perdeu todas as casas (outra foi posta por cima) sai da lista.
        private static void DropOrphans(BattleState st)
        {
            st.Hazards.RemoveAll(h => Array.IndexOf(st.HazardAt, h.Id) < 0);
        }

        private static void ClearHazard(BattleState st, sbyte id)
        {
            for (int c = 0; c < st.HazardAt.Length; c++) if (st.HazardAt[c] == id) st.HazardAt[c] = 0;
            st.Hazards.RemoveAll(h => h.Id == id);
        }

        // As áreas que o grupo toca (cada uma uma vez), na ordem em que nasceram.
        public static List<Hazard> HazardsTouched(BattleState st, int[] cells)
        {
            var outp = new List<Hazard>();
            if (st.HazardAt == null || st.Hazards.Count == 0) return outp;
            foreach (var h in st.Hazards)
                if (cells.Any(c => st.HazardAt[c] == h.Id)) outp.Add(h);
            return outp;
        }

        // `mover` disparou um grupo que passa por áreas: cada uma pega o herói de `mover` que o outro lado está atacando
        // (o alvo dele, ou o primeiro de pé) e some. Um herói tem no máximo UM efeito de dano por turno: um efeito
        // diferente toma o lugar do que estava; o mesmo efeito de novo renova os turnos e fica com o dano maior.
        private List<Affliction> Spring(BattleState st, Side mover, int[] cells)
        {
            var outp = new List<Affliction>();
            foreach (var h in HazardsTouched(st, cells))
            {
                ClearHazard(st, h.Id);
                int hero = EffectiveTarget(st, mover.Opp());
                if (hero < 0) continue;
                int k = DotIndex(h.Kind);
                var dots = st.Dots[(int)mover][hero];
                for (int other = 0; other < dots.Length; other++)
                    if (other != k && dots[other].Active) { dots[other].Turns = 0; dots[other].Dmg = 0; }   // o novo toma o lugar
                var dot = dots[k];
                if (!dot.Active) dot.Since = ++st.DotClock;          // um efeito novo vai para o fim da fila; renovar não muda o lugar
                dot.Turns = Math.Max(dot.Turns, h.Turns);
                dot.Dmg = Math.Max(dot.Dmg, h.Dmg);
                outp.Add(new Affliction { Side = mover, Hero = hero, Kind = h.Kind, Dmg = dot.Dmg, Turns = dot.Turns });
            }
            return outp;
        }

        // No começo do turno de `side`: cada herói de pé envenenado ou queimado perde o dano do efeito (pode cair).
        private List<Affliction> TickDots(BattleState st, Side side)
        {
            var outp = new List<Affliction>();
            int s = (int)side;
            for (int hero = 0; hero < Heroes; hero++)
                for (int k = 0; k < 2; k++)
                {
                    var dot = st.Dots[s][hero][k];
                    if (!dot.Active) continue;
                    if (!Alive(st, side, hero)) { dot.Turns = 0; continue; }
                    int dealt = Math.Min(dot.Dmg, st.HeroHp[s][hero]);
                    st.HeroHp[s][hero] -= dealt;
                    dot.Turns -= 1;
                    SyncHp(st, side);
                    if (st.HeroHp[s][hero] <= 0)
                    {
                        if (st.Target[(int)side.Opp()] == hero) st.Target[(int)side.Opp()] = -1;
                        foreach (var d in st.Dots[s][hero]) d.Turns = 0;
                    }
                    outp.Add(new Affliction { Side = side, Hero = hero, Kind = DotKind(k), Dmg = dealt, Turns = dot.Turns });
                }
            return outp;
        }

        // As áreas que passaram da hora somem (chamado quando a rodada muda).
        private static void ExpireHazards(BattleState st)
        {
            if (st.HazardAt == null) return;
            foreach (var h in st.Hazards.Where(h => st.Round >= h.Until).ToList()) ClearHazard(st, h.Id);
        }

        // O mapa das áreas para a tela: [casa] = 0 nada, 1 veneno, 2 queimadura.
        public static sbyte[] HazardMap(BattleState st)
        {
            var map = new sbyte[st.Colors.Length];
            if (st.HazardAt == null) return map;
            foreach (var h in st.Hazards)
            {
                sbyte k = (sbyte)(DotIndex(h.Kind) + 1);
                for (int c = 0; c < map.Length; c++) if (st.HazardAt[c] == h.Id) map[c] = k;
            }
            return map;
        }

        // Quanto um grupo nestas casas custaria a quem o dispara (para a IA desviar das áreas).
        public static int HazardCost(BattleState st, int[] cells) => HazardsTouched(st, cells).Sum(h => h.Dmg * h.Turns);

        // Onde `side` põe a área (a IA; o jogador escolhe com o dedo): o centro cuja área encosta no maior número de
        // grupos que o rival tem agora na frente (no empate, o mais perto do meio). -1 se não está esperando a área.
        public int ChooseZone(BattleState st, Side side)
        {
            if (!AwaitingZone(st, side)) return -1;
            var g = Geometry.For(st.N);
            var groups = GroupsIn(st.Colors, g);
            int best = -1; double bestScore = double.MinValue;
            double mid = (st.N - 1) / 2.0;
            for (int x = 0; x < st.N; x++)
                for (int y = 0; y < st.N; y++)
                {
                    int cell = FrontCell(g, x, y);
                    var area = new HashSet<int>(HazardArea(st.N, cell, AreaOf(st.Pending[(int)side])));
                    double score = groups.Count(grp => grp.Any(area.Contains)) - 0.01 * (Math.Abs(x - mid) + Math.Abs(y - mid));
                    if (score > bestScore) { bestScore = score; best = cell; }
                }
            return best;
        }
    }
}
