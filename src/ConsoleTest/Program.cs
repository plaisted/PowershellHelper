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
            var dummy = JsonConvert.SerializeObject(new {  });
            var stopwatch = new Stopwatch();
            var task = Task.Run(() => RunTest(new CancellationToken(), stopwatch));
            task.Wait();
            Console.WriteLine(stopwatch.ElapsedMilliseconds);
            Console.ReadLine();
        }

        static async void RunTest(CancellationToken token, Stopwatch stopwatch)
        {
            stopwatch.Start();
            var helper = new PowershellHelper();
            helper.AddInputObject("testObject", new TestClass { TestProperty = "myValue" });
            helper.AddCommand("notepad.exe");
            helper.AddCommand("notepad.exe");
            helper.AddCommand("notepad.exe");
            helper.AddOutputObject("testObject");

            
            var exitCode = await helper.Run(token);
            var returnedClass = helper.Output["testObject"].ToObject<TestClass>();
            stopwatch.Stop();
        }
    }
}