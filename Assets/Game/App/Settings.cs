using System;
using Newtonsoft.Json.Linq;

namespace Cubo.App
{
    // Os ajustes do jogador (ajustes.json). Lang null = seguir o idioma do aparelho.
    public sealed class Settings
    {
        public const int Version = 4;        // 2: o cubo passou a 6x6; 3: 8x8 para testar; 4: de volta a 6x6 (o tamanho gravado antes é esquecido)
        public static readonly int[] Sizes = { 4, 5, 6, 7, 8 };

        public string Lang;
        public int Size = 6;
        public string Mode = "facil";

        // Tolerante: o que faltar ou não valer volta ao padrão.
        public static Settings Clean(JToken j)
        {
            var s = new Settings();
            if (j == null || j.Type != JTokenType.Object) return s;
            string lang = (string)j["lang"];
            if (Array.IndexOf(Texts.Languages, lang) >= 0) s.Lang = lang;
            bool current = j["v"] != null && j["v"].Type == JTokenType.Integer && (int)j["v"] >= Version;
            int size = current && j["size"] != null && j["size"].Type == JTokenType.Integer ? (int)j["size"] : s.Size;
            if (Array.IndexOf(Sizes, size) >= 0) s.Size = size;
            if ((string)j["mode"] == "dificil") s.Mode = "dificil";
            return s;
        }

        public JObject ToJson() => new JObject
        {
            { "v", Version }, { "lang", Lang }, { "size", Size }, { "mode", Mode }
        };
    }
}