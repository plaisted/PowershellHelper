using Plaisted.PowershellHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Plaisted.PowershellHelper.FunctionalTests
{
    public class PowershellHelperTests
    {
        [Fact]
        public void It_Aborts_On_Cancellation_Request()
        {
            var helper = new PowershellHelper().WithProcessCleanup(CleanupType.RecursiveAdmin);
            helper.AddCommand("Start-Sleep -s 60");
            var source = new CancellationTokenSource();
            using (var timeout = new Timeout(5000))
            {
                var task = Task.Run(() => helper.RunAsync(source.Token));
                source.Cancel();
                task.Wait();
                Assert.True(task.Result == PowershellStatus.Cancelled);
            }
        }

        [Fact]
        public void It_Aborts_On_Timeout()
        {
            var helper = new PowershellHelper().WithProcessCleanup(CleanupType.RecursiveAdmin);
            helper.AddCommand("Start-Sleep -s 60");
            var source = new CancellationTokenSource();
            using (var timeout = new Timeout(5000))
            {
                var task = Task.Run(() => helper.RunAsync(source.Token, 10));
                task.Wait();
                Assert.True(task.Result == PowershellStatus.TimedOut);
            }
        }

        [Fact]
        public void It_Cleans_Up_Children_Admin()
        {
            var helper = new PowershellHelper().WithProcessCleanup(CleanupType.RecursiveAdmin);
            helper.AddCommand("notepad.exe");
            var source = new CancellationTokenSource();
            using (var timeout = new Timeout(5000))
            {
                var task = Task.Run(() => helper.RunAsync(source.Token));
                task.Wait();
                Assert.True(task.Result == PowershellStatus.Exited);
            }
            helper.WaitOnCleanup();
            var p = Process.GetProcessesByName("notepad.exe");
            Assert.False(p.Any());
        }

        [Fact]
        public void It_Cleans_Up_GrandChildren_Admin()
        {
            var helper = new PowershellHelper().WithProcessCleanup(CleanupType.RecursiveAdmin);
            helper.AddCommand("cmd.exe \"/c start notepad.exe\"");
            var source = new CancellationTokenSource();
            using (var timeout = new Timeout(5000))
            {
                var task = Task.Run(() => helper.RunAsync(source.Token));
                task.Wait();
                Assert.True(task.Result == PowershellStatus.Exited);
            }
            helper.WaitOnCleanup();
            var p = Process.GetProcessesByName("notepad.exe");
            Assert.False(p.Any());
        }

        [Fact]
        public void It_Sets_Value_In_Script()
        {
            var helper = new PowershellHelper().WithProcessCleanup(CleanupType.RecursiveAdmin);
            helper.AddInputObject("testObject", new { TestProperty = "testValue" });
            helper.AddCommand("if ($testObject.TestProperty -ne 'testValue') { exit 1 }");
            var source = new CancellationTokenSource();
            using (var timeout = new Timeout(5000))
            {
                var task = Task.Run(() => helper.RunAsync(source.Token));
                task.Wait();
                Assert.True(task.Result == PowershellStatus.Exited);
            }
            Assert.True(helper.ExitCode == 0);
        }
        [Fact]
        public void It_Gets_Value_From_Script()
        {
            var helper = new PowershellHelper().WithProcessCleanup(CleanupType.RecursiveAdmin);
            helper.AddOutputObject("testObject");
            helper.AddCommand("$testObject = @{}");
            helper.AddCommand("$testObject.TestProperty = 'testValue'");
            var source = new CancellationTokenSource();
            using (var timeout = new Timeout(5000))
            {
                var task = Task.Run(() => helper.RunAsync(source.Token));
                task.Wait();
                Assert.True(task.Result == PowershellStatus.Exited);
            }
            dynamic output = helper.Output["testObject"];
            Assert.True(output.TestProperty == "testValue");
        }
    }
}
