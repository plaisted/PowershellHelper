using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Plaisted.PowershellHelper
{
    internal class PowershellProcess
    {
        private ProcessStartInfo si;
        private string scriptPath;
        private Process process;
        private ILogger logger = new OptionalLogger();
        public int ProcessId { get { return process.Id; } }

        public PowershellProcess(string scriptPath)
        {
            this.scriptPath = scriptPath;
            si = new ProcessStartInfo();
            si.UseShellExecute = false;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            si.CreateNoWindow = true;
            si.FileName = "powershell.exe";
            si.Arguments = "-noprofile -executionpolicy bypass -file " + scriptPath;

            process = new Process();
            process.StartInfo = si;
            process.OutputDataReceived += HandleOutputDataReceived;
            process.ErrorDataReceived += HandleErrorDataReceived;
        }
        public PowershellProcess WithWorkingDirectory(string path)
        {
            si.WorkingDirectory = path;
            return this;
        }
        public PowershellProcess WithPowershellVersion(PowershellVersion psVersion)
        {
            if (psVersion != PowershellVersion.Default)
            {
                si.Arguments = "-version " + ((int)psVersion).ToString()+ " -noprofile -executionpolicy bypass -file " + scriptPath;
            }
            return this;
        }

        public PowershellProcess WithLogging(ILogger logger)
        {
            this.logger = logger;
            return this;
        }

        public async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            return await RunAsync(cancellationToken, -1);
        }

        public async Task<int> RunAsync(CancellationToken cancellationToken, int millisecondsTimeout)
        {

            var completion = new TaskCompletionSource<int>();
            var timeout = new TaskCompletionSource<object>();
            using (cancellationToken.Register(() => timeout.TrySetCanceled()))
            {

                process.EnableRaisingEvents = true;
                process.Exited += (s, e) => completion.SetResult(process.ExitCode);
                process.Start();
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
                }
                else if (cancellationToken.IsCancellationRequested)
                {
                    //cancellation requestion
                    logger.LogInformation("[{EventName}]", "ProcessCancelled");
                    process.Kill();
                }
                else
                {
                    //timeout
                    logger.LogInformation("[{EventName}] {TimeOut} elapsed.", "ProcessTimeoutExpired", millisecondsTimeout);
                    process.Kill();
                }
            }
            return process.ExitCode;
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
