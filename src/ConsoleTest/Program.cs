using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Plaisted.PowershellHelper;
using System;
using System.Diagnostics;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ILoggerFactory loggerFactory = new LoggerFactory()
                .AddConsole();

            var dummy = JsonConvert.SerializeObject(new {  });
            var stopwatch = new Stopwatch();
            var task = Task.Run(() => RunTest(new CancellationToken(), stopwatch, loggerFactory));
            task.Wait();
            Console.ReadLine();
        }

        static async void RunTest(CancellationToken cancellationToken, Stopwatch stopwatch, ILoggerFactory loggerFactory)
        {
            stopwatch.Start();

            //var helper = new PowershellHelper(loggerFactory).WithProcessCleanup(CleanupType.RecursiveAdmin);
            //helper.AddCommand("cmd.exe \"/c start notepad.exe\"");
            //helper.AddCommand("notepad.exe");
            //var exitCode = await helper.Run(cancellationToken);
            var pass = "pass";
            var ssPwd = new SecureString();
            for (int x = 0; x < pass.Length; x++)
            {
                ssPwd.AppendChar(pass[x]);
            }



            var helper = new PowershellHelper(loggerFactory).WithOptions(o =>
                {
                    o.CleanupMethod = CleanupType.RecursiveAdmin;
                    o.SharedTempPath = "c:\\temp";
                    o.WorkingPath = "c:\\temp";
                    o.Credentials = new RunCredentials { Domain = "domain", UserName = "user", Password = ssPwd, NoProfile=false };
                });

            //helper.AddInputObject("testObject", new TestClass { TestProperty = "testValue" });
            //helper.AddCommand("$testObject.TestProperty = 'newTestValue'");
            helper.AddCommand("notepad.exe");
            helper.AddCommand("Get-ChildItem Env:");
            helper.AddCommand(@"Write-Host ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] ""Administrator"")");
            //helper.AddCommand("cmd.exe '/c exit 1'");
            //helper.AddOutputObject("testObject");
            var exitCode = await helper.RunAsync(cancellationToken);

            stopwatch.Stop();
            Console.WriteLine(stopwatch.ElapsedMilliseconds);

            //var scriptOutput = helper.Output["testObject"].ToObject<TestClass>();
            //Console.WriteLine("New value is: " + scriptOutput.TestProperty);



        }
    }
}