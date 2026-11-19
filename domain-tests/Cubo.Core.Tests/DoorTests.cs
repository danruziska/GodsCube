using System.Linq;
using Cubo.App;
using Cubo.Domain;
using Xunit;

namespace Cubo.Core.Tests
{
    public class DoorTests
    {
        private static GameData Data => Kit.Data;

        private static GameFlow AtDoor()
        {
            var f = new GameFlow(Data, new Texts(new FileContentSource()), new MemoryFileStore(), 9);
            f.Load(); f.Texts.SetLang("en");
            f.Preview("door");
            Assert.Equal(Stage.Door, f.Stage);
            return f;
        }

        [Fact]
        public void The_door_screen_compares_the_elite_rival_with_the_normal_one()
        {
            var f = AtDoor();
            var v = f.DoorView();
            Assert.Equal("Floor 1 of " + Data.Run.Floors, v.Title);
            Assert.Equal(Data.Run.Floors, v.Floors);
            Assert.False(v.CanAct);
            Assert.Equal("Choose a door", v.ActionText);
            Assert.NotNull(v.Hint);
            Assert.Equal(4, v.Normal.Stats.Count);
            Assert.All(v.Normal.Stats, s => Assert.False(s.Worse || s.Better));     // a comum é a referência
            Assert.True(int.Parse(v.Elite.Stats[0].Value) > int.Parse(v.Normal.Stats[0].Value));
            Assert.True(v.Elite.Stats[0].Worse);                                      // mais vida no rival
            Assert.True(v.Elite.Stats[1].Worse);                                      // mais dano
            Assert.True(v.Elite.Stats[3].Better);                                     // mais ouro
            Assert.False(f.Run.Elite);                                                // calcular a ficha não muda a corrida
        }

        [Fact]
        public void Tapping_a_door_only_chooses_it_and_enter_starts_that_battle()
        {
            var f = AtDoor();
            f.FocusDoor("elite");
            var v = f.DoorView();
            Assert.True(v.Elite.Focused);
            Assert.False(v.Normal.Focused);
            Assert.True(v.CanAct);
            Assert.Equal("Enter", v.ActionText);
            Assert.Equal(Stage.Door, f.Stage);
            f.FocusDoor("nonsense");
            Assert.True(f.DoorView().Elite.Focused);
            f.DoorAct();
            Assert.Equal(Stage.Battle, f.Stage);
            Assert.True(f.Run.Elite);
        }
    }
}
