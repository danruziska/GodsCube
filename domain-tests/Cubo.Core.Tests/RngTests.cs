using System.IO;
using System.Text.Json;
using Xunit;
using Cubo.Domain;

namespace Cubo.Core.Tests
{
    public class RngTests
    {
        [Fact]
        public void Mulberry32_reproduz_os_valores_do_javascript()
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Fixtures.Vector("rng.json")));
            foreach (var c in doc.RootElement.GetProperty("cases").EnumerateArray())
            {
                var seed = c.GetProperty("seed").GetUInt32();
                var rng = new Mulberry32(seed);
                int i = 0;
                foreach (var expected in c.GetProperty("first").EnumerateArray())
                {
                    Assert.True(expected.GetDouble() == rng.Next(),
                        $"semente {seed}, valor {i}: esperado {expected.GetDouble():R}, obtido diferente");
                    i++;
                }
            }
        }
    }
}