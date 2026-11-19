using System;
using System.Collections.Generic;
using System.Linq;

namespace Cubo.Domain
{
    public sealed partial class BattleEngine
    {
        // Cria a batalha. O jogador começa; o rival, que joga depois, começa com SecondMana em cada barra.
        // A ordem de consumo do gerador importa: tabuleiro, garantia.
        public BattleState NewState(BattleOptions o, string[] playerTeam, Mulberry32 rng)
        {
            int n = o.Size == 0 ? R.DefaultSize : o.Size;
            var g = Geometry.For(n);
            bool easy = o.Mode == "facil";
            double hpBase = easy
                ? (R.HpEasy.TryGetValue(n, out var he) ? he : 85)
                : JsMath.RoundInt((R.HpHard.TryGetValue(n, out var hh) ? hh : 150) / 5.0) * 5;
            int bas = (int)hpBase;
            var team = Data.ResolveTeam(playerTeam);
            var equip = EquipFor(team, o.Equip, o.Tiers);
            int hpP = (o.HpPlayer ?? bas) + EquipSum(equip, EquipKinds.MaxHp);
            int hpE = o.HpEnemy ?? JsMath.RoundInt(bas * (1 + R.RivalHpBonus) * (o.EnemyScale == 0 ? 1 : o.EnemyScale) / 5) * 5;

            var st = new BattleState
            {
                N = n, NumColors = R.NumColors, Easy = easy,
                Colors = RandomBoard(g, R.NumColors, rng),
                EnemyDmg = o.EnemyDmg == 0 ? 1 : o.EnemyDmg,
                Teams = new[] { EquippedTeam(UpgradedTeam(team, o.Tiers), equip), Data.EnemyTeam },
                Equip = equip
            };
            // 3 contra 3: a vida do lado é repartida entre os três heróis (o Elmo soma no herói que o leva).
            var maxP = Split(hpP - EquipSum(equip, EquipKinds.MaxHp));
            for (int i = 0; i < Heroes; i++) if (equip[i] is EquipDef e && e.Kind == EquipKinds.MaxHp) maxP[i] += e.N;
            if (o.PlayerHeroMaxMod != null)                       // o Ídolo levou vida máxima de um herói (nunca abaixo de 1)
                for (int i = 0; i < Heroes && i < o.PlayerHeroMaxMod.Length; i++) maxP[i] = Math.Max(1, maxP[i] + o.PlayerHeroMaxMod[i]);
            SetHeroHp(st, Side.Player, maxP, o.PlayerHp != 0 ? Math.Min(hpP, o.PlayerHp) : hpP);
            // A corrida guarda a vida de cada herói: quem caiu continua caído (só a Poção o traz de volta).
            if (o.PlayerHeroHp != null && o.PlayerHeroHp.Length == Heroes && o.PlayerHeroHp.Any(h => h > 0))
            {
                for (int i = 0; i < Heroes; i++) st.HeroHp[0][i] = Math.Max(0, Math.Min(st.HeroMax[0][i], o.PlayerHeroHp[i]));
                SyncHp(st, Side.Player);
            }
            SetHeroHp(st, Side.Enemy, Split(hpE), hpE);
            for (int i = 0; i < Heroes; i++) st.StartAlive[i] = Alive(st, Side.Player, i);
            st.Vow = o.Vow;
            if (o.MarkBonus > 0)                                  // a marca vai no herói do rival com mais vida máxima
            {
                int best = -1;
                for (int i = 0; i < Heroes; i++)
                    if (Alive(st, Side.Enemy, i) && (best < 0 || st.HeroMax[1][i] > st.HeroMax[1][best])) best = i;
                st.Marked = best; st.MarkBonus = o.MarkBonus;
            }
            st.Crit[0] = o.CritPlayer ?? R.CritChance[0];
            st.Crit[1] = o.CritEnemy ?? R.CritChance[1];
            st.Combo[0] = o.ComboPlayer ?? R.ComboChance[0];
            st.Combo[1] = o.ComboEnemy ?? R.ComboChance[1];
            for (int s = 0; s < Slots; s++) st.Mana[1][s] = Math.Min(R.ManaCap, R.SecondMana);
            Assist(st, g, rng);
            st.Fx = new FxState();
            st.FoeFx = new FxState();
            ApplyEquip(st);
            ApplyIdols(st, o);
            return st;
        }

        // Os Ídolos (eventos): a mana inicial de cada poder, o dano a menos, o dano a mais e o desconto no custo.
        private void ApplyIdols(BattleState st, BattleOptions o)
        {
            if (o.StartMana > 0)
                for (int s = 0; s < Slots; s++) st.Mana[0][s] = Math.Min(R.ManaCap, st.Mana[0][s] + o.StartMana);
            if (o.Ward > 0) AddFx(st, Side.Player, "ward", o.Ward, null, true);
            st.DmgBonus = Math.Max(0, o.DmgBonus);
            st.CostCut = Math.Max(0, o.CostCut);
        }

        // A vida de um lado em três partes quase iguais (o resto vai para os primeiros).
        private static int[] Split(int total)
        {
            var parts = new int[Heroes];
            for (int i = 0; i < Heroes; i++) parts[i] = total / Heroes + (i < total % Heroes ? 1 : 0);
            return parts;
        }
    }
}