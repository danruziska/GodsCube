using System;

namespace Cubo.Domain
{
    // O que cada lado ganha com um grupo: dano e mana.
    public sealed class SideGain
    {
        public int Pieces;
        public int Value;                                   // o dano no rival
        public int[] Mana = new int[BattleEngine.Slots];
    }

    public sealed class GroupGainResult
    {
        public SideGain[] Gain = { new SideGain(), new SideGain() };   // [lado]; só quem disparou ganha
        public int Bonus;                                   // o bônus de tamanho (peças além da terceira)
        public bool Crit, Charge;                           // crítico; a Investida contou
    }

    public sealed partial class BattleEngine
    {
        // O que um grupo da frente rende para quem o dispara: 1 de dano por peça, mais o bônus de tamanho
        // (GroupBonus por peça além da BonusFrom, hoje a terceira: um par vale pouco, uma mancha grande vale muito), e 1 de mana por peça em cada poder da cor do grupo.
        // `crit`: o sorteio de crítico já foi feito (no disparo); aqui só se aplica o multiplicador.
        public GroupGainResult GroupGain(BattleState st, sbyte[] colors, int[] cells, Side mover, bool crit = false)
        {
            var t = new GroupGainResult();
            var gain = t.Gain[(int)mover];
            int color = GroupColor(colors, cells);
            gain.Pieces = cells.Length;
            foreach (int slot in SlotsOfColor(st, mover, color)) gain.Mana[slot] += R.ManaPerPiece * cells.Length;

            // Um grupo menor que DamageMin (um par) não ataca: só enche a mana. Sem dano, sem bônus, sem crítico.
            if (cells.Length < R.DamageMin)
            {
                t.Bonus = 0;
                gain.Value = 0;
                if (mover == Side.Enemy && st.Fx != null)
                {
                    double dm0 = DrainMult(st);
                    if (dm0 < 1)
                        for (int s = 0; s < Slots; s++) gain.Mana[s] = (int)Math.Floor(gain.Mana[s] * dm0);
                }
                return t;
            }
            t.Bonus = Math.Max(0, cells.Length - R.BonusFrom) * R.GroupBonus;
            double value = cells.Length + t.Bonus;
            t.Charge = Has(st, mover, Passives.Charge) && cells.Length >= R.ChargeMinGroup;
            if (t.Charge) value = JsMath.RoundInt(value * (1 + R.ChargeBonus));
            // Elementos: a cor do grupo contra as duas cores do herói que leva o dano.
            double el = ElementMult(st, mover, color, EffectiveTarget(st, mover));
            if (el != 1) value = JsMath.RoundInt(value * el);
            t.Crit = crit;
            if (crit) value *= R.CritMultiplier;
            gain.Value = (int)value;

            if (mover == Side.Enemy && st.EnemyDmg != 0 && st.EnemyDmg != 1)
                gain.Value = JsMath.RoundInt(gain.Value * st.EnemyDmg);
            if (st.Fx != null)
            {
                // cartas do jogador: mais dano no dele, proteção contra o do rival e mana do rival drenada
                if (mover == Side.Player) gain.Value = JsMath.RoundInt(gain.Value * DmgMult(st));
                else
                {
                    gain.Value = JsMath.RoundInt(gain.Value * WardMult(st));
                    double dm = DrainMult(st);
                    if (dm < 1)
                        for (int s = 0; s < Slots; s++) gain.Mana[s] = (int)Math.Floor(gain.Mana[s] * dm);
                }
            }
            return t;
        }

        // A chance de combo de `side` agora (uma peça nova que ele faz entrar copiar a cor de uma vizinha): a de
        // base, mais os efeitos "combo" (cartas, runas), até ComboMax.
        public double ComboChance(BattleState st, Side side)
        {
            var fx = st.FxOf(side);
            double add = fx == null ? 0 : FxSum(fx, "combo");
            return Math.Max(0, Math.Min(R.ComboMax, st.Combo[(int)side] + add));
        }

        // A chance de crítico de um ataque de `side` agora: a de base, mais os efeitos, até CritMax.
        public double CritChance(BattleState st, Side side)
        {
            var fx = st.FxOf(side);
            double add = fx == null ? 0 : FxSum(fx, "crit");
            return Math.Max(0, Math.Min(R.CritMax, st.Crit[(int)side] + add));
        }

        // O sorteio de crítico de um disparo. Sempre gasta um número do gerador, para a sequência não depender
        // de a chance ser zero ou não. "Próximo ataque crítico" vence o sorteio e é consumido. `attacks` falso (um par,
        // que não causa dano): gasta o número, mas nunca é crítico e não consome o "próximo ataque crítico".
        public bool RollCrit(BattleState st, Side side, Mulberry32 rng, bool attacks = true)
        {
            double r = rng.Next();
            if (!attacks) return false;
            var fx = st.FxOf(side);
            if (fx != null && fx.NextCrit == true) { fx.SetNextCrit(false); return true; }
            return r < CritChance(st, side);
        }
    }
}