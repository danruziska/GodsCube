using System.Linq;
using Cubo.Domain;

namespace Cubo.App
{
    // Só para testar: abre qualquer tela entre os andares agora, de qualquer ponto do jogo (preparação, batalha,
    // outra tela). Sem corrida, começa uma com o time atual. A batalha em curso é abandonada. Ao sair da tela aberta,
    // a corrida segue normalmente (a próxima batalha). Quem chama é o seletor de telas (ScreenPicker) e o menu Cubo ▸ Teste.
    public sealed partial class GameFlow
    {
        public static readonly string[] PreviewScreens = { "blessing", "idolo:rei", "idolo:rainha", "idolo:torre", "acampamento", "juramento", "shop", "door", "win", "lose" };

        public void Preview(string screen)
        {
            if (Run == null)
            {
                var team = _prepTeam.Count == 3 ? _prepTeam.ToArray() : Col.Team;
                Run = _runs.Start(team, Col.Equip);
                _runs.PlanEvents(Run, _rng);
            }
            FillHeroHp();
            Session = null; Record = null;
            Run.Blessing = null; Run.Event = null; Run.Shop = null; Run.Door = false;
            _blessFocus = null; _eventFocus = null; _doorFocus = null; _shopFocus = null; _shopSay = null; _hint = "";

            switch (screen)
            {
                case "blessing":
                    var b = _runs.NewBlessing(Run, _rng);
                    Run.Blessing = b.Count > 0 ? b : null;
                    break;
                case "shop": Run.Shop = _runs.NewShop(Run, _rng); break;
                case "door": Run.Door = true; break;
                case "win": case "lose":
                    _endWin = screen == "win";
                    Stage = Stage.End;
                    SaveRun();
                    Changed?.Invoke();
                    return;
                default:
                    string id = screen.Split(':')[0], variant = screen.Contains(":") ? screen.Substring(screen.IndexOf(':') + 1) : null;
                    if (id == "idolo" && variant != null) Run.Idol = variant;              // "idolo:rainha" abre o ídolo da Rainha
                    if (_d.EventIds.Contains(id)) Run.Event = new EventState { Id = id, Variant = id == "idolo" ? _runs.IdolOf(Run) : null };
                    break;
            }
            Resume();
        }

        // Uma corrida que ainda não lutou não tem vida por herói; o Ídolo e a loja precisam dela.
        private void FillHeroHp()
        {
            if (Run.HeroHp != null && Run.HeroMax != null) return;
            var o = _runs.Opts(Run);
            o.Size = Settings.Size; o.Mode = Settings.Mode; o.PlayerHeroHp = null;
            var st = _engine.NewState(o, Run.Team, new Mulberry32(1));
            Run.HeroHp = (int[])st.HeroHp[0].Clone();
            Run.HeroMax = (int[])st.HeroMax[0].Clone();
            RunService.SyncRunHp(Run);
        }

        // Feridos para testar a poção e o Ídolo: cada herói com um terço da vida, o terceiro caído.
        public void PreviewHurt()
        {
            if (Run == null) return;
            FillHeroHp();
            for (int h = 0; h < Run.HeroHp.Length; h++) Run.HeroHp[h] = h == Run.HeroHp.Length - 1 ? 0 : System.Math.Max(1, Run.HeroMax[h] / 3);
            RunService.SyncRunHp(Run);
            SaveRun();
            Changed?.Invoke();
        }

        // Ouro para testar a loja.
        public void PreviewGold(int gold)
        {
            if (Run == null) return;
            Run.Gold += gold;
            SaveRun();
            Changed?.Invoke();
        }
    }
}
