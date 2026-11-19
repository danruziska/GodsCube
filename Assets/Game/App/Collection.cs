using System;
using System.Collections.Generic;
using Cubo.Domain;

namespace Cubo.App
{
    // O que o jogador guarda entre corridas: o último time e os recordes.
    public sealed class Collection
    {
        public int V = 1;
        public string[] Team;
        public int Wins, Elites;
        public int BestCombo;                    // o maior combo (grupos numa cascata, contando o tocado) numa batalha
        public Dictionary<string, int> HeroWins = new Dictionary<string, int>();
        public Dictionary<string, string> Equip = new Dictionary<string, string>();   // [id do herói] = id do equipamento
    }

    // As regras da coleção. Não guarda estado: tudo o que muda mora no objeto Collection.
    public sealed class CollectionService
    {
        private readonly GameData _d;

        public CollectionService(GameData data) { _d = data; }

        public Collection NewCollection() => new Collection { Team = (string[])_d.DefaultTeam.Clone() };

        // Confere uma coleção salva: o que não valer cai fora.
        public Collection Clean(Collection raw)
        {
            var col = NewCollection();
            if (raw == null) return col;
            if (_d.ValidTeam(raw.Team)) col.Team = (string[])raw.Team.Clone();
            col.Wins = Math.Max(0, Math.Min(9999, raw.Wins));
            col.Elites = Math.Max(0, Math.Min(99999, raw.Elites));
            col.BestCombo = Math.Max(0, Math.Min(999, raw.BestCombo));
            if (raw.HeroWins != null)
                foreach (var id in _d.HeroIds)
                    if (raw.HeroWins.TryGetValue(id, out var w) && w > 0) col.HeroWins[id] = Math.Min(9999, w);
            col.Equip = _d.CleanEquip(raw.Equip);
            return col;
        }

        // Corrida vencida: uma vitória geral e uma para cada herói do time.
        public void Win(Collection col, string[] team)
        {
            col.Wins += 1;
            foreach (var id in team ?? new string[0])
                if (_d.Heroes.ContainsKey(id)) col.HeroWins[id] = (col.HeroWins.TryGetValue(id, out var w) ? w : 0) + 1;
        }
    }
}
