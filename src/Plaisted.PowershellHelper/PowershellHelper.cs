using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Plaisted.PowershellHelper
{
    public class PowershellHelper
    {
        private ILogger _logger;
        private Process process;
        private List<string> commands;
        private List<KeyValuePair<string, object>> inputObjects;
        private List<string> outputObjects;
        private PowershellScript script;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public PowershellHelper(ILoggerFactory logger)
        {
            _logger = logger.CreateLogger("Plaisted.PowershellHelper");
            process = new Process();
            process.OutputDataReceived += HandleOutputDataReceived;
            process.ErrorDataReceived += HandleErrorDataReceived;
        }

        public void AddCommands(IEnumerable<string> commands)
        {
            this.commands.AddRange(commands);
        }
        public void AddCommand(string command)
        {
            commands.Add(command);
        }

        public void AddInputObjects(IEnumerable<KeyValuePair<string, object>> inputObjects)
        {
            this.inputObjects.AddRange(inputObjects);
        }

        public void AddInputObject(KeyValuePair<string, object> inputObject)
        {
            inputObjects.Add(inputObject);
        }

        public void AddOutputObject(string outputObject)
        {
            outputObjects.Add(outputObject);
        }
        public void AddOutputObject(IEnumerable<string> outputObjects)
        {
            this.outputObjects.AddRange(outputObjects);
        }

        public async Task<Dictionary<string, object>> Run(CancellationToken cancellationToken)
        {
            return await Run(0, cancellationToken);
        }


        public async Task<Dictionary<string, object>> Run(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            using (var disposables = new DisposableContainer())
            {
                script = new PowershellScript();
                disposables.Add(script);

                //add inputs
                AddInputsToScript(inputObjects, script, disposables);

                //add commands
                script.AddCommands(commands);

                //add outputs
                var outputTempFiles = AddOutputsToScript(outputObjects, script);

                //run
                var completion = new TaskCompletionSource<int>();
                var scriptPath = script.CreateTempFile();
                process.StartInfo = GetDefaultStartInfo(scriptPath);
                process.EnableRaisingEvents = true;
                process.Exited += (s, e) => completion.SetResult(process.ExitCode);
                process.Start();

                if (millisecondsTimeout != 0)
                {
                    WaitHandle.WaitAny(new WaitHandle[2] { ((IAsyncResult)completion.Task).AsyncWaitHandle, cancellationToken.WaitHandle }, millisecondsTimeout);
                }
                else
                {
                    WaitHandle.WaitAny(new WaitHandle[2] { ((IAsyncResult)completion.Task).AsyncWaitHandle, cancellationToken.WaitHandle });
                }


                //read output files
                var outputDict = ReadOutputs(outputTempFiles, _logger);

                return outputDict;
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
            _logger.LogInformation("[{EventName}] {Data}", "StdOut", e.Data);
        }
        private void HandleErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            _logger.LogError("[{EventName}] {Data}", "StdErr", e.Data);
        }

        private static ProcessStartInfo GetDefaultStartInfo(string scriptPath)
        {
            var si = new ProcessStartInfo();
            si.UseShellExecute = false;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            si.CreateNoWindow = true;
            si.FileName = "powershell.exe";
            si.Arguments = scriptPath;
            return si;
        }

        private static void AddInputsToScript(List<KeyValuePair<string, object>> inputObjects, IPowershellScript script, IDisposableContainer disposables)
        {
            foreach (var inputObject in inputObjects)
            {
                var jsonObject = new JsonObject(inputObject.Key);
                disposables.Add(jsonObject);
                jsonObject.Object = inputObject.Value;
                script.SetObject(jsonObject);
            }
        }
        private static List<KeyValuePair<string, string>> AddOutputsToScript(List<string> outputObjects, IPowershellScript script)
        {
            var outputTempFiles = new List<KeyValuePair<string, string>>();
            foreach (var output in outputObjects)
            {
                var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
                script.SetOutObject(output, tempFile);
                outputTempFiles.Add(new KeyValuePair<string, string>(output, tempFile));
            }
            return outputTempFiles;
        }

        private static Dictionary<string, object> ReadOutputs(List<KeyValuePair<string, string>> outputTempFiles, ILogger logger)
        {
            using (var outputDisposables = new DisposableContainer())
            {
                var outputDict = new Dictionary<string, object>();
                foreach (var output in outputTempFiles)
                {
                    var jsonObject = new JsonObject(output.Key);
                    outputDisposables.Add(jsonObject);
                    if (File.Exists(output.Value))
                    {
                        //read values
                        try
                        {
                            jsonObject.FromTempFile(output.Value);
                        }
                        catch (JsonException e)
                        {
                            logger.LogError("[{EventName}] {Exception} for object {Name}.", "DeserializationError", e.ToString(), output.Key);
                        }
                    }
                    else
                    {
                        logger.LogError("[{EventName}] Object {Name} set to null.", "OutputNotFound", output.Key);
                        jsonObject.Object = null;
                    }
                    outputDict.Add(output.Key, jsonObject.Object);
                }
                return outputDict;
            }
        }
    }
}
