using System.Collections.Generic;

namespace Cubo.App
{
    // O que a tela do Arsenal mostra, já calculado e com texto. A tela só liga estes campos a componentes.
    public sealed class EquipHeroView
    {
        public string Id, Name;          // Id null = espaço do time vazio
        public string ItemId;            // o equipamento que ele leva, ou null
        public bool Selected;            // é o herói que a aba está equipando
    }

    public sealed class EquipItemView
    {
        public string Id, Name, Effect;
        public string Owner;             // o id do herói (do time ou não) que leva o equipamento, ou null
        public bool Focused;             // é o que o painel do meio está mostrando
        public bool OnSelected;          // o herói que a aba está equipando é quem o leva
    }

    public sealed class EquipView
    {
        public string TeamTitle;                                                  // a placa do painel do time: Time / Team / Equipo
        public string Title;                                                      // a placa do painel do Arsenal: Arsenal / Armory
        public readonly List<EquipHeroView> Heroes = new List<EquipHeroView>();   // sempre 3, na ordem do time
        public readonly List<EquipItemView> Items = new List<EquipItemView>();    // todos, na ordem de equipment.json
        public string DetailId, DetailName, DetailEffect, DetailOwner;            // DetailOwner: "Com o Cavaleiro" ou ""
        public string ActionText;                                                 // Equipar, Tirar ou Pegar
        public bool CanAct;                                                       // falso sem herói no time
    }
}
