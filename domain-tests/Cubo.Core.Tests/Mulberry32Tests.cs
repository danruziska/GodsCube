using Xunit;
using Cubo.Domain;

namespace Cubo.Core.Tests
{
    public class Mulberry32Tests
    {
        [Fact]
        public void State_permite_salvar_e_retomar_a_sequencia()
        {
            var original = new Mulberry32(42);
            original.Next();
            original.Next();
            var saved = original.State;
            var proximo = original.Next();

            var retomado = new Mulberry32(1);   // semente diferente de propósito
            retomado.State = saved;

            Assert.True(proximo == retomado.Next());
        }

        [Fact]
        public void NextInt_fica_no_intervalo_e_trunca_como_o_javascript()
        {
            var a = new Mulberry32(7);
            var b = new Mulberry32(7);
            for (int i = 0; i < 1000; i++)
            {
                int v = a.NextInt(6);
                Assert.InRange(v, 0, 5);
                Assert.Equal((int)(b.Next() * 6), v);
            }
        }

        [Fact]
        public void NextInt_com_n_igual_a_1_sempre_devolve_zero()
        {
            var rng = new Mulberry32(3);
            for (int i = 0; i < 100; i++) Assert.Equal(0, rng.NextInt(1));
        }
    }
}