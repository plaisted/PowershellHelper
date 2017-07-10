using System.Collections.Generic;

namespace Plaisted.PowershellHelper
{
    internal interface IPowershellScript
    {
        bool StopOnErrors { get; set; }

        void AddCommand(string command);
        void AddCommands(IEnumerable<string> commands);
        string CreateTempFile();
        void Dispose();
        void SetObject(IJsonObjectBridge jsonObject);
        void SetObject(string objectName, string tempJsonFile);
        void SetOutObject(string objectName, string tempJsonFile);
    }
}