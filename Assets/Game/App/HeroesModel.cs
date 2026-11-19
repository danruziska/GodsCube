using System.Collections.Generic;
using System.Linq;
using Cubo.Domain;

namespace Cubo.App
{
    // O que a tela de heróis mostra, já calculado e já com texto. A tela só liga estes campos a componentes.
    public sealed class HeroCardView
    {
        public string Id, Name, Tag;
        public int[] Colors;                   // os domínios do herói (0 a 5), na ordem dos poderes
        public bool InTeam, Selected;
    }

    public sealed class AbilityView
    {
        public int Color;                      // o domínio (0 a 5); a cor quem escolhe é a tela, pela paleta do cubo
        public int Cost;                       // a mana que o poder precisa para ser usado
        public string Name, Domain, Effect;
    }

    public sealed class HeroDetailView
    {
        public string Id, Name, Tag;
        public int[] Colors;                   // os domínios do herói (0 a 5), na ordem dos poderes
        public AbilityView[] Abilities;        // sempre dois
        public string PassiveId, PassiveName, PassiveText;   // PassiveId: o id de heroes.json (decreto_real...), ou null
        public bool HasPassive, InTeam;
    }

    public sealed class HeroesView
    {
        public string AbilitiesLabel, PassiveLabel, TeamLabel, TabHeroes, TabBattle, TabEquip, EmptySlot, Hint;
        public List<HeroCardView> Heroes = new List<HeroCardView>();   // todos os heróis, na ordem do heroes.json
        public string[] Team = new string[Slots];                      // o id do herói em cada espaço, ou null
        public HeroDetailView Detail;                                  // null se nenhum herói está em destaque
        public bool CanBattle;

        public const int Slots = 3;
    }

    public static class HeroesModel
    {
        public static HeroesView Build(GameData d, Texts t, IReadOnlyList<string> team, string selected, bool ready, string hint)
        {
            var v = new HeroesView
            {
                AbilitiesLabel = t.T("heroes.abilities"), PassiveLabel = t.T("heroes.passive"), TeamLabel = t.T("heroes.team"),
                TabHeroes = t.T("heroes.tab.heroes"), TabBattle = t.T("heroes.tab.battle"), TabEquip = t.T("heroes.tab.equip"),
                EmptySlot = t.T("heroes.slot.empty"), Hint = hint, CanBattle = ready
            };
            foreach (var id in d.HeroIds)
                v.Heroes.Add(new HeroCardView
                {
                    Id = id, Name = t.Name("hero." + id), Tag = t.Name("herotag." + id),
                    Colors = d.Heroes[id].Colors, InTeam = team.Contains(id), Selected = id == selected
                });
            for (int i = 0; i < HeroesView.Slots && i < team.Count; i++) v.Team[i] = team[i];

            if (selected != null && d.Heroes.TryGetValue(selected, out var hero))
            {
                bool has = hero.Passive != null;
                v.Detail = new HeroDetailView
                {
                    Id = selected, Name = t.Name("hero." + selected), Tag = t.Name("herotag." + selected),
                    Colors = hero.Colors,
                    Abilities = hero.Powers.Select(p => new AbilityView
                    {
                        Color = p.Color, Cost = p.Cost, Name = t.Name("power." + p.Id), Domain = t.Domain(p.Color), Effect = HudModel.PowerEffect(p, t)
                    }).ToArray(),
                    PassiveId = hero.Passive,
                    PassiveName = has ? t.Name("passive." + hero.Passive) : "",
                    PassiveText = has ? PassiveText(d, t, hero.Passive) : "",
                    HasPassive = has, InTeam = team.Contains(selected)
                };
            }
            return v;
        }

        // O texto de uma passiva, com os números de rules.json.
        public static string PassiveText(GameData d, Texts t, string passive)
        {
            var r = d.Rules;
            switch (passive)
            {
                case Passives.Decree: return t.T("passive." + passive, ("n", r.DecreeDiscount));
                case Passives.Charge: return t.T("passive." + passive, ("n", r.ChargeMinGroup), ("p", JsMath.RoundInt(r.ChargeBonus * 100)));
                case Passives.Communion: return t.T("passive." + passive, ("n", r.CommunionMana));
                default: return t.T("passive." + passive);
            }
        }
    }
}