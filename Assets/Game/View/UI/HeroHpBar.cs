using TMPro;
using UnityEngine;

namespace Cubo.View
{
    // A barra de vida em cima da cabeça de um herói na batalha (3 contra 3). A barra corre até o valor novo; o número
    // e a marca de alvo são opcionais. Herói caído: a barra some (a figura fica apagada, quem apaga é o BattleStage).
    public sealed class HeroHpBar : MonoBehaviour
    {
        [Tooltip("A barra (um FillBar). A parte cheia anda de um valor ao outro.")]
        [SerializeField] private FillBar bar;
        [Tooltip("Opcional: o texto \"32\" ou \"32/40\" ao lado ou dentro da barra.")]
        [SerializeField] private TMP_Text label;
        [Tooltip("Opcional: mostrar o máximo no texto (32/40). Desligado: só a vida (32).")]
        [SerializeField] private bool showMax;
        [Tooltip("Opcional: a marca (seta, mira, brilho) que aparece quando este herói é o alvo.")]
        [SerializeField] private GameObject targetMark;
        [Tooltip("Opcional: a fila dos efeitos (veneno, fogo...), um RectTransform com Horizontal Layout Group ao lado da " +
                 "barra. Os ícones são criados aqui dentro, um por efeito, na ordem em que os efeitos começaram.")]
        [SerializeField] private RectTransform statusRow;
        [Tooltip("Segundos para a barra correr até o valor novo.")]
        [SerializeField] private float seconds = 0.35f;

        private float _shown = -1f, _goal;
        private int _max = 1, _text = int.MinValue;

        // Liga a barra a um valor sem animação (começo da batalha, fim de um lote).
        public void Show(int hp, int max, bool alive, bool targeted)
        {
            gameObject.SetActive(alive);
            _max = Mathf.Max(1, max);
            _goal = hp;
            if (_shown < 0f || !Application.isPlaying) _shown = hp;
            if (targetMark != null) targetMark.SetActive(alive && targeted);
            Paint();
        }

        private readonly System.Collections.Generic.List<StatusIcon> _icons = new System.Collections.Generic.List<StatusIcon>();

        public event System.Action<StatusIcon> StatusTapped;      // tocaram num ícone de efeito desta barra

        // Os efeitos ativos do herói, na ordem em que começaram: um ícone (cópia de `prefab`) por efeito, na mesma ordem
        // dentro da fila. As cópias são reaproveitadas; as que sobram ficam desligadas.
        public void ShowStatuses(System.Collections.Generic.IReadOnlyList<Cubo.App.StatusView> statuses, StatusIcon prefab,
                                 System.Func<string, Sprite> spriteOf)
        {
            if (statusRow == null || prefab == null) return;
            int n = statuses == null ? 0 : statuses.Count;
            while (_icons.Count < n)
            {
                var icon = Instantiate(prefab, statusRow, false);
                icon.Tapped += i => StatusTapped?.Invoke(i);
                _icons.Add(icon);
            }
            for (int i = 0; i < _icons.Count; i++)
            {
                bool on = i < n;
                _icons[i].gameObject.SetActive(on);
                if (!on) continue;
                var s = statuses[i];
                _icons[i].Show(s.Kind, spriteOf?.Invoke(s.Kind), s.Detail);
                _icons[i].transform.SetSiblingIndex(i);
            }
        }

        // O valor muda no meio da apresentação: a barra corre até ele.
        public void MoveTo(int hp, int max)
        {
            _max = Mathf.Max(1, max);
            _goal = hp;
            if (hp > 0 && !gameObject.activeSelf) gameObject.SetActive(true);
        }

        public void SetTarget(bool targeted)
        {
            if (targetMark != null) targetMark.SetActive(targeted && gameObject.activeSelf);
        }

        private void Update()
        {
            if (Mathf.Approximately(_shown, _goal)) return;
            float step = Mathf.Max(1f, _max / Mathf.Max(0.01f, seconds)) * Time.unscaledDeltaTime;
            _shown = Mathf.MoveTowards(_shown, _goal, step);
            Paint();
        }

        private void Paint()
        {
            if (bar != null) bar.Set(_shown / _max);
            int whole = Mathf.CeilToInt(_shown);
            if (label != null && whole != _text)
            {
                _text = whole;
                label.text = showMax ? whole + "/" + _max : whole.ToString();
            }
        }

        public string MissingReference() => bar == null ? "bar" : bar.MissingReference() is string m ? "bar." + m : null;
    }
}
