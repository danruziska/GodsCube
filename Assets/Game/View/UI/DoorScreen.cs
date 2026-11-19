using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cubo.App;

namespace Cubo.View
{
    // A tela da porta: o fim de um corredor com duas portas desenhadas no cenário. As portas são os botões (áreas
    // invisíveis por cima da arte, filhas da imagem do cenário, então acompanham o corte da tela). No alto, o caminho
    // dos andares (o atual aceso, o último com a coroa do chefe); embaixo, a ficha de ferro da porta tocada, com cada
    // número comparado à outra porta; o botão Entrar embaixo à direita. Este script só copia o DoorView.
    public sealed class DoorScreen : MonoBehaviour
    {
        [Serializable]
        public struct Door
        {
            [Tooltip("normal ou elite")] public string key;
            public Button spot;            // a área invisível em cima da porta
            public Image glow;             // o brilho que pulsa na porta escolhida
            public Image shade;            // o escuro na outra porta
            public TMP_Text label;         // o nome embaixo da porta
            public Color color;            // a cor do brilho e do nome quando escolhida
        }

        [SerializeField] private CanvasGroup screen;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text text;
        [Tooltip("Opcional: o X que esconde a faixa do texto.")]
        [SerializeField] private TextPanelToggle textToggle;

        [Header("O caminho dos andares")]
        [SerializeField] private RectTransform track;
        [Tooltip("Uma marca do caminho (inativa): Image do espaço, filhos Number (texto) e Crown (imagem).")]
        [SerializeField] private RectTransform pipTemplate;
        [SerializeField] private Color pipPast = new Color(0.55f, 0.55f, 0.6f, 0.8f);
        [SerializeField] private Color pipNow = new Color(1f, 0.82f, 0.48f, 1f);
        [SerializeField] private Color pipNext = new Color(0.8f, 0.8f, 0.85f, 1f);

        [Header("As portas")]
        [SerializeField] private Door[] doors = new Door[0];

        [Header("A ficha")]
        [SerializeField] private GameObject sheet;
        [SerializeField] private TMP_Text sheetName;
        [SerializeField] private TMP_Text tagline;
        [Tooltip("Uma linha da ficha (inativa): filhos Label e Value (textos).")]
        [SerializeField] private RectTransform statTemplate;
        [SerializeField] private TMP_Text hint;
        [SerializeField] private Color valueColor = new Color(0.86f, 0.86f, 0.9f, 1f);
        [SerializeField] private Color worseColor = new Color(1f, 0.42f, 0.36f, 1f);
        [SerializeField] private Color betterColor = new Color(1f, 0.82f, 0.48f, 1f);

        [SerializeField] private Button action;
        [SerializeField] private TMP_Text actionLabel;
        [SerializeField] private float fadeInSeconds = 0.25f;
        [SerializeField] private float pulseSeconds = 0.9f;

        public event Action<string> DoorFocused;
        public event Action EnterPressed;
        public bool Visible => screen != null && screen.blocksRaycasts;

        private readonly List<RectTransform> _pips = new List<RectTransform>();
        private readonly List<RectTransform> _stats = new List<RectTransform>();
        private bool _wired;
        private Tween _fade;
        private Tween _pulse;
        private string _pulsing;

        private void Awake()
        {
            if (screen == null || action == null || title == null) { Debug.LogError("DoorScreen: falta ligar screen, title ou action no Inspector.", this); enabled = false; return; }
            Wire();
            Hide();
        }

        private void Wire()
        {
            if (_wired) return;
            _wired = true;
            action.onClick.AddListener(() => EnterPressed?.Invoke());
            foreach (var d in doors)
            {
                string key = d.key;
                if (d.spot != null) d.spot.onClick.AddListener(() => DoorFocused?.Invoke(key));
            }
            if (pipTemplate != null) pipTemplate.gameObject.SetActive(false);
            if (statTemplate != null) statTemplate.gameObject.SetActive(false);
        }

        public void Hide()
        {
            SetVisible(false);
            if (_pulse != null && _pulse.IsActive()) _pulse.Kill();
            _pulsing = null;
        }

        private void SetVisible(bool visible)
        {
            bool appearing = visible && !Visible;
            if (appearing && textToggle != null) textToggle.ShowPanel();
            if (_fade != null && _fade.IsActive()) _fade.Kill();
            if (appearing && fadeInSeconds > 0f && Application.isPlaying)
            {
                screen.alpha = 0f;
                _fade = DOTween.To(() => screen.alpha, a => screen.alpha = a, 1f, fadeInSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            }
            else screen.alpha = visible ? 1f : 0f;
            screen.interactable = visible;
            screen.blocksRaycasts = visible;
        }

        public void Show(DoorView v)
        {
            Wire();
            title.text = v.Title ?? "";
            if (text != null) text.text = v.Text ?? "";
            ShowTrack(v);

            DoorOptionView focus = v.Normal != null && v.Normal.Focused ? v.Normal : v.Elite != null && v.Elite.Focused ? v.Elite : null;
            foreach (var d in doors)
            {
                var o = d.key == "elite" ? v.Elite : v.Normal;
                bool on = focus != null && focus.Key == d.key, other = focus != null && !on;
                if (d.label != null) { d.label.text = o?.Name ?? ""; d.label.color = on ? d.color : Color.Lerp(d.color, Color.white, 0.55f); }
                if (d.shade != null) d.shade.gameObject.SetActive(other);
                if (d.glow != null) d.glow.gameObject.SetActive(on);
            }
            Pulse(focus?.Key);

            if (sheet != null) sheet.SetActive(focus != null);
            if (hint != null) { hint.gameObject.SetActive(focus == null); hint.text = v.Hint ?? ""; }
            if (focus != null) ShowSheet(focus);

            actionLabel.text = v.ActionText ?? "";
            action.interactable = v.CanAct;
            SetVisible(true);
        }

        // O caminho: uma marca por andar, o atual aceso, os já vencidos apagados, o último com a coroa.
        private void ShowTrack(DoorView v)
        {
            if (track == null || pipTemplate == null || v.Floors <= 0) return;
            while (_pips.Count < v.Floors)
            {
                var p = Instantiate(pipTemplate, track);
                p.gameObject.SetActive(true);
                _pips.Add(p);
            }
            for (int i = 0; i < _pips.Count; i++)
            {
                var p = _pips[i];
                bool used = i < v.Floors;
                p.gameObject.SetActive(used);
                if (!used) continue;
                int floor = i + 1;
                bool boss = floor == v.Floors, now = floor == v.Floor, past = floor < v.Floor;
                if (p.GetComponent<Image>() is Image img) img.color = now ? pipNow : past ? pipPast : pipNext;
                if (p.Find("Number")?.GetComponent<TMP_Text>() is TMP_Text n)
                {
                    n.gameObject.SetActive(!boss);
                    n.text = floor.ToString();
                    n.color = now ? pipNow : past ? pipPast : pipNext;
                }
                if (p.Find("Crown") is Transform c) c.gameObject.SetActive(boss);
                p.localScale = now ? Vector3.one * 1.25f : Vector3.one;
            }
        }

        private void ShowSheet(DoorOptionView o)
        {
            var d = Array.Find(doors, x => x.key == o.Key);
            if (sheetName != null) { sheetName.text = o.Name ?? ""; sheetName.color = d.key != null ? d.color : betterColor; }
            if (tagline != null) tagline.text = o.Tagline ?? "";
            if (statTemplate == null) return;
            while (_stats.Count < o.Stats.Count)
            {
                var r = Instantiate(statTemplate, statTemplate.parent);
                r.gameObject.SetActive(true);
                _stats.Add(r);
            }
            for (int i = 0; i < _stats.Count; i++)
            {
                bool used = i < o.Stats.Count;
                _stats[i].gameObject.SetActive(used);
                if (!used) continue;
                var s = o.Stats[i];
                if (_stats[i].Find("Label")?.GetComponent<TMP_Text>() is TMP_Text l) l.text = s.Label ?? "";
                if (_stats[i].Find("Value")?.GetComponent<TMP_Text>() is TMP_Text val)
                {
                    val.text = s.Value ?? "";
                    val.color = s.Worse ? worseColor : s.Better ? betterColor : valueColor;
                }
            }
        }

        // O brilho da porta escolhida respira devagar.
        private void Pulse(string key)
        {
            if (key == _pulsing) return;
            if (_pulse != null && _pulse.IsActive()) _pulse.Kill();
            _pulsing = key;
            if (key == null || !Application.isPlaying) return;
            var d = Array.Find(doors, x => x.key == key);
            if (d.glow == null) return;
            var c = d.color; c.a = 0.55f; d.glow.color = c;
            _pulse = DOTween.To(() => d.glow.color.a, a => { var k = d.glow.color; k.a = a; d.glow.color = k; }, 0.25f, pulseSeconds)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);
        }
    }
}
