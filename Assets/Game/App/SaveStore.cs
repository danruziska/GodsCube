using System;
using System.Collections.Generic;
using System.IO;

namespace Cubo.App
{
    // Onde os arquivos de salvamento moram. Read devolve null se o arquivo não existe (ou não dá para ler).
    public interface IFileStore
    {
        string Read(string name);
        void Write(string name, string text);
    }

    // Na memória: para os testes e para quando não há pasta de dados.
    public sealed class MemoryFileStore : IFileStore
    {
        public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
        public string Read(string name) => Files.TryGetValue(name, out var s) ? s : null;
        public void Write(string name, string text) => Files[name] = text;
    }

    // Em disco, com gravação atômica: escreve em nome.tmp e só então troca o arquivo de verdade.
    // Se o app morrer no meio da escrita, o arquivo antigo continua inteiro.
    public sealed class DiskFileStore : IFileStore
    {
        private readonly string _dir;

        public DiskFileStore(string dir)
        {
            _dir = dir;
            Directory.CreateDirectory(dir);
        }

        public string Read(string name)
        {
            try
            {
                string path = Path.Combine(_dir, name);
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (Exception) { return null; }
        }

        public void Write(string name, string text)
        {
            string path = Path.Combine(_dir, name), tmp = path + ".tmp";
            File.WriteAllText(tmp, text);
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }
    }
}