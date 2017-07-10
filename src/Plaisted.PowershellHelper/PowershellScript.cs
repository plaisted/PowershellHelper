using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Plaisted.PowershellHelper
{
    public class PowershellScript : IDisposable, IPowershellScript
    {
        private List<string> declarations = new List<string>();
        private List<string> commands = new List<string>();
        private List<string> outputs = new List<string>();
        private string tempFile;

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
        public void SetOutObject(string objectName, string tempJsonFile)
        {
            outputs.Add($"if (${objectName} -ne $null) {{ ${objectName} | ConvertTo-Json | Out-File \"{tempJsonFile}\" }}");
        }
        public void SetObject(string objectName, string tempJsonFile)
        {
            declarations.Add($"${objectName} = [IO.File]::ReadAllText(\"{tempJsonFile}\") | ConvertFrom-Json");
        }
        public void SetObject(IJsonObject jsonObject)
        {
            SetObject(jsonObject.Name, jsonObject.CreateTempFile());
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
                if (StopOnErrors) { contents += $"if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {{ throw \"Command ${count.ToString()} did not complete succesfully.\" }}" + Environment.NewLine; }
                contents += Environment.NewLine;
                count++;
            }
            //add output vars
            contents += string.Join(Environment.NewLine, outputs);
            tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".ps1");
            File.WriteAllText(tempFile, contents);
            return tempFile;
        }
        public void Dispose()
        {
            File.Delete(tempFile);
        }
    }
}
