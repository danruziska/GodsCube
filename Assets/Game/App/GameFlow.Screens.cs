using System;
using System.Collections.Generic;
using System.Linq;
using Cubo.Domain;

namespace Cubo.App
{
    // As telas do fluxo, descritas em dados, e os textos que elas usam. Só lê o estado; quem muda o estado são os
    // métodos do GameFlow.cs que os botões chamam.
    public sealed partial class GameFlow
    {
        // ---------------------------------------------------------------- nomes e textos

        private string RuneName(string id) => _t.Name("rune." + id);

        // "Lança ++": o nome da habilidade no nível dado.
        // Vale também para os itens que melhoram (veneno, fogo): "Presa de Víbora ++".
        private string AbilityName(string powerId, int tier) =>
            _t.Name((_runs.IsItem(powerId) ? "equip." : "power.") + powerId) + HudModel.TierTag(tier, tier > 0 && tier >= _engine.MaxTier(powerId));

        // O herói do poder ("rei.0" -> Rei) ou o herói que leva o item.
        private string HeroOf(string powerId)
        {
            if (!_runs.IsItem(powerId)) return _t.Name("hero." + powerId.Substring(0, powerId.IndexOf('.')));
            var owner = Run.Equip.FirstOrDefault(kv => kv.Value == powerId).Key;
            return owner != null ? _t.Name("hero." + owner) : "";
        }

        // O que a melhoria faz no próximo nível, com o valor de hoje entre parênteses depois de cada número que muda:
        // "Varre Guerra e Colheita da frente, 2 (1) de dano por peça · custo 6 (8)".
        private string UpgradeStats(string id)
        {
            if (_runs.IsItem(id))
                return StatText.WithOld(EquipText(_runs.PreviewItem(Run, id)), EquipText(_runs.PreviewItem(Run, id, 0)));
            string Line(PowerDef p) => HudModel.PowerEffect(p, _t) + " · " + _t.T("upgrade.cost", ("c", p.Cost));
            return StatText.WithOld(Line(_runs.Preview(Run, id)), Line(_runs.Preview(Run, id, 0)));
        }

        // "Cavaleiro · Lança + → Lança ++\n(o que ela passa a fazer)". Num item: o texto do item já melhorado.
        private string UpgradeLine(string powerId)
        {
            int t = _runs.TierOf(Run, powerId);
            return HeroOf(powerId) + " · " + AbilityName(powerId, t) + " → " + AbilityName(powerId, t + 1) + "\n" + UpgradeStats(powerId);
        }

        private string RuneText(string id)
        {
            var r = _d.RuneById[id];
            return _t.T("rune." + r.Kind, ("n", r.N), ("p", JsMath.RoundInt(r.V * 100)));
        }

        // ---------------------------------------------------------------- telas

        // A tela do estado atual. Estado Battle não tem tela (quem mostra é a interface da batalha): devolve null.
        public ScreenModel Screen()
        {
            switch (Stage)
            {
                case Stage.Prep: return PrepScreen();
                case Stage.Door: return DoorScreen();
                case Stage.Blessing: return BlessingScreen();
                case Stage.Event: return EventScreen();
                case Stage.Shop: return ShopList();
                case Stage.End: return EndScreen();
                default: return null;
            }
        }

        private static void Head(ScreenModel s, string text) => s.Items.Add(new ScreenItem { Kind = ItemKind.Heading, Text = text });
        private static void Para(ScreenModel s, string text) { if (!string.IsNullOrEmpty(text)) s.Items.Add(new ScreenItem { Kind = ItemKind.Text, Text = text }); }
        private static void Btn(ScreenModel s, string id, string text, Action click, bool enabled = true, bool on = false) =>
            s.Items.Add(new ScreenItem { Kind = ItemKind.Button, Id = id, Text = text, Click = click, Enabled = enabled, On = on });

        // A frase de baixo da preparação: o aviso do último toque ou quantos heróis ainda faltam. Tudo pronto: vazia.
        private string PrepHint()
        {
            if (_hint.Length > 0) return _hint;
            int gLeft = 3 - _prepTeam.Count;
            return gLeft > 0 ? _t.T("prep.need.heroes." + (gLeft == 1 ? "1" : "n"), ("n", gLeft)) : "";
        }

        // A tela de heróis (a aba Heróis da preparação), já calculada e com texto.
        public HeroesView HeroesScreen() => HeroesModel.Build(_d, _t, _prepTeam, _prepSel, PrepReady, PrepHint());

        private string EquipName(string id) => _t.Name("equip." + id);

        // O que o equipamento faz, com os números de equipment.json.
        public string EquipText(string id) => EquipText(_d.EquipById[id]);

        private string EquipText(EquipDef e) =>
            _t.T("equip." + e.Kind, ("n", e.N), ("p", JsMath.RoundInt(e.V * 100)), ("t", e.Turns), ("min", _d.Rules.HazardMin),
                 ("a", _engine.AreaOf(e)));

        // A tela desenhada do Arsenal (EquipScreen): os três heróis do time, o equipamento em foco e a grade.
        public EquipView EquipScreen()
        {
            var v = new EquipView { TeamTitle = _t.T("heroes.team"), Title = _t.T("heroes.tab.equip") };
            string sel = EquipHero, focus = EquipFocus;
            for (int i = 0; i < 3; i++)
            {
                string id = i < _prepTeam.Count ? _prepTeam[i] : null;
                v.Heroes.Add(new EquipHeroView
                {
                    Id = id, Name = id != null ? _t.Name("hero." + id) : "",
                    ItemId = id != null && Col.Equip.TryGetValue(id, out var e) ? e : null, Selected = id != null && id == sel
                });
            }
            foreach (var eq in _d.Equipment)
            {
                string owner = Col.Equip.FirstOrDefault(kv => kv.Value == eq.Id).Key;
                v.Items.Add(new EquipItemView
                {
                    Id = eq.Id, Name = EquipName(eq.Id), Effect = EquipText(eq.Id), Owner = owner,
                    Focused = eq.Id == focus, OnSelected = owner != null && owner == sel
                });
            }
            if (focus != null)
            {
                var item = v.Items.First(x => x.Id == focus);
                v.DetailId = focus; v.DetailName = item.Name; v.DetailEffect = item.Effect;
                v.DetailOwner = item.Owner != null ? _t.T("equip.with", ("hero", _t.Name("hero." + item.Owner))) : "";
                v.ActionText = _t.T(item.OnSelected ? "equip.btn.remove" : item.Owner != null ? "equip.btn.take" : "equip.btn.equip");
            }
            else v.ActionText = _t.T("equip.btn.equip");
            v.CanAct = sel != null && focus != null;
            return v;
        }

        // A terceira aba da preparação em lista (quando a tela desenhada não está na cena): os heróis do time em cima (toque escolhe quem equipar) e, embaixo, todos os
        // equipamentos. Cada herói leva um; um equipamento que está com outro herói muda de dono ao ser tocado.
        private ScreenModel PrepScreen()
        {
            var s = new ScreenModel { Title = _t.T("equip.title") };
            Para(s, _t.T("equip.how"));
            string sel = EquipHero;
            if (sel == null) { Para(s, _t.T("equip.noteam")); return s; }
            foreach (var hero in _prepTeam)
            {
                string h = hero;
                string carried = Col.Equip.TryGetValue(hero, out var e) ? EquipName(e) : _t.T("equip.none");
                Btn(s, "equiphero:" + hero, _t.Name("hero." + hero) + "\n" + carried, () => SelectEquipHero(h), true, hero == sel);
            }
            Head(s, _t.T("equip.for", ("hero", _t.Name("hero." + sel))));
            foreach (var eq in _d.Equipment)
            {
                string id = eq.Id;
                string owner = Col.Equip.FirstOrDefault(kv => kv.Value == id).Key;
                string text = EquipName(id) + "\n" + EquipText(id);
                if (owner != null && owner != sel) text += "\n" + _t.T("equip.with", ("hero", _t.Name("hero." + owner)));
                Btn(s, "equip:" + id, text, () => ToggleEquip(id), true, owner == sel);
            }
            Para(s, _hint);
            return s;
        }

        private ScreenModel DoorScreen()
        {
            var E = _d.Run; int f = Run.Floor;
            var s = new ScreenModel { Title = _t.T("door.title", ("f", f), ("n", E.Floors)) };
            Para(s, _t.T("door.how"));
            Btn(s, "door:normal", _t.T("door.normal") + "\n" + _t.T("door.normal.text", ("g", _runs.Gold(Run, false))), () => PickDoor(false));
            Btn(s, "door:elite", _t.T("door.elite") + "\n" + _t.T("door.elite.text",
                ("hp", JsMath.RoundInt((E.EliteHp - 1) * 100)), ("dmg", JsMath.RoundInt((E.EliteDmg - 1) * 100)),
                ("g", _runs.Gold(Run, true))), () => PickDoor(true));
            return s;
        }

        // A porta desenhada (DoorScreen): a ficha de cada porta, calculada como a batalha seria criada.
        public DoorView DoorView()
        {
            var v = new DoorView();
            if (Run == null) return v;
            v.Floor = Run.Floor; v.Floors = _d.Run.Floors;
            v.Title = _t.T("door.name", ("f", Run.Floor), ("n", _d.Run.Floors));
            v.Text = string.IsNullOrEmpty(Notice) ? _t.T("door.how") : Notice + "\n" + _t.T("door.how");
            v.BossLabel = _t.T("door.boss");
            var normal = DoorStats(false); var elite = DoorStats(true);
            v.Normal = DoorOption("normal", normal, null);
            v.Elite = DoorOption("elite", elite, normal);
            v.CanAct = _doorFocus != null;
            v.Hint = v.CanAct ? null : _t.T("door.tap");
            v.ActionText = _t.T(v.CanAct ? "door.enter" : "door.pick");
            return v;
        }

        // A vida do rival, o dano, como joga e o ouro, para a porta comum ou a de elite.
        private (int hp, double dmg, string lvl, int gold) DoorStats(bool elite)
        {
            bool was = Run.Elite;
            Run.Elite = elite;
            try
            {
                var o = _runs.Opts(Run);
                o.Size = Settings.Size; o.Mode = Settings.Mode;
                var st = _engine.NewState(o, Run.Team, new Mulberry32(1));
                return (st.Hp[1], o.EnemyDmg, _runs.Level(Run), _runs.Gold(Run, elite));
            }
            finally { Run.Elite = was; }
        }

        private DoorOptionView DoorOption(string key, (int hp, double dmg, string lvl, int gold) s, (int hp, double dmg, string lvl, int gold)? other)
        {
            var o = new DoorOptionView { Key = key, Name = _t.T("door." + key), Tagline = _t.T("door." + key + ".tag"), Focused = _doorFocus == key };
            bool cmp = other.HasValue;
            o.Stats.Add(new DoorStatView { Label = _t.T("door.stat.hp"), Value = s.hp.ToString(), Worse = cmp && s.hp > other.Value.hp });
            o.Stats.Add(new DoorStatView { Label = _t.T("door.stat.dmg"), Value = "×" + s.dmg.ToString("0.##"), Worse = cmp && s.dmg > other.Value.dmg });
            o.Stats.Add(new DoorStatView { Label = _t.T("door.stat.lvl"), Value = _t.T("lvl." + s.lvl), Worse = cmp && s.lvl != other.Value.lvl });
            o.Stats.Add(new DoorStatView { Label = _t.T("door.stat.gold"), Value = _t.T("door.gold", ("g", s.gold)), Better = cmp && s.gold > other.Value.gold });
            return o;
        }

        // A bênção desenhada (BlessingScreen): uma carta por opção; tocar escolhe, o botão aceita.
        public BlessingView BlessingView()
        {
            var v = new BlessingView
            {
                Title = _t.T("blessing.title"),
                Text = string.IsNullOrEmpty(Notice) ? _t.T("blessing.text") : Notice + "\n" + _t.T("blessing.text")
            };
            if (Run == null || Run.Blessing == null) return v;
            foreach (var id in Run.Blessing)
            {
                bool item = _runs.IsItem(id);
                int t = _runs.TierOf(Run, id);
                v.Cards.Add(new BlessingCardView
                {
                    Id = id, HeroId = item ? null : id.Substring(0, id.IndexOf('.')), IconId = item ? id : null,
                    Name = AbilityName(id, t + 1),
                    Tag = HudModel.TierTag(t + 1, t + 1 >= _engine.MaxTier(id)).Trim(),
                    Hero = HeroOf(id),
                    Change = AbilityName(id, t) + " → " + AbilityName(id, t + 1),
                    Effect = UpgradeStats(id),
                    Focused = id == _blessFocus
                });
            }
            v.CanAccept = _blessFocus != null && Run.Blessing.Contains(_blessFocus);
            v.ActionText = _t.T(v.CanAccept ? "blessing.accept" : "blessing.choose");
            return v;
        }

        private ScreenModel BlessingScreen()
        {
            var s = new ScreenModel { Title = _t.T("blessing.title") };
            Para(s, _t.T("blessing.text"));
            foreach (var id in Run.Blessing)
            {
                string pid = id;
                Btn(s, "bless:" + id, UpgradeLine(id), () => PickBlessing(pid));
            }
            return s;
        }

        // A tela do evento desenhada (EventScreen): a história, as opções (tocar escolhe), o botão de confirmar e, depois,
        // o resultado com Continuar.
        public EventView EventView()
        {
            var ev = Run?.Event;
            var v = new EventView();
            if (ev == null) return v;
            v.Id = ev.Id;
            v.Variant = ev.Variant;
            string head = ev.Variant != null ? "ev." + ev.Id + "." + ev.Variant : "ev." + ev.Id;   // cada ídolo tem o seu nome e a sua história
            v.Title = _t.T(head + ".name");
            v.Story = _t.T(head + ".text");
            v.Notice = Notice;
            if (ev.Id == "acampamento") v.Info = ScoutReport();
            v.Done = ev.Done != null;
            if (v.Done)
            {
                v.Result = EventResultText(ev.Done);
                v.ActionText = _t.T("ev.opt.continue");
                v.CanAct = true;
                return v;
            }
            foreach (var o in _runs.EventOptions(Run))
            {
                if (o.Key == "leave") continue;
                var p = EventParams(o.Key);
                string group = o.Key.Contains(":") ? o.Key.Substring(0, o.Key.IndexOf(':')) : o.Key;
                string tkey = o.Key.Replace(':', '.');
                bool blood = group == "blood";
                v.Options.Add(new EventOptionView
                {
                    Key = o.Key, Group = group, Enabled = o.Ok, Focused = o.Key == _eventFocus,
                    Title = _t.T("ev.opt." + (blood ? "blood" : tkey), p),
                    Text = _t.T("ev.desc." + (blood ? "blood" : tkey) + Suffix(group), p),
                    HeroId = blood ? Run.Team[int.Parse(o.Key.Substring(6))] : null,
                    Reason = o.Ok ? null : _t.T("ev.why." + group)
                });
            }
            var focus = v.Options.Find(x => x.Focused);
            v.CanAct = focus != null && focus.Enabled;
            v.ActionText = _t.T(v.CanAct ? "ev." + ev.Id + ".act" : "ev.choose");
            v.LeaveText = _t.T("ev." + ev.Id + ".leave");
            return v;
        }

        // O Acampamento: o que o batedor viu do próximo rival (andar, vida, dano, como joga e a vida de cada herói,
        // com o mais forte: o que "Marcar" marcaria).
        private string ScoutReport()
        {
            var o = _runs.Opts(Run);
            o.Size = Settings.Size; o.Mode = Settings.Mode;
            var st = _engine.NewState(o, Run.Team, new Mulberry32(1));
            bool boss = Run.Floor >= _d.Run.Floors;
            var lines = new List<string>
            {
                _t.T(boss ? "ev.scout.boss" : "ev.scout.floor", ("f", Run.Floor), ("n", _d.Run.Floors)),
                _t.T("ev.scout.stats", ("hp", st.Hp[1]), ("d", o.EnemyDmg.ToString("0.##")), ("lvl", _t.T("lvl." + _runs.Level(Run))))
            };
            int best = -1;
            for (int i = 0; i < BattleEngine.Heroes; i++) if (st.HeroHp[1][i] > 0 && (best < 0 || st.HeroMax[1][i] > st.HeroMax[1][best])) best = i;
            for (int i = 0; i < BattleEngine.Heroes; i++)
                lines.Add(_t.T(i == best ? "ev.scout.strongest" : "ev.scout.hero", ("name", _t.Name("hero." + st.Teams[1][i].Id)), ("hp", st.HeroMax[1][i])));
            return string.Join("\n", lines);
        }

        // O evento em lista (quando a tela desenhada não está na cena).
        private ScreenModel EventScreen()
        {
            var v = EventView();
            var s = new ScreenModel { Title = v.Title };
            if (!string.IsNullOrEmpty(v.Notice)) Para(s, v.Notice);
            Para(s, v.Done ? v.Result : v.Story);
            if (v.Info != null) Para(s, v.Info);
            if (v.Done) { Btn(s, "continue", v.ActionText, () => EventPick("continue")); return s; }
            foreach (var o in v.Options)
            {
                string key = o.Key;
                Btn(s, "ev:" + o.Key, o.Title + "\n" + o.Text, () => EventPick(key), o.Enabled);
            }
            Btn(s, "ev:leave", v.LeaveText, () => EventPick("leave"));
            return s;
        }

        // O Ídolo: o sangue e a ira dizem coisas diferentes em cada ídolo (".rei", ".rainha", ".torre").
        private string Suffix(string group) =>
            (group == "blood" || group == "wrath") && Run.Event?.Variant != null ? "." + Run.Event.Variant : "";

        private (string, object)[] EventParams(string key)
        {
            var E = _d.Run.Event;
            string hero = key.StartsWith("blood:") ? _t.Name("hero." + Run.Team[int.Parse(key.Substring(6))]) : "";
            return new (string, object)[]
            {
                ("hero", hero), ("n", E.IdolBlood), ("p", Pct(key)), ("w", JsMath.RoundInt((E.IdolWrath - 1) * 100)),
                ("m", E.IdolMana), ("k", E.IdolCost), ("h", E.IdolHp),
                ("g", key == "raid" ? E.CampRaid : E.VowGold), ("r", E.VowRounds)
            };
        }

        // O número em % de cada opção: o crítico do sangue, o dano a mais da ira, a vida a menos da emboscada, o estudo.
        private int Pct(string key)
        {
            var E = _d.Run.Event;
            string idol = Run.Event?.Variant;
            if (key.StartsWith("blood")) return JsMath.RoundInt((idol == "torre" ? E.IdolWard : E.IdolCrit) * 100);
            if (key == "wrath") return JsMath.RoundInt(E.IdolDmg * 100);           // o Rei: o dano a mais (os outros usam m, k, h)
            if (key == "ambush") return JsMath.RoundInt(E.CampAmbush * 100);
            if (key == "mark") return JsMath.RoundInt(E.CampMark * 100);
            return 0;
        }

        private string EventResultText(EventResult r)
        {
            var p = EventParams(r.Key);
            if (r.Key.StartsWith("vow:")) return _t.T("ev.res.vow", ("vow", _t.T("ev.opt.vow." + r.Key.Substring(4))), ("g", _d.Run.Event.VowGold));
            string group = r.Key.StartsWith("blood:") ? "blood" : r.Key;
            return _t.T("ev.res." + group + Suffix(group), p);
        }

        // A loja desenhada (ShopScreen): o vendedor, as prateleiras, o detalhe da oferta em foco e a bolsa de runas.
        public ShopView ShopScreen()
        {
            var v = new ShopView
            {
                Title = _t.T("shop.title"), UpgradesTitle = _t.T("shop.upgrades"), RunesTitle = _t.T("shop.runes"),
                ServicesTitle = _t.T("shop.services"), BagTitle = _t.T("shop.bag")
            };
            if (Run == null || Run.Shop == null) return v;
            var shop = Run.Shop; int gold = Run.Gold;
            bool full = Run.Runes.Count >= _d.Run.RuneSlots;
            v.Gold = gold.ToString();
            v.Health = Run.MaxHp > 0 ? Run.Hp + "/" + Run.MaxHp : "";
            v.Floor = _t.T("shop.floor", ("f", Run.Floor), ("n", _d.Run.Floors));
            for (int i = 0; i < Run.Team.Length; i++)
            {
                int hp = Run.HeroHp != null ? Run.HeroHp[i] : 0, max = Run.HeroMax != null ? Run.HeroMax[i] : 0;
                bool alive = Run.HeroHp == null || hp > 0;
                v.Heroes.Add(new ShopHeroView
                {
                    HeroId = Run.Team[i], Name = _t.Name("hero." + Run.Team[i]), Hp = hp, Max = max, Alive = alive,
                    Text = max <= 0 ? "" : alive ? hp + "/" + max : _t.T("shop.fallen")
                });
            }
            v.LeaveText = Run.Floor >= _d.Run.Floors ? _t.T("shop.next.boss") : _t.T("shop.next", ("f", Run.Floor), ("n", _d.Run.Floors));

            foreach (var id in shop.Upgrades)
            {
                bool item = _runs.IsItem(id), sold = shop.Sold.Contains(id), can = _runs.CanUpgrade(Run, id);
                int t = _runs.TierOf(Run, id), price = _runs.UpgradePrice(Run, id);
                v.Upgrades.Add(new ShopOfferView
                {
                    Id = "upg:" + id, Kind = "upgrade", IconId = item ? id : null, HeroId = item ? null : id.Substring(0, id.IndexOf('.')),
                    Name = AbilityName(id, sold ? t : t + 1),
                    Effect = HeroOf(id) + " · " + AbilityName(id, t) + " → " + AbilityName(id, t + 1) + "\n" + UpgradeStats(id),
                    Tag = HudModel.TierTag(sold ? t : t + 1, (sold ? t : t + 1) >= _engine.MaxTier(id)).Trim(),
                    Price = price, FullPrice = _runs.FullPrice(Run, id), OnSale = shop.Sale == id && !sold,
                    Sold = sold || !can, Affordable = gold >= price, CanBuy = !sold && can && gold >= price
                });
            }
            foreach (var id in shop.Runes)
            {
                bool sold = shop.Sold.Contains(id); int price = _runs.RunePrice(Run, id);
                v.Runes.Add(new ShopOfferView
                {
                    Id = "rune:" + id, Kind = "rune", IconId = id, Name = RuneName(id),
                    Effect = RuneText(id) + "\n" + _t.T("shop.rune.note"),
                    Price = price, FullPrice = _runs.FullPrice(Run, id), OnSale = shop.Sale == id && !sold,
                    Sold = sold, Affordable = gold >= price, CanBuy = !sold && !full && gold >= price
                });
            }
            {
                bool sold = shop.Sold.Contains(RunService.Potion); int price = _d.Run.PotionPrice;
                v.Services.Add(new ShopOfferView
                {
                    Id = "potion", Kind = "potion", IconId = "potion", Name = _t.T("shop.potion.name"),
                    Effect = _t.T("shop.potion.effect", ("n", _d.Run.PotionHp)) + (v.Health.Length > 0 ? "\n" + _t.T("shop.hp", ("hp", Run.Hp), ("max", Run.MaxHp)) : ""),
                    Price = price, FullPrice = price, Sold = sold, Affordable = gold >= price, CanBuy = _runs.CanBuyPotion(Run)
                });
                int rp = _runs.RerollPrice(Run);
                v.Services.Add(new ShopOfferView
                {
                    Id = "reroll", Kind = "reroll", IconId = "reroll", Name = _t.T("shop.reroll.name"),
                    Effect = _t.T("shop.reroll.effect", ("s", _d.Run.RerollStep)),
                    Price = rp, FullPrice = rp, Affordable = gold >= rp, CanBuy = _runs.CanReroll(Run)
                });
            }

            var focus = v.All.FirstOrDefault(o => o.Id == _shopFocus) ?? v.All.FirstOrDefault(o => !o.Sold) ?? v.All.First();
            focus.Focused = true;
            v.DetailId = focus.Id; v.DetailName = focus.Name; v.DetailEffect = focus.Effect;
            v.DetailIconId = focus.IconId; v.DetailHeroId = focus.HeroId;
            v.DetailPrice = focus.Price; v.DetailFullPrice = focus.FullPrice;
            v.CanBuy = focus.CanBuy;
            v.ActionText = focus.CanBuy ? _t.T(focus.Kind == "reroll" ? "shop.btn.reroll" : "shop.btn.buy", ("p", focus.Price))
                         : focus.Sold ? _t.T("shop.why.sold")
                         : !focus.Affordable ? _t.T("shop.why.gold")
                         : focus.Kind == "rune" ? _t.T("shop.why.full")
                         : focus.Kind == "potion" ? _t.T("shop.why.healthy")
                         : _t.T("shop.why.sold");

            for (int i = 0; i < _d.Run.RuneSlots; i++)
            {
                string rune = i < Run.Runes.Count ? Run.Runes[i] : null;
                v.Bag.Add(rune);
                v.BagInfo.Add(rune == null ? null : RuneName(rune) + "\n" + RuneText(rune) + "\n" + _t.T("shop.rune.note"));
            }
            string say = _shopSay ?? "shop.say.greet." + (1 + (Run.Floor % 3));
            if (_shopSay == null && shop.Sale != null && Run.Floor % 2 == 0) say = "shop.say.sale";
            v.Line = _shopSay == null && !string.IsNullOrEmpty(Notice) ? Notice : _t.T(say);
            return v;
        }

        // A loja em lista (quando a tela desenhada não está na cena).
        private ScreenModel ShopList()
        {
            var shop = Run.Shop; int gold = Run.Gold; bool full = Run.Runes.Count >= _d.Run.RuneSlots;
            var s = new ScreenModel { Title = _t.T("shop.title") };
            Para(s, _t.T("shop.gold", ("g", gold)));
            if (Run.MaxHp > 0) Para(s, _t.T("shop.hp", ("hp", Run.Hp), ("max", Run.MaxHp)));
            // A Poção de Cura: a única forma de recuperar vida entre os andares (além da Fonte).
            Head(s, _t.T("shop.potion.head"));
            int ph = _d.Run.PotionHp, pp = _d.Run.PotionPrice;
            if (shop.Sold.Contains(RunService.Potion)) Para(s, _t.T("shop.potion", ("n", ph)) + " — " + _t.T("shop.sold"));
            else if (_runs.PotionHeal(Run) <= 0) Para(s, _t.T("shop.potion", ("n", ph)) + " — " + _t.T("shop.potion.full"));
            else
            {
                Para(s, _t.T("shop.potion", ("n", ph)) + " — " + _t.T("shop.price", ("p", pp)));
                Btn(s, "potion", _t.T("shop.buy") + ": " + _t.T("shop.potion.name"), ShopBuyPotion, _runs.CanBuyPotion(Run));
            }
            Para(s, Run.Runes.Count > 0 ? _t.T("shop.held", ("list", string.Join(", ", Run.Runes.Select(RuneName)))) : _t.T("shop.held.none"));
            if (shop.Upgrades.Count > 0)
            {
                Head(s, _t.T("shop.upgrades"));
                Para(s, _t.T("shop.upgrades.how"));
                foreach (var id in shop.Upgrades)
                {
                    bool sold = shop.Sold.Contains(id), can = _runs.CanUpgrade(Run, id);
                    if (sold || !can) { Para(s, HeroOf(id) + " · " + AbilityName(id, _runs.TierOf(Run, id)) + " — " + _t.T("shop.sold")); continue; }
                    int price = _runs.UpgradePrice(Run, id);
                    Para(s, UpgradeLine(id) + " — " + _t.T("shop.price", ("p", price)));
                    string pid = id;
                    Btn(s, "upg:" + id, _t.T("shop.buy") + ": " + AbilityName(id, _runs.TierOf(Run, id) + 1), () => ShopBuyUpgrade(pid), gold >= price);
                }
            }
            Head(s, _t.T("shop.runes") + "  " + Run.Runes.Count + "/" + _d.Run.RuneSlots);
            Para(s, _t.T("shop.runes.how", ("n", _d.Run.RuneSlots)));
            foreach (var id in shop.Runes)
            {
                var r = _d.RuneById[id]; bool sold = shop.Sold.Contains(id);
                int rp = _runs.RunePrice(Run, id);
                Para(s, RuneName(id) + " — " + RuneText(id) + " — " + (sold ? _t.T("shop.sold") : _t.T("shop.price", ("p", rp))));
                string rid = id;
                if (!sold) Btn(s, "rune:" + id, _t.T("shop.buy") + ": " + RuneName(id), () => ShopBuyRune(rid), gold >= rp && !full);
            }
            if (full) Para(s, _t.T("shop.full", ("n", _d.Run.RuneSlots)));
            Btn(s, "reroll", _t.T("shop.btn.reroll", ("p", _runs.RerollPrice(Run))), ShopReroll, _runs.CanReroll(Run));
            Para(s, _hint);
            Btn(s, "leave", Run.Floor >= _d.Run.Floors ? _t.T("shop.next.boss") : _t.T("shop.next", ("f", Run.Floor), ("n", _d.Run.Floors)), ShopLeave);
            return s;
        }

        private ScreenModel EndScreen()
        {
            var s = new ScreenModel { Title = _t.T(_endWin ? "run.win.title" : "run.lose.title") };
            Para(s, _endWin
                ? _t.T("run.win.text", ("n", _d.Run.Floors)) + (Col.Wins == 1 ? " " + _t.T("run.win.unlock") : "")
                : _t.T("run.lose.text", ("f", Run.Floor), ("n", _d.Run.Floors)));
            Btn(s, "again", _t.T("run.again"), NewRun);
            return s;
        }
    }
}