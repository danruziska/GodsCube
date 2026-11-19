using System.Collections.Generic;
using Cubo.App;
using Cubo.Data;
using Cubo.Domain;

namespace Cubo.Core.Tests
{
    // Ajudantes dos testes da batalha: o conteúdo de verdade (os JSON do jogo) e estados montados à mão.
    public static class Kit
    {
        public static readonly GameData Data = ContentLoader.Load(new FileContentSource());
        public static readonly BattleEngine Engine = new BattleEngine(Data);

        public static readonly string[] Royal = { "rei", "cavaleiro", "bispo" };     // o time padrão
        public static readonly string[] Tricky = { "joker", "cavaleiro", "bispo" };

        // Uma batalha de verdade (NewState), com a semente dada.
        public static BattleState New(uint seed = 1, int size = 5, string[] team = null, string mode = "facil") =>
            Engine.NewState(new BattleOptions { Size = size, Mode = mode }, team ?? Royal, new Mulberry32(seed));

        // Uma batalha com itens: [id do herói] = id do equipamento ("rei" -> "presa").
        public static BattleState NewWith(Dictionary<string, string> equip, uint seed = 1, int size = 5) =>
            Engine.NewState(new BattleOptions { Size = size, Mode = "facil", Equip = equip }, Royal, new Mulberry32(seed));

        public static (BattleSession s, List<IBattleEvent> log) Session(uint seed = 5000, int size = 5, string[] team = null, string level = "atento")
        {
            var rng = new Mulberry32(seed);
            var st = Engine.NewState(new BattleOptions { Size = size, Mode = "facil" }, team ?? Royal, rng);
            var s = new BattleSession(Engine, st, rng, level);
            st.Target[0] = 0;                                   // o jogador já escolheu o primeiro rival (3 contra 3)
            var log = new List<IBattleEvent>();
            s.Played += ev => log.AddRange(ev);
            return (s, log);
        }

        // Pinta todas as faces sem grupo nenhum: cor (u + 2v + face) % 5 (nunca três iguais em linha).
        public static void NoGroups(BattleState st)
        {
            var g = Geometry.For(st.N);
            for (int c = 0; c < g.Total; c++)
            {
                int f = g.CellFace[c], r = c - f * g.NN, u = r / g.N, v = r % g.N;
                st.Colors[c] = (sbyte)((u + 2 * v + f) % 5);
            }
        }

        // Pinta a frente a partir de linhas como a tela mostra: a primeira é a de cima, da esquerda para a direita.
        // '.' deixa a peça como está.
        public static void Front(BattleState st, params string[] rows)
        {
            var g = Geometry.For(st.N);
            for (int i = 0; i < rows.Length; i++)
            {
                int y = st.N - 1 - i;
                for (int x = 0; x < rows[i].Length; x++)
                    if (rows[i][x] != '.') st.Colors[BattleEngine.FrontCell(g, x, y)] = (sbyte)(rows[i][x] - '0');
            }
        }

        // A frente inteira de uma cor só: uma mancha de N×N peças (com NumColors = 1, toda peça nova também é dessa cor).
        public static void FillFront(BattleState st, sbyte color = 0)
        {
            var g = Geometry.For(st.N);
            for (int x = 0; x < st.N; x++) for (int y = 0; y < st.N; y++) st.Colors[BattleEngine.FrontCell(g, x, y)] = color;
        }

        public static int Cell(BattleState st, int x, int y) => BattleEngine.FrontCell(Geometry.For(st.N), x, y);


        public static void FillMana(BattleState st, Side side, int value = 15)
        {
            for (int s = 0; s < BattleEngine.Slots; s++) st.Mana[(int)side][s] = value;
        }

        // Um turno inteiro do jogador pela sessão, com a IA no lugar dele: grupos até a fase de entrega,
        // depois a face. Devolve false se a sessão recusou algo.
        public static bool PlayTurn(BattleSession s, Mulberry32 bot, string level = "atento")
        {
            for (int guard = 0; guard < 12 && s.IsMatching; guard++)
            {
                if (s.NeedsTarget)
                {
                    if (!s.TryTarget(Engine.ChooseTarget(s.State, Side.Player, level, bot))) return false;
                    s.PresentationDone();
                }
                var m = Engine.ChooseMove(s.State, Side.Player, level, bot);
                if (m == null) return false;
                if (!s.TryFire(m.Cell)) return false;
                s.PresentationDone();
            }
            if (s.State.Winner != null) return true;
            if (!s.IsHanding) return false;
            int face = Engine.ChooseHand(s.State, Side.Player, level, bot);
            if (!s.TryHand(face)) return false;
            s.PresentationDone();
            if (s.IsZoning)                                     // veneno ou fogo armado: a área vem antes do rival
            {
                if (!s.TryZone(Engine.ChooseZone(s.State, Side.Player))) return false;
                s.PresentationDone();
            }
            return true;
        }

        // Uma batalha inteira, IA contra IA, só no domínio (sem sessão). Devolve o estado final.
        public static BattleState PlayOut(uint seed, string[] team = null, int size = 5, string level = "atento", int maxTurns = 400)
        {
            var rng = new Mulberry32(seed);
            var st = Engine.NewState(new BattleOptions { Size = size, Mode = "facil" }, team ?? Royal, rng);
            for (int turn = 0; turn < maxTurns && st.Winner == null; turn++)
            {
                var side = st.Turn;
                if (side == Side.Enemy)
                    foreach (int slot in Engine.EnemyCasts(st)) if (Engine.CanCast(st, side, slot)) Engine.Cast(st, side, slot, rng);
                for (int g = 0; g < 12 && st.Winner == null && st.Phase == Phase.Match; g++)
                {
                    var m = Engine.ChooseMove(st, side, level, rng);
                    Engine.Fire(st, side, m.Cell, rng);
                }
                if (st.Winner == null) Engine.HandOver(st, side, Engine.ChooseHand(st, side, level, rng), rng);
                if (BattleEngine.AwaitingZone(st, side)) Engine.PlaceHazard(st, side, Engine.ChooseZone(st, side));
            }
            return st;
        }
    }
}