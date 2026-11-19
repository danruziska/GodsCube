using System;
using System.Collections.Generic;

namespace Cubo.Domain
{
    // Efeitos ativos (state.Fx do jogador, state.FoeFx do rival): somas e multiplicadores que mudam os ataques.
    public sealed partial class BattleEngine
    {
        public static double FxSum(FxState fx, string type)
        {
            double x = 0;
            var a = fx == null ? null : fx.List(type);
            if (a != null) for (int i = 0; i < a.Count; i++) x += a[i].V;
            return x;
        }

        public double FxSum(BattleState st, string type) => FxSum(st.Fx, type);

        public double DmgMult(BattleState st) => Math.Max(0, 1 + FxSum(st, "dmg"));
        public double WardMult(BattleState st) => Math.Max(0.2, 1 - FxSum(st, "ward"));
        public double DrainMult(BattleState st) => Math.Max(0, 1 - FxSum(st, "drain"));

        // Um efeito do jogador (cartas e runas).
        public void AddFx(BattleState st, string type, double v, int? turns, bool hasTurns) =>
            AddFx(st, Side.Player, type, v, turns, hasTurns);

        public void AddFx(BattleState st, Side side, string type, double v, int? turns, bool hasTurns)
        {
            if (side == Side.Player && st.Fx == null) st.Fx = new FxState();
            if (side == Side.Enemy && st.FoeFx == null) st.FoeFx = new FxState();
            var fx = st.FxOf(side);
            if (!fx.Lists.ContainsKey(type)) fx.SetList(type, new List<FxEntry>());
            fx.Lists[type].Add(new FxEntry { V = v, Turns = turns, HasTurns = hasTurns });
        }

        // Passa um turno de `side`. O que dura "turnos" do jogador (dmg, crit, combo, focus) conta quando o jogador
        // passa a vez; o que age sobre o rival (ward, drain) e o crítico e o combo do rival contam quando o rival passa.
        public void TickFx(BattleState st, Side side)
        {
            if (side == Side.Player) Tick(st.Fx, "dmg", "crit", "combo", "focus");
            else { Tick(st.Fx, "ward", "drain"); Tick(st.FoeFx, "crit", "combo"); }
        }

        private static void Tick(FxState fx, params string[] keys)
        {
            if (fx == null) return;
            foreach (var key in keys)
            {
                if (!fx.Lists.ContainsKey(key)) continue;
                var kept = new List<FxEntry>();
                foreach (var e in fx.List(key))
                {
                    if (e.Turns == null) { kept.Add(e); continue; }
                    e.Turns -= 1;
                    if (e.Turns > 0) kept.Add(e);
                }
                fx.SetList(key, kept);
            }
        }
    }
}