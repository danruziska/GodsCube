using System.Collections.Generic;

namespace Cubo.App
{
    // Uma carta da bênção: uma habilidade (ou um item de veneno/fogo) do time que sobe um nível de graça.
    public sealed class BlessingCardView
    {
        public string Id;                // o id do poder ("rei.0") ou do item ("presa")
        public string HeroId;            // habilidade: o herói dela (a carta mostra o rosto); item: null
        public string IconId;            // item: o id do item (o ícone dele); habilidade: null
        public string Name, Tag;         // "Lança ++" e o nível ("++", "★")
        public string Hero;              // o nome do herói ("Cavaleiro"), ou o de quem leva o item
        public string Change;            // "Lança + → Lança ++"
        public string Effect;            // o que passa a fazer, com o valor antigo entre parênteses
        public bool Focused;
    }

    // A tela da bênção desenhada (BlessingScreen): o título, a frase, as cartas e o botão de aceitar.
    public sealed class BlessingView
    {
        public string Title, Text;
        public readonly List<BlessingCardView> Cards = new List<BlessingCardView>();
        public string ActionText;        // "Aceitar a bênção" com uma carta escolhida; "Escolha uma bênção" sem
        public bool CanAccept;
    }
}
