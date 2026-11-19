using UnityEditor;
using UnityEngine;
using Cubo.App;
using Cubo.Domain;
using Cubo.View;

namespace Cubo.Build
{
    // Atalhos para testar as telas entre os andares sem jogar a batalha inteira. Só no Play.
    public static class TestTools
    {
        // F9: a batalha atual termina com vitória do jogador e o jogo segue (bênção, evento, loja ou porta).
        [MenuItem("Cubo/Teste/Vencer a batalha agora _F9")]
        public static void WinNow()
        {
            var flow = Flow();
            if (flow == null || flow.Stage != Stage.Battle || flow.Session == null) { Debug.Log("Teste: nenhuma batalha em andamento."); return; }
            flow.Session.State.Winner = Side.Player;
            flow.BattleFinished();
            Debug.Log("Teste: batalha vencida.");
        }

        // F10: cada herói do rival de pé fica com 1 de vida; o próximo grupo vence a batalha do jeito normal.
        [MenuItem("Cubo/Teste/Rival com 1 de vida _F10")]
        public static void RivalAtOne()
        {
            var flow = Flow();
            if (flow == null || flow.Stage != Stage.Battle || flow.Session == null) { Debug.Log("Teste: nenhuma batalha em andamento."); return; }
            var st = flow.Session.State;
            for (int h = 0; h < BattleEngine.Heroes; h++)
                if (st.HeroHp[1][h] > 1) st.HeroHp[1][h] = 1;
            BattleEngine.SyncHp(st, Side.Enemy);
            Debug.Log("Teste: os heróis do rival estão com 1 de vida (as barras atualizam no próximo lance).");
        }


        // F11: o próximo andar terá um evento, um de cada vez (Ídolo, Acampamento, Juramento). Aperte e depois F9.
        private static int _nextEvent;
        [MenuItem("Cubo/Teste/Evento no próximo andar _F11")]
        public static void EventNext()
        {
            var flow = Flow();
            if (flow == null || flow.Run == null) { Debug.Log("Teste: nenhuma corrida em andamento."); return; }
            var ids = new[] { "idolo", "acampamento", "juramento" };
            var id = ids[_nextEvent++ % ids.Length];
            flow.Run.EventFloors = new System.Collections.Generic.List<int> { flow.Run.Floor + 1 };
            flow.Run.EventOrder = new System.Collections.Generic.List<string> { id };
            Debug.Log("Teste: o próximo andar terá o evento " + id + ". Vença a batalha (F9).");
        }


        // Abrir direto qualquer tela entre os andares (o mesmo que o botão "Screens" no jogo).
        [MenuItem("Cubo/Teste/Abrir tela/Bênção")] public static void OpenBlessing() => Open("blessing");
        [MenuItem("Cubo/Teste/Abrir tela/Ídolo do Rei")] public static void OpenIdolKing() => Open("idolo:rei");
        [MenuItem("Cubo/Teste/Abrir tela/Ídolo da Rainha")] public static void OpenIdolQueen() => Open("idolo:rainha");
        [MenuItem("Cubo/Teste/Abrir tela/Ídolo da Torre")] public static void OpenIdolRook() => Open("idolo:torre");
        [MenuItem("Cubo/Teste/Abrir tela/Evento: Acampamento")] public static void OpenCamp() => Open("acampamento");
        [MenuItem("Cubo/Teste/Abrir tela/Evento: Juramento")] public static void OpenVow() => Open("juramento");
        [MenuItem("Cubo/Teste/Abrir tela/Loja")] public static void OpenShop() => Open("shop");
        [MenuItem("Cubo/Teste/Abrir tela/Porta")] public static void OpenDoor() => Open("door");
        [MenuItem("Cubo/Teste/Abrir tela/Fim: vitória")] public static void OpenWin() => Open("win");
        [MenuItem("Cubo/Teste/Abrir tela/Fim: derrota")] public static void OpenLose() => Open("lose");

        private static void Open(string screen)
        {
            var flow = Flow();
            if (flow == null) { Debug.Log("Teste: aperte Play primeiro."); return; }
            flow.Preview(screen);
        }

        [MenuItem("Cubo/Teste/Vencer a batalha agora _F9", true)]
        [MenuItem("Cubo/Teste/Evento no próximo andar _F11", true)]
        [MenuItem("Cubo/Teste/Rival com 1 de vida _F10", true)]
        private static bool InPlay() => EditorApplication.isPlaying;

        private static GameFlow Flow()
        {
            var boot = Object.FindAnyObjectByType<GameBootstrap>();
            return boot != null ? boot.Flow : null;
        }
    }
}
