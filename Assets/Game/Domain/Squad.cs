using System;

namespace Cubo.Domain
{
    // 3 contra 3: cada herói tem a sua vida. O lado escolhe um herói do rival para atacar (o alvo); o dano dos grupos
    // e dos poderes vai para ele, com a vantagem ou a desvantagem dos elementos. Um lado perde quando os três caem.
    // Hp[lado] continua sendo a soma, para quem só quer saber "quanto falta" (o fim da batalha usa a soma).
    public sealed partial class BattleEngine
    {
        public const int Heroes = 3;

        public static bool Alive(BattleState st, Side side, int hero) =>
            hero >= 0 && hero < Heroes && st.HeroHp[(int)side][hero] > 0;

        public static int AliveCount(BattleState st, Side side)
        {
            int n = 0;
            for (int i = 0; i < Heroes; i++) if (Alive(st, side, i)) n++;
            return n;
        }

        // Hp e MaxHp do lado voltam a ser a soma dos heróis.
        public static void SyncHp(BattleState st, Side side)
        {
            int s = (int)side, hp = 0, max = 0;
            for (int i = 0; i < Heroes; i++) { hp += Math.Max(0, st.HeroHp[s][i]); max += st.HeroMax[s][i]; }
            st.Hp[s] = hp; st.MaxHp[s] = max;
        }

        // A vida de cada herói: o máximo de cada um e a vida com que o lado entra (`total`), repartida na proporção
        // do máximo (todo herói começa vivo). total <= 0 ou >= a soma dos máximos: todos cheios.
        public static void SetHeroHp(BattleState st, Side side, int[] max, int total)
        {
            int s = (int)side, sum = 0;
            for (int i = 0; i < Heroes; i++) { st.HeroMax[s][i] = Math.Max(1, max[i]); sum += st.HeroMax[s][i]; }
            if (total <= 0 || total >= sum)
                for (int i = 0; i < Heroes; i++) st.HeroHp[s][i] = st.HeroMax[s][i];
            else
            {
                int given = 0;
                for (int i = 0; i < Heroes; i++)
                {
                    st.HeroHp[s][i] = Math.Max(1, (int)Math.Floor((double)st.HeroMax[s][i] * total / sum));
                    given += st.HeroHp[s][i];
                }
                for (int i = 0; given < total && i < Heroes * 4; i++)            // o que sobrou do arredondamento
                {
                    int k = i % Heroes;
                    if (st.HeroHp[s][k] < st.HeroMax[s][k]) { st.HeroHp[s][k]++; given++; }
                }
            }
            SyncHp(st, side);
        }

        // ---------------------------------------------------------------- o alvo

        // `side` já escolheu um alvo vivo neste turno?
        public static bool HasTarget(BattleState st, Side side) => Alive(st, side.Opp(), st.Target[(int)side]);

        // O herói que o dano de `side` atinge agora: o alvo escolhido, ou (se ele não existe ou já caiu) o primeiro
        // herói vivo do rival. -1 se o rival não tem ninguém de pé.
        public static int EffectiveTarget(BattleState st, Side side)
        {
            if (HasTarget(st, side)) return st.Target[(int)side];
            for (int i = 0; i < Heroes; i++) if (Alive(st, side.Opp(), i)) return i;
            return -1;
        }

        public bool SetTarget(BattleState st, Side side, int hero)
        {
            if (st.Winner != null || !Alive(st, side.Opp(), hero)) return false;
            st.Target[(int)side] = hero;
            return true;
        }

        // ---------------------------------------------------------------- elementos

        // A cor `a` vence a cor `b`? (rules.json "beats": [cor] = a cor que ela vence)
        public bool Beats(int a, int b) => R.Beats != null && a >= 0 && a < R.Beats.Length && R.Beats[a] == b;

        // O multiplicador de um dano da cor `color` de `attacker` contra o herói `hero` do rival: +ElementStrong para
        // cada cor do herói que a cor vence, -ElementWeak para cada cor do herói que vence a cor. Nunca menos de 0,25.
        public double ElementMult(BattleState st, Side attacker, int color, int hero)
        {
            if (hero < 0 || color < 0) return 1;
            var cols = st.Hero(attacker.Opp(), hero).Colors;
            if (cols == null) return 1;
            int adv = 0, dis = 0;
            foreach (int c in cols) { if (Beats(color, c)) adv++; if (Beats(c, color)) dis++; }
            return Math.Max(0.25, 1 + R.ElementStrong * adv - R.ElementWeak * dis);
        }

        // ---------------------------------------------------------------- dano e cura

        // O dano `amount` de `attacker` no alvo dele (ou no primeiro vivo). Devolve quanto tirou de fato (o que passa
        // da vida do herói se perde) e qual herói levou. Se ele cair, o alvo de `attacker` volta a -1.
        public int Hit(BattleState st, Side attacker, int amount, out int hero)
        {
            hero = EffectiveTarget(st, attacker);
            if (hero < 0 || amount <= 0) return 0;
            int o = (int)attacker.Opp();
            if (attacker == Side.Player)                                       // o Ídolo do Rei e o rival marcado do Acampamento
            {
                double more = st.DmgBonus + (hero == st.Marked ? st.MarkBonus : 0);
                if (more > 0) amount = JsMath.RoundInt(amount * (1 + more));
            }
            int dealt = Math.Min(amount, st.HeroHp[o][hero]);
            st.HeroHp[o][hero] -= dealt;
            SyncHp(st, attacker.Opp());
            if (st.HeroHp[o][hero] <= 0) st.Target[(int)attacker] = -1;
            return dealt;
        }

        // O herói vivo de `side` mais ferido (menor fração da vida), ou -1.
        public static int MostWounded(BattleState st, Side side)
        {
            int best = -1; double frac = 2;
            for (int i = 0; i < Heroes; i++)
            {
                if (!Alive(st, side, i)) continue;
                double f = (double)st.HeroHp[(int)side][i] / st.HeroMax[(int)side][i];
                if (f < frac) { frac = f; best = i; }
            }
            return best;
        }

        // A cura de um poder: vai inteira para o herói vivo mais ferido (sem passar do máximo dele).
        public int HealOne(BattleState st, Side side, int amount, out int hero)
        {
            hero = MostWounded(st, side);
            if (hero < 0 || amount <= 0) return 0;
            int s = (int)side;
            int before = st.HeroHp[s][hero];
            st.HeroHp[s][hero] = Math.Min(st.HeroMax[s][hero], before + amount);
            SyncHp(st, side);
            return st.HeroHp[s][hero] - before;
        }

        // Uma cura espalhada (runas, eventos): um ponto por vez, sempre para o vivo mais ferido. Devolve o total curado.
        public int HealSpread(BattleState st, Side side, int amount)
        {
            int s = (int)side, healed = 0;
            for (int k = 0; k < amount; k++)
            {
                int h = MostWounded(st, side);
                if (h < 0 || st.HeroHp[s][h] >= st.HeroMax[s][h]) break;
                st.HeroHp[s][h]++; healed++;
            }
            SyncHp(st, side);
            return healed;
        }

        // A vida que falta no herói vivo mais ferido (para a IA decidir se vale curar).
        public static int MissingOfMostWounded(BattleState st, Side side)
        {
            int h = MostWounded(st, side);
            return h < 0 ? 0 : st.HeroMax[(int)side][h] - st.HeroHp[(int)side][h];
        }

        public static int[][] CloneHeroHp(BattleState st) => new[] { (int[])st.HeroHp[0].Clone(), (int[])st.HeroHp[1].Clone() };
    }
}

namespace Cubo.Domain
{
    // O Juramento: o voto do jogador nesta batalha, conferido pelo estado (rodadas, poderes usados, quem caiu).
    public sealed partial class BattleEngine
    {
        public const string VowSwift = "swift", VowSilent = "silent", VowUnbroken = "unbroken";

        // O voto ainda está de pé? (no fim da batalha vencida: foi cumprido?)
        public bool VowHolds(BattleState st)
        {
            switch (st.Vow)
            {
                case VowSwift: return st.Round <= Data.Run.Event.VowRounds;
                case VowSilent: return st.PlayerCasts == 0;
                case VowUnbroken:
                    for (int i = 0; i < Heroes; i++) if (st.StartAlive[i] && !Alive(st, Side.Player, i)) return false;
                    return true;
                default: return false;
            }
        }
    }
}
