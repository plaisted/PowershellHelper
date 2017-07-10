using System;
using Xunit;

namespace Plaisted.PowershellHelper.Tests
{
    public class PowershellScriptTests
    {
        [Fact]
        public void Testing()
        {
            using (var script = new PowershellScript())
            {
                using (var jsonObject = new JsonObjectBridge("Test"))
                {
                    jsonObject.Object = new { Test = "test" };
                    script.SetObject(jsonObject);
                    script.AddCommand("Write-Host \"Test\"");
                    script.AddCommand("$test = \"Test\"");
                    script.SetOutObject("test", @"c:\temp\output.txt");
                    var tempFile = script.CreateTempFile();
                }
            }

        }
    }
}
