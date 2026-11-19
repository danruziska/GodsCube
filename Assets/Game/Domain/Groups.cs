using System;
using System.Collections.Generic;

namespace Cubo.Domain
{
    // Grupos: manchas de MinGroup ou mais peças da mesma cor que se tocam (em cima, embaixo e dos lados; diagonal
    // não conta), de qualquer forma. Só a frente conta: as outras faces não formam grupo.
    public sealed partial class BattleEngine
    {
        // A peça da frente na coluna x (0 = esquerda) e na altura y (0 = embaixo).
        public static int FrontCell(Geometry g, int x, int y) => Faces.Front * g.NN + x * g.N + y;

        public static bool OnFront(Geometry g, int cell) => cell >= 0 && cell < g.Total && g.CellFace[cell] == Faces.Front;

        // A mancha que contém a peça: ela e as vizinhas da mesma cor, e as vizinhas delas, e assim por diante. Ordenada.
        private static int[] Flood(sbyte[] colors, Geometry g, bool[] seen, int c0)
        {
            int n = g.N;
            var cells = new List<int>();
            var stack = new Stack<int>(); stack.Push(c0); seen[c0] = true;
            while (stack.Count > 0)
            {
                int c = stack.Pop(); cells.Add(c);
                int r = c - Faces.Front * g.NN, cx = r / n, cy = r % n;
                for (int k = 0; k < 4; k++)
                {
                    int nx = cx + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = cy + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                    int d = FrontCell(g, nx, ny);
                    if (seen[d] || colors[d] != colors[c]) continue;
                    seen[d] = true; stack.Push(d);
                }
            }
            cells.Sort();
            return cells.ToArray();
        }

        // Todas as manchas da frente com pelo menos `min` peças, na ordem da primeira peça de cada uma.
        public List<int[]> GroupsIn(sbyte[] colors, Geometry g, int min)
        {
            var seen = new bool[g.Total];
            var groups = new List<int[]>();
            for (int i = Faces.Front * g.NN; i < (Faces.Front + 1) * g.NN; i++)
            {
                if (seen[i] || colors[i] < 0) continue;
                var cells = Flood(colors, g, seen, i);
                if (cells.Length >= min) groups.Add(cells);
            }
            groups.Sort((a, b) => a[0].CompareTo(b[0]));
            return groups;
        }

        // Os grupos que se podem tocar (MinGroup ou mais).
        public List<int[]> GroupsIn(sbyte[] colors, Geometry g) => GroupsIn(colors, g, R.MinGroup);

        // O grupo da peça, ou null (peça fora da frente, ou numa mancha menor que MinGroup).
        public int[] GroupOf(sbyte[] colors, Geometry g, int cell)
        {
            if (!OnFront(g, cell) || colors[cell] < 0) return null;
            var cells = Flood(colors, g, new bool[g.Total], cell);
            return cells.Length >= R.MinGroup ? cells : null;
        }

        public int GroupColor(sbyte[] colors, int[] cells) => colors[cells[0]];

        public int[] GroupAt(BattleState st, int cell) => GroupOf(st.Colors, Geometry.For(st.N), cell);
        public List<int[]> FindGroups(BattleState st) => GroupsIn(st.Colors, Geometry.For(st.N));
    }
}