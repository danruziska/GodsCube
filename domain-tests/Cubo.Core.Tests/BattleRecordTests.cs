using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    public class BattleRecordTests
    {
        private static readonly CollectionService Cols = new CollectionService(Kit.Data);

        public static BattleRecord Rec(uint seed)
        {
            var t = Kit.Data.DefaultTeam;
            return new BattleRecord { Seed = seed, Team = (string[])t.Clone(), Size = 5 };
        }

        // Turnos inteiros do jogador (grupos, giros e a entrega), com a IA no lugar dele.
        public static void PlaySome(BattleSession s, int turns)
        {
            var bot = new Mulberry32(3);
            for (int i = 0; i < turns && s.State.Winner == null; i++) Assert.True(Kit.PlayTurn(s, bot));
        }

        [Fact]
        public void Replaying_the_commands_gives_the_same_battle()
        {
            var runs = new RunService(Kit.Data, Cols, Kit.Engine);
            var rec = Rec(1234);
            var live = BattleBuilder.Create(Kit.Engine, runs, rec);
            PlaySome(live, 6);
            rec.Commands = live.Commands.ToList();
            var again = BattleBuilder.Replay(Kit.Engine, runs, rec);
            Assert.NotNull(again);
            Assert.Equal(live.State.Colors, again.State.Colors);
            Assert.Equal(live.State.Hp, again.State.Hp);
            Assert.Equal(live.State.Round, again.State.Round);
            Assert.Equal(live.State.FaceId, again.State.FaceId);
            Assert.Equal(live.State.LastFace, again.State.LastFace);
            Assert.Contains(live.Commands, c => c.Kind == 'H');
        }

        [Fact]
        public void Replay_refuses_an_impossible_command()
        {
            var runs = new RunService(Kit.Data, Cols, Kit.Engine);
            var rec = Rec(5);
            rec.Commands.Add(new Command('F', 9999));
            Assert.Null(BattleBuilder.Replay(Kit.Engine, runs, rec));
            rec.Commands.Clear();
            rec.Commands.Add(new Command('H', 0));               // entregar antes de disparar não vale
            Assert.Null(BattleBuilder.Replay(Kit.Engine, runs, rec));
        }
    }
}