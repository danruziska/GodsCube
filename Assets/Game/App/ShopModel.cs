using System.Collections.Generic;

namespace Cubo.App
{
    // Uma oferta da loja desenhada: uma melhoria, uma runa, a Poção ou a Troca. A tela só liga estes campos a componentes.
    public sealed class ShopOfferView
    {
        public string Id;                // "upg:rei.0", "rune:folego", "potion", "reroll": o que a tela devolve ao tocar
        public string Kind;              // upgrade, rune, potion, reroll
        public string IconId;            // a chave da lista de ícones da tela: id da runa, do item, "potion" ou "reroll"
        public string HeroId;            // melhoria de habilidade: o herói dela (a tela mostra o rosto); senão null
        public string Name, Effect;
        public string Tag;               // o nível da melhoria ("++", "★"), ou ""
        public int Price, FullPrice;     // FullPrice > Price só na promoção (o preço antigo aparece riscado)
        public bool OnSale, Sold, Affordable, CanBuy, Focused;
    }

    // A vida de um herói do time na loja (cada um guarda a sua entre os andares).
    public sealed class ShopHeroView
    {
        public string HeroId, Name, Text;    // Text: "28/42", ou "Caído"
        public int Hp, Max;
        public bool Alive;
    }

    public sealed class ShopView
    {
        public string Title;                         // a placa: Loja / Shop / Tienda
        public string Gold, Health, Floor;           // "85", "72/125", "Andar 3/6"
        public string Line;                          // a fala do vendedor
        public string UpgradesTitle, RunesTitle, ServicesTitle, BagTitle;
        public readonly List<ShopOfferView> Upgrades = new List<ShopOfferView>();
        public readonly List<ShopOfferView> Runes = new List<ShopOfferView>();
        public readonly List<ShopOfferView> Services = new List<ShopOfferView>();   // a Poção e a Troca
        public string DetailId, DetailName, DetailEffect, DetailIconId, DetailHeroId;
        public int DetailPrice, DetailFullPrice;
        public string ActionText;                    // "Comprar · 45", ou o motivo de não dar (sem ouro, bolsa cheia...)
        public bool CanBuy;
        public readonly List<ShopHeroView> Heroes = new List<ShopHeroView>();   // os 3 do time, na ordem
        public readonly List<string> Bag = new List<string>();   // as runas que o jogador leva, uma por espaço (null = vazio)
        public readonly List<string> BagInfo = new List<string>();   // o texto do balão de cada espaço (nome, efeito), ou null
        public string LeaveText;                     // "Para o andar 3" ou "Enfrentar o chefe"

        public IEnumerable<ShopOfferView> All
        {
            get { foreach (var o in Upgrades) yield return o; foreach (var o in Runes) yield return o; foreach (var o in Services) yield return o; }
        }
    }
}
