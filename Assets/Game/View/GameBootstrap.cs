using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Cubo.App;
using Cubo.Data;

namespace Cubo.View
{
    // O ponto de montagem: cria o GameFlow e liga o que ele decide à tela. Não tem regra de jogo.
    // Em batalha mostra o cubo e a interface da batalha; nas outras telas, o ScreenUi por cima.
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const float AfterBattleSeconds = 0.7f;      // o resultado fica à vista este tempo; um toque adianta

        [SerializeField] private GameContent content;
        [SerializeField] private CubeView cube;
        [SerializeField] private CubeInput input;
        [SerializeField] private CubeOrbit orbit;
        [SerializeField] private BattlePresenter presenter;
        [SerializeField] private BattleUi ui;
        [SerializeField] private ScreenUi screens;
        [SerializeField] private GodsScreen gods;
        [Tooltip("A aba Arsenal desenhada. Vazio: a aba mostra a lista simples do ScreenUi.")]
        [SerializeField] private EquipScreen equip;
        [Tooltip("Opcional: a loja desenhada (ShopScreen). Vazio: a loja aparece na tela em lista.")]
        [SerializeField] private ShopScreen shop;
        [Tooltip("Opcional: a bênção desenhada (BlessingScreen). Vazio: a bênção aparece na tela em lista.")]
        [SerializeField] private BlessingScreen blessing;
        [Tooltip("Opcional: as telas desenhadas dos eventos (uma por evento: Ídolo, Acampamento, Juramento). Um evento sem tela aparece em lista.")]
        [SerializeField] private EventScreen[] events = new EventScreen[0];
        [Tooltip("Opcional: a porta desenhada (comum ou elite). Vazio = a lista de sempre.")]
        [SerializeField] private DoorScreen door;
        [Tooltip("A barra de baixo da preparação. Os itens têm de estar na ordem de Tab: Heróis, Batalha, Equipamento.")]
        [SerializeField] private TabBar tabs;
        [SerializeField] private Camera cam;
        [Tooltip("Folga em volta do cubo dentro da área do cubo. 1,15 = o cubo quase encosta; maior = cubo menor na tela. " +
                 "A área de toque continua a mesma, então sobra espaço dos lados para girar a vista.")]
        [SerializeField, Min(1f)] private float cubeMargin = 1.15f;
        [SerializeField] private uint seed;                // 0 = uma semente nova a cada abertura
        [Tooltip("Mostra os números de FPS, tempo de quadro e memória no canto da tela. Desligue para a build de teste.")]
        [SerializeField] private bool showPerfOverlay = true;

        private GameFlow _flow;
        public GameFlow Flow => _flow;                         // para as ferramentas de teste do editor (Cubo ▸ Teste)
        private BattleSession _session;
        private int _cubeSize;
        private float _after;
        private enum Tab { Gods = 0, Battle = 1, Equip = 2 }      // o índice de cada aba nos Items da barra
        private Tab _tab = Tab.Gods;                               // a aba da preparação em que o jogador está

        // 60 quadros por segundo e, em desenvolvimento, os números de desempenho na tela.
        private void Awake()
        {
            QualitySettings.vSyncCount = 0;               // o targetFrameRate só vale com vsync desligado
            Application.targetFrameRate = 60;
            if (showPerfOverlay && Debug.isDebugBuild) gameObject.AddComponent<PerfOverlay>();
        }

        private void Start()
        {
            var data = ContentLoader.Load(content);
            var texts = new Texts(content);
            var store = new DiskFileStore(Application.persistentDataPath);
            _flow = new GameFlow(data, texts, store, seed != 0 ? seed : (uint)Environment.TickCount, SystemLang());

            ui.Init(cube, input, presenter, orbit);
            ui.LayoutChanged += Reframe;
            ui.LanguageRequested += _flow.CycleLanguage;
            screens.LanguageRequested += _flow.CycleLanguage;
            gods.Init(cube.PaletteColor);
            gods.GodSelected += _flow.SelectHero;
            gods.GodPlaced += _flow.PlaceHero;
            gods.GodRemoved += _flow.RemoveHero;
            if (equip != null)
            {
                equip.HeroSelected += _flow.SelectEquipHero;
                equip.ItemFocused += _flow.FocusEquip;
                equip.ActionPressed += _flow.PressEquip;
            }
            if (shop != null)
            {
                shop.OfferFocused += _flow.FocusShop;
                shop.BuyPressed += _flow.ShopBuyFocused;
                shop.LeavePressed += _flow.ShopLeave;
            }
            if (blessing != null)
            {
                blessing.CardFocused += _flow.FocusBlessing;
                blessing.AcceptPressed += _flow.AcceptBlessing;
            }
            if (door != null)
            {
                door.DoorFocused += _flow.FocusDoor;
                door.EnterPressed += _flow.DoorAct;
            }
            foreach (var ev in events)
            {
                if (ev == null) continue;
                ev.OptionFocused += _flow.FocusEvent;
                ev.ActPressed += _flow.EventAct;
                ev.LeavePressed += _flow.EventLeave;
            }
            tabs.Pressed += i =>
            {
                if ((Tab)i == Tab.Battle) _flow.StartRun(); else SetTab((Tab)i);
            };
            input.IsBlocked = ui.BlocksCube;
            input.Tapped = cell =>
            {
                if (_session == null) return;
                if (_session.IsHanding) { ui.PickFace(cell, input.TapPosition); return; }     // Fase 10: depois do grupo, o toque escolhe a face
                if (!_session.TryFire(cell)) Debug.Log("Sem grupo nessa peça");
            };
            input.Touched = cell => { if (_session != null) presenter.Highlight(_session.GroupPreview(cell)); };
            input.Untouched = presenter.ClearHighlight;
            // Veneno e fogo: depois da entrega, segurar o dedo mira a área e soltar a põe.
            input.AimMode = () => _session != null && _session.IsZoning;
            input.Aimed = cell => { if (_session != null) presenter.Aim(_session.ZonePreview(cell), _session.ZoneKind); };
            input.AimDone = cell =>
            {
                presenter.ClearAim();
                if (_session != null && !_session.TryZone(cell)) Debug.Log("A área precisa ficar na face da frente");
            };

            if (Debug.isDebugBuild) gameObject.AddComponent<ScreenPicker>().Init(_flow);   // teste: o botão "Screens" leva a qualquer tela
            _flow.BattleCreated += OnBattle;
            _flow.Changed += OnChanged;
            _flow.Load();                                     // lê os três arquivos e volta para onde o jogador parou
        }

        private static string SystemLang()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Portuguese: return "pt";
                case SystemLanguage.Spanish: return "es";
                default: return "en";
            }
        }

        // Uma batalha nova (ou retomada): liga cubo, apresentador e interface à sessão.
        private void OnBattle(BattleSession s)
        {
            _session = s; _after = 0f;
            if (_cubeSize != s.State.N)                       // primeira batalha, ou outro tamanho (uma batalha 5x5 salva antes do 6x6)
            {
                _cubeSize = s.State.N;
                cube.Build(_cubeSize);
                Reframe();
            }
            orbit.ResetView();                                // cada batalha começa olhando a frente, com a inclinação leve de sempre
            cube.Paint(s.State.Colors);
            presenter.Attach(s, _flow.Texts);
            ui.Bind(s, _flow.Texts);
            s.Played += _ => { if (s == _flow.Session) _flow.SaveRun(); };   // cada comando aceito já fica salvo
        }

        private EventScreen EventScreenFor(string id)
        {
            foreach (var ev in events) if (ev != null && ev.EventId == id) return ev;
            return null;
        }

        private void SetTab(Tab tab)
        {
            _tab = tab;
            OnChanged();
        }

        // O fluxo mudou de tela: mostra a batalha, a aba Heróis ou a lista certa. A barra de abas só existe na preparação.
        private void OnChanged()
        {
            bool battle = _flow.Stage == Stage.Battle;
            bool prep = _flow.Stage == Stage.Prep;
            if (!prep) _tab = Tab.Gods;                         // cada preparação nova abre em Heróis
            bool showGods = prep && _tab == Tab.Gods;
            bool showEquip = prep && _tab == Tab.Equip && equip != null;
            ui.SetVisible(battle);
            input.Locked = !battle || presenter.Ended;
            input.ViewLocked = !battle || presenter.Ended;

            bool showShop = _flow.Stage == Stage.Shop && shop != null;
            bool showBlessing = _flow.Stage == Stage.Blessing && blessing != null;
            var eventScreen = _flow.Stage == Stage.Event && _flow.Run?.Event != null ? EventScreenFor(_flow.Run.Event.Id) : null;
            bool showDoor = _flow.Stage == Stage.Door && door != null;
            if (battle || showGods || showEquip || showShop || showBlessing || showDoor || eventScreen != null) screens.Hide();
            else screens.Show(_flow.Screen(), _flow.Texts.Lang);

            var view = prep ? _flow.HeroesScreen() : null;
            if (showGods) gods.Show(view); else gods.Hide();
            if (equip != null) { if (showEquip) equip.Show(_flow.EquipScreen()); else equip.Hide(); }
            if (shop != null) { if (showShop) shop.Show(_flow.ShopScreen()); else shop.Hide(); }
            if (blessing != null) { if (showBlessing) blessing.Show(_flow.BlessingView()); else blessing.Hide(); }
            if (door != null) { if (showDoor) door.Show(_flow.DoorView()); else door.Hide(); }
            foreach (var ev in events)
            {
                if (ev == null) continue;
                if (ev == eventScreen) ev.Show(_flow.EventView()); else ev.Hide();
            }
            if (prep) ShowTabs(view);
            tabs.SetVisible(prep);
        }

        // Textos no idioma atual, Batalha travada sem time completo, e a aba certa acesa.
        private void ShowTabs(HeroesView view)
        {
            tabs.SetLabel((int)Tab.Gods, view.TabHeroes);
            tabs.SetLabel((int)Tab.Battle, view.TabBattle);
            tabs.SetLabel((int)Tab.Equip, view.TabEquip);
            tabs.SetLocked((int)Tab.Battle, !view.CanBattle);
            tabs.SetIndex((int)_tab);
        }

        // O cubo ocupa só a faixa do meio; o resto é da interface.
        private void Reframe()
        {
            if (_cubeSize == 0) return;
            cam.rect = ui.FreeArea;
            CubeCamera.Frame(cam, _cubeSize, cubeMargin);
        }

        // Fim da batalha: um toque na área do cubo, ou 0,7 s, e o fluxo segue.
        private void Update()
        {
            if (_flow == null || _flow.Stage != Stage.Battle || !presenter.AwaitingRestart) { _after = 0f; return; }
            _after += Time.unscaledDeltaTime;
            var ptr = Pointer.current;
            bool tap = ptr != null && ptr.press.wasPressedThisFrame && cam.pixelRect.Contains(ptr.position.ReadValue());
            if (tap || _after >= AfterBattleSeconds) _flow.BattleFinished();
        }

        private void OnApplicationPause(bool paused) { if (paused && _flow != null) _flow.SaveAll(); }
        private void OnApplicationQuit() { if (_flow != null) _flow.SaveAll(); }
    }
}