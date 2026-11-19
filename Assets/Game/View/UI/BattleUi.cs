using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Cubo.App;
using Cubo.Domain;

namespace Cubo.View
{
    // A interface da batalha. O layout mora no prefab BattleHud, montado no editor; este script não cria
    // nenhum objeto. Ele só copia o HudView (já calculado e com texto) para as partes da tela e manda os
    // toques para a sessão. Nenhuma conta de custo, mana ou idioma acontece aqui.
    public sealed class BattleUi : MonoBehaviour
    {

        [Header("Grupos")]
        [Tooltip("CanvasGroup da raiz: some com a interface inteira fora da batalha.")]
        [SerializeField] private CanvasGroup screen;
        [Tooltip("CanvasGroup do painel de baixo: desliga poderes, mão e botões enquanto um lote toca.")]
        [SerializeField] private CanvasGroup controls;
        [SerializeField] private SafeArea safeArea;
        [Tooltip("Um RectTransform vazio que marca onde o cubo aparece. A câmera desenha só ali.")]
        [SerializeField] private RectTransform cubeArea;

        [Header("Partes")]
        [SerializeField] private HpBar enemyHp;
        [SerializeField] private HpBar playerHp;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private TMP_Text bannerText;
        [SerializeField] private TextButton langButton;
        [Tooltip("Um botão só com ícone que volta a vista para a sua face.")]
        [SerializeField] private Button homeButton;
        [Tooltip("O balão da entrega (Confirmar e Cancelar), que abre onde a face foi tocada.")]
        [SerializeField] private HandPopup handPopup;
        [Tooltip("Opcional: o palco no alto da tela, com o cenário e os heróis dos dois lados.")]
        [SerializeField] private BattleStage stage;
        [Tooltip("O painel de poderes no alto: os 6 do jogador e os 6 do rival, com o detalhe ao tocar.")]
        [SerializeField] private PowerBoard powerBoard;
        [SerializeField] private UiTheme theme;

        public event Action LayoutChanged;                   // a região do cubo mudou: reenquadrar a câmera
        public event Action LanguageRequested;               // o botão de idioma; sem ninguém ouvindo, troca aqui mesmo
        public Rect FreeArea { get; private set; } = new Rect(0f, 0f, 1f, 1f);   // a região do cubo, de 0 a 1

        public PowerBoard PowerBoard => powerBoard;
        public UiTheme Theme => theme;
        public Button HomeButton => homeButton;
        public HandPopup HandPopup => handPopup;

        private CubeView _cube;
        private CubeInput _input;
        private BattlePresenter _presenter;
        private CubeOrbit _orbit;

        private BattleSession _session;
        private Texts _texts;
        private HudView _view;
        private int _face = -1;                              // a face escolhida para entregar (física, 0 a 3), ou -1
        private string _playerLabel = "", _enemyLabel = "";  // "Sua vida · Crítico 5%", refeitos em Refresh
        private bool _lastAwaiting;
        private readonly Vector3[] _corners = new Vector3[4];

        // O que não mora no prefab vem da cena. O GameBootstrap chama isto uma vez, antes do primeiro Bind.
        public void Init(CubeView cube, CubeInput input, BattlePresenter presenter, CubeOrbit orbit)
        {
            _cube = cube;
            _input = input;
            _presenter = presenter;
            _orbit = orbit;
            if (_orbit != null) _orbit.Changed += OnViewMoved;
        }

        // ---------------------------------------------------------------- montagem

        private void Awake()
        {
            var missing = MissingReference();
            if (missing != null)
            {
                Debug.LogError("BattleHud: falta ligar no Inspector: " + missing, this);
                enabled = false;
                return;
            }
            langButton.OnClick(NextLanguage);
            homeButton.onClick.AddListener(() => _orbit?.Home());
            handPopup.Init();
            handPopup.Confirmed += OnHand;
            handPopup.Cancelled += CancelHand;
            handPopup.Hide();
            powerBoard.UseRequested += OnPower;
            safeArea.Changed += UpdateFreeArea;
            bannerText.text = "";
        }

        private void Start()
        {
            if (EventSystem.current == null)
                Debug.LogError("A cena não tem EventSystem: GameObject > UI > Event System (e troque o módulo pelo do Input System).", this);
            UpdateFreeArea();
        }

        private void OnDestroy() => Unbind();

        // Liga ou desliga a interface inteira (na Fase 6, as outras telas ficam por cima dela).
        public void SetVisible(bool visible)
        {
            screen.alpha = visible ? 1f : 0f;
            screen.interactable = visible;
            screen.blocksRaycasts = visible;
        }

        // Num Canvas Screen Space Overlay os cantos de um RectTransform já estão em pixels de tela.
        private void UpdateFreeArea()
        {
            Canvas.ForceUpdateCanvases();
            cubeArea.GetWorldCorners(_corners);
            FreeArea = UiMath.ToViewport(_corners[0], _corners[2], Screen.width, Screen.height);
            LayoutChanged?.Invoke();
        }

        // ---------------------------------------------------------------- ligação com a batalha

        public void Bind(BattleSession session, Texts texts)
        {
            if (_presenter == null || _cube == null || _input == null || _orbit == null)
            {
                Debug.LogError("BattleUi: chame Init(cube, input, presenter, orbit) antes de Bind.", this);
                return;
            }
            Unbind();
            _session = session;
            _texts = texts;
            _presenter.BatchStarted += OnBatchStarted;
            _presenter.BatchFinished += OnBatchFinished;
            _presenter.ManaShown += OnManaShown;
            _presenter.HeroHpShown += OnHeroHp;
            _presenter.TargetShown += OnTarget;
            if (stage != null) stage.EnemyTapped += OnEnemyTapped;
            if (stage != null) stage.StatusTapped += OnStatusTapped;
            _texts.Changed += Refresh;
            _face = -1;
            powerBoard.Init(_cube.FaceSprite, _cube.PaletteColor, id => stage != null ? stage.FaceOf(id) : null);
            powerBoard.HideTip();
            controls.interactable = true;
            if (stage != null) stage.Show(session.State, UnityEngine.Random.Range(0, int.MaxValue));   // o cenário é só enfeite
            Refresh();
        }

        private void Unbind()
        {
            if (_session == null) return;
            _presenter.BatchStarted -= OnBatchStarted;
            _presenter.BatchFinished -= OnBatchFinished;
            _presenter.ManaShown -= OnManaShown;
            _presenter.HeroHpShown -= OnHeroHp;
            _presenter.TargetShown -= OnTarget;
            if (stage != null) stage.EnemyTapped -= OnEnemyTapped;
            if (stage != null) stage.StatusTapped -= OnStatusTapped;
            _texts.Changed -= Refresh;
            _session = null;
        }

        private void OnBatchStarted()
        {
            if (handPopup != null) handPopup.Hide();
            powerBoard.HideTip();
            controls.interactable = false;
        }

        // A mana do jogador anda junto com a animação: a barra enche no momento do grupo, não só ao fim do turno do rival.
        private void OnManaShown(Side side, int[] mana)
        {
            if (_view == null) return;
            powerBoard.SetMana(side, mana);
        }

        private void OnHeroHp(Side side, int hero, int hp, int max)
        {
            if (stage != null) stage.MoveHp(side, hero, hp, max);
        }

        private void OnTarget(Side side, int hero)
        {
            if (stage != null) stage.MarkTarget(side, hero);
            if (side == Side.Player) powerBoard.MarkEnemyTarget(hero);
        }

        // Tocar num ícone de efeito (veneno, fogo) em cima de uma barra: o balão dos poderes com quanto tira e quantas rodadas faltam.
        private void OnStatusTapped(string detail, RectTransform at)
        {
            if (!string.IsNullOrEmpty(detail)) powerBoard.ShowInfo(detail, at);
        }

        // 3 contra 3: tocar num rival escolhe quem o jogador ataca neste turno.
        private void OnEnemyTapped(int hero)
        {
            if (_session != null) _session.TryTarget(hero);
        }

        private void OnBatchFinished()
        {
            controls.interactable = true;
            Refresh();
        }

        // Relê o estado e refaz tudo o que a tela mostra, menos a vida (que anda sozinha, em Update).
        private void Refresh()
        {
            if (_session == null) return;
            _view = HudModel.Build(_session, _texts);
            if (!_view.Handing || !_session.CanHand(_face)) _face = -1;
            if (_face < 0 && handPopup != null) handPopup.Hide();

            langButton.Text = _texts.Lang.ToUpperInvariant();
            _playerLabel = _texts.T("hp.player") + " · " + _view.CritPlayer + (_view.ComboPlayer.Length > 0 ? " · " + _view.ComboPlayer : "");
            _enemyLabel = _texts.T("hp.enemy") + " · " + _view.CritEnemy + (_view.ComboEnemy.Length > 0 ? " · " + _view.ComboEnemy : "");
            if (!_presenter.Playing) _presenter.MarkFaces(_view.Handing ? _face : -1, _view.Handing ? _session.BannedFace : -1);
            _lastAwaiting = false;                       // refaz o aviso de fim no idioma novo

            powerBoard.Show(_view.Powers, _view.EnemyPowers);
            if (stage != null) stage.ShowTeams(_view.PlayerTeam, _view.EnemyTeam);
            powerBoard.MarkEnemyTarget(TargetedIndex(_view.EnemyTeam));
            ShowInfo();
        }

        private static int TargetedIndex(FighterView[] team)
        {
            if (team != null) for (int i = 0; i < team.Length; i++) if (team[i].Targeted) return i;
            return -1;
        }

        private void ShowInfo()
        {
            if (_view == null) return;
            infoText.text = _view.Handing ? _view.HandText : _view.TurnText;
        }

        // ---------------------------------------------------------------- toques

        private void OnPower(int slot)
        {
            if (_session != null) _session.TryCast(slot);
        }

        // ---- a entrega (Fase 10) ----

        // O dedo tocou uma peça na fase da entrega: a face dela fica escolhida (se puder ser entregue).
        // Tocar de novo na mesma face desfaz a escolha.
        // Tocar numa face que pode ir a escolhe e abre o balão ali; tocar em outra que não pode desfaz.
        public void PickFace(int cell, Vector2 screenPos)
        {
            if (_session == null || !_session.IsHanding || _cube.Geo == null) return;
            int face = _cube.Geo.CellFace[cell];
            if (_session.CanHand(face))
            {
                _face = face;
                handPopup.Show(screenPos);
            }
            else
            {
                _face = -1;
                handPopup.Hide();
            }
            Refresh();
        }

        // O toque está em cima do balão aberto: é dele, não do cubo.
        public bool BlocksCube(Vector2 screenPos) => handPopup != null && handPopup.Contains(screenPos);

        private void CancelHand()
        {
            _face = -1;
            Refresh();
        }

        // A vista girou com o balão aberto: ele ficaria apontando para o lugar errado, então a escolha se desfaz.
        private void OnViewMoved()
        {
            if (handPopup != null && handPopup.IsOpen) { handPopup.Hide(); CancelHand(); }
        }

        public int PickedFace => _face;

        private void OnHand()
        {
            if (handPopup != null) handPopup.Hide();
            if (_session == null || _face < 0) return;
            if (_session.TryHand(_face)) _face = -1;
        }

        private void NextLanguage()
        {
            if (LanguageRequested != null) { LanguageRequested(); return; }
            if (_texts == null) return;
            int i = Array.IndexOf(Texts.Languages, _texts.Lang);
            _texts.SetLang(Texts.Languages[(i + 1) % Texts.Languages.Length]);
        }

        // ---------------------------------------------------------------- por quadro

        private void Update()
        {
            if (_session == null) return;

            if (enemyHp != null) enemyHp.Show(_presenter.Hp[1], _presenter.MaxHp[1], _enemyLabel);     // opcional: a vida somada do time
            if (playerHp != null) playerHp.Show(_presenter.Hp[0], _presenter.MaxHp[0], _playerLabel);

            bool awaiting = _presenter.AwaitingRestart;
            if (awaiting && !_lastAwaiting) bannerText.text = EndBanner();
            else if (!awaiting && bannerText.text != _presenter.Banner) bannerText.text = _presenter.Banner;
            _lastAwaiting = awaiting;
        }

        private string EndBanner() =>
            _texts.T(_presenter.Winner == Side.Player ? "end.win" : "end.lose") + "\n" + _texts.T("ev.opt.continue");

        // ---------------------------------------------------------------- conferência (testes e Awake)

        // O nome do primeiro campo sem ligar (com o caminho, por exemplo "powers[2].mana"), ou null se está tudo ligado.
        public string MissingReference()
        {
            if (screen == null) return "screen";
            if (controls == null) return "controls";
            if (safeArea == null) return "safeArea";
            if (cubeArea == null) return "cubeArea";
            if (enemyHp != null && enemyHp.MissingReference() is string a) return "enemyHp." + a;
            if (playerHp != null && playerHp.MissingReference() is string b) return "playerHp." + b;
            if (infoText == null) return "infoText";
            if (bannerText == null) return "bannerText";
            if (langButton == null) return "langButton";
            if (langButton.MissingReference() is string l) return "langButton." + l;
            if (homeButton == null) return "homeButton";
            if (stage != null && stage.MissingReference() is string st) return "stage." + st;
            if (handPopup == null) return "handPopup";
            if (handPopup.MissingReference() is string hp) return "handPopup." + hp;
            if (powerBoard == null) return "powerBoard";
            if (powerBoard.MissingReference() is string pb) return "powerBoard." + pb;
            if (theme == null) return "theme";
            return null;
        }
    }
}