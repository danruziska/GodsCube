using System;

namespace Cubo.Domain
{
    // Arredondamentos do JavaScript, para o resultado bater com o protótipo.
    public static class JsMath
    {
        // Math.round do JavaScript: .5 sobe.
        public static double Round(double x) => Math.Floor(x + 0.5);
        public static int RoundInt(double x) => (int)Math.Floor(x + 0.5);
        // Math.round(x * 100) / 100
        public static double Round2(double x) => Math.Floor(x * 100 + 0.5) / 100;
    }
}