using System;
using System.Collections.Generic;
using System.Linq;
using Cubo.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cubo.App
{
    public enum Stage { Prep, Door, Battle, Blessing, Event, Shop, End }
    public enum ItemKind { Heading, Text, Button }

    // Uma linha de tela: um título de seção, um parágrafo ou um botão. Id identifica o botão (os testes clicam por ele).
    public sealed class ScreenItem
    {
        public ItemKind Kind;
        public string Id, Text;
        public bool Enabled = true, On;       // On: o botão está "ligado" (herói escolhido)
        public Action Click;
    }

    public sealed class ScreenModel
    {
        public string Title = "";
        public readonly List<ScreenItem> Items = new List<ScreenItem>();

        public ScreenItem Button(string id) => Items.FirstOrDefault(i => i.Kind == ItemKind.Button && i.Id == id);
        public IEnumerable<ScreenItem> Buttons => Items.Where(i => i.Kind == ItemKind.Button);
    }

    // O fluxo do jogo: preparação, porta, batalha, evento, loja e fim. É uma máquina de estados sem
    // Unity: cada tela é descrita em dados (ScreenModel) e cada botão chama um método daqui. A batalha em si é a
    // BattleSession; quando ela termina, quem a toca chama BattleFinished().
    public sealed partial class GameFlow
    {
        public const string RunFile = "corrida.json", CollectionFile = "colecao.json", SettingsFile = "ajustes.json";

        private readonly GameData _d;
        private readonly Texts _t;
        private readonly IFileStore _store;
        private readonly BattleEngine _engine;
        private readonly CollectionService _cols;
        private readonly RunService _runs;
        private readonly SaveCodec _codec;
        private readonly string _systemLang;
        private Mulberry32 _rng;                              // eventos, loja e a semente de cada batalha

        public Settings Settings { get; private set; } = new Settings();
        public Collection Col { get; private set; }
        public RunState Run { get; private set; }
        public Stage Stage { get; private set; } = Stage.Prep;
        public BattleSession Session { get; private set; }
        public BattleRecord Record { get; private set; }
        public BattleEngine Engine => _engine;
        public Texts Texts => _t;

        public event Action Changed;                          // a tela mudou: reconstruir
        public event Action<BattleSession> BattleCreated;     // há uma batalha nova (ou retomada) para ligar à tela

        private List<string> _prepTeam = new List<string>();
        private string _prepSel;                                  // o herói que a tela de heróis mostra em detalhe (não é salvo)
        private string _equipSel;                                 // o herói que a aba de equipamento está equipando (não é salvo)
        private string _equipFocus;                               // o equipamento que o painel do meio mostra (não é salvo)
        private string _hint = "";
        private string _blessFocus;
        private string _eventFocus;                                // a opção escolhida no evento (não é salva)
        private string _doorFocus;                                 // a porta tocada ("normal" ou "elite"; não é salva)

        // Um aviso para a próxima tela (o voto cumprido ou quebrado); some quando o jogador segue para a batalha.
        public string Notice { get; private set; }                                // a carta escolhida na bênção (não é salva)
        private string _shopFocus;                                 // a oferta que o painel da loja mostra (não é salva)
        private string _shopSay;                                   // a chave da fala do vendedor (null = a saudação)
        private bool _endWin;

        public GameFlow(GameData data, Texts texts, IFileStore store, uint seed, string systemLang = "pt")
        {
            _d = data; _t = texts; _store = store; _systemLang = systemLang;
            _engine = new BattleEngine(data);
            _cols = new CollectionService(data);
            _runs = new RunService(data, _cols, _engine);
            _codec = new SaveCodec(data, _cols);
            _rng = new Mulberry32(seed);
            Col = _cols.NewCollection();
        }

        // ---------------------------------------------------------------- começo e salvamento

        private static JToken Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            try { return JToken.Parse(text); } catch (Exception) { return null; }
        }

        // Lê os três arquivos. Arquivo ausente ou ruim vira estado novo, nunca exceção.
        public void Load()
        {
            Settings = Settings.Clean(Parse(_store.Read(SettingsFile)));
            _t.SetLang(Settings.Lang ?? Texts.Detect(_systemLang));
            Col = _codec.CollectionFrom(Parse(_store.Read(CollectionFile)));
            if (!TryRestoreRun(Parse(_store.Read(RunFile)))) EnterPrep(false);
            Changed?.Invoke();
        }

        public void SaveSettings() => _store.Write(SettingsFile, Settings.ToJson().ToString(Formatting.None));
        public void SaveCollection() => _store.Write(CollectionFile, _codec.ToJson(Col).ToString(Formatting.None));

        // A corrida e, se houver, a batalha em curso. Chame ao fim de cada comando, a cada troca de tela e ao pausar o app.
        public void SaveRun()
        {
            var o = new JObject { { "v", SaveCodec.Version }, { "stage", Stage.ToString() } };
            if (Run != null)
            {
                o["run"] = _codec.ToJson(Run);
                o["rng"] = (long)_rng.State;
                o["endWin"] = _endWin;
                if (Stage == Stage.Battle && Record != null && Session != null)
                    o["record"] = _codec.ToJson(Record, Session.Commands);
            }
            _store.Write(RunFile, o.ToString(Formatting.None));
        }

        public void SaveAll() { SaveSettings(); SaveCollection(); SaveRun(); }

        private bool TryRestoreRun(JToken j)
        {
            try
            {
                if (!(j is JObject o) || (int?)o["v"] != SaveCodec.Version || o["run"] == null) return false;
                var run = _codec.RunFrom(o["run"]);
                if (run == null || !Enum.TryParse((string)o["stage"], out Stage stage)) return false;
                _runs.EnsureHeroHp(run, Settings.Size, Settings.Mode);        // corridas salvas antes da vida por herói
                BattleSession session = null; BattleRecord rec = null;
                if (stage == Stage.Battle)
                {
                    rec = _codec.RecordFrom(o["record"]);
                    if (rec == null) return false;
                    session = BattleBuilder.Replay(_engine, _runs, rec);
                    if (session == null) return false;
                }
                else if (stage == Stage.Blessing && (run.Blessing == null || run.Blessing.Count == 0)) return false;
                else if (stage == Stage.Event && run.Event == null) return false;
                else if (stage == Stage.Shop && run.Shop == null) return false;
                else if (stage == Stage.Door && !run.Door) return false;
                else if (stage == Stage.Prep) return false;
                else if (stage == Stage.End && (bool?)o["endWin"] != true) return false;     // derrota: a única saída seria "de novo", então abre direto na preparação

                Run = run; Stage = stage; Record = rec; Session = session;
                _rng = new Mulberry32((uint)(long)o["rng"]);
                _endWin = (bool?)o["endWin"] == true;
                if (stage == Stage.Battle)
                {
                    BattleCreated?.Invoke(Session);
                    if (Session.State.Winner != null) BattleFinished();      // o app fechou com a batalha já decidida
                }
                return true;
            }
            catch (Exception) { return false; }
        }

        public void CycleLanguage()
        {
            int i = Array.IndexOf(Texts.Languages, _t.Lang);
            Settings.Lang = Texts.Languages[(i + 1) % Texts.Languages.Length];
            _t.SetLang(Settings.Lang);
            SaveSettings();
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- preparação

        private void EnterPrep(bool notify = true)
        {
            Run = null; Session = null; Record = null;
            Stage = Stage.Prep;
            _prepTeam = Col.Team.ToList();
            _prepSel = _prepTeam.Count > 0 ? _prepTeam[0] : _d.HeroIds.FirstOrDefault();
            _equipSel = null; _equipFocus = null;
            _hint = "";
            SaveRun();
            if (notify) Changed?.Invoke();
        }

        public bool PrepReady => _prepTeam.Count == 3;

        // Guarda o time a cada mudança, mas só quando ele está completo: um time com menos de 3 heróis é inválido
        // e, ao reabrir, seria trocado pelo padrão. Com um espaço vazio, vale o último time completo.
        private void SaveTeam()
        {
            if (_prepTeam.Count != 3) return;
            Col.Team = _prepTeam.ToArray();
            SaveCollection();
        }

        public void ToggleHero(string id)
        {
            if (Stage != Stage.Prep || !_d.Heroes.ContainsKey(id)) return;
            int k = _prepTeam.IndexOf(id);
            if (k >= 0) _prepTeam.RemoveAt(k);
            else if (_prepTeam.Count < 3) _prepTeam.Add(id);
            else { _hint = _t.T("prep.full"); Changed?.Invoke(); return; }
            _hint = "";
            SaveTeam();
            Changed?.Invoke();
        }

        // ---- tela de heróis: escolher para ver, arrastar para o time ----

        public string PrepSelected => _prepSel;
        public IReadOnlyList<string> PrepTeam => _prepTeam;

        // Toque num herói: mostra os detalhes dele. Não mexe no time.
        public void SelectHero(string id)
        {
            if (Stage != Stage.Prep || !_d.Heroes.ContainsKey(id) || id == _prepSel) return;
            _prepSel = id;
            Changed?.Invoke();
        }

        // Soltar um herói num espaço do time (0 a 2). Quem já estava no time troca de lugar com o do espaço;
        // quem veio de fora toma o lugar do que estava ali (ou entra no fim, se o espaço está vazio).
        public void PlaceHero(string id, int slot)
        {
            if (Stage != Stage.Prep || !_d.Heroes.ContainsKey(id) || slot < 0 || slot > 2) return;
            int from = _prepTeam.IndexOf(id);
            if (from >= 0)
            {
                int to = Math.Min(slot, _prepTeam.Count - 1);
                if (to != from) { string other = _prepTeam[to]; _prepTeam[to] = id; _prepTeam[from] = other; }
            }
            else if (slot < _prepTeam.Count) _prepTeam[slot] = id;
            else _prepTeam.Add(id);
            _prepSel = id;
            _hint = "";
            SaveTeam();
            Changed?.Invoke();
        }

        // Tirar um herói do time. O destaque continua nele.
        public void RemoveHero(string id)
        {
            if (Stage != Stage.Prep || !_prepTeam.Remove(id)) return;
            _hint = "";
            SaveTeam();
            Changed?.Invoke();
        }

        public void StartRun()
        {
            if (Stage != Stage.Prep || !PrepReady) return;
            Col.Team = _prepTeam.ToArray();
            SaveCollection();
            Run = _runs.Start(Col.Team, Col.Equip);
            _runs.PlanEvents(Run, _rng);                                       // os 3 eventos: em que andares e em que ordem
            if (_runs.CanElite(Col, Run)) { Run.Door = true; Stage = Stage.Door; SaveRun(); Changed?.Invoke(); }
            else StartBattle();
        }

        // ---- aba de equipamento: cada herói leva um, e cada equipamento fica com um herói só ----

        // O herói que a aba está equipando: o escolhido, se ainda está no time; senão o primeiro do time.
        public string EquipHero => _equipSel != null && _prepTeam.Contains(_equipSel) ? _equipSel : _prepTeam.FirstOrDefault();

        // O equipamento que o painel do meio mostra: o tocado por último; sem ele, o do herói da aba; sem esse, o primeiro.
        public string EquipFocus
        {
            get
            {
                if (_equipFocus != null && _d.EquipById.ContainsKey(_equipFocus)) return _equipFocus;
                string hero = EquipHero;
                if (hero != null && Col.Equip.TryGetValue(hero, out var mine)) return mine;
                return _d.Equipment.Count > 0 ? _d.Equipment[0].Id : null;
            }
        }

        // Toque num herói: ele passa a ser o equipado, e o painel do meio mostra o que ele leva (se leva algo).
        public void SelectEquipHero(string id)
        {
            if (Stage != Stage.Prep || !_prepTeam.Contains(id)) return;
            _equipSel = id;
            if (Col.Equip.TryGetValue(id, out var mine)) _equipFocus = mine;
            _hint = "";
            Changed?.Invoke();
        }

        // Toque num equipamento da grade: só mostra no painel do meio. Quem equipa é o botão (PressEquip).
        public void FocusEquip(string equipId)
        {
            if (Stage != Stage.Prep || !_d.EquipById.ContainsKey(equipId) || equipId == _equipFocus) return;
            _equipFocus = equipId;
            Changed?.Invoke();
        }

        // O botão do painel do meio: equipa, pega de outro herói ou tira o equipamento mostrado.
        public void PressEquip()
        {
            string focus = EquipFocus;
            if (focus != null) ToggleEquip(focus);
        }

        // Toque num equipamento: o herói da aba passa a levá-lo (sai de quem estava com ele e o dele volta ao baú).
        // Tocar no que ele já leva tira.
        public void ToggleEquip(string equipId)
        {
            string hero = EquipHero;
            if (Stage != Stage.Prep || hero == null || !_d.EquipById.ContainsKey(equipId)) return;
            if (Col.Equip.TryGetValue(hero, out var cur) && cur == equipId) Col.Equip.Remove(hero);
            else
            {
                foreach (var other in Col.Equip.Where(kv => kv.Value == equipId).Select(kv => kv.Key).ToList()) Col.Equip.Remove(other);
                Col.Equip[hero] = equipId;
            }
            _hint = "";
            SaveCollection();
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- porta e batalha

        // A porta desenhada: tocar numa porta só a escolhe; Entrar confirma.
        public void FocusDoor(string key)
        {
            if (Stage != Stage.Door || (key != "normal" && key != "elite") || key == _doorFocus) return;
            _doorFocus = key;
            Changed?.Invoke();
        }

        public void DoorAct() { if (_doorFocus != null) PickDoor(_doorFocus == "elite"); }

        public void PickDoor(bool elite)
        {
            if (Stage != Stage.Door || Run == null || !Run.Door) return;
            _doorFocus = null;
            Run.Door = false;
            Run.Elite = elite;
            StartBattle();
        }

        private uint NextSeed() => (uint)(_rng.Next() * 4294967296.0);

        private void StartBattle()
        {
            var ro = _runs.Opts(Run);
            Record = new BattleRecord
            {
                Seed = NextSeed(), AiLevel = _runs.Level(Run), Team = (string[])Run.Team.Clone(),
                Size = Settings.Size, Mode = Settings.Mode,
                EnemyScale = ro.EnemyScale, EnemyDmg = ro.EnemyDmg, PlayerHp = ro.PlayerHp, PlayerHeroHp = ro.PlayerHeroHp, ComboEnemy = ro.ComboEnemy,
                Tiers = ro.Tiers, Equip = ro.Equip,
                PlayerHeroMaxMod = ro.PlayerHeroMaxMod, CritPlayer = ro.CritPlayer, ComboPlayer = ro.ComboPlayer,
                MarkBonus = ro.MarkBonus, Vow = ro.Vow,
                DmgBonus = ro.DmgBonus, Ward = ro.Ward, StartMana = ro.StartMana, CostCut = ro.CostCut
            };
            Notice = null;                                                    // o aviso do voto já foi visto
            Session = BattleBuilder.Create(_engine, _runs, Record, Run);       // as runas de batalha são gastas aqui
            Stage = Stage.Battle;
            SaveRun();
            BattleCreated?.Invoke(Session);
            Changed?.Invoke();
        }

        // A batalha terminou e a tela já mostrou o resultado: decide para onde vai a corrida.
        public void BattleFinished()
        {
            if (Stage != Stage.Battle || Session == null || Session.State.Winner == null) return;
            bool win = Session.State.Winner == Side.Player, last = Run.Floor >= _d.Run.Floors;
            if (Session.BestCombo > Col.BestCombo) Col.BestCombo = Session.BestCombo;   // o recorde vale mesmo numa derrota
            if (win && last)
            {
                // o chefe caiu: a corrida está vencida
                _cols.Win(Col, Run.Team);
                SaveCollection();
                _endWin = true; Stage = Stage.End;
            }
            else if (win)
            {
                // andar vencido: ouro e vida, depois a bênção, o evento (às vezes), a loja e a próxima batalha
                if (Run.Elite) Col.Elites += 1;
                SaveCollection();
                var vow = _runs.CheckVow(Run, Session.State);                  // o voto do Juramento (se havia)
                Notice = vow == null ? null : vow.Kept ? _t.T("vow.kept", ("g", vow.Gold)) : _t.T("vow.broken");
                _runs.Advance(Run, Session.State);
                var blessing = _runs.NewBlessing(Run, _rng);
                Run.Blessing = blessing.Count > 0 ? blessing : null;
                Run.Shop = _runs.NewShop(Run, _rng);
                _shopFocus = null; _shopSay = null; _blessFocus = null; _eventFocus = null;
                Run.Event = _runs.EventFor(Run);                             // os 3 eventos da corrida, nos andares sorteados
                Resume();
                return;
            }
            else { SaveCollection(); _endWin = false; Stage = Stage.End; }
            SaveRun();
            Changed?.Invoke();
        }

        // O que a corrida espera antes da próxima batalha: a bênção, o evento, a loja ou a porta.
        private void Resume()
        {
            if (Run.Blessing != null) Stage = Stage.Blessing;
            else if (Run.Event != null) Stage = Stage.Event;
            else if (Run.Shop != null) Stage = Stage.Shop;
            else if (Run.Door) Stage = Stage.Door;
            else { StartBattle(); return; }
            SaveRun();
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- bênção

        public void PickBlessing(string powerId)
        {
            if (Stage != Stage.Blessing || !_runs.Bless(Run, powerId)) return;
            _blessFocus = null;
            Resume();
        }

        // A bênção desenhada: tocar numa carta a escolhe (tocar de novo não muda nada); o botão aceita a escolhida.
        public void FocusBlessing(string id)
        {
            if (Stage != Stage.Blessing || Run.Blessing == null || !Run.Blessing.Contains(id) || id == _blessFocus) return;
            _blessFocus = id;
            Changed?.Invoke();
        }

        public void AcceptBlessing()
        {
            if (_blessFocus != null) PickBlessing(_blessFocus);
        }

        // ---------------------------------------------------------------- evento

        // O evento desenhado: tocar numa opção a escolhe; o botão confirma (ou, depois do resultado, continua).
        public void FocusEvent(string key)
        {
            if (Stage != Stage.Event || Run.Event == null || Run.Event.Done != null || key == _eventFocus) return;
            if (!_runs.EventOptions(Run).Any(o => o.Key == key && o.Key != "leave")) return;
            _eventFocus = key;
            Changed?.Invoke();
        }

        public void EventAct()
        {
            if (Stage != Stage.Event || Run.Event == null) return;
            if (Run.Event.Done != null) EventPick("continue");
            else if (_eventFocus != null) EventPick(_eventFocus);
        }

        public void EventLeave() => EventPick("leave");

        public void EventPick(string key)
        {
            if (Stage != Stage.Event || Run.Event == null) return;
            if (Run.Event.Done != null || key == "leave")
            {
                _eventFocus = null;
                Run.Event = null;
                Resume();
                return;
            }
            var res = _runs.EventChoose(Run, key, _rng);
            if (res == null) return;
            SaveRun();
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- loja

        public void ShopBuyRune(string id)
        {
            if (Stage != Stage.Shop || !_runs.BuyRune(Run, id)) return;
            _hint = _t.T("shop.bought.rune", ("name", RuneName(id)));
            SaveRun();
            Changed?.Invoke();
        }

        public void ShopBuyUpgrade(string powerId)
        {
            if (Stage != Stage.Shop || !_runs.BuyUpgrade(Run, powerId)) return;
            _hint = _t.T("shop.bought.upgrade", ("name", AbilityName(powerId, _runs.TierOf(Run, powerId))));
            SaveRun();
            Changed?.Invoke();
        }

        public void ShopBuyPotion()
        {
            if (Stage != Stage.Shop) return;
            int heal = _runs.PotionHeal(Run);
            if (!_runs.BuyPotion(Run)) return;
            _hint = _t.T("shop.bought.potion", ("n", heal));
            SaveRun();
            Changed?.Invoke();
        }

        public void ShopReroll()
        {
            if (Stage != Stage.Shop || !_runs.Reroll(Run, _rng)) return;
            _shopSay = "shop.say.reroll";
            SaveRun();
            Changed?.Invoke();
        }

        // ---- a loja desenhada (ShopScreen) ----

        // Tocou numa oferta: ela vai para o painel de detalhe. O vendedor comenta se não dá para comprar.
        public void FocusShop(string offerId)
        {
            if (Stage != Stage.Shop || Run.Shop == null) return;
            _shopFocus = offerId;
            var o = ShopScreen().All.FirstOrDefault(x => x.Id == offerId);
            if (o != null && !o.Sold && !o.CanBuy)
                _shopSay = !o.Affordable ? "shop.say.poor" : o.Kind == "rune" ? "shop.say.full" : o.Kind == "potion" ? "shop.say.healthy" : _shopSay;
            Changed?.Invoke();
        }

        // O botão do painel: compra a oferta em foco (ou troca as ofertas, se o foco é a Troca).
        public void ShopBuyFocused()
        {
            if (Stage != Stage.Shop || Run.Shop == null || _shopFocus == null) return;
            string f = _shopFocus;
            bool ok;
            if (f == "reroll") { ok = _runs.Reroll(Run, _rng); if (ok) _shopSay = "shop.say.reroll"; }
            else if (f == "potion") ok = _runs.BuyPotion(Run);
            else if (f.StartsWith("rune:")) ok = _runs.BuyRune(Run, f.Substring(5));
            else if (f.StartsWith("upg:")) ok = _runs.BuyUpgrade(Run, f.Substring(4));
            else ok = false;
            if (!ok) return;
            if (f != "reroll") _shopSay = Run.Shop.Sold.Count % 2 == 1 ? "shop.say.thanks.1" : "shop.say.thanks.2";
            SaveRun();
            Changed?.Invoke();
        }

        public void ShopLeave()
        {
            if (Stage != Stage.Shop) return;
            Run.Shop = null;
            _shopFocus = null; _shopSay = null;
            _hint = "";
            if (_runs.CanElite(Col, Run)) { Run.Door = true; Stage = Stage.Door; SaveRun(); Changed?.Invoke(); }
            else StartBattle();
        }

        // ---------------------------------------------------------------- fim

        public void NewRun() { if (Stage == Stage.End) EnterPrep(); }
    }
}