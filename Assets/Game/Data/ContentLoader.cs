using System.Collections.Generic;
using System.Linq;
using Cubo.Domain;
using Newtonsoft.Json.Linq;

namespace Cubo.Data
{
    public static class ContentLoader
    {
        public static GameData Load(IContentSource src)
        {
            var d = new GameData();
            d.Rules = ReadRules(JObject.Parse(src.Read("rules")));

            var heroes = JObject.Parse(src.Read("heroes"));
            var pool = (JArray)heroes["player"];
            d.Heroes = new Dictionary<string, HeroDef>();
            d.HeroIds = new string[pool.Count];
            for (int i = 0; i < pool.Count; i++)
            {
                var h = ReadHero(pool[i]);
                d.Heroes[h.Id] = h; d.HeroIds[i] = h.Id;
            }
            d.DefaultTeam = ((JArray)heroes["defaultTeam"]).Select(t => (string)t).ToArray();
            d.EnemyTeam = ((JArray)heroes["enemyTeam"]).Select(t => ReadHero(t)).ToArray();

            ReadRun(d, src);
            d.Upgrades = ReadUpgrades(JObject.Parse(src.Read("upgrades")));
            foreach (var e in JArray.Parse(src.Read("equipment")))
            {
                var eq = new EquipDef
                {
                    Id = (string)e["id"], Kind = (string)e["kind"], N = e["n"] == null ? 0 : (int)e["n"], V = Num(e["v"]),
                    Turns = e["turns"] == null ? 0 : (int)e["turns"]
                };
                d.Equipment.Add(eq); d.EquipById[eq.Id] = eq;
            }
            return d;
        }

        private static double[] Doubles(JToken t) => ((JArray)t).Select(x => (double)x).ToArray();

        // Corrida, runas e eventos.
        private static void ReadRun(GameData d, IContentSource src)
        {
            var j = JObject.Parse(src.Read("run"));
            d.Run = new RunRules
            {
                Floors = (int)j["floors"], HealFrac = (double)j["healFrac"],
                EnemyDmg = Doubles(j["enemyDmg"]), EnemyScale = Doubles(j["enemyScale"]),
                Levels = ((JArray)j["levels"]).Select(x => (string)x).ToArray(),
                GoldBase = (int)j["goldBase"], GoldPerFloor = (int)j["goldPerFloor"],
                ShopRunes = (int)j["shopRunes"], RuneSlots = (int)j["runeSlots"],
                ShopUpgrades = (int)j["shopUpgrades"], BlessingSize = (int)j["blessingSize"], UpgradePrices = Ints(j["upgradePrices"]),
                EliteHp = (double)j["elite"]["hp"], EliteDmg = (double)j["elite"]["dmg"],
                EliteGold = (double)j["elite"]["gold"],
                EliteCombo = (double?)j["elite"]["combo"] ?? 0, BossCombo = (double?)j["bossCombo"] ?? 0,
                RerollBase = j["reroll"] != null ? (int)j["reroll"]["base"] : 0,
                RerollStep = j["reroll"] != null ? (int)j["reroll"]["step"] : 0,
                Sale = j["sale"] != null ? (double)j["sale"] : 0,
                PotionHp = j["potion"] != null ? (int)j["potion"]["hp"] : 0,
                PotionPrice = j["potion"] != null ? (int)j["potion"]["price"] : 0,
            };


            d.Runes = new List<RuneDef>();
            d.RuneById = new Dictionary<string, RuneDef>();
            foreach (var r in (JArray)JArray.Parse(src.Read("runes")))
            {
                var rune = new RuneDef
                {
                    Id = (string)r["id"], Kind = (string)r["kind"], Price = (int)r["price"],
                    N = r["n"] == null ? 0 : (int)r["n"], V = r["v"] == null ? 0 : (double)r["v"]
                };
                d.Runes.Add(rune); d.RuneById[rune.Id] = rune;
            }


            var ev = JObject.Parse(src.Read("events"));
            d.EventIds = ((JArray)ev["ids"]).Select(x => (string)x).ToArray();
            var n = (JObject)ev["numbers"];
            d.Run.Event = new EventRules
            {
                PerRun = ev["perRun"] != null ? (int)ev["perRun"] : 3,
                IdolBlood = (int)n["idolBlood"], IdolCrit = (double)n["idolCrit"], IdolWrath = (double)n["idolWrath"],
                IdolDmg = (double)n["idolDmg"], IdolWard = (double)n["idolWard"], IdolMana = (int)n["idolMana"],
                IdolCost = (int)n["idolCost"], IdolHp = (int)n["idolHp"],
                Idols = ev["idols"] is JArray idols ? idols.Select(x => (string)x).ToArray() : new[] { "rei", "rainha", "torre" },
                CampAmbush = (double)n["campAmbush"], CampMark = (double)n["campMark"],
                CampRaid = (int)n["campRaid"], VowGold = (int)n["vowGold"], VowRounds = (int)n["vowRounds"]
            };
        }

        // As melhorias de habilidade: cada nível soma custo, quantidade, crítico e chance; ★ traz efeitos.
        private static Dictionary<string, UpgradeTier[]> ReadUpgrades(JObject j)
        {
            var outp = new Dictionary<string, UpgradeTier[]>();
            // Os itens que melhoram (veneno e fogo): n = dano por turno a mais, turns = turnos a mais, area = lado a mais.
            if (j["equipment"] is JObject eq)
                foreach (JProperty a in eq.Properties())
                    outp[a.Name] = ((JArray)a.Value).Select(t => new UpgradeTier
                    {
                        Amount = t["n"] == null ? 0 : (int)t["n"],
                        Turns = t["turns"] == null ? 0 : (int)t["turns"],
                        Area = t["area"] == null ? 0 : (int)t["area"]
                    }).ToArray();
            foreach (JProperty a in ((JObject)j["abilities"]).Properties())
                outp[a.Name] = ((JArray)a.Value).Select(t => new UpgradeTier
                {
                    Cost = t["cost"] == null ? 0 : (int)t["cost"], Amount = t["amount"] == null ? 0 : (int)t["amount"],
                    Crit = Num(t["crit"]), V = Num(t["v"]),
                    Star = t["star"] is JArray st ? st.Select(f => new StarFx
                    {
                        Type = (string)f["type"], V = Num(f["v"]), N = f["n"] == null ? 0 : (int)f["n"],
                        Turns = f["turns"] == null || f["turns"].Type == JTokenType.Null ? (int?)null : (int)f["turns"]
                    }).ToArray() : new StarFx[0]
                }).ToArray();
            return outp;
        }

        private static int[] Ints(JToken t) => t == null ? null : ((JArray)t).Select(x => (int)x).ToArray();
        private static string Str(JToken t) => t == null || t.Type == JTokenType.Null ? null : (string)t;
        private static double Num(JToken t) => t == null || t.Type == JTokenType.Null ? 0 : (double)t;

        private static Dictionary<int, int> IntMap(JToken t)
        {
            var m = new Dictionary<int, int>();
            foreach (JProperty p in ((JObject)t).Properties()) m[int.Parse(p.Name)] = (int)p.Value;
            return m;
        }

        private static Rules ReadRules(JObject j)
        {
            var p = (JObject)j["passives"];
            var r = new Rules
            {
                NumColors = (int)j["numColors"], MinGroup = (int)j["minGroup"],
                CascadeMin = j["cascadeMin"] != null ? (int)j["cascadeMin"] : 4,
                BonusFrom = j["bonusFrom"] != null ? (int)j["bonusFrom"] : 3,
                DamageMin = j["damageMin"] != null ? (int)j["damageMin"] : 3,
                DamagePerPiece = (int)j["damagePerPiece"],
                ManaPerPiece = (int)j["manaPerPiece"], GroupBonus = (int)j["groupBonus"], ManaCap = (int)j["manaCap"],
                RivalHpBonus = (double)j["rivalHpBonus"],
                CritChance = new[] { (double)j["critChance"]["player"], (double)j["critChance"]["enemy"] },
                CritMax = (double)j["critMax"], CritMultiplier = (int)j["critMultiplier"],
                ComboChance = new[] { (double)j["comboChance"]["player"], (double)j["comboChance"]["enemy"] },
                ComboMax = (double)j["comboMax"],
                SecondMana = (int)j["secondMana"],
                DefaultSize = (int)j["defaultSize"], Sizes = Ints(j["sizes"]),
                HpEasy = IntMap(j["hpEasy"]), HpHard = IntMap(j["hpHard"]),
                ChargeMinGroup = (int)p["chargeMinGroup"], ChargeBonus = (double)p["chargeBonus"],
                DecreeDiscount = (int)p["decreeDiscount"],
                CommunionMana = (int)p["communionMana"]
            };
            if (j["hazards"] is JObject hz)
            {
                r.HazardMin = (int)hz["min"];
                r.HazardArea = (int)hz["area"];
                r.HazardRounds = (int)hz["rounds"];
            }
            if (j["elements"] is JObject el)
            {
                r.Beats = Ints(el["beats"]);
                r.ElementStrong = (double)el["strong"];
                r.ElementWeak = (double)el["weak"];
            }
            r.CostScale = new Dictionary<string, Dictionary<int, double>>();
            foreach (JProperty mode in ((JObject)j["costScale"]).Properties())
            {
                var m = new Dictionary<int, double>();
                foreach (JProperty q in ((JObject)mode.Value).Properties()) m[int.Parse(q.Name)] = (double)q.Value;
                r.CostScale[mode.Name] = m;
            }
            r.AiLevels = new Dictionary<string, int>();
            foreach (JProperty q in ((JObject)j["ai"]["levels"]).Properties()) r.AiLevels[q.Name] = (int)q.Value;
            r.AiHandLook = new Dictionary<string, int>();
            foreach (JProperty q in ((JObject)j["ai"]["handLook"]).Properties()) r.AiHandLook[q.Name] = (int)q.Value;
            return r;
        }

        private static HeroDef ReadHero(JToken j)
        {
            var h = new HeroDef { Id = (string)j["id"], Passive = Str(j["passive"]), Colors = Ints(j["colors"]) };
            h.Powers = ((JArray)j["powers"]).Select(p => new PowerDef
            {
                Id = (string)p["id"], Index = (int)p["index"], Color = (int)p["color"], Kind = (string)p["kind"],
                Cost = (int)p["cost"], Amount = p["amount"] == null ? 0 : (int)p["amount"],
                Crit = Num(p["crit"]), V = Num(p["v"]), PurgeColors = Ints(p["purgeColors"])
            }).ToArray();
            return h;
        }


    }
}