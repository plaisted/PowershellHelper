using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Plaisted.PowershellHelper
{
    internal class PowershellProcess
    {
        private ProcessStartInfo si;
        private string scriptPath;
        private Process process;
        private PowershellVersion version = PowershellVersion.Default;
        private bool noProfile;
        private ILogger logger = new OptionalLogger();
        public int ProcessId { get; internal set; }
        public int ExitCode { get { return process.ExitCode; } }

        public PowershellProcess(string scriptPath)
        {
            this.scriptPath = scriptPath;
            si = new ProcessStartInfo();
            si.UseShellExecute = false;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            si.CreateNoWindow = true;
            si.FileName = "powershell.exe";

            si.WorkingDirectory = Path.GetDirectoryName(scriptPath);

            process = new Process();
            process.StartInfo = si;
            
            process.OutputDataReceived += HandleOutputDataReceived;
            process.ErrorDataReceived += HandleErrorDataReceived;
            
        }

        public PowershellProcess WithNoProfile(bool value)
        {
            noProfile = value;
            return this;
        }
        public PowershellProcess WithWorkingDirectory(string path)
        {
            si.WorkingDirectory = path;
            return this;
        }

        public PowershellProcess WithCredentials(RunCredentials credentials)
        {
            si.UserName = credentials.UserName;
            si.Password = credentials.Password;
            if (!string.IsNullOrEmpty(credentials.Domain))
            {
                si.Domain = credentials.Domain;
            }
            noProfile = credentials.NoProfile;
            si.LoadUserProfile = !noProfile;
            return this;
        }

        public PowershellProcess WithPowershellVersion(PowershellVersion psVersion)
        {
            version = psVersion;
            return this;
        }

        public PowershellProcess WithLogging(ILogger logger)
        {
            this.logger = logger;
            return this;
        }
        public PowershellProcess AddArgs(string args)
        {
            si.Arguments += " " + args;
            return this;
        }

        public PowershellProcess AddEnv(string name, string value)
        {
            si.Environment.Add(name, value);
            return this;
        }

        public PowershellProcess AddEnvs(IEnumerable<KeyValuePair<string,string>> envs)
        {
            foreach (var env in envs)
            {
                si.Environment.Add(env.Key, env.Value);
            }
            return this;
        }

        public async Task<PowershellStatus> RunAsync(CancellationToken cancellationToken)
        {
            return await RunAsync(cancellationToken, -1);
        }

        public async Task<PowershellStatus> RunAsync(CancellationToken cancellationToken, int millisecondsTimeout)
        {
            var completion = new TaskCompletionSource<int>();
            var timeout = new TaskCompletionSource<object>();
            using (cancellationToken.Register(() => timeout.TrySetCanceled()))
            {
                si.Arguments = "-executionpolicy bypass -file " + scriptPath;
                if (version != PowershellVersion.Default)
                {
                    si.Arguments = "-version " + ((int)version).ToString() + " " + si.Arguments;
                }
                if (noProfile == true)
                {
                    si.Arguments = "-noprofile " + si.Arguments;
                }
                process.EnableRaisingEvents = true;
                process.Exited += (s, e) => completion.SetResult(process.ExitCode);
                process.Start();
                ProcessId = process.Id;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var helperTask = (millisecondsTimeout == -1) ? timeout.Task :
                    Task.Delay(millisecondsTimeout, cancellationToken);

                var result = await Task.WhenAny(completion.Task, helperTask);

                if (result == completion.Task)
                {
                    //completed
                    await completion.Task;
                    logger.LogInformation("[{EventName}] {ExitCode}", "ProcessCompleted", process.ExitCode);
                    return PowershellStatus.Exited;
                }
                else if (cancellationToken.IsCancellationRequested)
                {
                    //cancellation requestion
                    logger.LogInformation("[{EventName}]", "ProcessCancelled");
                    process.Kill();
                    return PowershellStatus.Cancelled;
                }
                else
                {
                    //timeout
                    logger.LogInformation("[{EventName}] {TimeOut} elapsed.", "ProcessTimeoutExpired", millisecondsTimeout);
                    process.Kill();
                    return PowershellStatus.TimedOut;
                }
            }
        }

        public void AddOutputDataReceivedHandler(DataReceivedEventHandler handler)
        {
            process.OutputDataReceived += handler;
        }
        public void RemoveOutputDataReceivedHandler(DataReceivedEventHandler handler)
        {
            process.OutputDataReceived -= handler;
        }
        public void AddErrorDataReceivedHandler(DataReceivedEventHandler handler)
        {
            process.ErrorDataReceived += handler;
        }
        public void RemoveErrorDataReceivedHandler(DataReceivedEventHandler handler)
        {
            process.ErrorDataReceived -= handler;
        }

        private void HandleOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null) logger.LogInformation("[{EventName}] {Data}", "StdOut", e.Data);
        }
        private void HandleErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null) logger.LogError("[{EventName}] {Data}", "StdErr", e.Data);
        }
    }
}
