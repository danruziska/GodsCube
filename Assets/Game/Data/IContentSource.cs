namespace Cubo.Data
{
    public interface IContentSource
    {
        // name é o nome do arquivo sem .json, por exemplo "rules", "cards" ou "ui.pt".
        string Read(string name);
    }
}