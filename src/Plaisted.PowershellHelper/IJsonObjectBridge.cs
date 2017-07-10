namespace Plaisted.PowershellHelper
{
    internal interface IJsonObjectBridge
    {
        string Name { get; set; }
        object Object { get; set; }

        string CreateTempFile();
        void Dispose();
    }
}