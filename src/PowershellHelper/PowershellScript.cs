using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Plaisted.PowershellHelper
{
    internal class PowershellScript : IDisposable, IPowershellScript
    {
        private List<string> declarations = new List<string>();
        private List<string> commands = new List<string>();
        private List<string> outputs = new List<string>();
        private string tempFile;
        private string tempPath = Path.GetTempPath();

        /// <summary>
        /// Sets if the powershell script should stop on errors or continue
        /// </summary>
        public bool StopOnErrors { get; set; } = true;

        public void AddCommands(IEnumerable<string> commands)
        {
            this.commands.AddRange(commands);
        }
        public void AddCommand(string command)
        {
            this.commands.Add(command);
        }
        public void SetTempPath(string path)
        {
            tempPath = path;
        }
        public void SetOutObject(string objectName, string tempJsonFile)
        {
            outputs.Add($"if (${objectName} -ne $null) {{ ${objectName} | ConvertTo-Json | Out-File \"{tempJsonFile}\" }}");
        }
        public void SetObject(string objectName, string tempJsonFile)
        {
            declarations.Add($"${objectName} = [IO.File]::ReadAllText(\"{tempJsonFile}\") | ConvertFrom-Json");
        }
        public void SetObject(IJsonObjectBridge jsonObject)
        {
            SetObject(jsonObject.EscapedName, jsonObject.CreateTempFile());
        }
        public void SetOutObject(IJsonObjectBridge jsonObject)
        {
            SetOutObject(jsonObject.EscapedName, jsonObject.TemporaryFile);
        }
        public string CreateTempFile()
        {
            var contents = "";
            //add stop action if needed
            if (StopOnErrors) { contents += "$ErrorActionPreference = \"Stop\"" + Environment.NewLine; }
            //add all object declarations
            contents += string.Join(Environment.NewLine, declarations) + Environment.NewLine;
            //add commands with error handling where needed
            int count = 0;
            foreach (var command in commands)
            {
                contents += "##Command " + count.ToString() + Environment.NewLine;
                contents += command + Environment.NewLine;
                if (StopOnErrors) { contents += ExitCodeVerify(count.ToString()); }
                contents += Environment.NewLine;
                count++;
            }
            //add output vars
            contents += string.Join(Environment.NewLine, outputs);
            if (StopOnErrors) { contents += LegacyExitCodeHelper(); }
            tempFile = Path.Combine(tempPath, Guid.NewGuid().ToString() + ".ps1");
            File.WriteAllText(tempFile, contents);
            return tempFile;
        }
        public void Dispose()
        {
            if (!string.IsNullOrEmpty(tempFile)) File.Delete(tempFile);
        }

        private static string LegacyExitCodeHelper()
        {
            return "trap { Write-Error $_;  exit 1; }";
        }

        private static string ExitCodeVerify(string commandNumber)
        {
            return $"if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {{ throw \"Command {commandNumber} did not complete succesfully.\" }}" + Environment.NewLine;
        }
    }
}
