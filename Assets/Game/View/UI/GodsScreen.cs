using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cubo.App;

namespace Cubo.View
{
    // A aba Heróis da preparação (Fase 10: heróis no lugar dos deuses; o script e o prefab mantêm o nome
    // GodsScreen para não perder as ligações): o herói em destaque com seus poderes e a passiva, a grade de heróis
    // e o time de três. Este script só copia o HeroesView para os componentes e avisa o que o jogador fez
    // (tocou, soltou, tirou); quem muda o time é o GameFlow.
    public sealed class GodsScreen : MonoBehaviour
    {
        private const int AbilityCount = 2;

        [Header("Grupos")]
        [Tooltip("CanvasGroup da raiz: mostra e esconde a tela inteira.")]
        [SerializeField] private CanvasGroup screen;
        [SerializeField] private ScrollRect scroll;
        [Tooltip("O Content da grade: os cartões nascem aqui dentro.")]
        [SerializeField] private RectTransform grid;
        [Tooltip("O prefab GodCard, arrastado do Project (não da Hierarchy).")]
        [SerializeField] private GodCard cardPrefab;

        [Header("Time")]
        [SerializeField] private TMP_Text teamLabel;
        [Tooltip("Os três espaços do time, de cima para baixo.")]
        [SerializeField] private TeamSlot[] slots;

        [Header("Herói em destaque")]
        [SerializeField] private TMP_Text godName;
        [Tooltip("A etiqueta embaixo da arte: o papel do herói (Controle, Força bruta...).")]
        [SerializeField] private TMP_Text civName;
        [SerializeField] private TMP_Text abilitiesLabel;
        [SerializeField] private TMP_Text passiveLabel;
        [Tooltip("As duas habilidades, na ordem.")]
        [SerializeField] private AbilityRow[] abilities;
        [SerializeField] private AbilityRow passive;
        [SerializeField] private Color passiveColor = new Color(0.55f, 0.55f, 0.62f);
        [Tooltip("Um item por passiva: o id de heroes.json (decreto_real, investida, trapaca, comunhao) e o ícone. Sem ícone, o quadrado cinza.")]
        [SerializeField] private PassiveIcon[] passiveIcons;
        [Tooltip("Opcional: a moldura das passivas, a mesma para todas. O ícone da passiva aparece dentro dela.")]
        [SerializeField] private Sprite passiveFrame;
        [Tooltip("A linha grande do meio da tela: mostra o ícone e a descrição completa da habilidade tocada. Opcional.")]
        [SerializeField] private AbilityRow detail;

        [Header("Retratos dos heróis")]
        [Tooltip("O Image do GodArt: o retrato grande do herói em destaque.")]
        [SerializeField] private Image godArt;
        [Tooltip("Um item por herói: Sprite = retrato grande (GodArt); Card = retrato pequeno da grade, do time e do arrasto (opcional: sem ele vale o grande); Frames + Fps = retrato grande animado (opcional). Quem não estiver na lista fica sem imagem.")]
        [SerializeField] private HeroPortrait[] portraits;
        [Tooltip("O tamanho de quadro de referência dos retratos grandes, em pixels (o maior dos quadros, hoje 80). Todos os " +
                 "heróis saem com o mesmo tamanho de pixel, mesmo exportados em quadros menores (68, 64). 0 = cada um enche o GodArt.")]
        [SerializeField] private int portraitCanvas = 80;
        [Tooltip("Duração do fade ao trocar o retrato, em segundos. 0 desliga a animação.")]
        [SerializeField] private float portraitFade = 0.18f;
        [Tooltip("Quantos pixels o retrato desliza (vindo da direita) ao entrar.")]
        [SerializeField] private float portraitSlide = 28f;
        [Tooltip("Opcional: a luz atrás do herói em destaque, nas cores dos domínios dele.")]
        [SerializeField] private HeroGlow glow;

        [Header("Entrada da tela")]
        [Tooltip("Duração do fade da tela inteira ao aparecer. 0 = aparece na hora.")]
        [SerializeField] private float fadeInSeconds = 0.2f;
        [Tooltip("Opcional: os grupos que entram em cascata quando a tela aparece, na ordem (ex.: habilidades, time, grade).")]
        [SerializeField] private UiEnter[] entrance;
        [Tooltip("Segundos entre um grupo e o próximo.")]
        [SerializeField] private float groupDelay = 0.08f;

        [Header("Arrastar")]
        [Tooltip("O herói que acompanha o dedo. Última filha da raiz, desligada, com Raycast Target desmarcado.")]
        [SerializeField] private RectTransform ghost;
        [SerializeField] private Image ghostArt;
        [SerializeField] private TMP_Text ghostLabel;
        [Tooltip("Escala do herói arrastado (1,12 = 12% maior que o cartão, como se saísse da mesa).")]
        [SerializeField] private float ghostLift = 1.12f;
        [Tooltip("Graus de inclinação por pixel de movimento lateral do dedo.")]
        [SerializeField] private float ghostTilt = 0.6f;
        [Tooltip("Inclinação máxima do herói arrastado, em graus.")]
        [SerializeField] private float ghostMaxTilt = 12f;

        [Serializable]
        public struct PassiveIcon
        {
            public string passiveId;   // o id de heroes.json: decreto_real, investida, trapaca, comunhao
            public Sprite sprite;
        }

        private Sprite PassiveFrameFor(HeroDetailView d) => d.HasPassive ? passiveFrame : null;

        private Sprite PassiveArt(string passiveId)
        {
            if (passiveId == null || passiveIcons == null) return null;
            foreach (var p in passiveIcons) if (p.passiveId == passiveId) return p.sprite;
            return null;
        }

        [Serializable]
        public struct HeroPortrait
        {
            public string heroId;      // o id do heroes.json: rei, cavaleiro, bispo, joker...
            public Sprite sprite;      // o retrato grande
            public Sprite card;        // o retrato pequeno (opcional)
            [Tooltip("Opcional: o retrato grande animado (os quadros do GIF, na ordem). Com quadros, eles tocam no lugar do Sprite.")]
            public Sprite[] frames;
            [Tooltip("Quadros por segundo da animação (o GIF original costuma ter 8 a 12).")]
            public float fps;
            [Tooltip("Quantos pixels vazios há embaixo dos pés no quadro. O retrato desce isso, para os pés de todos os heróis " +
                     "ficarem na mesma linha (a borda de baixo do GodArt, onde fica a sombra).")]
            public int feetPadding;

            public bool Animated => frames != null && frames.Length > 0 && frames[0] != null;

            // O retrato grande parado: o Sprite; sem ele, o primeiro quadro da animação.
            public Sprite Still => sprite != null ? sprite : (Animated ? frames[0] : null);
        }

        public event Action<string> GodSelected;            // tocou num herói: mostrar os detalhes
        public event Action<string, int> GodPlaced;         // herói, espaço do time
        public event Action<string> GodRemoved;             // arrastou um herói do time para fora

        public bool Visible => screen != null && screen.blocksRaycasts;
        public int CardCount => _cards.FindAll(c => c.gameObject.activeSelf).Count;
        public GodCard Card(int index) => _cards[index];
        public TeamSlot Slot(int index) => slots[index];
        public bool GhostVisible => ghost != null && ghost.gameObject.activeSelf;

        private readonly List<GodCard> _cards = new List<GodCard>();
        private Func<int, Color> _palette = _ => Color.white;
        private bool _wired;

        private HeroDetailView _detail;
        private string _detailHeroId;
        private int _selected;                              // 0 e 1: as habilidades; 2: a passiva

        private string _revealed;                           // o herói e a habilidade que a linha grande já mostrou
        private string _portraitId;
        private Tween _portraitTween;
        private Tween _fadeTween;
        private Tween _ghostTween;
        private Vector2 _portraitHome;
        private bool _portraitHomeSet;

        private void Awake()
        {
            var missing = MissingReference();
            if (missing != null)
            {
                Debug.LogError("GodsScreen: falta ligar no Inspector: " + missing, this);
                enabled = false;
                return;
            }
            Wire();
            Hide();
        }

        // A cor de cada domínio vem do cubo, para a interface usar a mesma das peças.
        public void Init(Func<int, Color> palette)
        {
            if (palette != null) _palette = palette;
        }

        private void Wire()
        {
            if (_wired) return;
            _wired = true;
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                slot.Index = i;
                slot.Dropped += (index, god) => GodPlaced?.Invoke(god, index);
                slot.Clicked += id => GodSelected?.Invoke(id);
                slot.DragStarted += BeginGhost;
                slot.Dragging += MoveGhost;
                slot.DragEnded += EndGhost;
            }
            for (int i = 0; i < abilities.Length; i++) abilities[i].Clicked += OnAbilityClicked;
            passive.Clicked += OnAbilityClicked;
            ghost.gameObject.SetActive(false);
        }

        public void Hide() { _portraitId = null; SetVisible(false); }

        // Ao aparecer (estava escondida), a tela entra com fade e os grupos em cascata. Esconder é na hora.
        private void SetVisible(bool visible)
        {
            bool appearing = visible && !Visible;
            if (_fadeTween != null && _fadeTween.IsActive()) _fadeTween.Kill();
            if (appearing && fadeInSeconds > 0f && Application.isPlaying)
            {
                screen.alpha = 0f;
                _fadeTween = DOTween.To(() => screen.alpha, a => screen.alpha = a, 1f, fadeInSeconds)
                    .SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            }
            else screen.alpha = visible ? 1f : 0f;
            if (appearing && entrance != null)
                for (int i = 0; i < entrance.Length; i++)
                    if (entrance[i] != null) entrance[i].Play(groupDelay * i);
            screen.interactable = visible;
            screen.blocksRaycasts = visible;
        }

        public void Show(HeroesView view)
        {
            Wire();
            teamLabel.text = view.TeamLabel;
            abilitiesLabel.text = view.AbilitiesLabel;
            passiveLabel.text = view.PassiveLabel;

            int n = 0;
            foreach (var hero in view.Heroes) TakeCard(n++).Show(hero, CardPortrait(hero.Id));
            for (int i = n; i < _cards.Count; i++) _cards[i].gameObject.SetActive(false);

            for (int i = 0; i < slots.Length; i++)
            {
                string id = i < view.Team.Length ? view.Team[i] : null;
                var card = id == null ? null : CardOf(id);
                bool selected = view.Detail != null && view.Detail.Id == id;
                slots[i].Show(id, card != null ? card.Label : null, card != null ? card.ArtColor : Color.gray, CardPortrait(id), selected, view.EmptySlot);
            }
            ShowDetail(view.Detail);
            SetVisible(true);
        }

        // O retrato pequeno do herói (cartão, espaço do time e arrasto); sem ele, o grande; sem nenhum, null.
        // Público: o Arsenal (EquipScreen) usa os mesmos retratos.
        public Sprite CardPortrait(string heroId)
        {
            if (portraits == null || heroId == null) return null;
            foreach (var p in portraits)
                if (p.heroId == heroId) return p.card != null ? p.card : p.Still;
            return null;
        }

        private void ShowDetail(HeroDetailView d)
        {
            _detail = d;
            if (d == null)
            {
                godName.text = ""; civName.text = ""; ShowPortrait(null);
                if (glow != null) glow.Hide();
                _detailHeroId = null;
                if (detail != null) detail.Show(Color.clear, "", "");
                return;
            }
            godName.text = d.Name;
            civName.text = d.Tag;
            ShowPortrait(d.Id);
            ShowGlow(d.Colors);
            for (int i = 0; i < abilities.Length; i++)
            {
                bool has = i < d.Abilities.Length;
                abilities[i].gameObject.SetActive(has);
                if (has) abilities[i].Show(_palette(d.Abilities[i].Color), d.Abilities[i].Color, d.Abilities[i].Name, d.Abilities[i].Effect, d.Abilities[i].Cost);
            }
            passive.Show(passiveColor, PassiveArt(d.PassiveId), d.HasPassive ? d.PassiveName : "—", d.PassiveText, 0, PassiveFrameFor(d));

            if (d.Id != _detailHeroId) { _detailHeroId = d.Id; _selected = 0; }   // outro herói: volta à primeira habilidade
            SelectAbility(_selected);
        }

        // A luz nas cores dos dois domínios do herói; com um domínio só, as duas manchas na mesma cor.
        private void ShowGlow(int[] colors)
        {
            if (glow == null) return;
            if (colors == null || colors.Length == 0) { glow.Hide(); return; }
            glow.Show(_palette(colors[0]), _palette(colors[colors.Length > 1 ? 1 : 0]));
        }

        private void OnAbilityClicked(AbilityRow row)
        {
            int index = row == passive ? abilities.Length : Array.IndexOf(abilities, row);
            if (index >= 0) SelectAbility(index);
        }

        // Acende a linha escolhida no topo e copia o ícone, o nome e a descrição para a linha grande do meio.
        private void SelectAbility(int index)
        {
            _selected = index;
            for (int i = 0; i < abilities.Length; i++) abilities[i].SetSelected(i == index);
            passive.SetSelected(index == abilities.Length);
            if (detail == null || _detail == null) return;

            if (index < abilities.Length && index < _detail.Abilities.Length)
            {
                var a = _detail.Abilities[index];
                detail.Show(_palette(a.Color), a.Color, a.Name, a.Effect, a.Cost);
            }
            else
            {
                detail.Show(passiveColor, PassiveArt(_detail.PassiveId), _detail.HasPassive ? _detail.PassiveName : "—", _detail.PassiveText, 0, PassiveFrameFor(_detail));
            }

            // Outra habilidade ou outro herói: a linha grande entra com fade. A mesma (a tela redesenhada) não pisca.
            string shown = _detail.Id + ":" + index;
            if (shown != _revealed) { _revealed = shown; detail.Reveal(); }
        }

        private void ShowPortrait(string heroId)
        {
            if (godArt == null) return;
            var rt = godArt.rectTransform;
            if (!_portraitHomeSet) { _portraitHome = rt.anchoredPosition; _portraitHomeSet = true; }
            if (heroId != null && heroId == _portraitId) return;     // o mesmo herói: não anima de novo
            _portraitId = heroId;

            HeroPortrait portrait = default;
            if (portraits != null && heroId != null)
                foreach (var p in portraits)
                    if (p.heroId == heroId) { portrait = p; break; }
            Sprite sprite = portrait.Still;

            if (_portraitTween != null && _portraitTween.IsActive()) _portraitTween.Kill();
            godArt.sprite = sprite;
            PlayFrames(portrait);
            godArt.preserveAspect = true;
            godArt.enabled = sprite != null;                         // sem retrato, o GodArt some
            var home = PortraitFit(rt, sprite, portrait.feetPadding);
            rt.anchoredPosition = home;
            if (sprite == null) return;

            if (portraitFade <= 0f || !isActiveAndEnabled || !Application.isPlaying) { godArt.color = Color.white; return; }

            // Entra desbotado e deslizando da direita, rápido no começo e suave no fim.
            godArt.color = new Color(1f, 1f, 1f, 0f);
            rt.anchoredPosition = home + new Vector2(portraitSlide, 0f);
            _portraitTween = DOTween.Sequence()
                .Join(DOTween.To(() => godArt.color.a, a => godArt.color = new Color(1f, 1f, 1f, a), 1f, portraitFade).SetEase(Ease.OutCubic))
                .Join(DOTween.To(() => rt.anchoredPosition, p => rt.anchoredPosition = p, home, portraitFade).SetEase(Ease.OutCubic))
                .SetUpdate(true).SetLink(gameObject);
        }

        // Mesmo tamanho de pixel para todos: um quadro de 64 encolhe para 64/80 do GodArt, um de 80 fica inteiro.
        // O Pivot do GodArt fica embaixo, então ele encolhe para baixo; os pés descem o feetPadding para a mesma linha.
        private Vector2 PortraitFit(RectTransform rt, Sprite sprite, int feetPadding)
        {
            if (portraitCanvas <= 0 || sprite == null) { rt.localScale = Vector3.one; return _portraitHome; }
            float side = Mathf.Max(sprite.rect.width, sprite.rect.height);
            float k = side / portraitCanvas;
            rt.localScale = new Vector3(k, k, 1f);
            float pixel = Mathf.Min(rt.rect.width, rt.rect.height) / portraitCanvas;   // um pixel da arte, em unidades do pai
            return _portraitHome - new Vector2(0f, feetPadding * pixel);
        }

        // Herói com quadros: o GodArt toca a animação; sem quadros, fica no Sprite parado.
        // O SpriteFlipbook entra sozinho no GodArt na primeira vez que precisa.
        private void PlayFrames(HeroPortrait portrait)
        {
            var flipbook = godArt.GetComponent<SpriteFlipbook>();
            if (!portrait.Animated)
            {
                if (flipbook != null) flipbook.Stop();
                return;
            }
            if (flipbook == null) flipbook = godArt.gameObject.AddComponent<SpriteFlipbook>();
            flipbook.Play(portrait.frames, portrait.fps);
        }

        // Os cartões já criados são reaproveitados; o que sobra fica desligado.
        private GodCard TakeCard(int index)
        {
            if (index == _cards.Count)
            {
                var card = Instantiate(cardPrefab, grid, false);
                card.Scroll = scroll;
                card.Palette = c => _palette(c);
                card.Clicked += id => GodSelected?.Invoke(id);
                card.DragStarted += BeginGhost;
                card.Dragging += MoveGhost;
                card.DragEnded += EndGhost;
                _cards.Add(card);
            }
            var row = _cards[index];
            row.gameObject.SetActive(true);
            return row;
        }

        private GodCard CardOf(string id)
        {
            foreach (var c in _cards) if (c.GodId == id) return c;
            return null;
        }

        private void BeginGhost(IGodSource source, PointerEventData e)
        {
            var card = CardOf(source.GodId);
            if (card == null) return;
            ghostArt.color = card.ArtColor;
            ghostArt.sprite = card.ArtSprite;
            ghostArt.preserveAspect = card.ArtSprite != null;
            ghostLabel.text = card.Label;
            ghost.gameObject.SetActive(true);
            ghost.localRotation = Quaternion.identity;
            LiftGhost();
            foreach (var slot in slots) slot.SetDropHint(true);   // mostra onde o herói pode cair
            MoveGhost(source, e);
        }

        // O herói sai da mesa: cresce até ghostLift com um leve passar do ponto.
        private void LiftGhost()
        {
            if (_ghostTween != null && _ghostTween.IsActive()) _ghostTween.Kill();
            if (!Application.isPlaying) { ghost.localScale = Vector3.one * ghostLift; return; }
            ghost.localScale = Vector3.one;
            _ghostTween = ghost.DOScale(ghostLift, 0.18f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(ghost.gameObject);
        }

        private void MoveGhost(IGodSource source, PointerEventData e)
        {
            var parent = (RectTransform)ghost.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, null, out var local))
                ghost.localPosition = local;

            // Inclina para o lado do movimento, como uma carta segurada pela ponta; parado, volta a ficar reto.
            float tilt = Mathf.Clamp(-e.delta.x * ghostTilt, -ghostMaxTilt, ghostMaxTilt);
            ghost.localRotation = Quaternion.Slerp(ghost.localRotation, Quaternion.Euler(0f, 0f, tilt), 0.35f);
        }

        // Soltar num espaço já foi tratado pelo próprio espaço. Um herói do time largado fora de qualquer espaço sai do time.
        private void EndGhost(IGodSource source, PointerEventData e)
        {
            if (_ghostTween != null && _ghostTween.IsActive()) _ghostTween.Kill();
            ghost.localScale = Vector3.one;
            ghost.localRotation = Quaternion.identity;
            ghost.gameObject.SetActive(false);
            foreach (var slot in slots) slot.SetDropHint(false);
            bool overSlot = e.pointerEnter != null && e.pointerEnter.GetComponentInParent<TeamSlot>() != null;
            if (source is TeamSlot && !overSlot) GodRemoved?.Invoke(source.GodId);
        }

        // O nome do primeiro campo sem ligar, ou null se está tudo ligado. Usado no Awake e no teste do prefab.
        public string MissingReference()
        {
            if (screen == null) return "screen";
            if (scroll == null) return "scroll";
            if (grid == null) return "grid";
            if (scroll.content != grid) return "scroll.content (o Content do Scroll Rect precisa ser o mesmo objeto do campo Grid)";
            if (cardPrefab == null) return "cardPrefab";
            if (cardPrefab.MissingReference() is string c) return "cardPrefab." + c;
            if (teamLabel == null) return "teamLabel";
            if (slots == null || slots.Length != HeroesView.Slots) return "slots (precisa de exatamente " + HeroesView.Slots + ")";
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) return "slots[" + i + "]";
                if (slots[i].MissingReference() is string s) return "slots[" + i + "]." + s;
            }
            if (godName == null) return "godName";
            if (civName == null) return "civName";
            if (abilitiesLabel == null) return "abilitiesLabel";
            if (passiveLabel == null) return "passiveLabel";
            if (abilities == null || abilities.Length != AbilityCount) return "abilities (precisa de exatamente " + AbilityCount + ")";
            for (int i = 0; i < abilities.Length; i++)
            {
                if (abilities[i] == null) return "abilities[" + i + "]";
                if (abilities[i].MissingReference() is string a) return "abilities[" + i + "]." + a;
            }
            if (passive == null) return "passive";
            if (passive.MissingReference() is string p) return "passive." + p;
            if (ghost == null) return "ghost";
            if (ghostArt == null) return "ghostArt";
            if (ghostLabel == null) return "ghostLabel";
            return null;
        }
    }
}