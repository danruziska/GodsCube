using System.Collections.Generic;
using Cubo.Domain;

namespace Cubo.App
{
    // O que a sessão conta para a tela. A tela só toca estes eventos, na ordem; nunca lê o domínio para decidir o que mostrar.
    // Board = o tabuleiro logo depois do evento (cópia): é o que a tela pinta, porque o estado do domínio já está no fim do lote.
    public interface IBattleEvent { }

    // Link: 0 é o grupo tocado; 1, 2... são os da cascata (as peças novas formaram outro grupo e ele disparou sozinho).
    public sealed record GroupFired(Side Side, int[] Cells, int Color, bool Crit, int Damage, int Link = 0) : IBattleEvent;
    public sealed record HpChanged(Side Side, int From, int To) : IBattleEvent;
    public sealed record ManaChanged(Side Side, int[] Mana) : IBattleEvent;
    // Cells: as peças novas (no alto das colunas, depois de um grupo). Falls: as que caíram para tapar o buraco.
    public sealed record CellsRefilled(int[] Cells, List<Edit> Retouch, sbyte[] Board, List<Fall> Falls = null) : IBattleEvent;
    public sealed record PowerCast(Side Side, int Slot, string Kind, int Amount, int[] Cells, bool Crit) : IBattleEvent;
    // O cubo inteiro girou: `Face` (física, 0 a 3) foi para a frente de quem joga agora. Board já é o girado.
    public sealed record CubeTurned(Side Side, Move Move, int Face, sbyte[] Board) : IBattleEvent;
    // Quem joga terminou os grupos do turno e agora escolhe a face que vai entregar.
    public sealed record HandPhase(Side Side) : IBattleEvent;
    public sealed record TurnChanged(Side Side, int Round) : IBattleEvent;
    public sealed record BattleEnded(Side Winner) : IBattleEvent;

    // 3 contra 3: a vida de um herói mudou (dano ou cura). Max: o máximo dele.
    public sealed record HeroHpChanged(Side Side, int Hero, int From, int To, int Max) : IBattleEvent;
    // `Side` escolheu atacar o herói `Hero` do outro lado.
    public sealed record TargetChanged(Side Side, int Hero) : IBattleEvent;

    // Veneno e queimadura (Kind: "poison" ou "burn").
    // As áreas mudaram (uma nova, uma que pegou alguém, uma que sumiu, o cubo girou): Map[casa] = 0 nada, 1 veneno, 2 fogo.
    public sealed record HazardsChanged(sbyte[] Map) : IBattleEvent;
    // O grupo tocado armou o item de `Side`: depois da entrega ele escolhe a área.
    public sealed record HazardArmed(Side Side, string Kind) : IBattleEvent;
    // `Side` entregou a face e agora escolhe a área nela (o rival espera).
    public sealed record ZonePhase(Side Side, string Kind) : IBattleEvent;
    // Um grupo de `Side` passou por uma área: o herói `Hero` dele pegou o efeito (Dmg por turno, Turns turnos).
    public sealed record Afflicted(Side Side, int Hero, string Kind, int Dmg, int Turns) : IBattleEvent;
    // Começo do turno de `Side`: o efeito tirou Dmg de vida do herói `Hero` (faltam TurnsLeft turnos).
    public sealed record DotTicked(Side Side, int Hero, string Kind, int Dmg, int TurnsLeft) : IBattleEvent;
}
