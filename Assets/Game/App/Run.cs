using System;
using System.Collections.Generic;
using System.Linq;
using Cubo.Domain;

namespace Cubo.App
{
    public sealed class ShopState
    {
        public List<string> Runes = new List<string>(), Sold = new List<string>();
        public List<string> Upgrades = new List<string>();     // ids de poder: cada um sobe um nível ao comprar
        public int Rerolls;                                     // quantas vezes as ofertas foram trocadas nesta loja
        public string Sale;                                     // a runa ou melhoria em promoção nesta loja (ou null)
    }

    // O que aconteceu depois de escolher uma opção do evento. Só os campos do evento escolhido vêm preenchidos.
    public sealed class EventResult
    {
        public string Key, Rune;
        public bool? Won;
    }

    // Os votos do Juramento e o resultado de um voto depois da batalha.
    public sealed class VowResult
    {
        public string Vow;
        public bool Kept;
        public int Gold;
    }

    public sealed class EventState
    {
        public string Id;
        public string Variant;                          // o Ídolo: qual dos três ("rei", "rainha", "torre"); null nos outros
        public EventResult Done;                        // null até escolher
    }

    public sealed class EventOption
    {
        public string Key;
        public bool Ok;
    }

    // Uma corrida em andamento.
    public sealed class RunState
    {
        public int Floor = 1, Hp, Gold;
        public int MaxHp;                               // a vida máxima do time (a da última batalha; 0 = ainda não lutou)
        public int[] HeroHp, HeroMax;                   // [herói]: a vida e o máximo de cada um entre os andares (null = ainda não lutou)

        // Os eventos: os andares em que aparecem e em que ordem (sorteados no começo da corrida, cada um uma vez).
        public List<int> EventFloors = new List<int>();
        public List<string> EventOrder = new List<string>();
        // O que os eventos deixaram: para a corrida toda (crítico, combo, vida máxima por herói) e só para a próxima
        // batalha (o rival com menos vida, a vantagem de elemento, o voto).
        public double BonusCrit, BonusCombo;
        public string Idol;                             // o ídolo desta corrida (sorteado no começo)
        public double BonusDmg, BonusWard;              // os Ídolos: dano a mais (Rei) e dano a menos (Torre)
        public int BonusMana, BonusCost;                // ...a mana inicial dos poderes e o desconto no custo (Rainha)
        public int[] MaxHpMod = new int[3];
        public double NextEnemyHp = 1, NextMark;
        public string Vow;
        public string[] Team;
        public List<string> Runes = new List<string>();
        public ShopState Shop;
        public bool Elite, Door;
        public EventState Event;
        public double Curse;
        public Dictionary<string, int> Tiers = new Dictionary<string, int>();   // [id do poder] = nível de melhoria nesta corrida
        public List<string> Blessing;                   // a bênção aberta: ids de poder para escolher um (null = nenhuma)
        public Dictionary<string, string> Equip = new Dictionary<string, string>();   // [id do herói] = equipamento, fixo na corrida
    }

    // As regras da corrida: andares, eventos, loja e runas.
    public sealed class RunService
    {
        private readonly GameData _d;
        private readonly RunRules _r;
        private readonly CollectionService _cols;
        private readonly BattleEngine _engine;

        public RunService(GameData data, CollectionService cols, BattleEngine engine)
        {
            _d = data; _r = data.Run; _cols = cols; _engine = engine;
        }

        // `team` vem da preparação; sem um time válido vale o padrão. `equip`: os equipamentos da coleção; só os dos
        // heróis do time vão para a corrida.
        public RunState Start(string[] team, IDictionary<string, string> equip = null)
        {
            var run = new RunState { Team = _d.ValidTeam(team) ? (string[])team.Clone() : (string[])_d.DefaultTeam.Clone() };
            run.Equip = _d.CleanEquip(equip, run.Team);
            return run;
        }

        // A soma de V dos equipamentos da corrida desse tipo (a Bolsa: "gold").
        public double EquipV(RunState run, string kind) =>
            run.Equip.Values.Where(id => _d.EquipById.TryGetValue(id, out var e) && e.Kind == kind).Sum(id => _d.EquipById[id].V);

        // Opções da próxima batalha.
        public BattleOptions Opts(RunState run)
        {
            double hp = run.Elite ? _r.EliteHp : 1, dmg = run.Elite ? _r.EliteDmg : 1;
            return new BattleOptions
            {
                EnemyScale = At(_r.EnemyScale, run.Floor) * hp * (run.NextEnemyHp > 0 ? run.NextEnemyHp : 1),
                EnemyDmg = JsMath.Round2(At(_r.EnemyDmg, run.Floor) * dmg * (run.Curse == 0 ? 1 : run.Curse)),
                PlayerHp = run.Hp,
                PlayerHeroMaxMod = (int[])run.MaxHpMod.Clone(),
                CritPlayer = run.BonusCrit > 0 ? _d.Rules.CritChance[0] + run.BonusCrit : (double?)null,
                ComboPlayer = run.BonusCombo > 0 ? _d.Rules.ComboChance[0] + run.BonusCombo : (double?)null,
                MarkBonus = run.NextMark,
                DmgBonus = run.BonusDmg, Ward = run.BonusWard, StartMana = run.BonusMana, CostCut = run.BonusCost,
                Vow = run.Vow,
                PlayerHeroHp = run.HeroHp == null ? null : (int[])run.HeroHp.Clone(),
                Tiers = new Dictionary<string, int>(run.Tiers),
                Equip = new Dictionary<string, string>(run.Equip),
                // o chefe (último andar) e as elites já chegam com chance de combo; os outros rivais, a de base
                ComboEnemy = run.Floor >= _r.Floors ? _r.BossCombo : run.Elite ? _r.EliteCombo : (double?)null
            };
        }

        // `|| 1` do protótipo: andar fora da tabela vale 1.
        private static double At(double[] a, int floor) => floor - 1 >= 0 && floor - 1 < a.Length && a[floor - 1] != 0 ? a[floor - 1] : 1;

        // O rival de elite joga um nível acima do normal do andar.
        public string Level(RunState run)
        {
            var order = new[] { "distraido", "atento", "implacavel" };
            string lv = run.Floor - 1 >= 0 && run.Floor - 1 < _r.Levels.Length ? _r.Levels[run.Floor - 1] : "atento";
            return run.Elite ? order[Math.Min(order.Length - 1, Array.IndexOf(order, lv) + 1)] : lv;
        }

        public int Gold(int floor, bool elite, double bonus = 0) =>
            JsMath.RoundInt((_r.GoldBase + _r.GoldPerFloor * floor) * (elite ? _r.EliteGold : 1) * (1 + bonus));

        // O ouro do andar atual desta corrida, com a Bolsa.
        public int Gold(RunState run, bool elite) => Gold(run.Floor, elite, EquipV(run, EquipKinds.Gold));

        // Elites aparecem depois da primeira corrida vencida, em qualquer andar menos o do chefe.
        public bool ElitesOpen(Collection col) => col != null && col.Wins >= 1;
        public bool CanElite(Collection col, RunState run) => ElitesOpen(col) && run.Floor < _r.Floors;

        // Vitória no andar: o jogador recupera vida, ganha o ouro do andar e sobe um andar.
        public RunState Advance(RunState run, BattleState state)
        {
            int max = state.MaxHp[(int)Side.Player];
            run.MaxHp = max;
            run.HeroMax = (int[])state.HeroMax[0].Clone();
            run.HeroHp = (int[])state.HeroHp[0].Clone();                     // cada herói como terminou (caído = 0)
            for (int i = 0; i < run.HeroHp.Length; i++)                        // HealFrac (0 hoje): só os de pé se recuperam
                if (run.HeroHp[i] > 0) run.HeroHp[i] = Math.Min(run.HeroMax[i], run.HeroHp[i] + JsMath.RoundInt(run.HeroMax[i] * _r.HealFrac));
            SyncRunHp(run);
            run.Gold += Gold(run, run.Elite);
            run.Elite = false;
            run.Floor += 1;
            run.NextEnemyHp = 1; run.NextMark = 0; run.Vow = null;   // o que valia só para a batalha que passou
            return run;
        }

        // ---- eventos ----

        // Sorteia os andares dos eventos (entre o 2º e o último, depois de uma batalha vencida) e a ordem deles.
        public void PlanEvents(RunState run, Mulberry32 rng)
        {
            var floors = Enumerable.Range(2, Math.Max(0, _r.Floors - 1)).ToList();
            run.EventFloors = BattleEngine.Shuffle(floors, rng).Take(Math.Min(_r.Event.PerRun, floors.Count)).OrderBy(f => f).ToList();
            run.EventOrder = BattleEngine.Shuffle(_d.EventIds.ToList(), rng).ToList();
            var idols = _r.Event.Idols;
            run.Idol = idols.Length > 0 ? idols[(int)(rng.Next() * idols.Length)] : null;
        }

        // O evento deste andar (run.Floor já é o do próximo), ou null.
        public EventState EventFor(RunState run)
        {
            int k = run.EventFloors.IndexOf(run.Floor);
            if (k < 0 || run.EventOrder.Count == 0) return null;
            string id = run.EventOrder[k % run.EventOrder.Count];
            return new EventState { Id = id, Variant = id == "idolo" ? IdolOf(run) : null };
        }

        // O ídolo da corrida (o sorteado no começo; corridas antigas, sem ele, ficam com o Rei).
        public string IdolOf(RunState run) =>
            run.Idol != null && Array.IndexOf(_r.Event.Idols, run.Idol) >= 0 ? run.Idol : _r.Event.Idols.FirstOrDefault() ?? "rei";

        public EventState NewEvent(RunState run, Mulberry32 rng, string id)
        {
            if (id == null || Array.IndexOf(_d.EventIds, id) < 0) id = _d.EventIds[(int)(rng.Next() * _d.EventIds.Length)];
            return new EventState { Id = id };
        }

        // As opções do evento aberto. `Ok` diz se dá para escolher agora; "leave" sempre dá.
        public List<EventOption> EventOptions(RunState run)
        {
            var ev = run.Event; var E = _r.Event; var outp = new List<EventOption>();
            if (ev == null || ev.Done != null) return outp;
            switch (ev.Id)
            {
                case "idolo":
                    for (int i = 0; i < run.Team.Length; i++)          // o sangue de um herói de pé que ainda aguente
                    {
                        bool ok = run.HeroMax != null && run.HeroHp != null && run.HeroHp[i] > 0 && run.HeroMax[i] > E.IdolBlood;
                        outp.Add(new EventOption { Key = "blood:" + i, Ok = ok });
                    }
                    outp.Add(new EventOption { Key = "wrath", Ok = true });
                    break;
                case "acampamento":
                    outp.Add(new EventOption { Key = "ambush", Ok = true });
                    outp.Add(new EventOption { Key = "mark", Ok = true });
                    outp.Add(new EventOption { Key = "raid", Ok = true });
                    break;
                case "juramento":
                    foreach (var v in new[] { BattleEngine.VowSwift, BattleEngine.VowSilent, BattleEngine.VowUnbroken })
                        outp.Add(new EventOption { Key = "vow:" + v, Ok = true });
                    break;
            }
            outp.Add(new EventOption { Key = "leave", Ok = true });
            return outp;
        }

        // Escolhe uma opção: muda a corrida e guarda o resultado em run.Event.Done.
        public EventResult EventChoose(RunState run, string key, Mulberry32 rng)
        {
            var ev = run.Event; var E = _r.Event;
            var opt = EventOptions(run).LastOrDefault(o => o.Key == key);
            if (opt == null || !opt.Ok || key == "leave") return null;
            var res = new EventResult { Key = key };
            if (key.StartsWith("blood:") && int.TryParse(key.Substring(6), out int h))
            {
                run.MaxHpMod[h] -= E.IdolBlood;                         // para as próximas batalhas
                run.HeroMax[h] -= E.IdolBlood;                          // e já agora, na vida que o herói leva
                run.HeroHp[h] = Math.Min(run.HeroHp[h], run.HeroMax[h]);
                SyncRunHp(run);
                switch (ev.Variant)
                {
                    case "rainha": run.BonusMana += E.IdolMana; break;
                    case "torre": run.BonusWard += E.IdolWard; break;
                    default: run.BonusCrit += E.IdolCrit; break;
                }
            }
            else if (key == "wrath")
            {
                run.Curse = E.IdolWrath;
                switch (ev.Variant)
                {
                    case "rainha": run.BonusCost += E.IdolCost; break;
                    case "torre":
                        for (int i = 0; i < run.MaxHpMod.Length; i++)
                        {
                            run.MaxHpMod[i] += E.IdolHp;                   // nas próximas batalhas
                            if (run.HeroMax != null && run.HeroHp != null)     // e já agora (quem caiu continua caído)
                            {
                                run.HeroMax[i] += E.IdolHp;
                                if (run.HeroHp[i] > 0) run.HeroHp[i] += E.IdolHp;
                            }
                        }
                        SyncRunHp(run);
                        break;
                    default: run.BonusDmg += E.IdolDmg; break;
                }
            }
            else if (key == "ambush") run.NextEnemyHp = 1 - E.CampAmbush;
            else if (key == "mark") run.NextMark = E.CampMark;
            else if (key == "raid") run.Gold += E.CampRaid;
            else if (key.StartsWith("vow:")) run.Vow = key.Substring(4);
            ev.Done = res;
            return res;
        }

        // Depois de uma batalha vencida: o voto do Juramento foi cumprido? Paga o prêmio. null se não havia voto.
        public VowResult CheckVow(RunState run, BattleState st)
        {
            if (string.IsNullOrEmpty(st.Vow)) return null;
            var r = new VowResult { Vow = st.Vow, Kept = st.Winner == Side.Player && _engine.VowHolds(st) };
            if (r.Kept) { r.Gold = _r.Event.VowGold; run.Gold += r.Gold; }
            return r;
        }

        // ---- melhorias de habilidade ----

        public int TierOf(RunState run, string powerId) => run.Tiers.TryGetValue(powerId, out var t) ? t : 0;

        // Os 6 poderes do time da corrida, na ordem do painel.
        public List<string> TeamPowers(RunState run) =>
            _d.ResolveTeam(run.Team).SelectMany(h => h.Powers.Select(p => p.Id)).ToList();

        // O que pode melhorar na corrida: os 6 poderes do time e os itens levados por ele que têm níveis (veneno, fogo).
        public List<string> TeamUpgrades(RunState run) =>
            TeamPowers(run).Concat(run.Team.Where(h => run.Equip.ContainsKey(h)).Select(h => run.Equip[h])
                                      .Where(id => _engine.MaxTier(id) > 0)).ToList();

        public bool IsItem(string id) => _d.EquipById.ContainsKey(id);

        // Ainda dá para subir: é do time (poder ou item levado) e não chegou ao último nível.
        public bool CanUpgrade(RunState run, string powerId) =>
            TeamUpgrades(run).Contains(powerId) && TierOf(run, powerId) < _engine.MaxTier(powerId);

        // O item como ele fica com um nível a mais (para mostrar a oferta).
        public EquipDef PreviewItem(RunState run, string itemId, int extra = 1) =>
            _engine.UpgradedEquip(_d.EquipById[itemId], TierOf(run, itemId) + extra);

        // O poder do time como ele fica com um nível a mais (para mostrar a oferta).
        public PowerDef Preview(RunState run, string powerId, int extra = 1)
        {
            var p = _d.ResolveTeam(run.Team).SelectMany(h => h.Powers).First(x => x.Id == powerId);
            return _engine.Upgraded(p, TierOf(run, powerId) + extra);
        }

        // O preço na loja do próximo nível (com o desconto, se for a oferta em promoção).
        public int UpgradePrice(RunState run, string powerId)
        {
            var prices = _r.UpgradePrices;
            int t = TierOf(run, powerId);
            return SalePrice(run, powerId, prices[Math.Min(t, prices.Length - 1)]);
        }

        // O preço de tabela de uma runa ou melhoria, com o desconto se ela é a promoção desta loja.
        public int SalePrice(RunState run, string id, int price) =>
            run.Shop != null && run.Shop.Sale == id ? Math.Max(1, JsMath.RoundInt(price * (1 - _r.Sale))) : price;

        public int RunePrice(RunState run, string id) => SalePrice(run, id, _d.RuneById[id].Price);

        // O preço sem desconto (para riscar ao lado do preço da promoção).
        public int FullPrice(RunState run, string id) =>
            _d.RuneById.TryGetValue(id, out var r) ? r.Price : _r.UpgradePrices[Math.Min(TierOf(run, id), _r.UpgradePrices.Length - 1)];

        private List<string> Upgradable(RunState run) => TeamUpgrades(run).Where(id => CanUpgrade(run, id)).ToList();

        // A bênção depois do andar: até BlessingSize poderes sorteados entre os que ainda sobem. Vazia = sem bênção.
        public List<string> NewBlessing(RunState run, Mulberry32 rng) =>
            BattleEngine.Shuffle(Upgradable(run), rng).Take(_r.BlessingSize).ToList();

        // Escolhe um poder da bênção aberta: ele sobe um nível e a bênção fecha.
        public bool Bless(RunState run, string powerId)
        {
            if (run.Blessing == null || !run.Blessing.Contains(powerId) || !CanUpgrade(run, powerId)) return false;
            run.Tiers[powerId] = TierOf(run, powerId) + 1;
            run.Blessing = null;
            return true;
        }

        public bool BuyUpgrade(RunState run, string powerId)
        {
            var shop = run.Shop;
            if (shop == null || !shop.Upgrades.Contains(powerId) || shop.Sold.Contains(powerId) || !CanUpgrade(run, powerId)) return false;
            int price = UpgradePrice(run, powerId);
            if (run.Gold < price) return false;
            run.Gold -= price;
            shop.Sold.Add(powerId);
            run.Tiers[powerId] = TierOf(run, powerId) + 1;
            return true;
        }

        // ---- loja e runas ----

        // A loja depois de vencer um andar (run.Floor já é o seguinte): runas sorteadas, depois melhorias.
        public ShopState NewShop(RunState run, Mulberry32 rng)
        {
            var shop = new ShopState { Runes = BattleEngine.Shuffle(_d.Runes.Select(r => r.Id).ToList(), rng).Take(_r.ShopRunes).ToList() };
            shop.Upgrades = BattleEngine.Shuffle(Upgradable(run), rng).Take(_r.ShopUpgrades).ToList();
            var pool = shop.Runes.Concat(shop.Upgrades).ToList();          // uma oferta em promoção
            shop.Sale = _r.Sale > 0 && pool.Count > 0 ? pool[rng.NextInt(pool.Count)] : null;
            return shop;
        }

        // ---- trocar as ofertas ----

        public int RerollPrice(RunState run) => run.Shop == null ? 0 : _r.RerollBase + _r.RerollStep * run.Shop.Rerolls;

        public bool CanReroll(RunState run) => run.Shop != null && _r.RerollBase > 0 && run.Gold >= RerollPrice(run);

        // Paga e sorteia runas, melhorias e a promoção de novo. A poção comprada continua comprada; o preço da próxima
        // troca sobe.
        public bool Reroll(RunState run, Mulberry32 rng)
        {
            if (!CanReroll(run)) return false;
            var shop = run.Shop;
            run.Gold -= RerollPrice(run);
            var fresh = NewShop(run, rng);
            shop.Runes = fresh.Runes; shop.Upgrades = fresh.Upgrades; shop.Sale = fresh.Sale;
            shop.Sold = shop.Sold.Where(id => id == Potion).ToList();
            shop.Rerolls += 1;
            return true;
        }

        // ---- a Poção de Cura ----

        public const string Potion = "pocao";

        // Uma corrida salva antes de cada herói guardar a sua vida: reparte o total como a próxima batalha faria.
        public void EnsureHeroHp(RunState run, int size, string mode)
        {
            if (run.HeroHp != null || run.Hp <= 0) return;
            var o = Opts(run);
            o.Size = size; o.Mode = mode; o.PlayerHeroHp = null;
            var st = _engine.NewState(o, run.Team, new Mulberry32(1));
            run.HeroHp = (int[])st.HeroHp[0].Clone();
            run.HeroMax = (int[])st.HeroMax[0].Clone();
            SyncRunHp(run);
        }

        // Hp e MaxHp da corrida voltam a ser a soma dos heróis.
        public static void SyncRunHp(RunState run)
        {
            if (run.HeroHp == null || run.HeroMax == null) return;
            run.Hp = run.HeroHp.Sum(); run.MaxHp = run.HeroMax.Sum();
        }

        // Cura `amount` em CADA herói (caídos também: voltam à luta), sem passar do máximo de cada um. Devolve o total.
        public static int HealEach(RunState run, int amount, bool apply)
        {
            if (run.HeroHp == null || run.HeroMax == null) return 0;
            int total = 0;
            for (int i = 0; i < run.HeroHp.Length; i++)
            {
                int gain = Math.Max(0, Math.Min(amount, run.HeroMax[i] - run.HeroHp[i]));
                total += gain;
                if (apply) run.HeroHp[i] += gain;
            }
            if (apply) SyncRunHp(run);
            return total;
        }

        // Cura `amount` espalhada: um ponto por vez para o herói DE PÉ mais ferido (a Fonte). Devolve o total.
        public static int HealSpread(RunState run, int amount)
        {
            if (run.HeroHp == null || run.HeroMax == null) { run.Hp += amount; return amount; }
            int healed = 0;
            for (int k = 0; k < amount; k++)
            {
                int best = -1; double frac = 2;
                for (int i = 0; i < run.HeroHp.Length; i++)
                {
                    if (run.HeroHp[i] <= 0 || run.HeroHp[i] >= run.HeroMax[i]) continue;
                    double f = (double)run.HeroHp[i] / run.HeroMax[i];
                    if (f < frac) { frac = f; best = i; }
                }
                if (best < 0) break;
                run.HeroHp[best]++; healed++;
            }
            SyncRunHp(run);
            return healed;
        }

        // Quanto a Poção cura no total: PotionHp em cada herói (também nos caídos), sem passar do máximo de cada um.
        public int PotionHeal(RunState run) => HealEach(run, _r.PotionHp, false);

        // Dá para comprar: a loja está aberta, a poção desta loja não foi comprada, há vida para recuperar e ouro.
        public bool CanBuyPotion(RunState run) =>
            run.Shop != null && _r.PotionHp > 0 && !run.Shop.Sold.Contains(Potion) && PotionHeal(run) > 0 && run.Gold >= _r.PotionPrice;

        public bool BuyPotion(RunState run)
        {
            if (!CanBuyPotion(run)) return false;
            run.Gold -= _r.PotionPrice;
            HealEach(run, _r.PotionHp, true);
            run.Shop.Sold.Add(Potion);
            return true;
        }

        public bool BuyRune(RunState run, string id)
        {
            var shop = run.Shop;
            if (shop == null || !shop.Runes.Contains(id) || shop.Sold.Contains(id) || run.Runes.Count >= _r.RuneSlots
                || run.Gold < RunePrice(run, id)) return false;
            run.Gold -= RunePrice(run, id);
            shop.Sold.Add(id);
            run.Runes.Add(id);
            return true;
        }

        // No começo da batalha: gasta as runas e aplica os efeitos no estado. Devolve as usadas.
        public List<string> Boost(RunState run, BattleState st)
        {
            var used = new List<string>();
            run.Curse = 0;      // a maldição valeu na criação desta batalha (Opts)
            foreach (var id in run.Runes)
            {
                if (!_d.RuneById.TryGetValue(id, out var r)) continue;
                if (r.Kind == "mana")
                    for (int g = 0; g < BattleEngine.Slots; g++)
                        st.Mana[(int)Side.Player][g] = Math.Min(_d.Rules.ManaCap, st.Mana[(int)Side.Player][g] + r.N);
                else if (r.Kind == "hp")
                    _engine.HealSpread(st, Side.Player, r.N);       // espalhada pelos heróis mais feridos
                else _engine.AddFx(st, r.Kind, r.V, null, true);
                used.Add(id);
            }
            run.Runes = new List<string>();
            return used;
        }

        // Vida que o jogador recupera entre andares.
        public int Heal(BattleState st) => JsMath.RoundInt(st.MaxHp[(int)Side.Player] * _r.HealFrac);
    }
}
