using System;
using System.Collections.Generic;
using System.Linq;

namespace Cubo.Domain
{
    // As melhorias de habilidade da corrida (+, ++, +++, ★): o poder com os níveis somados.
    public sealed partial class BattleEngine
    {
        // Quantos níveis a habilidade tem (0 = não melhora).
        public int MaxTier(string powerId) => Data.Upgrades != null && Data.Upgrades.TryGetValue(powerId, out var t) ? t.Length : 0;

        // O poder no nível `tier`: custo, quantidade, crítico e chance somados nível a nível, e os efeitos ★.
        // Nível 0 devolve o próprio poder da tabela.
        public PowerDef Upgraded(PowerDef p, int tier)
        {
            tier = Math.Max(0, Math.Min(MaxTier(p.Id), tier));
            if (tier == 0) return p;
            var o = p.Clone();
            o.Tier = tier;
            var star = new List<StarFx>(o.Star);
            foreach (var u in Data.Upgrades[p.Id].Take(tier))
            {
                o.Cost += u.Cost; o.Amount += u.Amount; o.Crit += u.Crit; o.V += u.V;
                star.AddRange(u.Star);
            }
            o.Cost = Math.Max(1, o.Cost);
            o.Star = star.ToArray();
            return o;
        }

        // O time do jogador com os poderes melhorados (cópias: os heróis da tabela não mudam).
        private HeroDef[] UpgradedTeam(HeroDef[] team, IDictionary<string, int> tiers)
        {
            if (tiers == null || tiers.Count == 0) return team;
            return team.Select(h => !h.Powers.Any(p => tiers.ContainsKey(p.Id)) ? h : new HeroDef
            {
                Id = h.Id, Passive = h.Passive, Colors = h.Colors,
                Powers = h.Powers.Select(p => Upgraded(p, tiers.TryGetValue(p.Id, out var t) ? t : 0)).ToArray()
            }).ToArray();
        }
    }
}
