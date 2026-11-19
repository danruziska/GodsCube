using System;
using System.Collections.Generic;
using System.Linq;

namespace Cubo.Domain
{
    // A escolha do rival (e da IA dos testes): o grupo, os poderes e a face que entrega.
    public sealed partial class BattleEngine
    {
        // Embaralha no lugar (Fisher-Yates, de trás para frente).
        public static List<T> Shuffle<T>(List<T> list, Mulberry32 rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                var tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
            return list;
        }

        private int Look(Dictionary<string, int> table, string level, string fallback) =>
            table.TryGetValue(level ?? "", out var lv) && lv != 0 ? lv : table[fallback];

        // `level`: "distraido", "atento", "implacavel" (quantos grupos da frente ele compara antes de escolher;
        // o distraído olha só alguns, sorteados). Devolve o grupo escolhido, ou null se não há nenhum.
        public MoveEval ChooseMove(BattleState st, Side side, string level, Mulberry32 rng)
        {
            var g = Geometry.For(st.N);
            var all = Shuffle(EvaluateMoves(st, g, side), rng);
            int look = Math.Max(1, Look(R.AiLevels, level, "atento"));
            MoveEval best = null;
            for (int k = 0; k < all.Count && k < look; k++)
                if (best == null || all[k].Score > best.Score) best = all[k];
            return best;
        }

        // Quem `side` ataca: o herói do rival contra quem o melhor grupo da frente rende mais (os elementos contam),
        // com preferência por derrubar alguém já neste toque. O distraído escolhe ao acaso. -1 se não há ninguém de pé.
        public int ChooseTarget(BattleState st, Side side, string level, Mulberry32 rng)
        {
            var alive = new List<int>();
            for (int i = 0; i < Heroes; i++) if (Alive(st, side.Opp(), i)) alive.Add(i);
            if (alive.Count == 0) return -1;
            if (level == "distraido") return alive[rng.NextInt(alive.Count)];
            var g = Geometry.For(st.N);
            int keep = st.Target[(int)side], best = alive[0];
            double bestScore = double.MinValue;
            foreach (int h in alive)
            {
                st.Target[(int)side] = h;
                double score = 0;
                foreach (var m in EvaluateMoves(st, g, side)) score = Math.Max(score, m.Score + (m.Mine >= st.HeroHp[(int)side.Opp()][h] ? 100 : 0));
                score -= st.HeroHp[(int)side.Opp()][h] * 0.01;                  // no empate, o mais fraco
                if (score > bestScore) { bestScore = score; best = h; }
            }
            st.Target[(int)side] = keep;
            return best;
        }

        // O alvo único que `side` anuncia para o PRÓXIMO turno dele (o aviso que o jogador vê), escolhido sem saber que
        // face vai receber: o herói do rival que as cores do time de `side` mais machucam (elementos), com peso para quem
        // já está ferido (para derrubar). O distraído vai no primeiro de pé. Não usa o gerador: refazer a batalha dá o mesmo.
        public int IntentTarget(BattleState st, Side side, string level)
        {
            int foe = (int)side.Opp(), best = -1;
            double bestScore = double.MinValue;
            for (int h = 0; h < Heroes; h++)
            {
                if (!Alive(st, side.Opp(), h)) continue;
                if (level == "distraido") return h;
                double el = 0; int n = 0;
                for (int k = 0; k < Heroes; k++)
                {
                    if (!Alive(st, side, k) || st.Hero(side, k).Colors == null) continue;
                    foreach (int c in st.Hero(side, k).Colors) { el += ElementMult(st, side, c, h); n++; }
                }
                el = n > 0 ? el / n : 1;
                double hurt = 1 - (double)st.HeroHp[foe][h] / st.HeroMax[foe][h];
                double score = el + 0.8 * hurt - 0.001 * st.HeroHp[foe][h];
                if (score > bestScore) { bestScore = score; best = h; }
            }
            return best;
        }

        // O dano do melhor grupo que o outro lado teria na frente se recebesse a face física `face`
        // (sem girar). Gira uma cópia das cores; o estado não muda.
        public int BestAfter(BattleState st, int face, Side receiver)
        {
            var g = Geometry.For(st.N);
            var m = TurnToFront(st.N, face);
            if (m == null) return 0;
            var b = BestGroup(Apply(g, st.Colors, m), g, st, receiver);
            return b == null ? 0 : b.Value;
        }

        // A face que `side` entrega: entre as permitidas, a que deixa o pior grupo para o rival. O nível decide
        // quantas faces ele compara (o distraído olha só algumas, sorteadas).
        public int ChooseHand(BattleState st, Side side, string level, Mulberry32 rng)
        {
            var opts = Shuffle(HandOptions(st, side), rng);
            int look = Math.Max(1, Look(R.AiHandLook, level, "atento"));
            int best = -1, bestV = int.MaxValue;
            for (int k = 0; k < opts.Count && k < look; k++)
            {
                int v = BestAfter(st, opts[k], side.Opp());
                if (v < bestV) { best = opts[k]; bestV = v; }
            }
            return best;
        }

        // Poderes que o rival usa antes de cada disparo: os que a mana paga e que servem agora.
        public List<int> EnemyCasts(BattleState st)
        {
            var list = new List<int>();
            if (st.Silenced[1]) return list;
            for (int s = 0; s < Slots; s++)
            {
                var p = st.Power(Side.Enemy, s);
                if (st.Mana[1][s] < CostOf(st, Side.Enemy, s)) continue;
                if (p.Kind == "heal" && MissingOfMostWounded(st, Side.Enemy) < p.Amount) continue;
                if (p.Kind == "purge" && PurgeCount(st, Side.Enemy, s) == 0) continue;
                if (p.Kind == "critUp" && CritChance(st, Side.Enemy) >= R.CritMax) continue;
                if (p.Kind == "critNext" && st.FoeFx != null && st.FoeFx.NextCrit == true) continue;
                list.Add(s);
            }
            return list;
        }
    }
}