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

        static async void RunTest(CancellationToken cancellationToken, Stopwatch stopwatch, ILoggerFactory loggerFactory)
        {
            stopwatch.Start();

            //var helper = new PowershellHelper(loggerFactory).WithProcessCleanup(CleanupType.RecursiveAdmin);
            //helper.AddCommand("cmd.exe \"/c start notepad.exe\"");
            //helper.AddCommand("notepad.exe");
            //var exitCode = await helper.Run(cancellationToken);

            var helper = new PowershellHelper(loggerFactory).WithProcessCleanup(CleanupType.None);
            //helper.AddInputObject("testObject", new TestClass { TestProperty = "testValue" });
            //helper.AddCommand("$testObject.TestProperty = 'newTestValue'");
            helper.AddCommand("Write-Output hello");
            //helper.AddOutputObject("testObject");
            var exitCode = await helper.Run(cancellationToken);

            stopwatch.Stop();
            Console.WriteLine(stopwatch.ElapsedMilliseconds);

            //var scriptOutput = helper.Output["testObject"].ToObject<TestClass>();
            //Console.WriteLine("New value is: " + scriptOutput.TestProperty);



        }
    }
}