namespace Cubo.Domain
{
    public sealed class Mulberry32
    {
        private uint _a;
        public Mulberry32(uint seed) { _a = seed; }
        public uint State { get => _a; set => _a = value; }   // para salvar e retomar

        public double Next()
        {
            unchecked
            {
                _a += 0x6D2B79F5u;
                uint t = _a;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }

        public int NextInt(int n) => (int)(Next() * n);        // equivale a (rng() * n) | 0
    }
}