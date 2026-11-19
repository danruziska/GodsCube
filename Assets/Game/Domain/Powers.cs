using System;
using System.Collections.Generic;

namespace Cubo.Domain
{
    // Poderes dos heróis. Valem antes do grupo, quantas vezes a mana deixar, e não gastam a vez.
    public sealed partial class BattleEngine
    {
        private static int[][] CloneMana(BattleState st) => new[] { (int[])st.Mana[0].Clone(), (int[])st.Mana[1].Clone() };

        // Quantas peças da frente um poder de varrer tiraria agora.
        public int PurgeCount(BattleState st, Side side, int slot)
        {
            var p = st.Power(side, slot);
            if (p.Kind != "purge") return 0;
            var g = Geometry.For(st.N);
            int bas = Faces.Front * g.NN, count = 0;
            for (int i = bas; i < bas + g.NN; i++) if (Array.IndexOf(p.PurgeColors, (int)st.Colors[i]) >= 0) count++;
            return count;
        }

        public bool CanCast(BattleState st, Side side, int slot) =>
            InMatch(st, side) && !st.Silenced[(int)side] && Alive(st, side, slot >> 1) && st.Mana[(int)side][slot] >= CostOf(st, side, slot);

        public CastResult Cast(BattleState st, Side side, int slot, Mulberry32 rng)
        {
            if (!CanCast(st, side, slot)) return null;
            var g = Geometry.For(st.N);
            var p = st.Power(side, slot);
            st.Mana[(int)side][slot] -= CostOf(st, side, slot);
            if (side == Side.Player) st.PlayerCasts++;            // o voto "sem poderes" do Juramento
            var ev = new CastResult { Side = side, Slot = slot, Hero = slot >> 1, Power = slot & 1, Kind = p.Kind };
            // ★: os efeitos entram antes do poder agir (o focus e o combo já valem para as peças que ele faz entrar)
            foreach (var fx in p.Star) AddFx(st, side, fx.Type, fx.Type == "focus" ? fx.N : fx.V, fx.Turns, true);

            switch (p.Kind)
            {
                case "damage":
                {
                    int amount = p.Amount;
                    if (p.Crit > 0 && rng.Next() < p.Crit) { amount *= R.CritMultiplier; ev.Crit = true; }   // só poderes que dizem ter crítico
                    if (st.Fx != null) amount = JsMath.RoundInt(amount * (side == Side.Player ? DmgMult(st) : WardMult(st)));
                    amount = JsMath.RoundInt(amount * ElementMult(st, side, p.Color, EffectiveTarget(st, side)));
                    ev.Amount = Hit(st, side, amount, out ev.Target);
                    break;
                }
                case "heal":
                {
                    ev.Amount = HealOne(st, side, p.Amount, out ev.Target);    // o herói de pé mais ferido
                    break;
                }
                case "extra":
                    st.MovesLeft += 1;
                    break;
                case "purge":
                {
                    // Tira da frente as peças das cores indicadas: 1 de dano cada, sem mana. As de cima caem e entram novas.
                    int bas = Faces.Front * g.NN;
                    for (int i = bas; i < bas + g.NN; i++)
                        if (Array.IndexOf(p.PurgeColors, (int)st.Colors[i]) >= 0) { ev.Cells.Add(i); ev.Before.Add(st.Colors[i]); }
                    int sweep = JsMath.RoundInt(ev.Cells.Count * R.DamagePerPiece * ElementMult(st, side, p.Color, EffectiveTarget(st, side)));
                    ev.Amount = Hit(st, side, sweep, out ev.Target);
                    ev.Drop = DropFront(st, g, side, ev.Cells, rng);
                    ev.Fills = ev.Drop.Fills;
                    ev.Board = (sbyte[])st.Colors.Clone();
                    ev.Chain = Cascade(st, g, side, ev.Drop.Changed, rng);
                    break;
                }
                case "critUp":
                    AddFx(st, side, "crit", p.V, null, true);       // até o fim da batalha
                    break;
                case "critNext":
                    if (st.FxOf(side) == null) { if (side == Side.Player) st.Fx = new FxState(); else st.FoeFx = new FxState(); }
                    st.FxOf(side).SetNextCrit(true);
                    break;
            }

            ev.Hp = (int[])st.Hp.Clone(); ev.Mana = CloneMana(st);
            CheckWinner(st, side);
            if (p.Kind == "purge") ev.Retouch = Assist(st, g, rng);
            return ev;
        }
    }
}