using System.Collections.Generic;

namespace Cubo.App
{
    // Uma linha da ficha de uma porta: "Vida do rival · 262". Worse: pior para o jogador (a tela pinta de vermelho);
    // Better: melhor (dourado). As duas falsas: igual à outra porta.
    public sealed class DoorStatView
    {
        public string Label, Value;
        public bool Worse, Better;
    }

    // Uma das duas portas: a comum ("normal") e a de elite ("elite").
    public sealed class DoorOptionView
    {
        public string Key, Name, Tagline;
        public readonly List<DoorStatView> Stats = new List<DoorStatView>();
        public bool Focused;
    }

    // A tela da porta desenhada: o caminho dos andares no alto, as duas portas no cenário (tocar escolhe),
    // a ficha da porta escolhida embaixo e o botão Entrar.
    public sealed class DoorView
    {
        public string Title, Text;
        public int Floor, Floors;                // o andar desta batalha e o total (o último é o chefe)
        public string BossLabel;                 // "Chefe": a legenda da última marca do caminho
        public DoorOptionView Normal, Elite;
        public string Hint;                      // sem porta escolhida: "Toque numa porta"
        public string ActionText;
        public bool CanAct;
    }
}
