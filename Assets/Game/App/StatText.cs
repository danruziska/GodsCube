using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Cubo.App
{
    // O texto de uma melhoria com o valor antigo ao lado do novo: "2 (1) de dano por peça · custo 6 (8)".
    // `next` e `now` saem do mesmo modelo de texto, só os números mudam; cada número que mudou ganha o antigo entre
    // parênteses logo depois. Onde os textos deixam de bater (o nível ★ acrescenta uma frase), o resto fica como em `next`.
    public static class StatText
    {
        private static readonly Regex Number = new Regex(@"[+\-−]?\d+(?:[.,]\d+)?%?", RegexOptions.Compiled);

        public static string WithOld(string next, string now)
        {
            if (string.IsNullOrEmpty(next) || string.IsNullOrEmpty(now) || next == now) return next ?? "";
            var a = Split(next); var b = Split(now);
            var sb = new StringBuilder();
            int i = 0;
            for (; i < a.Count && i < b.Count; i++)
            {
                bool isNum = i % 2 == 1;                       // texto, número, texto, número...
                if (!isNum && a[i] != b[i]) break;             // o modelo mudou: daqui para a frente, só o novo
                sb.Append(a[i]);
                if (isNum && a[i] != b[i]) sb.Append(" (").Append(b[i]).Append(')');
            }
            for (; i < a.Count; i++) sb.Append(a[i]);
            return sb.ToString();
        }

        // Alterna texto e número, começando sempre por texto (pode ser vazio).
        private static List<string> Split(string s)
        {
            var parts = new List<string>();
            int last = 0;
            foreach (Match m in Number.Matches(s))
            {
                parts.Add(s.Substring(last, m.Index - last));
                parts.Add(m.Value);
                last = m.Index + m.Length;
            }
            parts.Add(s.Substring(last));
            return parts;
        }
    }
}
