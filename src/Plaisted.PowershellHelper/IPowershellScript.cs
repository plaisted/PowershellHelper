using System.Collections.Generic;

namespace Plaisted.PowershellHelper
{
    public interface IPowershellScript
    {
        bool StopOnErrors { get; set; }

        void AddCommand(string command);
        void AddCommands(IEnumerable<string> commands);
        string CreateTempFile();
        void Dispose();
        void SetObject(IJsonObject jsonObject);
        void SetObject(string objectName, string tempJsonFile);
        void SetOutObject(string objectName, string tempJsonFile);
    }
}