namespace Plaisted.PowershellHelper
{
    public interface IJsonObject
    {
        string Name { get; set; }
        object Object { get; set; }

        string CreateTempFile();
        void Dispose();
    }
}