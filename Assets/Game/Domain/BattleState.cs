using System.Collections.Generic;

namespace Cubo.Domain
{
    // Um giro: axis 0 a 2, slice 0 a n-1, dir +1 ou -1. Slice = -1 gira o cubo inteiro (todas as fatias do eixo).
    public sealed class Move
    {
        public readonly int Axis, Slice, Dir;
        public Move(int axis, int slice, int dir) { Axis = axis; Slice = slice; Dir = dir; }
        public Move Inverse() => new Move(Axis, Slice, -Dir);
    }

    // Uma troca de cor feita pelo jogo (a garantia): a peça `Cell` passou de `From` para `To`.
    public sealed class Edit
    {
        public int Cell, From, To;
    }

    // Um efeito temporário ou permanente ativo (dmg, ward, drain, crit, echo).
    public sealed class FxEntry
    {
        public double V;
        public int? Turns;        // null = até o fim da batalha
        public bool HasTurns;     // false só para echo, que não tem duração
    }

    // Efeitos ativos de um lado. Guarda a ordem em que as chaves nasceram (as etiquetas da tela seguem essa ordem).
    public sealed class FxState
    {
        public readonly List<string> Order = new List<string>();
        public readonly Dictionary<string, List<FxEntry>> Lists = new Dictionary<string, List<FxEntry>>();
        private static readonly List<FxEntry> Empty = new List<FxEntry>();
        private bool? _nextCrit;

        public List<FxEntry> List(string type) => Lists.TryGetValue(type, out var l) ? l : Empty;

        public void SetList(string type, List<FxEntry> list)
        {
            if (!Lists.ContainsKey(type)) Order.Add(type);
            Lists[type] = list;
        }

        public bool? NextCrit => _nextCrit;
        public void SetNextCrit(bool value)
        {
            if (_nextCrit == null) Order.Add("nextCrit");
            _nextCrit = value;
        }
    }

    // Uma área envenenada (ou em chamas) numa face: as casas dela estão em BattleState.HazardAt com este Id.
    // Quem disparar um grupo que passe por ela leva o efeito; aí ela some. Sem ninguém tocar, some na rodada Until.
    public sealed class Hazard
    {
        public sbyte Id;
        public string Kind;               // EquipKinds.Poison ou EquipKinds.Burn
        public int Dmg, Turns, Until;     // dano por turno, quantos turnos, a rodada em que some sozinha
        public Side By;                   // quem a pôs
    }

    // Veneno ou queimadura num herói: perde Dmg de vida no começo de cada turno do lado dele, por mais Turns turnos.
    public sealed class Dot
    {
        public int Dmg, Turns;
        public int Since;                 // quando começou (BattleState.DotClock): a tela mostra os efeitos nessa ordem
        public bool Active => Turns > 0 && Dmg > 0;
    }

    public sealed class BattleState
    {
        public int N, NumColors;
        public bool Easy;                                 // modo fácil: vida e custos da tabela "facil"
        public sbyte[] Colors;
        public int[] FaceId = { 0, 1, 2, 3, 4, 5 };       // [face física] = id da face (o id anda com o cubo)
        public int[] Hp = new int[2], MaxHp = new int[2];  // [lado]: a soma da vida dos três heróis (0 = o lado perdeu)
        public int[][] HeroHp = { new int[3], new int[3] }, HeroMax = { new int[3], new int[3] };   // [lado][herói]
        public int[] Target = { -1, -1 };                 // [lado]: o herói do outro lado que ele ataca, ou -1 (ainda não escolheu). O do jogador.
        public double EnemyDmg = 1;
        public double[] Crit = new double[2];             // [lado]: a chance de crítico de base
        public double[] Combo = new double[2];            // [lado]: a chance de base de uma peça nova copiar uma vizinha
        public int[][] Mana = { new int[6], new int[6] }; // [lado][poder]
        public Side Turn = Side.Player;
        public Phase Phase = Phase.Match;
        public int MovesLeft = 1, Round = 1;
        public int?[] LastFace = new int?[2];             // [lado]: o id da face em que esse lado jogou por último
        public bool[] Silenced = new bool[2];             // [lado]: não usa poderes no próximo turno dele
        public Side? Winner;
        public FxState Fx;                // efeitos do jogador (runas); null até a batalha criar
        public FxState FoeFx;             // efeitos do rival (só crítico, vindo dos poderes dele)
        public HeroDef[][] Teams;         // [lado][herói]
        public EquipDef[] Equip = new EquipDef[3];   // [herói do jogador]: o equipamento que ele leva, ou null

        // Veneno e queimadura. HazardAt[casa] = o Id da área que cobre a casa (0 = nenhuma); gira com o cubo, como as
        // cores, mas não cai com as peças: a área é um lugar da face, não um grupo de peças.
        public sbyte[] HazardAt;
        public List<Hazard> Hazards = new List<Hazard>();
        public EquipDef[] Pending = new EquipDef[2];  // [lado]: o item armado neste turno, à espera da área (ou null)
        public Dot[][][] Dots = NewDots();            // [lado][herói][0 veneno, 1 queimadura]
        public int DotClock;                          // conta os efeitos que começaram (para a ordem na tela)

        // O Juramento (evento): o voto desta batalha ("swift", "silent", "unbroken") ou null; quantos poderes o jogador
        // usou; e quem do time estava de pé no começo (para o voto "ninguém cai").
        public string Vow;
        public int PlayerCasts;
        public bool[] StartAlive = { true, true, true };
        // O Acampamento ("Marcar o mais forte"): o herói do rival marcado (-1 = nenhum) e o dano a mais que ele leva do jogador.
        public int Marked = -1;
        public double MarkBonus;
        // Os Ídolos (para a corrida toda): o dano a mais dos ataques do jogador (o Rei) e o desconto no custo dos poderes (a Rainha).
        public double DmgBonus;
        public int CostCut;

        public static Dot[][][] NewDots()
        {
            var d = new Dot[2][][];
            for (int s = 0; s < 2; s++)
            {
                d[s] = new Dot[3][];
                for (int h = 0; h < 3; h++) d[s][h] = new[] { new Dot(), new Dot() };
            }
            return d;
        }

        public HeroDef Hero(Side side, int index) => Teams[(int)side][index];
        public PowerDef Power(Side side, int slot) => Teams[(int)side][slot >> 1].Powers[slot & 1];
        public FxState FxOf(Side side) => side == Side.Player ? Fx : FoeFx;
    }

    // Opções de uma batalha.
    public sealed class BattleOptions
    {
        public int Size = 6;
        public string Mode = "dificil";        // "facil" ou "dificil": muda a vida e os custos
        public double? CritPlayer, CritEnemy;
        public double? ComboPlayer, ComboEnemy;    // null = o de base das regras
        public Dictionary<string, int> Tiers;      // [id do poder] = nível de melhoria dos poderes do jogador (null = nenhum)
        public Dictionary<string, string> Equip;   // [id do herói do jogador] = id do equipamento (null = nenhum)
        public double EnemyScale = 1, EnemyDmg = 1;
        public int? HpPlayer, HpEnemy;         // vida inicial fixa
        public int PlayerHp;                   // vida com que o jogador entra (0 = cheia)
        public int[] PlayerHeroHp;             // a vida de cada herói do jogador (da corrida); 0 = caído. null = usa PlayerHp
        public int[] PlayerHeroMaxMod;         // [herói]: vida máxima a mais (ou a menos: o Ídolo) nesta corrida
        public double MarkBonus;               // o Acampamento: o rival de mais vida leva este dano a mais do jogador (0 = sem marca)
        // Os Ídolos: dano a mais dos ataques (Rei), mana inicial de cada poder e desconto no custo (Rainha), dano a menos (Torre).
        public double DmgBonus, Ward;
        public int StartMana, CostCut;
        public string Vow;                     // o voto do Juramento para esta batalha
    }
}