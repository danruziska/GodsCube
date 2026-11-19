using System.Collections.Generic;

namespace Cubo.Domain
{
    // Números da corrida (run.json).
    public sealed class RunRules
    {
        public int Floors;
        public double HealFrac;                         // vida recuperada sozinha depois de cada andar (0: só com a Poção)
        public int PotionHp, PotionPrice;               // a Poção de Cura da loja: vida por herói e preço
        public int RerollBase, RerollStep;              // trocar as ofertas da loja: o 1º preço e quanto sobe a cada troca
        public double Sale;                             // o desconto da oferta em promoção (0,5 = metade do preço)
        public double[] EnemyDmg, EnemyScale;           // por andar
        public string[] Levels;                         // nível da IA por andar
        public int GoldBase, GoldPerFloor;
        public int ShopRunes, RuneSlots;
        public int ShopUpgrades, BlessingSize;          // melhorias à venda na loja; opções da bênção depois do andar
        public int[] UpgradePrices;                     // [nível atual] = o preço do próximo nível na loja
        public double EliteHp, EliteDmg, EliteGold;
        public double EliteCombo, BossCombo;     // a chance de combo do rival de elite e do chefe (os outros: a de base)
        public EventRules Event;
    }

    // Os eventos da corrida (events.json): PerRun eventos por corrida, cada um uma vez, em andares sorteados.
    public sealed class EventRules
    {
        public int PerRun = 3;
        public int IdolBlood;                           // O Ídolo: a vida máxima que o herói escolhido dá
        public double IdolWrath;                        // ...e o dano do rival na próxima batalha (a ira)
        public string[] Idols = { "rei", "rainha", "torre" };   // os três ídolos; cada corrida sorteia um
        // Rei (ataque): sangue = +IdolCrit de crítico, ira = +IdolDmg de dano. Rainha (mana): sangue = +IdolMana de mana
        // inicial em cada poder, ira = os poderes custam IdolCost a menos. Torre (defesa): sangue = IdolWard de dano a menos,
        // ira = +IdolHp de vida máxima em cada herói. Tudo até o fim da corrida.
        public double IdolCrit, IdolDmg, IdolWard;
        public int IdolMana, IdolCost, IdolHp;
        public double CampAmbush, CampMark;             // O Acampamento: a vida a menos do próximo rival; o dano a mais no rival marcado
        public int CampRaid;                            // ...e o ouro do saque
        public int VowGold, VowRounds;                  // O Juramento: o prêmio e o limite de rodadas do voto "rápido"
    }

    // Uma runa de uso único, gasta no começo da próxima batalha. Kind: mana, crit, ward, hp, combo.
    public sealed class RuneDef
    {
        public string Id, Kind;
        public int Price, N;
        public double V;
    }


}