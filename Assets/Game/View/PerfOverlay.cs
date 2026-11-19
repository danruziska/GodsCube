using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using Cubo.App;

namespace Cubo.View
{
    // Faixa de números no canto da tela, só em build de desenvolvimento (e no Editor).
    // Meio segundo por atualização: o texto é remontado poucas vezes e sem alocar a cada quadro.
    public sealed class PerfOverlay : MonoBehaviour
    {
        private const float RefreshSeconds = 0.5f;
        private const float SlowFrameMs = 33.4f;           // pior que 30 fps

        private readonly FrameStats _stats = new FrameStats(120);
        private readonly StringBuilder _sb = new StringBuilder(96);
        private string _text = "";
        private float _next;
        private GUIStyle _style;

        private void Update()
        {
            _stats.Add(Time.unscaledDeltaTime);
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + RefreshSeconds;
            _sb.Clear()
               .Append(_stats.Fps.ToString("0")).Append(" fps  pior ").Append(_stats.WorstMs.ToString("0"))
               .Append(" ms  lentos ").Append(_stats.SlowFrames(SlowFrameMs))
               .Append("  mem ").Append(Profiler.GetMonoUsedSizeLong() >> 20).Append(" MB");
            _text = _sb.ToString();
        }

        private void OnGUI()
        {
            if (_style == null) { _style = new GUIStyle(GUI.skin.label); _style.fontSize = Mathf.Max(14, Screen.height / 50); }
            var safe = Screen.safeArea;
            float top = Screen.height - safe.yMax + 6f;       // abaixo da área segura do topo
            GUI.Label(new Rect(safe.x + 8f, top, safe.width - 16f, _style.fontSize * 2f), _text, _style);
        }
    }
}