using System;
using System.Collections.Generic;
using System.Linq;

namespace Cubo.Domain
{
    // Regras numéricas do jogo (vêm de rules.json).
    public sealed class Rules
    {
        public int NumColors, MinGroup, DamagePerPiece, ManaPerPiece, GroupBonus, ManaCap;
        // MinGroup: o menor grupo que se pode tocar (uma mancha de peças iguais que se tocam). CascadeMin: o menor grupo
        // que as peças que caem fazem disparar sozinho (a cascata). BonusFrom: o bônus de tamanho conta a partir da peça seguinte a esta.
        public int CascadeMin = 4, BonusFrom = 3;
        // DamageMin: o menor grupo que causa dano. Menores (os pares) só enchem a mana dos poderes da cor.
        public int DamageMin = 3;
        // Elementos: Beats[cor] = a cor que ela vence (ou -1). Contra um herói, cada cor dele que a cor do ataque vence
        // soma ElementStrong ao dano; cada cor dele que vence a do ataque tira ElementWeak.
        public int[] Beats;
        public double ElementStrong = 0.5, ElementWeak = 0.25;
        // Veneno e queimadura: um grupo TOCADO de HazardMin peças ou mais arma o item; a área é HazardArea x HazardArea
        // casas da face entregue e some sozinha depois de HazardRounds rodadas se ninguém a tocar.
        public int HazardMin = 4, HazardArea = 3, HazardRounds = 3;
        public Dictionary<string, Dictionary<int, double>> CostScale;   // [modo]["tamanho"] = fator
        public double RivalHpBonus;
        // [lado]: a chance de base do "combo", a de uma peça nova copiar a cor de uma vizinha (o que forma cascatas).
        // Começa baixa de propósito: o jogador a aumenta ao longo da corrida (runas e, depois, melhorias).
        public double[] ComboChance;
        public double ComboMax;                                          // a chance nunca passa disto, some o que somar
        public Dictionary<int, int> HpEasy, HpHard;
        public double[] CritChance;                                      // [lado]: a chance de crítico de cada ataque, no começo
        public double CritMax;                                           // a chance nunca passa disto, some o que somar
        public int CritMultiplier, DefaultSize;
        public int SecondMana;                                           // mana a mais em cada barra de quem joga depois
        public int[] Sizes;
        public Dictionary<string, int> AiLevels;                         // quantos grupos da frente o rival compara
        public Dictionary<string, int> AiHandLook;                       // quantas faces ele compara antes de entregar
        // números das passivas
        public int ChargeMinGroup, DecreeDiscount, CommunionMana;
        public double ChargeBonus;
    }

    // Kind: damage (Amount; com Crit > 0 pode ser crítico), heal (Amount), extra, purge (PurgeColors),
    // critUp (+V de chance de crítico até o fim da batalha) e critNext (o próximo ataque é crítico).
    public sealed class PowerDef
    {
        public string Id, Kind;
        public int Index, Color, Cost, Amount;
        public double Crit, V;
        public int[] PurgeColors;
        public int Tier;                                   // nível de melhoria nesta corrida (0 = o de base)
        public StarFx[] Star = new StarFx[0];              // o que o nível ★ acrescenta: efeitos ao usar o poder

        public PowerDef Clone()
        {
            var o = (PowerDef)MemberwiseClone();
            o.Star = (StarFx[])Star.Clone();
            return o;
        }
    }

    // Um efeito que o poder ★ põe em quem o usa (combo, focus, chainDmg, chainMana). Turns null = até o fim da batalha.
    public sealed class StarFx
    {
        public string Type;
        public double V;
        public int N;
        public int? Turns;
    }

    // Um nível de melhoria de uma habilidade (upgrades.json). Soma-se aos níveis anteriores.
    public sealed class UpgradeTier
    {
        public int Cost, Amount;
        public int Turns, Area;            // só nos itens de veneno e fogo: turnos a mais e lado da área a mais
        public double Crit, V;
        public StarFx[] Star = new StarFx[0];
    }

    // Um equipamento (equipment.json): um herói o leva para a corrida inteira. Kind diz o que ele faz:
    // poison e burn: um grupo tocado de 4+ peças deixa uma área envenenada (ou em chamas) na face entregue; quem
    // tocar nela perde N de vida por turno durante Turns turnos.
    // amount (+N no dano e na cura dos poderes do herói), mana (os poderes do herói começam cada batalha com N),
    // cost (os poderes do herói custam N a menos), maxHp (+N de vida máxima), ward (V a menos de dano do rival),
    // combo (+V de chance de combo), chainDmg (+V de dano por elo encadeado) e gold (+V do ouro de cada andar).
    public sealed class EquipDef
    {
        public string Id, Kind;
        public int N;
        public double V;
        public int Turns;                  // poison e burn: quantos turnos o efeito dura no herói atingido
        public int Area;                   // poison e burn: o lado da área (0 = o de rules.json, hazards.area)
        public int Tier;                   // nível de melhoria nesta corrida (0 = o de base)

        public EquipDef Clone() => (EquipDef)MemberwiseClone();
    }

    public static class EquipKinds
    {
        public const string Amount = "amount", Mana = "mana", Cost = "cost", MaxHp = "maxHp",
                            Ward = "ward", Combo = "combo", ChainDmg = "chainDmg", Gold = "gold",
                            Poison = "poison", Burn = "burn";

        public static bool IsHazard(string kind) => kind == Poison || kind == Burn;
    }

    // Um herói: duas cores (as dos dois poderes, na ordem), uma passiva (ou null) e dois poderes.
    public sealed class HeroDef
    {
        public string Id, Passive;
        public int[] Colors;
        public PowerDef[] Powers;              // sempre dois
    }

    // Passivas (os valores de "passive" em heroes.json).
    public static class Passives
    {
        public const string Decree = "decreto_real";     // os poderes do Rei custam 1 a menos
        public const string Charge = "investida";        // grupos de 4 ou mais peças: +15% de dano
        public const string Trick = "trapaca";           // ignora a face proibida
        public const string Communion = "comunhao";      // cada grupo: +1 de mana no poder mais vazio do time
    }

    // Tudo o que o jogo lê dos JSON, já em objetos.
    public sealed class GameData
    {
        public Rules Rules;
        public Dictionary<string, HeroDef> Heroes;
        public string[] HeroIds;                        // na ordem do arquivo
        public string[] DefaultTeam;
        public HeroDef[] EnemyTeam;
        public RunRules Run;
        public Dictionary<string, UpgradeTier[]> Upgrades;   // [id do poder ou do item] = os níveis, na ordem (+, ++, +++, ★...)
        public List<RuneDef> Runes;                     // na ordem do arquivo (o sorteio da loja depende dela)
        public Dictionary<string, RuneDef> RuneById;
        public string[] EventIds;                       // na ordem do arquivo
        public List<EquipDef> Equipment = new List<EquipDef>();                       // na ordem do arquivo
        public Dictionary<string, EquipDef> EquipById = new Dictionary<string, EquipDef>();


        public bool ValidTeam(string[] ids)
        {
            if (ids == null || ids.Length != 3) return false;
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] == null || !Heroes.ContainsKey(ids[i])) return false;
                for (int j = 0; j < i; j++) if (ids[j] == ids[i]) return false;
            }
            return true;
        }

        // Os equipamentos que valem: herói e equipamento existem, e cada equipamento está com um herói só (na
        // ordem dos heróis, o primeiro fica com ele). Com `team`, só os heróis desse time contam.
        public Dictionary<string, string> CleanEquip(IDictionary<string, string> raw, string[] team = null)
        {
            var outp = new Dictionary<string, string>();
            if (raw == null) return outp;
            var used = new HashSet<string>();
            foreach (var hero in team ?? HeroIds)
                if (hero != null && Heroes.ContainsKey(hero) && raw.TryGetValue(hero, out var id) && id != null
                    && EquipById.ContainsKey(id) && used.Add(id))
                    outp[hero] = id;
            return outp;
        }

        // O time do jogador; se não valer, fica o padrão.
        public HeroDef[] ResolveTeam(string[] ids)
        {
            var use = ValidTeam(ids) ? ids : DefaultTeam;
            var team = new HeroDef[3];
            for (int i = 0; i < 3; i++) team[i] = Heroes[use[i]];
            return team;
        }
    }
}