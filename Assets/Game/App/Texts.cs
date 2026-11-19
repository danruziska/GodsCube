using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Cubo.Data;
using Cubo.Domain;

namespace Cubo.App
{
    // Os textos do jogo em três idiomas. Sem engine: lê os JSON por IContentSource.
    // Uma chave que falta no idioma cai no português; se falta também lá, aparece ⟦chave⟧ (a falta fica visível).
    public sealed class Texts
    {
        public static readonly string[] Languages = { "pt", "en", "es" };
        public static readonly string[] DomainIds = { "sol", "mar", "guerra", "morte", "colheita", "tempestade" };   // domains.json, por índice

        private readonly Dictionary<string, Dictionary<string, string>> _ui = new Dictionary<string, Dictionary<string, string>>();
        private readonly Dictionary<string, Dictionary<string, string>> _names = new Dictionary<string, Dictionary<string, string>>();

        public string Lang { get; private set; } = "pt";
        public event Action Changed;                 // a tela refaz os textos quando o idioma muda

        public Texts(IContentSource src)
        {
            foreach (var lang in Languages)
            {
                var d = new Dictionary<string, string>();
                foreach (var p in JObject.Parse(src.Read("ui." + lang)).Properties()) d[p.Name] = (string)p.Value;
                _ui[lang] = d;
            }
            foreach (var p in JObject.Parse(src.Read("names")).Properties())
            {
                var d = new Dictionary<string, string>();
                foreach (var q in ((JObject)p.Value).Properties()) d[q.Name] = (string)q.Value;
                _names[p.Name] = d;
            }
        }

        public IEnumerable<string> UiKeys(string lang) => _ui[lang].Keys;
        public IEnumerable<string> NameKeys => _names.Keys;
        public bool HasName(string key, string lang) => _names.TryGetValue(key, out var d) && d.ContainsKey(lang);

        public void SetLang(string lang)
        {
            if (Array.IndexOf(Languages, lang) < 0) lang = "pt";
            if (lang == Lang) return;
            Lang = lang;
            Changed?.Invoke();
        }

        // pt, es ou en (qualquer outro idioma cai no inglês), a partir de um código como "pt-BR".
        public static string Detect(string code)
        {
            string l = (code ?? "pt").ToLowerInvariant();
            if (l.StartsWith("pt")) return "pt";
            if (l.StartsWith("es")) return "es";
            return "en";
        }

        // T("fx.heal", ("n", 12)) troca {n} por 12.
        public string T(string key, params (string name, object value)[] args)
        {
            if (!Find(key, out var s)) return "⟦" + key + "⟧";
            foreach (var (name, value) in args)
                s = s.Replace("{" + name + "}", Convert.ToString(value, CultureInfo.InvariantCulture));
            return s;
        }

        private bool Find(string key, out string s) => _ui[Lang].TryGetValue(key, out s) || _ui["pt"].TryGetValue(key, out s);

        // Nomes próprios: Name("hero.rei"), Name("power.rei.0"), Name("passive.investida"), Name("domain.sol").
        public string Name(string key)
        {
            if (_names.TryGetValue(key, out var d) && (d.TryGetValue(Lang, out var s) || d.TryGetValue("pt", out s))) return s;
            return "⟦" + key + "⟧";
        }

        public string Domain(int color) => Name("domain." + DomainIds[color]);

        public static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

    }
}