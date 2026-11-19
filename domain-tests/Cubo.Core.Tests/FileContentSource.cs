using System.IO;
using Cubo.Data;

namespace Cubo.Core.Tests
{
    // Lê os JSON de Assets/Game/Content/Json (copiados para a pasta de saída dos testes).
    public sealed class FileContentSource : IContentSource
    {
        public string Read(string name) => File.ReadAllText(Fixtures.Content(name));
    }
}