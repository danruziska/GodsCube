using System.Collections.Generic;
using System.Linq;

namespace Cubo.Domain
{
    // Uma peça que caiu na frente: estava em From e agora está em To (mesma coluna, mais embaixo).
    public struct Fall
    {
        public int From, To;
    }

    // O que a gravidade fez na frente depois de tirar peças: as que caíram e as novas que entraram no alto.
    public sealed class Drop
    {
        public List<Fall> Falls = new List<Fall>();
        public int[] New = new int[0];             // as peças novas, no alto de cada coluna (de baixo para cima)
        public List<int> Fills = new List<int>();  // as cores delas, na mesma ordem
        // As casas que mudaram de peça: a cascata procura grupos que passem por elas.
        public IEnumerable<int> Changed => Falls.Select(f => f.To).Concat(New);
    }

    // O que aconteceu num disparo: a sessão usa isto para contar à tela; os testes olham o estado.
    public sealed class FireResult
    {
        public Side Side;
        public int Color, Damage, Bonus;
        public bool Crit, Charge;
        public int[] Cells, Before;
        public List<int> Fills;
        public GroupGainResult Gain;
        public int[] Hp;                 // [lado], logo depois do dano
        public int[][] HeroHp;           // [lado][herói], logo depois do dano
        public int Target = -1;          // o herói do rival que levou o dano deste grupo
        public int[][] Mana;             // [lado][poder], logo depois do ganho
        public List<Edit> Retouch = new List<Edit>();   // a garantia, quando o mesmo lado dispara de novo
        public sbyte[] Board;            // o tabuleiro logo depois das peças novas deste grupo (antes da cascata)
        public Drop Drop;                // o que caiu e o que entrou depois deste grupo
        public List<ChainLink> Chain = new List<ChainLink>();   // os grupos que as peças novas formaram, na ordem
        public List<Affliction> Struck = new List<Affliction>();  // veneno ou queimadura que o grupo tocado pegou
        public bool Armed;               // este toque armou o veneno ou a queimadura (a área vem depois da entrega)
    }

    // Um elo da cascata: um grupo que as peças novas formaram sozinhas e que disparou sem toque.
    // Vale como um grupo tocado (dano, mana, bônus, passivas) e sorteia o próprio crítico. Não gasta a vez.
    public sealed class ChainLink
    {
        public int Color, Damage, Bonus;
        public bool Crit, Charge;
        public int[] Cells, Before;
        public List<int> Fills;
        public GroupGainResult Gain;
        public int[] HpFrom, Hp;         // [lado], antes e depois deste elo
        public int[][] HeroHpFrom, HeroHp;   // [lado][herói], antes e depois deste elo
        public int Target = -1;          // o herói do rival que levou o dano deste elo
        public int[][] ManaFrom, Mana;   // [lado][poder], antes e depois deste elo
        public sbyte[] Board;            // o tabuleiro logo depois das peças novas deste elo
        public Drop Drop;                // o que caiu e o que entrou depois deste elo
        public List<Affliction> Struck = new List<Affliction>();  // veneno ou queimadura que este elo pegou
    }

    // O que aconteceu na entrega: o giro do cubo inteiro e a garantia de quem vai jogar.
    public sealed class HandResult
    {
        public Side Side;
        public int Face;                 // a face física entregue (0 a 3), que agora está na frente
        public Move Move;                // o giro do cubo inteiro (Slice = -1)
        public int Played, Received;     // o id da face que `Side` jogou e o da face que o rival recebeu
        public List<Edit> Retouch = new List<Edit>();
        public List<Affliction> Ticks = new List<Affliction>();   // o veneno e a queimadura de quem vai jogar
        public bool Zone;                // quem entregou armou uma área: ele a põe antes de o rival jogar
    }

    // O que aconteceu ao usar um poder de herói.
    public sealed class CastResult
    {
        public Side Side;
        public int Slot, Hero, Power;
        public string Kind;
        public int Amount;                         // dano causado ou vida curada
        public int Target = -1;                    // dano: o herói do rival atingido; cura: o herói curado
        public bool Crit;                          // o dano deste poder foi crítico
        public List<int> Cells = new List<int>();  // peças destruídas (purge)
        public List<int> Before = new List<int>();
        public List<int> Fills = new List<int>();
        public List<Edit> Retouch = new List<Edit>();
        public int[] Hp;
        public int[][] Mana;
        public sbyte[] Board;                      // purge: o tabuleiro logo depois das peças novas (antes da cascata)
        public Drop Drop;                          // purge: o que caiu e o que entrou
        public List<ChainLink> Chain = new List<ChainLink>();   // purge: a cascata que as peças novas causaram
    }

}
namespace Cubo.Domain
{
    // Veneno ou queimadura num herói: quando a área pega alguém (Dmg e Turns = o efeito que ficou) ou quando o efeito
    // machuca no começo do turno (Dmg = a vida que tirou, Turns = os turnos que ainda faltam).
    public sealed class Affliction
    {
        public Side Side;
        public int Hero, Dmg, Turns;
        public string Kind;
    }
}
