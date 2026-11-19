namespace Cubo.View
{
    // Quem pode ser arrastado como "um deus": o cartão da grade e o espaço ocupado do time.
    // O espaço do time que recebe o arrasto lê daqui qual deus chegou.
    public interface IGodSource
    {
        string GodId { get; }                 // null: não há deus aqui
    }
}