using System;
using System.Collections.Generic;

namespace Cubo.Domain
{
    // As regras da batalha. Não guarda estado: cada método recebe o BattleState.
    // O arquivo é dividido em partes (partial): Groups, Gains, Effects, Options, Factory, Battle, Whole, Powers,
    // e Ai.
    public sealed partial class BattleEngine
    {
        public const int Slots = 6;              // 3 heróis x 2 poderes

        public readonly GameData Data;
        public readonly Rules R;

        public BattleEngine(GameData data)
        {
            Data = data;
            R = data.Rules;
        }

        private double Scale(BattleState st) =>
            R.CostScale.TryGetValue(st.Easy ? "facil" : "dificil", out var bySize) && bySize.TryGetValue(st.N, out var f) ? f : 1;

        // O custo de um poder agora: o da tabela com a escala do modo e do tamanho, menos o desconto do Decreto real
        // (os poderes do herói que tem essa passiva) e o do Cetro. Nunca menos de 1.
        public int CostOf(BattleState st, Side side, int slot)
        {
            int cost = JsMath.RoundInt(st.Power(side, slot).Cost * Scale(st));
            if (st.Hero(side, slot >> 1).Passive == Passives.Decree) cost -= R.DecreeDiscount;
            if (side == Side.Player && st.Equip[slot >> 1] is EquipDef e && e.Kind == EquipKinds.Cost) cost -= e.N;
            if (side == Side.Player) cost -= st.CostCut;                       // o Ídolo da Rainha
            return Math.Max(1, cost);
        }

        // Os poderes do time que uma peça dessa cor enche.
        public List<int> SlotsOfColor(BattleState st, Side side, int color)
        {
            var out2 = new List<int>();
            for (int k = 0; k < Slots; k++) if (st.Power(side, k).Color == color && Alive(st, side, k >> 1)) out2.Add(k);   // herói caído não enche
            return out2;
        }

        // Algum herói de pé de `side` tem essa passiva? (a passiva de quem caiu para de valer)
        public bool Has(BattleState st, Side side, string passive)
        {
            var team = st.Teams[(int)side];
            for (int i = 0; i < team.Length; i++) if (team[i].Passive == passive && Alive(st, side, i)) return true;
            return false;
        }
    }
}