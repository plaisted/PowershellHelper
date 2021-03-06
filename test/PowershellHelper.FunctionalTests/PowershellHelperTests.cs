﻿using Plaisted.PowershellHelper;
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
            using (var helper = new PowershellHelper().WithOptions(o => { o.CleanupMethod = CleanupType.RecursiveAdmin; }))
            {
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
        }

        [Fact]
        public void It_Aborts_On_Timeout()
        {
            using (var helper = new PowershellHelper().WithOptions(o => { o.CleanupMethod = CleanupType.RecursiveAdmin; }))
            {
                helper.AddCommand("Start-Sleep -s 60");
                var source = new CancellationTokenSource();
                using (var timeout = new Timeout(5000))
                {
                    var task = Task.Run(() => helper.RunAsync(source.Token, 10));
                    task.Wait();
                    Assert.True(task.Result == PowershellStatus.TimedOut);
                }
            }
                
        }

        [Fact]
        public void It_Cleans_Up_Children_Admin()
        {
            using (var helper = new PowershellHelper().WithOptions(o => { o.CleanupMethod = CleanupType.RecursiveAdmin; }))
            {
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
        }

        [Fact]
        public void It_Cleans_Up_GrandChildren_Admin()
        {
            using (var helper = new PowershellHelper().WithOptions(o => { o.CleanupMethod = CleanupType.RecursiveAdmin; }))
            {
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
        }

        [Fact]
        public void It_Sets_Value_In_Script()
        {
            using (var helper = new PowershellHelper().WithOptions(o =>
            {
                o.CleanupMethod = CleanupType.RecursiveAdmin; o.SharedTempPath = "c:\\temp";
            }))
            {
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

        }
        [Fact]
        public void It_Gets_Value_From_Script()
        {
            using (var helper = new PowershellHelper().WithOptions(o => { o.CleanupMethod = CleanupType.RecursiveAdmin; }))
            {
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

        [Fact]
        public void It_Sets_Env()
        {
            using (var helper = new PowershellHelper().WithOptions(o => { o.CleanupMethod = CleanupType.RecursiveAdmin; }))
            {
                helper.AddEnv("myTest", "value");
                helper.AddCommand("if ($env:myTest -ne 'value') { exit 1 }");
                var source = new CancellationTokenSource();
                using (var timeout = new Timeout(5000))
                {
                    var task = Task.Run(() => helper.RunAsync(source.Token));
                    task.Wait();
                    Assert.True(task.Result == PowershellStatus.Exited);
                }
                Assert.True(helper.ExitCode == 0);
            }

        }

        [Fact]
        public void It_Sets_Envs()
        {
            using (var helper = new PowershellHelper().WithOptions(o => { o.CleanupMethod = CleanupType.RecursiveAdmin; }))
            {
                var envs = new List<KeyValuePair<string, string>>();
                envs.Add(new KeyValuePair<string, string>("env1", "value1"));
                envs.Add(new KeyValuePair<string, string>("env2", "value2"));
                helper.AddEnvs(envs);
                helper.AddCommand("if ($env:env1 -ne 'value1') { exit 1 }");
                helper.AddCommand("if ($env:env2 -ne 'value2') { exit 1 }");
                var source = new CancellationTokenSource();
                using (var timeout = new Timeout(5000))
                {
                    var task = Task.Run(() => helper.RunAsync(source.Token));
                    task.Wait();
                    Assert.True(task.Result == PowershellStatus.Exited);
                }
                Assert.True(helper.ExitCode == 0);
            }
        }
    }
}
