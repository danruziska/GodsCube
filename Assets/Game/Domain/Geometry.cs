using System.Collections.Generic;

namespace Cubo.Domain
{
    public sealed class Geometry
    {
        public static readonly int[] FaceAxis = { 0, 0, 1, 1, 2, 2 };
        public static readonly int[] FaceSign = { 1, -1, 1, -1, 1, -1 };
        private static readonly int[][] Other = { new[] { 1, 2 }, new[] { 0, 2 }, new[] { 0, 1 } };

        public readonly int N, NN, Total;
        public readonly int[] CellFace;      // [peça] = face
        public readonly int[][] CellPos;     // [peça] = {x, y, z}, cada um de 0 a n-1
        public readonly int[][][] Perms;     // [eixo][fatia][antigo] = novo, giro de +90°
        public readonly int[][][] InvPerms;
        public readonly int[][][] Rings;         // [eixo][fatia] = as 4n peças da fatia, em volta do cubo
        public readonly int[][] Neighbors;       // [peça] = vizinhas, na ordem do protótipo
        public readonly bool[][] NeighborCross;  // [peça][k] = true se a vizinha k fica em outra face
        private static readonly Dictionary<int, Geometry> Cache = new Dictionary<int, Geometry>();

        // Uma geometria por tamanho, criada na primeira vez e reaproveitada.
        public static Geometry For(int n)
        {
            lock (Cache)
            {
                if (!Cache.TryGetValue(n, out var g)) Cache[n] = g = new Geometry(n);
                return g;
            }
        }

        public static int FaceIndex(int axis, int sign) => axis * 2 + (sign > 0 ? 0 : 1);

        public int Index(int f, int[] p)
        {
            var o = Other[FaceAxis[f]];
            return f * NN + p[o[0]] * N + p[o[1]];
        }

        public Geometry(int n)
        {
            N = n; NN = n * n; Total = 6 * NN;
            CellFace = new int[Total]; CellPos = new int[Total][];
            for (int i = 0; i < Total; i++)
            {
                int f = i / NN, r = i - f * NN, u = r / n, v = r - u * n;
                int a = FaceAxis[f]; var o = Other[a];
                var p = new int[3];
                p[a] = FaceSign[f] > 0 ? n - 1 : 0;
                p[o[0]] = u; p[o[1]] = v;
                CellFace[i] = f; CellPos[i] = p;
            }

            Perms = new int[3][][]; InvPerms = new int[3][][];
            for (int A = 0; A < 3; A++)
            {
                int B = (A + 1) % 3, C = (A + 2) % 3;
                Perms[A] = new int[n][]; InvPerms[A] = new int[n][];
                for (int s = 0; s < n; s++)
                {
                    var perm = new int[Total]; var inv = new int[Total];
                    for (int k = 0; k < Total; k++)
                    {
                        var p = CellPos[k];
                        if (p[A] != s) { perm[k] = k; continue; }
                        var q = (int[])p.Clone();
                        q[B] = n - 1 - p[C];
                        q[C] = p[B];
                        int f = CellFace[k], fa = FaceAxis[f], sg = FaceSign[f], nf;
                        if (fa == A) nf = f;
                        else if (fa == B) nf = FaceIndex(C, sg);
                        else nf = FaceIndex(B, -sg);
                        perm[k] = Index(nf, q);
                    }
                    for (int m = 0; m < Total; m++) inv[perm[m]] = m;
                    Perms[A][s] = perm; InvPerms[A][s] = inv;
                }
            }
            Rings = new int[3][][];
            var ringsOf = new List<int[]>[Total];     // por peça: {eixo, fatia, posição no anel}
            for (int t = 0; t < Total; t++) ringsOf[t] = new List<int[]>();
            for (int A = 0; A < 3; A++)
            {
                int B = (A + 1) % 3, C = (A + 2) % 3;
                Rings[A] = new int[n][];
                for (int s = 0; s < n; s++)
                {
                    var ring = new int[4 * n]; int kk = 0;
                    var p = new int[3]; p[A] = s;
                    for (int j = 0; j < n; j++)      { p[B] = n - 1; p[C] = j;     ring[kk++] = Index(FaceIndex(B, 1), p); }
                    for (int j = n - 1; j >= 0; j--) { p[B] = j;     p[C] = n - 1; ring[kk++] = Index(FaceIndex(C, 1), p); }
                    for (int j = n - 1; j >= 0; j--) { p[B] = 0;     p[C] = j;     ring[kk++] = Index(FaceIndex(B, -1), p); }
                    for (int j = 0; j < n; j++)      { p[B] = j;     p[C] = 0;     ring[kk++] = Index(FaceIndex(C, -1), p); }
                    Rings[A][s] = ring;
                    for (int pos = 0; pos < ring.Length; pos++) ringsOf[ring[pos]].Add(new[] { A, s, pos });
                }
            }

            // Vizinhas: em cada anel que passa pela peça, a anterior e a seguinte.
            Neighbors = new int[Total][]; NeighborCross = new bool[Total][];
            for (int t = 0; t < Total; t++)
            {
                var nb = new List<int>(); var cross = new List<bool>();
                foreach (var r in ringsOf[t])
                {
                    var ring = Rings[r[0]][r[1]]; int len = ring.Length;
                    foreach (int c in new[] { ring[(r[2] + 1) % len], ring[(r[2] + len - 1) % len] })
                    { nb.Add(c); cross.Add(CellFace[c] != CellFace[t]); }
                }
                Neighbors[t] = nb.ToArray(); NeighborCross[t] = cross.ToArray();
            }
        }

        public int[] Dest(int axis, int slice, int dir) => dir > 0 ? Perms[axis][slice] : InvPerms[axis][slice];

        public sbyte[] Apply(sbyte[] colors, int axis, int slice, int dir)
        {
            var dest = Dest(axis, slice, dir); var o = new sbyte[colors.Length];
            for (int i = 0; i < colors.Length; i++) o[dest[i]] = colors[i];
            return o;
        }
    }
}