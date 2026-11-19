using System.Collections.Generic;

namespace Cubo.App
{
    // Uma opção de um evento desenhado (uma tábua do Ídolo, uma placa do Acampamento, um estandarte do Juramento).
    public sealed class EventOptionView
    {
        public string Key;               // "blood:0", "wrath", "ambush", "vow:swift"...
        public string Group;             // a parte antes de ":" ("blood", "vow"): a tela põe cada grupo na sua fileira
        public string Title, Text;       // o nome e o que faz (com os números)
        public string HeroId;            // o Ídolo: o herói que dá o sangue (a carta mostra o rosto); senão null
        public bool Enabled, Focused;
        public string Reason;            // por que não dá (sem Enabled), ou null
    }

    // A tela de um evento desenhada: a história, a opção escolhida com o botão de confirmar, ou o resultado depois.
    public sealed class EventView
    {
        public string Id, Title, Story;
        public string Variant;           // o Ídolo: "rei", "rainha" ou "torre" (a tela troca a imagem do ídolo); senão null
        public string Info;              // o Acampamento: o relatório do próximo rival (senão null)
        public string Notice;            // um aviso da batalha anterior (o voto cumprido ou quebrado), ou null
        public readonly List<EventOptionView> Options = new List<EventOptionView>();
        public bool Done;                // já escolheu: a tela mostra o resultado e o botão Continuar
        public string Result;
        public string ActionText;        // "Fazer o pacto" / "Continuar", ou "Escolha uma opção"
        public bool CanAct;
        public string LeaveText;         // "Ir embora" (some depois de escolher)
    }
}
