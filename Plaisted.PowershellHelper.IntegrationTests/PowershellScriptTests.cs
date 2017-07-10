using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace Plaisted.PowershellHelper.IntegrationTests
{
    public class PowershellScriptTests
    {
        [Fact]
        public void It_Adds_Command_To_Script()
        {
            using (var workspace = new TemporaryWorkspace())
            {
                //setup dummy file to delete
                var testFile = workspace.AddFileWithContents("test.txt", "testing");

                using (var script = new PowershellScript())
                {
                    //add delete command
                    script.AddCommand("Remove-Item "+ testFile);
                    var tempFile = script.CreateTempFile();

                    //run
                    var process = new Process();
                    var si = new ProcessStartInfo();
                    si.FileName = "powershell.exe";
                    si.Arguments = tempFile;
                    si.UseShellExecute = false;
                    process.StartInfo = si;
                    process.Start();
                    process.WaitForExit();
                }

                //verify deletion
                Assert.False(File.Exists(testFile));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void It_Should_Use_StopOnErrors_Flag_For_Cmdlet(bool stopOnErrors)
        {
            using (var workspace = new TemporaryWorkspace())
            {
                //setup dummy file to delete
                var testFile = workspace.AddFileWithContents("test.txt", "testing");

                using (var script = new PowershellScript())
                {
                    script.StopOnErrors = stopOnErrors;
                    //add error
                    script.AddCommand("Invalid-command-command");
                    //add delete command
                    script.AddCommand("Remove-Item " + testFile);
                    var tempFile = script.CreateTempFile();

                    //run
                    var process = new Process();
                    var si = new ProcessStartInfo();
                    si.FileName = "powershell.exe";
                    si.Arguments = tempFile;
                    si.UseShellExecute = false;
                    process.StartInfo = si;
                    process.Start();
                    process.WaitForExit();
                }

                //verify deletion
                Assert.Equal(stopOnErrors, File.Exists(testFile));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void It_Should_Use_StopOnErrors_Flag_For_ExitCode(bool stopOnErrors)
        {
            using (var workspace = new TemporaryWorkspace())
            {
                //setup dummy file to delete
                var testFile = workspace.AddFileWithContents("test.txt", "testing");

                using (var script = new PowershellScript())
                {
                    script.StopOnErrors = stopOnErrors;
                    //add error (robocopy returns odd exit codes)
                    script.AddCommand("cmd.exe /c \"exit /B 1\" | Out-Null");
                    //add delete command
                    script.AddCommand("Remove-Item " + testFile);
                    var tempFile = script.CreateTempFile();

                    //run
                    var process = new Process();
                    var si = new ProcessStartInfo();
                    si.FileName = "powershell.exe";
                    si.Arguments = tempFile;
                    si.RedirectStandardOutput = true;
                    si.RedirectStandardError = true;
                    si.UseShellExecute = false;
                    process.StartInfo = si;
                    process.Start();
                    process.WaitForExit();
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                }

                //verify deletion
                Assert.Equal(stopOnErrors, File.Exists(testFile));
            }
        }

        [Fact]
        public void It_Adds_Object_To_Script()
        {
            using (var workspace = new TemporaryWorkspace())
            {
                //setup mock jsonobject from test object
                var jsonFile = workspace.AddFileWithContents("testObject.json", JsonConvert.SerializeObject(new {Property = "testValue" }));
                var mockJson = new Mock<IJsonObject>();
                mockJson.Setup(x => x.CreateTempFile()).Returns(jsonFile);
                mockJson.Setup(x => x.Name).Returns("testObject");

                using (var script = new PowershellScript())
                {
                    //add object
                    script.SetObject(mockJson.Object);

                    //add commands to verify object exists in powershell env
                    script.AddCommand("$result  = ($testObject -ne $null)");
                    script.AddCommand("$result2  = ($testObject.Property -eq \"testValue\")");
                    //add command to output verification so c# can assert on it
                    script.AddCommand("Write-Host $result $result2");
                    var tempFile = script.CreateTempFile();

                    //run and record output
                    var process = new Process();
                    var si = new ProcessStartInfo();
                    si.FileName = "powershell.exe";
                    si.Arguments = tempFile;
                    si.RedirectStandardOutput = true;
                    si.RedirectStandardError = true;
                    si.UseShellExecute = false;
                    process.StartInfo = si;
                    process.Start();
                    process.WaitForExit();
                    var output = process.StandardOutput.ReadToEnd();

                    //verify results
                    Assert.Equal(true, output.Contains("True True"));
                }
            }
        }

        [Fact]
        public void It_Returns_Object_From_Script()
        {
            using (var workspace = new TemporaryWorkspace())
            {
                using (var script = new PowershellScript())
                {
                    //setup output object
                    var outputFile = Path.Combine(workspace.TempPath, "output.json");
                    script.SetOutObject("test", outputFile);
                    
                    //create object with same name in script
                    script.AddCommand("$test  = New-Object -TypeName PSObject -Property @{'TestProperty'=\"TestValue\"}");
                    
                    var tempFile = script.CreateTempFile();

                    //run
                    var process = new Process();
                    var si = new ProcessStartInfo();
                    si.FileName = "powershell.exe";
                    si.Arguments = tempFile;
                    si.UseShellExecute = false;
                    process.StartInfo = si;
                    process.Start();
                    process.WaitForExit();

                    //verify object value was captured
                    dynamic output = JObject.Parse(File.ReadAllText(outputFile));
                    Assert.Equal("TestValue", (string)output.TestProperty);
                }
            }
        }
    }
}
