using System.IO;
using Cubo.App;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Cubo.Core.Tests
{
    public class SaveStoreTests
    {
        [Fact]
        public void Settings_default_when_missing_or_invalid()
        {
            Assert.Equal(6, Settings.Clean(null).Size);
            Assert.Equal(6, Settings.Clean(JToken.Parse("[]")).Size);
            var s = Settings.Clean(JToken.Parse("{\"lang\":\"xx\",\"size\":99,\"mode\":\"?\",\"bias\":\"?\"}"));
            Assert.Null(s.Lang); Assert.Equal(6, s.Size); Assert.Equal("facil", s.Mode);
        }

        [Fact]
        public void Settings_round_trip()
        {
            var a = new Settings { Lang = "es", Size = 7, Mode = "dificil" };
            var b = Settings.Clean(JToken.Parse(a.ToJson().ToString()));
            Assert.Equal("es", b.Lang); Assert.Equal(7, b.Size); Assert.Equal("dificil", b.Mode);
        }

        [Fact]
        public void Settings_from_an_older_cube_size_forget_the_old_size()
        {
            var old = Settings.Clean(JToken.Parse("{\"v\":3,\"lang\":\"pt\",\"size\":8}"));
            Assert.Equal(6, old.Size);
            Assert.Equal("pt", old.Lang);
        }

        [Fact]
        public void Memory_store_reads_what_was_written_and_null_when_missing()
        {
            var m = new MemoryFileStore();
            Assert.Null(m.Read("x.json"));
            m.Write("x.json", "1");
            Assert.Equal("1", m.Read("x.json"));
        }

        [Fact]
        public void Disk_store_writes_atomically_and_reads_back()
        {
            var dir = Path.Combine(Path.GetTempPath(), "cubo-" + System.Guid.NewGuid());
            var d = new DiskFileStore(dir);
            Assert.Null(d.Read("a.json"));
            d.Write("a.json", "um"); d.Write("a.json", "dois");
            Assert.Equal("dois", d.Read("a.json"));
            Assert.False(File.Exists(Path.Combine(dir, "a.json.tmp")));
            Directory.Delete(dir, true);
        }
    }
}