namespace Cubo.Domain
{
    // O giro do cubo inteiro: no fim do turno, a face escolhida vai para a frente do rival.
    public sealed partial class BattleEngine
    {
        // O giro do cubo inteiro que traz a face física `face` (uma das 4 vizinhas) para a frente.
        // É o mesmo giro de fatia aplicado a todas as fatias do eixo: Move com Slice = -1.
        public static Move TurnToFront(int n, int face)
        {
            if (!Faces.IsNeighbor(face)) return null;      // a frente já está lá; a de trás não vem com um quarto de volta
            var g = Geometry.For(n);
            int cell = face * g.NN;
            for (int axis = 0; axis < 2; axis++)
                for (int dir = 1; dir >= -1; dir -= 2)
                {
                    int to = g.Dest(axis, g.CellPos[cell][axis], dir)[cell];
                    if (g.CellFace[to] == Faces.Front) return new Move(axis, -1, dir);
                }
            return null;
        }

        // Aplica um giro de fatia (Slice >= 0) ou do cubo inteiro (Slice = -1) às cores.
        public static sbyte[] Apply(Geometry g, sbyte[] colors, Move m)
        {
            if (m.Slice >= 0) return g.Apply(colors, m.Axis, m.Slice, m.Dir);
            var c = colors;
            for (int s = 0; s < g.N; s++) c = g.Apply(c, m.Axis, s, m.Dir);
            return c;
        }

        // Gira o cubo inteiro: as cores e os ids das faces andam juntos.
        public static void TurnWhole(BattleState st, Move m)
        {
            var g = Geometry.For(st.N);
            var ids = new int[6];
            for (int f = 0; f < 6; f++)
            {
                int cell = f * g.NN;
                int to = g.Dest(m.Axis, g.CellPos[cell][m.Axis], m.Dir)[cell];
                ids[g.CellFace[to]] = st.FaceId[f];
            }
            st.FaceId = ids;
            st.Colors = Apply(g, st.Colors, m);
            if (st.HazardAt != null) st.HazardAt = Apply(g, st.HazardAt, m);   // as áreas de veneno andam com as faces
        }

        // A face física onde está o id agora (ou -1).
        public static int FaceOf(BattleState st, int id)
        {
            for (int f = 0; f < 6; f++) if (st.FaceId[f] == id) return f;
            return -1;
        }
    }
}