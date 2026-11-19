namespace Cubo.Domain
{
    public enum Side { Player = 0, Enemy = 1 }

    public static class SideExt
    {
        public static Side Opp(this Side s) => s == Side.Player ? Side.Enemy : Side.Player;
        public static string Id(this Side s) => s == Side.Player ? "player" : "enemy";
        public static Side Parse(string id) => id == "enemy" ? Side.Enemy : Side.Player;
    }

    // As faces físicas (as casas do cubo, que não giram com ele): 0 +X direita, 1 -X esquerda, 2 +Y cima,
    // 3 -Y baixo, 4 +Z frente (virada para a câmera) e 5 -Z trás. Só a frente se joga; das 4 vizinhas sai
    // a face que vai para o rival. A de trás nunca é entregue.
    public static class Faces
    {
        public const int Front = 4, Back = 5;
        public static readonly int[] Neighbors = { 0, 1, 2, 3 };     // direita, esquerda, cima, baixo

        public static bool IsNeighbor(int face) => face >= 0 && face < 4;
    }

    // Antes do grupo (cartas, poderes, disparo), depois dele (escolher a face que vai para o rival) ou, logo depois da
    // entrega, quando o lado que entregou armou veneno ou queimadura: escolher a área na face que acabou de entregar.
    // Em Zone o turno já é do rival, mas ele só joga quando a área estiver posta.
    public enum Phase { Match, Hand, Zone }
}