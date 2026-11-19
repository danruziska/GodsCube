using System;
using System.Collections.Generic;
using System.Linq;
using Cubo.Domain;

namespace Cubo.App
{
    // O que a interface mostra, já calculado e já com texto. A tela só liga estes campos a componentes:
    // nenhuma conta de custo, mana ou idioma acontece em código de Unity.
    public sealed class PowerView
    {
        public int Slot, Hero, Color, Cost, Mana;
        public string HeroId, HeroName, Name, Domain, Effect, Detail;
        public float Fill;                 // 0 a 1: a barra de mana
        public bool Enabled;               // dá para usar agora
    }

    // Um efeito num herói (veneno, queimadura e os que vierem): Kind é o id do efeito ("poison", "burn").
    public sealed class StatusView
    {
        public string Kind;
        public int Turns, Dmg;             // turnos que faltam, dano por turno
        public string Detail;              // o texto do balão ao tocar no ícone: o nome, quanto tira e quantas rodadas faltam
    }

    // Um herói na luta (3 contra 3): a barra de vida em cima da cabeça, se está de pé e se é o alvo de alguém.
    public sealed class FighterView
    {
        public string HeroId, Name;
        public int Hp, Max;
        public bool Alive, Targeted;       // Targeted: é o alvo do outro lado (num herói do jogador: o aviso do próximo ataque do rival)
        public int[] Colors;               // os dois elementos (cores) do herói
        public string Elements;            // "Fogo · Folha": os nomes dos elementos, para um texto de ajuda
        public List<StatusView> Statuses = new List<StatusView>();   // os efeitos ativos, na ordem em que começaram
    }

    public sealed class HudView
    {
        public FighterView[] PlayerTeam, EnemyTeam;   // 3 cada, na ordem do time
        public bool NeedsTarget;           // é a vez do jogador e ele ainda não escolheu o rival
        public PowerView[] Powers;
        public PowerView[] EnemyPowers;    // os do rival: só para ler (custo, mana, efeito); nunca usáveis
        public List<string> Pills;         // efeitos ativos, em etiquetas curtas
        public string TurnText;
        public string CritPlayer, CritEnemy;   // "Crítico 5%", com a marca do próximo crítico garantido
        public string ComboPlayer, ComboEnemy; // "Combo 20%"; vazio quando a chance é 0
        public string HandText;            // na fase de entrega: o que fazer e qual face está proibida
        public bool Over, Handing;
    }

    public static class HudModel
    {
        public static HudView Build(BattleSession s, Texts t)
        {
            var st = s.State;
            var e = s.Engine;
            var v = new HudView
            {
                Powers = new PowerView[BattleEngine.Slots],
                EnemyPowers = new PowerView[BattleEngine.Slots],
                Pills = Pills(e, st, t),
                Over = st.Winner != null,
                Handing = s.IsHanding,
                CritPlayer = CritText(e, st, Side.Player, t),
                CritEnemy = CritText(e, st, Side.Enemy, t),
                ComboPlayer = ComboText(e, st, Side.Player, t),
                ComboEnemy = ComboText(e, st, Side.Enemy, t)
            };

            for (int slot = 0; slot < BattleEngine.Slots; slot++)
            {
                v.Powers[slot] = PowerOf(e, st, t, Side.Player, slot, s.CanCast(slot));
                v.EnemyPowers[slot] = PowerOf(e, st, t, Side.Enemy, slot, false);
            }


            v.PlayerTeam = Team(st, t, Side.Player);
            v.EnemyTeam = Team(st, t, Side.Enemy);
            v.NeedsTarget = s.NeedsTarget;
            v.TurnText = TurnText(s, t);
            v.HandText = v.Handing ? HandText(s, t) : "";
            return v;
        }

        public static string FaceName(Texts t, int face) => t.T("face." + face);

        private static PowerView PowerOf(BattleEngine e, BattleState st, Texts t, Side side, int slot, bool enabled)
        {
            var hero = st.Hero(side, slot >> 1);
            var p = st.Power(side, slot);
            int cost = e.CostOf(st, side, slot), mana = st.Mana[(int)side][slot];
            string effect = PowerEffect(p, t);
            string heroName = t.Name("hero." + hero.Id);
            string name = t.Name("power." + p.Id) + TierTag(p.Tier, p.Star.Length > 0);
            return new PowerView
            {
                Slot = slot, Hero = slot >> 1, Color = p.Color, Cost = cost, Mana = mana,
                HeroId = hero.Id, HeroName = heroName, Name = name, Domain = t.Domain(p.Color),
                Effect = effect,
                Detail = heroName + " · " + name + ": " + effect + ". " + t.T("tip.mana", ("m", mana), ("c", cost)),
                Fill = cost <= 0 ? 1f : Math.Min(1f, mana / (float)cost),
                Enabled = enabled
            };
        }

        // " +", " ++", " +++" ou " ★" depois do nome de uma habilidade melhorada; "" no nível de base.
        public static string TierTag(int tier, bool star) => tier <= 0 ? "" : star ? " ★" : " " + new string('+', tier);

        // O que o poder faz, com o que o nível ★ acrescenta.
        public static string PowerEffect(PowerDef p, Texts t)
        {
            string main = BaseEffect(p, t);
            foreach (var fx in p.Star) main += "; ★ " + StarText(fx, t);
            return main;
        }

        public static string StarText(StarFx fx, Texts t)
        {
            int pct = JsMath.RoundInt(fx.V * 100);
            switch (fx.Type)
            {
                case "focus": return t.T("star.focus", ("n", fx.N));
                case "chainMana": return t.T("star.chainMana", ("n", JsMath.RoundInt(fx.V)));
                case "chainDmg": return t.T("star.chainDmg", ("p", pct));
                case "combo": return t.T(fx.Turns == null ? "star.combo" : "star.combo.1", ("p", pct));
                default: return fx.Type;
            }
        }

        private static string BaseEffect(PowerDef p, Texts t)
        {
            switch (p.Kind)
            {
                case "damage":
                    return p.Crit > 0 ? t.T("eff.dmgcrit.me", ("n", p.Amount), ("p", JsMath.RoundInt(p.Crit * 100))) : t.T("eff.dmg.me", ("n", p.Amount));
                case "heal": return t.T("eff.heal.me", ("n", p.Amount));
                case "extra": return t.T("eff.extra.me");
                case "critUp": return t.T("eff.critUp.me", ("p", JsMath.RoundInt(p.V * 100)));
                case "critNext": return t.T("eff.critNext.me");
                default:
                    var names = p.PurgeColors.Select(t.Domain).ToList();
                    string d = names.Count > 1 ? string.Join(", ", names.Take(names.Count - 1)) + t.T("and") + names[names.Count - 1] : names[0];
                    return t.T("eff.purge.me", ("d", d));
            }
        }

        // "Crítico 5%", mais a marca quando o próximo ataque já é crítico.
        public static string CritText(BattleEngine e, BattleState st, Side side, Texts t)
        {
            string text = t.T("crit.chance", ("p", JsMath.RoundInt(e.CritChance(st, side) * 100)));
            var fx = st.FxOf(side);
            if (fx != null && fx.NextCrit == true) text += " · " + t.T("crit.next");
            return text;
        }

        // "Combo 20%": a chance de uma peça nova copiar a cor de uma vizinha. Vazio com 0 (o começo da corrida).
        public static string ComboText(BattleEngine e, BattleState st, Side side, Texts t)
        {
            int p = JsMath.RoundInt(e.ComboChance(st, side) * 100);
            return p > 0 ? t.T("combo.chance", ("p", p)) : "";
        }


        // Os efeitos que estão valendo, como etiquetas curtas.
        public static List<string> Pills(BattleEngine e, BattleState st, Texts t)
        {
            var outp = new List<string>();
            if (st.Fx != null)
            {
                foreach (var type in new[] { "ward", "dmg", "drain", "crit", "combo" })
                    foreach (var f in st.Fx.List(type))
                    {
                        string key = type == "dmg" && f.V < 0 ? "pill.dmg.down" : "pill." + type;
                        outp.Add(t.T(key, ("p", Math.Abs(JsMath.RoundInt(f.V * 100)))) + (f.Turns == null ? "" : " · " + f.Turns));
                    }
                int echo = (int)Math.Round(st.Fx.List("echo").Sum(f => f.V));
                if (echo != 0) outp.Add(t.T("pill.echo", ("n", echo)));
                foreach (var f in st.Fx.List("focus")) outp.Add(t.T("pill.focus", ("n", (int)f.V)) + (f.Turns == null ? "" : " · " + f.Turns));
                double chainDmg = st.Fx.List("chainDmg").Sum(f => f.V);
                if (chainDmg > 0) outp.Add(t.T("pill.chainDmg", ("p", JsMath.RoundInt(chainDmg * 100))));
                int chainMana = (int)Math.Round(st.Fx.List("chainMana").Sum(f => f.V));
                if (chainMana != 0) outp.Add(t.T("pill.chainMana", ("n", chainMana)));
                if (st.Fx.NextCrit == true) outp.Add(t.T("pill.nextCrit"));
            }
            if (st.Silenced[1]) outp.Add(t.T("pill.foe.silenced"));
            // o voto do Juramento: o que falta cumprir, ou "quebrado"
            if (!string.IsNullOrEmpty(st.Vow))
                outp.Add(e.VowHolds(st) ? t.T("pill.vow." + st.Vow, ("r", e.Data.Run.Event.VowRounds), ("n", st.Round)) : t.T("pill.vow.broken"));
            return outp;
        }

        private static FighterView[] Team(BattleState st, Texts t, Side side)
        {
            var outp = new FighterView[BattleEngine.Heroes];
            int s = (int)side, foe = (int)side.Opp();
            for (int i = 0; i < BattleEngine.Heroes; i++)
            {
                var h = st.Hero(side, i);
                outp[i] = new FighterView
                {
                    HeroId = h.Id, Name = t.Name("hero." + h.Id),
                    Hp = st.HeroHp[s][i], Max = st.HeroMax[s][i],
                    Alive = BattleEngine.Alive(st, side, i),
                    Targeted = st.Target[foe] == i && BattleEngine.Alive(st, side, i),
                    Colors = h.Colors,
                    Elements = h.Colors == null ? "" : string.Join(" · ", h.Colors.Select(c => t.Domain(c))),
                    Statuses = Enumerable.Range(0, st.Dots[s][i].Length)
                        .Where(k => st.Dots[s][i][k].Active)
                        .OrderBy(k => st.Dots[s][i][k].Since)
                        .Select(k => StatusOf(t, BattleEngine.DotKind(k), st.Dots[s][i][k]))
                        .ToList()
                };
                if (side == Side.Enemy && st.Marked == i && outp[i].Alive)      // o Acampamento: a marca fica antes dos outros efeitos
                    outp[i].Statuses.Insert(0, new StatusView
                    {
                        Kind = "mark",
                        Detail = t.T("status.mark") + "\n" + t.T("status.mark.desc", ("p", JsMath.RoundInt(st.MarkBonus * 100)))
                    });
            }
            return outp;
        }

        // Um efeito com o texto do balão: "Veneno / Perde 3 de vida no começo de cada rodada. / Faltam 2 rodadas."
        public static StatusView StatusOf(Texts t, string kind, Dot dot) => new StatusView
        {
            Kind = kind, Turns = dot.Turns, Dmg = dot.Dmg,
            Detail = t.T("status." + kind) + "\n" + t.T("status.dmg", ("n", dot.Dmg)) + "\n" +
                     t.T(dot.Turns == 1 ? "status.left.1" : "status.left.n", ("t", dot.Turns))
        };

        public static string TurnText(BattleSession s, Texts t)
        {
            var st = s.State;
            if (st.Winner != null) return t.T("turn.over");
            if (s.IsZoning) return t.T("turn.zone." + s.ZoneKind);
            if (st.Turn != Side.Player) return t.T("turn.foe");
            if (st.Phase == Phase.Hand) return t.T("turn.hand");
            if (s.NeedsTarget) return t.T("turn.target");
            string main = t.T("turn.can");
            return main + (st.MovesLeft > 1 ? t.T("turn.shots", ("n", st.MovesLeft)) : "");
        }

        // Na entrega: o que fazer e, se houver, a face que não pode ser entregue.
        public static string HandText(BattleSession s, Texts t)
        {
            int ban = s.BannedFace;
            string text = t.T("hand.how");
            if (Faces.IsNeighbor(ban)) text += " " + t.T("hand.ban", ("face", FaceName(t, ban)));
            return text;
        }
    }
}