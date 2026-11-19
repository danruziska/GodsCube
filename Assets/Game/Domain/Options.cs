using System;
using System.Collections.Generic;
using System.Linq;

namespace Cubo.Domain
{
    // O que disparar um grupo da frente rende.
    public sealed class MoveEval
    {
        public int Cell = -1, Size;
        public double Mine, Score;
    }

    public sealed class BestGroupResult
    {
        public int[] Cells;
        public GroupGainResult Gain;
        public int Value;
        public double Score;
        public int Bonus;
    }

    public sealed partial class BattleEngine
    {
        public sbyte[] ApplyMove(sbyte[] colors, Geometry g, Move m) => Apply(g, colors, m);

        private double ManaWorth(BattleState st, Side side, int slot) => st.Mana[(int)side][slot] >= R.ManaCap ? 0 : 1;

        private double ScoreGain(BattleState st, Side side, SideGain gain)
        {
            double score = gain.Value * R.DamagePerPiece;
            for (int s = 0; s < Slots; s++) score += gain.Mana[s] * ManaWorth(st, side, s);
            return score;
        }

        // O melhor grupo de `side` na frente desse tabuleiro, ou null.
        public BestGroupResult BestGroup(sbyte[] colors, Geometry g, BattleState st, Side side)
        {
            BestGroupResult best = null;
            foreach (var cells in GroupsIn(colors, g))
            {
                var t = GroupGain(st, colors, cells, side);
                double sc = ScoreGain(st, side, t.Gain[(int)side]);
                if (best == null || sc > best.Score)
                    best = new BestGroupResult { Cells = cells, Gain = t, Value = t.Gain[(int)side].Value, Score = sc, Bonus = t.Bonus };
            }
            return best;
        }

        // Cada grupo que `side` tem agora na frente, com o que rende. Sem giro: o tabuleiro é o que está aí.
        public List<MoveEval> EvaluateMoves(BattleState st, Geometry g, Side side)
        {
            var outp = new List<MoveEval>();
            foreach (var cells in GroupsIn(st.Colors, g))
            {
                var t = GroupGain(st, st.Colors, cells, side);
                outp.Add(new MoveEval
                {
                    Cell = cells[0],
                    Size = cells.Length,
                    Mine = t.Gain[(int)side].Value,
                    Score = ScoreGain(st, side, t.Gain[(int)side]) - HazardCost(st, cells)   // desvia do veneno e do fogo
                });
            }
            return outp;
        }

        // Garantia: quem vai jogar sempre tem um grupo na frente. Se não há nenhum, o jogo
        // troca a cor de uma peça da frente para formar um (sorteando entre as trocas possíveis); se nem isso
        // dá, pinta três em fila na linha de baixo. Devolve as trocas.
        public List<Edit> Assist(BattleState st, Geometry g, Mulberry32 rng)
        {
            if (st.Winner != null || GroupsIn(st.Colors, g).Count > 0) return new List<Edit>();
            var cand = new List<Edit>();
            for (int i = Faces.Front * g.NN; i < (Faces.Front + 1) * g.NN; i++)
            {
                sbyte old = st.Colors[i];
                for (int c = 0; c < st.NumColors; c++)
                {
                    if (c == old) continue;
                    st.Colors[i] = (sbyte)c;
                    if (GroupOf(st.Colors, g, i) != null) cand.Add(new Edit { Cell = i, From = old, To = c });
                }
                st.Colors[i] = old;
            }
            if (cand.Count == 0)
            {
                int to = rng.NextInt(st.NumColors);
                var edits = new List<Edit>();
                for (int x = 0; x < 3; x++)
                {
                    int cell = FrontCell(g, x, 0);
                    if (st.Colors[cell] == to) continue;
                    edits.Add(new Edit { Cell = cell, From = st.Colors[cell], To = to });
                    st.Colors[cell] = (sbyte)to;
                }
                return edits;
            }
            var pick = cand[rng.NextInt(cand.Count)];
            st.Colors[pick.Cell] = (sbyte)pick.To;
            return new List<Edit> { pick };
        }

        // Peças novas: sorteio uniforme; com a chance de combo, parte delas copia a cor de uma vizinha já no lugar.
        // `side`: quem fez as peças entrarem; vale a chance de combo dele.
        // Com "focus" ativo (a carta Ordem), as novas só vêm nas K cores mais comuns da frente agora (sem contar as
        // casas que estão sendo repostas); a cópia de vizinha também só copia essas cores.
        public List<int> RefillCells(BattleState st, Geometry g, IList<int> cells, Mulberry32 rng, Side side)
        {
            double p = ComboChance(st, side);
            var open = new HashSet<int>(cells);
            var palette = FocusPalette(st, g, side, open);
            var outp = new List<int>();
            foreach (int cell in cells)
            {
                int col = -1;
                if (p > 0 && rng.Next() < p)
                {
                    var pool = new List<int>();
                    foreach (int nb in g.Neighbors[cell])
                        if (!open.Contains(nb) && st.Colors[nb] >= 0 && (palette == null || palette.Contains(st.Colors[nb])))
                            pool.Add(st.Colors[nb]);
                    if (pool.Count > 0) col = pool[rng.NextInt(pool.Count)];
                }
                if (col < 0) col = palette != null ? palette[rng.NextInt(palette.Count)] : rng.NextInt(st.NumColors);
                st.Colors[cell] = (sbyte)col; open.Remove(cell); outp.Add(col);
            }
            return outp;
        }

        // As cores permitidas para peças novas de `side` com "focus" ativo (a menor contagem, se houver várias), ou
        // null sem focus. São as K cores mais comuns da frente (no empate, a de número menor), sem contar `skip`.
        public List<int> FocusPalette(BattleState st, Geometry g, Side side, ICollection<int> skip)
        {
            var list = st.FxOf(side)?.List("focus");
            if (list == null || list.Count == 0) return null;
            int k = int.MaxValue;
            foreach (var e in list) k = Math.Min(k, (int)e.V);
            k = Math.Max(1, Math.Min(st.NumColors, k));
            var count = new int[st.NumColors];
            for (int i = Faces.Front * g.NN; i < (Faces.Front + 1) * g.NN; i++)
                if (!skip.Contains(i) && st.Colors[i] >= 0 && st.Colors[i] < st.NumColors) count[st.Colors[i]]++;
            return Enumerable.Range(0, st.NumColors).OrderByDescending(c => count[c]).ThenBy(c => c).Take(k).ToList();
        }

        // A gravidade na frente: em cada coluna que perdeu peças, as de cima caem até encostar, e as novas
        // entram no alto (sorteadas como em RefillCells, de baixo para cima). "Embaixo" é a linha y = 0, a de
        // baixo na tela. Peças fora da frente (não deveria haver) só são trocadas no lugar.
        public Drop DropFront(BattleState st, Geometry g, Side side, IList<int> cells, Mulberry32 rng)
        {
            var gone = new HashSet<int>(cells);
            var drop = new Drop();
            var fresh = new List<int>();
            foreach (int c in cells) if (!OnFront(g, c)) fresh.Add(c);
            for (int x = 0; x < g.N; x++)
            {
                bool hit = false;
                for (int y = 0; y < g.N && !hit; y++) hit = gone.Contains(FrontCell(g, x, y));
                if (!hit) continue;
                int write = 0;
                for (int y = 0; y < g.N; y++)
                {
                    int c = FrontCell(g, x, y);
                    if (gone.Contains(c)) continue;
                    if (y != write)
                    {
                        int to = FrontCell(g, x, write);
                        st.Colors[to] = st.Colors[c];
                        drop.Falls.Add(new Fall { From = c, To = to });
                    }
                    write++;
                }
                for (int y = write; y < g.N; y++) fresh.Add(FrontCell(g, x, y));
            }
            drop.New = fresh.ToArray();
            drop.Fills = RefillCells(st, g, fresh, rng, side);
            return drop;
        }

        public sbyte[] RandomBoard(Geometry g, int numColors, Mulberry32 rng)
        {
            var colors = new sbyte[g.Total];
            for (int i = 0; i < g.Total; i++) colors[i] = (sbyte)rng.NextInt(numColors);
            return colors;
        }
    }
}