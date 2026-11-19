using System;

namespace Cubo.App
{
    // Estatística dos últimos quadros, sem alocar: um anel de tempos em milissegundos.
    // Quem mede (a View) chama Add a cada quadro; quem mostra lê as propriedades.
    public sealed class FrameStats
    {
        private readonly float[] _ms;
        private int _next;

        public FrameStats(int window = 120)
        {
            if (window < 1) throw new ArgumentOutOfRangeException(nameof(window));
            _ms = new float[window];
        }

        public int Count { get; private set; }

        public void Add(float seconds)
        {
            _ms[_next] = seconds * 1000f;
            _next = (_next + 1) % _ms.Length;
            if (Count < _ms.Length) Count++;
        }

        public void Reset() { _next = 0; Count = 0; }

        public float AverageMs
        {
            get
            {
                if (Count == 0) return 0f;
                float sum = 0f;
                for (int i = 0; i < Count; i++) sum += _ms[i];
                return sum / Count;
            }
        }

        public float WorstMs
        {
            get
            {
                float worst = 0f;
                for (int i = 0; i < Count; i++) if (_ms[i] > worst) worst = _ms[i];
                return worst;
            }
        }

        public float Fps => AverageMs > 0f ? 1000f / AverageMs : 0f;

        // Quantos dos quadros guardados passaram do limite (um "engasgo").
        public int SlowFrames(float limitMs)
        {
            int n = 0;
            for (int i = 0; i < Count; i++) if (_ms[i] > limitMs) n++;
            return n;
        }
    }
}