namespace Plaisted.PowershellHelper
{
    internal interface IJsonObjectBridge
    {
        string Name { get; set; }
        string EscapedName { get; }
        object Object { get; set; }
        void ReadFromTempFile();
        string TemporaryFile { get; }
        string CreateTempFile();
        void Dispose();
    }
}