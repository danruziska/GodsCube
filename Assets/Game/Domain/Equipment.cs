using System;
using System.Collections.Generic;
using System.Linq;

namespace Cubo.Domain
{
    // Os equipamentos dos heróis do jogador: cada herói leva um, escolhido antes da corrida.
    public sealed partial class BattleEngine
    {
        // O equipamento de cada herói do time, na ordem do time (null = nenhum). Ids que não existem são ignorados.
        // `tiers` traz os níveis de melhoria da corrida (os itens que melhoram usam o próprio id como chave).
        private EquipDef[] EquipFor(HeroDef[] team, IDictionary<string, string> equip, IDictionary<string, int> tiers = null)
        {
            var outp = new EquipDef[team.Length];
            if (equip == null) return outp;
            for (int i = 0; i < team.Length; i++)
                if (equip.TryGetValue(team[i].Id, out var id) && id != null && Data.EquipById.TryGetValue(id, out var e))
                    outp[i] = UpgradedEquip(e, tiers != null && tiers.TryGetValue(id, out var t) ? t : 0);
            return outp;
        }

        // O item no nível `tier`: dano por turno, turnos e lado da área somados nível a nível. A área nunca passa do
        // lado do cubo (isso é conferido ao pôr a área). Nível 0 devolve o próprio item da tabela.
        public EquipDef UpgradedEquip(EquipDef e, int tier)
        {
            tier = Math.Max(0, Math.Min(MaxTier(e.Id), tier));
            if (tier == 0) return e;
            var o = e.Clone();
            o.Tier = tier;
            if (o.Area <= 0) o.Area = R.HazardArea;
            foreach (var u in Data.Upgrades[e.Id].Take(tier)) { o.N += u.Amount; o.Turns += u.Turns; o.Area += u.Area; }
            return o;
        }

        // O lado da área do item (o do item melhorado, ou o de rules.json).
        public int AreaOf(EquipDef e) => e != null && e.Area > 0 ? e.Area : R.HazardArea;

        private static int EquipSum(EquipDef[] equip, string kind) => equip.Where(e => e != null && e.Kind == kind).Sum(e => e.N);

        // A Lâmina: os poderes de dano e de cura do herói que a leva rendem N a mais (cópias: a tabela não muda).
        private static HeroDef[] EquippedTeam(HeroDef[] team, EquipDef[] equip)
        {
            var outp = (HeroDef[])team.Clone();
            for (int i = 0; i < team.Length; i++)
            {
                var e = equip[i];
                if (e == null || e.Kind != EquipKinds.Amount) continue;
                var h = team[i];
                outp[i] = new HeroDef
                {
                    Id = h.Id, Passive = h.Passive, Colors = h.Colors,
                    Powers = h.Powers.Select(p =>
                    {
                        if (p.Amount <= 0) return p;
                        var c = p.Clone();
                        c.Amount += e.N;
                        return c;
                    }).ToArray()
                };
            }
            return outp;
        }

        // No começo da batalha: a mana inicial do Anel e os efeitos que valem a batalha toda (ward, combo, chainDmg).
        private void ApplyEquip(BattleState st)
        {
            for (int i = 0; i < st.Equip.Length; i++)
            {
                var e = st.Equip[i];
                if (e == null) continue;
                switch (e.Kind)
                {
                    case EquipKinds.Mana:
                        for (int k = 0; k < 2; k++) st.Mana[0][i * 2 + k] = Math.Min(R.ManaCap, st.Mana[0][i * 2 + k] + e.N);
                        break;
                    case EquipKinds.Ward:
                    case EquipKinds.Combo:
                    case EquipKinds.ChainDmg:
                        AddFx(st, Side.Player, e.Kind, e.V, null, true);
                        break;
                }
            }
        }
    }
}
