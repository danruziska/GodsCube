using System;
using System.Collections.Generic;
using System.Linq;
using Cubo.Domain;
using Newtonsoft.Json.Linq;

namespace Cubo.App
{
    // Converte coleção, corrida e batalha de e para JSON. Ler é tolerante: o que não valer é descartado
    // (coleção) ou o arquivo inteiro é recusado (corrida e batalha, que devolvem null). Nunca lança exceção.
    public sealed class SaveCodec
    {
        public const int Version = 3;       // 3: sem cartas nem deck; corridas salvas antes disso não valem mais
        private readonly GameData _d;
        private readonly CollectionService _cols;

        public SaveCodec(GameData data, CollectionService cols) { _d = data; _cols = cols; }

        private static JArray Strs(IEnumerable<string> a) => new JArray((a ?? new string[0]).Select(x => (object)x).ToArray());
        private static List<string> StrList(JToken t) =>
            t is JArray a ? a.Select(x => x.Type == JTokenType.String ? (string)x : null).Where(x => x != null).ToList() : new List<string>();

        // ---- coleção ----

        public JObject ToJson(Collection c)
        {
            var hw = new JObject();
            foreach (var kv in c.HeroWins) hw[kv.Key] = kv.Value;
            return new JObject
            {
                { "v", c.V }, { "team", Strs(c.Team) },
                { "wins", c.Wins }, { "heroWins", hw }, { "elites", c.Elites }, { "bestCombo", c.BestCombo },
                { "equip", EquipJson(c.Equip) }
            };
        }

        public Collection CollectionFrom(JToken j)
        {
            var raw = new Collection();
            try
            {
                if (j is JObject o)
                {
                    raw.Team = StrList(o["team"]).ToArray();       // "cards" e "deck" (de versões antigas) são ignorados
                    raw.Wins = Int(o["wins"], 0); raw.Elites = Int(o["elites"], 0); raw.BestCombo = Int(o["bestCombo"], 0);
                    if (o["heroWins"] is JObject hw)
                        foreach (JProperty p in hw.Properties()) raw.HeroWins[p.Name] = Int(p.Value, 0);
                    raw.Equip = EquipFrom(o["equip"]);
                }
            }
            catch (Exception) { raw = new Collection(); }
            return _cols.Clean(raw);
        }

        private static int Int(JToken t, int dflt) =>
            t != null && (t.Type == JTokenType.Integer || t.Type == JTokenType.Float) ? (int)Math.Floor((double)t) : dflt;

        // ---- corrida ----

        public JObject ToJson(RunState r)
        {
            var o = new JObject
            {
                { "floor", r.Floor }, { "hp", r.Hp }, { "maxHp", r.MaxHp },
                { "heroHp", r.HeroHp == null ? JValue.CreateNull() : (JToken)new JArray(r.HeroHp) },
                { "eventFloors", new JArray(r.EventFloors) }, { "eventOrder", Strs(r.EventOrder) },
                { "bonusCrit", r.BonusCrit }, { "bonusCombo", r.BonusCombo }, { "maxHpMod", new JArray(r.MaxHpMod) },
                { "nextEnemyHp", r.NextEnemyHp }, { "nextMark", r.NextMark },
                { "idol", r.Idol == null ? JValue.CreateNull() : (JToken)r.Idol },
                { "bonusDmg", r.BonusDmg }, { "bonusWard", r.BonusWard }, { "bonusMana", r.BonusMana }, { "bonusCost", r.BonusCost },
                { "vow", r.Vow == null ? JValue.CreateNull() : (JToken)r.Vow },
                { "heroMax", r.HeroMax == null ? JValue.CreateNull() : (JToken)new JArray(r.HeroMax) }, { "gold", r.Gold }, { "team", Strs(r.Team) },
                { "runes", Strs(r.Runes) }, { "elite", r.Elite }, { "door", r.Door }, { "curse", r.Curse },
                { "tiers", TiersJson(r.Tiers) }, { "equip", EquipJson(r.Equip) },
                { "blessing", r.Blessing == null ? JValue.CreateNull() : (JToken)Strs(r.Blessing) }
            };
            o["shop"] = r.Shop == null ? JValue.CreateNull() : (JToken)new JObject
            {
                { "runes", Strs(r.Shop.Runes) }, { "upgrades", Strs(r.Shop.Upgrades) }, { "sold", Strs(r.Shop.Sold) },
                { "rerolls", r.Shop.Rerolls }, { "sale", r.Shop.Sale == null ? JValue.CreateNull() : (JToken)r.Shop.Sale }
            };
            if (r.Event == null) o["event"] = JValue.CreateNull();
            else
            {
                var e = new JObject { { "id", r.Event.Id } };
                if (r.Event.Variant != null) e["variant"] = r.Event.Variant;
                if (r.Event.Done != null)
                {
                    var d = r.Event.Done;
                    e["done"] = new JObject { { "key", d.Key }, { "rune", d.Rune }, { "won", d.Won ?? false } };
                }
                o["event"] = e;
            }
            return o;
        }

        // null = esta corrida não vale (o jogo volta à preparação).
        public RunState RunFrom(JToken j)
        {
            try
            {
                if (!(j is JObject o)) return null;
                int floor = Int(o["floor"], 0);
                if (floor < 1 || floor > _d.Run.Floors) return null;
                var team = StrList(o["team"]).ToArray();
                if (!_d.ValidTeam(team)) return null;
                var r = new RunState
                {
                    Floor = floor, Team = team,
                    Hp = Math.Max(0, Int(o["hp"], 0)), MaxHp = Math.Max(0, Int(o["maxHp"], 0)), Gold = Math.Max(0, Int(o["gold"], 0)),
                    Runes = StrList(o["runes"]).Where(id => _d.RuneById.ContainsKey(id)).Take(_d.Run.RuneSlots).ToList(),
                    Elite = (bool?)o["elite"] == true, Door = (bool?)o["door"] == true
                };
                r.Tiers = TiersFrom(o["tiers"]);
                r.Equip = _d.CleanEquip(EquipFrom(o["equip"]), team);
                r.EventFloors = o["eventFloors"] is JArray ef ? ef.Where(x => x.Type == JTokenType.Integer).Select(x => (int)x)
                                    .Where(f => f >= 2 && f <= _d.Run.Floors).Distinct().ToList() : new List<int>();
                r.EventOrder = StrList(o["eventOrder"]).Where(id => Array.IndexOf(_d.EventIds, id) >= 0).Distinct().ToList();
                r.BonusCrit = Math.Max(0, Math.Min(1, Dbl(o["bonusCrit"])));
                r.BonusCombo = Math.Max(0, Math.Min(1, Dbl(o["bonusCombo"])));
                r.MaxHpMod = ModArray(o["maxHpMod"]) ?? new int[3];
                double nh = Dbl(o["nextEnemyHp"]); r.NextEnemyHp = nh > 0 && nh <= 1 ? nh : 1;
                r.NextMark = Math.Max(0, Math.Min(1, Dbl(o["nextMark"])));
                r.Idol = Str(o["idol"]) is string idol && Array.IndexOf(_d.Run.Event.Idols, idol) >= 0 ? idol : null;
                r.BonusDmg = Math.Max(0, Math.Min(2, Dbl(o["bonusDmg"])));
                r.BonusWard = Math.Max(0, Math.Min(0.8, Dbl(o["bonusWard"])));
                r.BonusMana = Math.Max(0, Math.Min(99, Int(o["bonusMana"], 0)));
                r.BonusCost = Math.Max(0, Math.Min(9, Int(o["bonusCost"], 0)));
                r.Vow = Str(o["vow"]) is string rv && IsVow(rv) ? rv : null;
                r.HeroMax = HeroArray(o["heroMax"]);
                r.HeroHp = r.HeroMax == null ? null : HeroArray(o["heroHp"]);
                if (r.HeroHp == null) r.HeroMax = null;
                else for (int i = 0; i < 3; i++) r.HeroHp[i] = Math.Min(r.HeroHp[i], Math.Max(1, r.HeroMax[i]));
                RunService.SyncRunHp(r);
                if (o["blessing"] is JArray bl)
                {
                    var ids = StrList(bl).Where(id => _d.Upgrades.ContainsKey(id)).Take(6).ToList();
                    if (ids.Count > 0) r.Blessing = ids;
                }
                double curse = o["curse"] != null && (o["curse"].Type == JTokenType.Float || o["curse"].Type == JTokenType.Integer) ? (double)o["curse"] : 0;
                r.Curse = curse >= 1 && curse <= 3 ? curse : 0;
                if (o["shop"] is JObject sh)
                {
                    var runes = StrList(sh["runes"]);
                    if (runes.All(id => _d.RuneById.ContainsKey(id)))
                        r.Shop = new ShopState
                        {
                            Runes = runes.Take(6).ToList(),
                            Upgrades = StrList(sh["upgrades"]).Where(id => _d.Upgrades.ContainsKey(id)).Take(6).ToList(),
                            Sold = StrList(sh["sold"]).Where(id => _d.RuneById.ContainsKey(id) || _d.Upgrades.ContainsKey(id) || id == RunService.Potion).ToList(),
                            Rerolls = Math.Max(0, Math.Min(99, Int(sh["rerolls"], 0))),
                            Sale = Str(sh["sale"]) is string sale && (_d.RuneById.ContainsKey(sale) || _d.Upgrades.ContainsKey(sale)) ? sale : null
                        };
                }
                if (o["event"] is JObject ev && Array.IndexOf(_d.EventIds, (string)ev["id"]) >= 0)
                {
                    var es = new EventState { Id = (string)ev["id"] };
                    if (es.Id == "idolo")
                        es.Variant = Str(ev["variant"]) is string vr && Array.IndexOf(_d.Run.Event.Idols, vr) >= 0 ? vr : (_d.Run.Event.Idols.FirstOrDefault() ?? "rei");
                    if (ev["done"] is JObject d && d["key"] != null && d["key"].Type == JTokenType.String)
                    {
                        string rune = (string)d["rune"];
                        es.Done = new EventResult
                        {
                            Key = (string)d["key"], Rune = rune != null && _d.RuneById.ContainsKey(rune) ? rune : null,
                            Won = (bool?)d["won"] == true
                        };
                    }
                    r.Event = es;
                }
                return r;
            }
            catch (Exception) { return null; }
        }

        // Os níveis de melhoria: { "cavaleiro.0": 2, ... }. Lendo, só valem poderes que melhoram e níveis que existem.
        private static string Str(JToken t) => t == null || t.Type != JTokenType.String ? null : (string)t;

        private static bool IsVow(string v) => v == BattleEngine.VowSwift || v == BattleEngine.VowSilent || v == BattleEngine.VowUnbroken;

        private static double Dbl(JToken t) => t != null && (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) ? (double)t : 0;

        private static double? Chance(JToken t) => t != null && (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) ? Math.Max(0, Math.Min(1, (double)t)) : (double?)null;

        // Três inteiros (positivos ou negativos, a vida máxima a mais ou a menos de cada herói), ou null.
        private static int[] ModArray(JToken t)
        {
            if (!(t is JArray a) || a.Count != 3 || a.Any(x => x.Type != JTokenType.Integer)) return null;
            return a.Select(x => Math.Max(-500, Math.Min(500, (int)x))).ToArray();
        }

        // Três números inteiros >= 0 (a vida de cada herói), ou null.
        private static int[] HeroArray(JToken t)
        {
            if (!(t is JArray a) || a.Count != 3 || a.Any(x => x.Type != JTokenType.Integer)) return null;
            return a.Select(x => Math.Max(0, (int)x)).ToArray();
        }

        private static JObject TiersJson(Dictionary<string, int> tiers)
        {
            var o = new JObject();
            foreach (var kv in tiers) o[kv.Key] = kv.Value;
            return o;
        }

        private Dictionary<string, int> TiersFrom(JToken t)
        {
            var outp = new Dictionary<string, int>();
            if (t is JObject o)
                foreach (JProperty p in o.Properties())
                    if (_d.Upgrades.TryGetValue(p.Name, out var levels))
                    {
                        int v = Int(p.Value, 0);
                        if (v > 0) outp[p.Name] = Math.Min(levels.Length, v);
                    }
            return outp;
        }

        // Os equipamentos: { "rei": "elmo", ... }. Lendo, só strings; quem confere se valem é CleanEquip.
        private static JObject EquipJson(Dictionary<string, string> equip)
        {
            var o = new JObject();
            foreach (var kv in equip) o[kv.Key] = kv.Value;
            return o;
        }

        private static Dictionary<string, string> EquipFrom(JToken t)
        {
            var outp = new Dictionary<string, string>();
            if (t is JObject o)
                foreach (JProperty p in o.Properties())
                    if (p.Value.Type == JTokenType.String) outp[p.Name] = (string)p.Value;
            return outp;
        }

        // ---- batalha ----

        public JObject ToJson(BattleRecord b, IEnumerable<Command> commands)
        {
            return new JObject
            {
                { "seed", (long)b.Seed }, { "ai", b.AiLevel }, { "team", Strs(b.Team) }, { "size", b.Size },
                { "mode", b.Mode }, { "enemyScale", b.EnemyScale }, { "enemyDmg", b.EnemyDmg }, { "playerHp", b.PlayerHp },
                { "playerHeroHp", b.PlayerHeroHp == null ? JValue.CreateNull() : (JToken)new JArray(b.PlayerHeroHp) },
                { "heroMaxMod", b.PlayerHeroMaxMod == null ? JValue.CreateNull() : (JToken)new JArray(b.PlayerHeroMaxMod) },
                { "critPlayer", b.CritPlayer.HasValue ? (JToken)b.CritPlayer.Value : JValue.CreateNull() },
                { "comboPlayer", b.ComboPlayer.HasValue ? (JToken)b.ComboPlayer.Value : JValue.CreateNull() },
                { "markBonus", b.MarkBonus }, { "vow", b.Vow == null ? JValue.CreateNull() : (JToken)b.Vow },
                { "dmgBonus", b.DmgBonus }, { "ward", b.Ward }, { "startMana", b.StartMana }, { "costCut", b.CostCut },
                { "comboEnemy", b.ComboEnemy.HasValue ? (JToken)b.ComboEnemy.Value : JValue.CreateNull() },
                { "tiers", TiersJson(b.Tiers) }, { "equip", EquipJson(b.Equip) },
                { "boost", Strs(b.Boost) }, { "commands", Strs(commands.Select(c => c.Kind + ":" + c.A + ":" + c.B + ":" + c.C)) }
            };
        }

        public BattleRecord RecordFrom(JToken j)
        {
            try
            {
                if (!(j is JObject o)) return null;
                var team = StrList(o["team"]).ToArray();
                if (!_d.ValidTeam(team)) return null;
                int size = Int(o["size"], 0);
                if (Array.IndexOf(Settings.Sizes, size) < 0) return null;
                string mode = (string)o["mode"], ai = (string)o["ai"];       // "bias" (de versões antigas) é ignorado
                if ((mode != "facil" && mode != "dificil") || !_d.Rules.AiLevels.ContainsKey(ai ?? "")) return null;
                var b = new BattleRecord
                {
                    Seed = (uint)(long)o["seed"], AiLevel = ai, Team = team, Size = size, Mode = mode,
                    EnemyScale = (double)o["enemyScale"], EnemyDmg = (double)o["enemyDmg"], PlayerHp = Math.Max(0, Int(o["playerHp"], 0)),
                    Boost = StrList(o["boost"]).Where(id => _d.RuneById.ContainsKey(id)).ToList()
                };
                b.Tiers = TiersFrom(o["tiers"]);
                b.Equip = _d.CleanEquip(EquipFrom(o["equip"]), team);
                b.PlayerHeroHp = HeroArray(o["playerHeroHp"]);
                b.PlayerHeroMaxMod = ModArray(o["heroMaxMod"]);
                b.CritPlayer = Chance(o["critPlayer"]); b.ComboPlayer = Chance(o["comboPlayer"]);
                b.MarkBonus = Math.Max(0, Math.Min(1, Dbl(o["markBonus"])));
                b.DmgBonus = Math.Max(0, Math.Min(2, Dbl(o["dmgBonus"])));
                b.Ward = Math.Max(0, Math.Min(0.8, Dbl(o["ward"])));
                b.StartMana = Math.Max(0, Math.Min(99, Int(o["startMana"], 0)));
                b.CostCut = Math.Max(0, Math.Min(9, Int(o["costCut"], 0)));
                b.Vow = Str(o["vow"]) is string vw && IsVow(vw) ? vw : null;
                var ce = o["comboEnemy"];
                if (ce != null && (ce.Type == JTokenType.Float || ce.Type == JTokenType.Integer))
                    b.ComboEnemy = Math.Max(0, Math.Min(1, (double)ce));
                foreach (var s in StrList(o["commands"]))
                {
                    var parts = s.Split(':');
                    if (parts.Length != 4 || parts[0].Length != 1 || "SFHCTZ".IndexOf(parts[0][0]) < 0) return null;
                    b.Commands.Add(new Command(parts[0][0], int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3])));
                }
                return b;
            }
            catch (Exception) { return null; }
        }
    }
}