using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Cubo.Core.Tests
{
    public class SaveCodecTests
    {
        private static readonly CollectionService Cols = new CollectionService(Kit.Data);
        private static readonly SaveCodec Codec = new SaveCodec(Kit.Data, Cols);

        // ---- coleção ----

        [Fact]
        public void Collection_round_trips()
        {
            var c = Cols.NewCollection();
            c.Wins = 2; c.Elites = 1; c.Team = new[] { "joker", "rei", "bispo" };
            var back = Codec.CollectionFrom(JToken.Parse(Codec.ToJson(c).ToString()));
            Assert.Equal(2, back.Wins); Assert.Equal(1, back.Elites);
            Assert.Equal(c.Team, back.Team);
            c.HeroWins["joker"] = 4;
            Assert.Equal(4, Codec.CollectionFrom(JToken.Parse(Codec.ToJson(c).ToString())).HeroWins["joker"]);
        }

        [Theory]
        [InlineData("{\"cards\":{\"naoexiste\":{\"lv\":9,\"copies\":9}}}")]
        [InlineData("{\"cards\":5}")]
        [InlineData("\"texto\"")]
        public void Collection_from_garbage_is_a_valid_new_collection(string json)
        {
            var c = Codec.CollectionFrom(JToken.Parse(json));
            Assert.True(Kit.Data.ValidTeam(c.Team));
            Assert.Equal(0, c.Wins);
        }

        // ---- corrida ----

        private static RunState NewRun()
        {
            var runs = new RunService(Kit.Data, Cols, Kit.Engine);
            return runs.Start(Cols.NewCollection().Team);
        }

        [Fact]
        public void Run_round_trips()
        {
            var r = NewRun();
            r.Floor = 3; r.Gold = 41; r.Hp = 33; r.Elite = true;
            var b = Codec.RunFrom(JToken.Parse(Codec.ToJson(r).ToString()));
            Assert.NotNull(b);
            Assert.Equal(3, b.Floor); Assert.Equal(41, b.Gold); Assert.Equal(33, b.Hp); Assert.True(b.Elite);
            Assert.Equal(r.Team, b.Team);
        }

        [Theory]
        [InlineData("floor", "0")]
        [InlineData("floor", "99")]
        [InlineData("team", "[\"a\",\"b\",\"c\"]")]
        [InlineData("team", "[\"ra\",\"poseidon\",\"thor\"]")]
        public void Run_with_an_invalid_field_is_refused(string field, string value)
        {
            var o = Codec.ToJson(NewRun());
            o[field] = JToken.Parse(value);
            Assert.Null(Codec.RunFrom(o));
        }

        // ---- batalha ----

        [Fact]
        public void Record_survives_json_and_still_replays()
        {
            var runs = new RunService(Kit.Data, Cols, Kit.Engine);
            var rec = BattleRecordTests.Rec(77);
            var live = BattleBuilder.Create(Kit.Engine, runs, rec);
            BattleRecordTests.PlaySome(live, 4);
            var back = Codec.RecordFrom(JToken.Parse(Codec.ToJson(rec, live.Commands).ToString()));
            Assert.NotNull(back);
            Assert.Equal(live.Commands.Count, back.Commands.Count);
            var again = BattleBuilder.Replay(Kit.Engine, runs, back);
            Assert.Equal(live.State.Colors, again.State.Colors);
            Assert.Equal(live.State.FaceId, again.State.FaceId);
            Assert.Contains("\"H:", Codec.ToJson(rec, live.Commands).ToString());
        }

        [Theory]
        [InlineData("size", "3")]
        [InlineData("mode", "\"x\"")]
        [InlineData("ai", "\"x\"")]
        [InlineData("commands", "[\"S:1:2\"]")]
        [InlineData("commands", "[\"Q:1:2:3\"]")]
        public void Record_with_an_invalid_field_is_refused(string field, string value)
        {
            var o = Codec.ToJson(BattleRecordTests.Rec(5), new Command[0]);
            o[field] = JToken.Parse(value);
            Assert.Null(Codec.RecordFrom(o));
        }
    }
}