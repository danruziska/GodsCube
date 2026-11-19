using System;
using System.IO;

namespace Cubo.Core.Tests
{
    public static class Fixtures
    {
        public static string Vector(string file) =>
            Path.Combine(AppContext.BaseDirectory, "vectors", file);

        // Procura em qualquer subpasta de content/, por exemplo "cards" ou "ui.pt".
        public static string Content(string name)
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "content");
            var found = Directory.GetFiles(dir, name + ".json", SearchOption.AllDirectories);
            if (found.Length == 0) throw new FileNotFoundException("Conteúdo não encontrado: " + name);
            return found[0];
        }
    }
}