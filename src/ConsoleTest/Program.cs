using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Plaisted.PowershellHelper;
using System;
using System.Diagnostics;
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

        static async void RunTest(CancellationToken token, Stopwatch stopwatch, ILoggerFactory loggerFactory)
        {
            stopwatch.Start();
            var helper = new PowershellHelper(loggerFactory).WithProcessCleanup(CleanupType.Children);
            //helper.AddInputObject("testObject", new TestClass { TestProperty = "myValue" });
            //helper.AddCommand("cmd.exe \"/c start notepad.exe\"");
            //helper.AddCommand("cmd.exe \"/c start notepad.exe\"");
            //helper.AddCommand("cmd.exe \"/c start notepad.exe\"");
            //helper.AddCommand("cmd.exe \"/c start notepad.exe\"");
            //helper.AddCommand("cmd.exe \"/c start notepad.exe\"");
            helper.AddCommand("Write-Host 'test'");
            //helper.AddOutputObject("testObject");

            var exitCode = await helper.Run(token);

            stopwatch.Stop();
            Console.WriteLine(stopwatch.ElapsedMilliseconds);
        }
    }
}