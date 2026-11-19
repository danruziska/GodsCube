using System;
using System.Collections.Generic;
using System.Linq;
using Cubo.Domain;

namespace Cubo.App
{
    // Um comando do jogador, como a sessão o aceitou. Kind: S giro (A eixo, B fatia, C sentido), F disparo (A peça),
    // H entrega (A face física, 0 a 3), C poder (A posição).
    public sealed class Command
    {
        public char Kind;
        public int A, B, C;
        public Command() { }
        public Command(char kind, int a = 0, int b = 0, int c = 0) { Kind = kind; A = a; B = b; C = c; }
        public override string ToString() => Kind + ":" + A + "," + B + "," + C;
    }

    // Tudo o que é preciso para refazer uma batalha do zero: a semente e as opções que a criaram, as runas gastas
    // no começo e os comandos do jogador. Como o jogo é determinístico, rejogar os comandos devolve o mesmo
    // tabuleiro, a mesma vida e o mesmo gerador. Isso é o salvamento no meio da batalha.
    public sealed class BattleRecord
    {
        public uint Seed;
        public string AiLevel = "atento";
        public string[] Team;
        public int Size = 6;
        public string Mode = "facil";
        public double EnemyScale = 1, EnemyDmg = 1;
        public int PlayerHp;
        public int[] PlayerHeroHp;                              // a vida de cada herói ao entrar (null = PlayerHp repartida)
        public int[] PlayerHeroMaxMod;                          // a vida máxima a mais ou a menos de cada herói (o Ídolo)
        public double? CritPlayer, ComboPlayer;                 // o crítico e o combo do jogador, se os eventos mudaram
        public double MarkBonus;                                // o dano a mais no rival marcado (o Acampamento)
        public double DmgBonus, Ward;                           // os Ídolos: dano a mais e dano a menos
        public int StartMana, CostCut;                          // ...a mana inicial dos poderes e o desconto no custo
        public string Vow;                                      // o voto do Juramento nesta batalha
        public double? ComboEnemy;                              // null = a chance de combo de base do rival
        public Dictionary<string, int> Tiers = new Dictionary<string, int>();   // os níveis de melhoria dos poderes do jogador
        public Dictionary<string, string> Equip = new Dictionary<string, string>();   // [id do herói] = equipamento
        public List<string> Boost = new List<string>();         // runas de batalha gastas no começo
        public List<Command> Commands = new List<Command>();

        public BattleOptions Options() => new BattleOptions
        {
            Size = Size, Mode = Mode, EnemyScale = EnemyScale, EnemyDmg = EnemyDmg, PlayerHp = PlayerHp,
            PlayerHeroHp = PlayerHeroHp == null ? null : (int[])PlayerHeroHp.Clone(),
            PlayerHeroMaxMod = PlayerHeroMaxMod == null ? null : (int[])PlayerHeroMaxMod.Clone(),
            CritPlayer = CritPlayer, ComboPlayer = ComboPlayer, MarkBonus = MarkBonus, Vow = Vow,
            DmgBonus = DmgBonus, Ward = Ward, StartMana = StartMana, CostCut = CostCut,
            ComboEnemy = ComboEnemy, Tiers = new Dictionary<string, int>(Tiers),
            Equip = new Dictionary<string, string>(Equip)
        };
    }

    public static class BattleBuilder
    {
        // Uma batalha nova, ainda sem comandos. Com `run`, as runas de batalha dela são gastas agora e anotadas em
        // rec.Boost; sem `run` (ao refazer uma batalha gravada), vale o que já está em rec.Boost.
        public static BattleSession Create(BattleEngine engine, RunService runs, BattleRecord rec, RunState run = null)
        {
            var rng = new Mulberry32(rec.Seed);
            var st = engine.NewState(rec.Options(), rec.Team, rng);
            if (run != null) rec.Boost = runs.Boost(run, st);
            else if (rec.Boost.Count > 0) runs.Boost(new RunState { Runes = rec.Boost.ToList() }, st);
            return new BattleSession(engine, st, rng, rec.AiLevel);
        }

        // Rejoga os comandos gravados. Um comando recusado quer dizer que o arquivo não vale: devolve null.
        public static BattleSession Replay(BattleEngine engine, RunService runs, BattleRecord rec)
        {
            BattleSession s;
            try { s = Create(engine, runs, rec); }
            catch (Exception) { return null; }
            try
            {
                foreach (var c in rec.Commands)
                {
                    bool ok;
                    switch (c.Kind)
                    {
                        case 'S': return null;      // um giro: batalha gravada antes de o giro sair do jogo, não dá para refazer
                        case 'T': ok = s.TryTarget(c.A); break;
                        case 'F': ok = s.TryFire(c.A); break;
                        case 'H': ok = s.TryHand(c.A); break;
                        case 'C': ok = s.TryCast(c.A); break;
                        case 'Z': ok = s.TryZone(c.A); break;
                        default: ok = false; break;
                    }
                    if (!ok) return null;
                    s.PresentationDone();
                }
            }
            catch (Exception) { return null; }      // um índice fora do tabuleiro num arquivo adulterado também é recusa
            return s;
        }
    }
}