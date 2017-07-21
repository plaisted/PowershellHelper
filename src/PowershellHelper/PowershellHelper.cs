using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Plaisted.ProcessMonitor;
using System.Security;

namespace Plaisted.PowershellHelper
{
    public class PowershellHelper
    {
        private ILogger _logger = new OptionalLogger();
        private List<string> commands = new List<string>();
        private List<KeyValuePair<string, string>> procEnvs = new List<KeyValuePair<string, string>>();
        private List<KeyValuePair<string, object>> inputObjects = new List<KeyValuePair<string, object>>();
        private List<string> outputObjects = new List<string>();
        private ProcessMonitor.Monitor monitor;
        private HelperOptions options = new HelperOptions();

        /// <summary>
        /// Output from the Poweshell script. Objects to be returned set using <see cref="AddOutputObject(string)"/>. If no value set in script the result will be null in dictionary.
        /// </summary>
        public Dictionary<string, JObject> Output { get; private set; }
        public int ExitCode { get; private set; }

        /// <summary>
        /// PowershellHelper runs powershell scripts in their own process allowing proper cleanup of spawned processes.
        /// </summary>
        public PowershellHelper() { }

        /// <summary>
        /// PowershellHelper runs powershell scripts in their own process allowing proper cleanup of spawned processes.
        /// </summary>
        /// <param name="logger">ILoggerFactory to be used for creating an ILogger for PowershellHelper logging.</param>
        public PowershellHelper(ILoggerFactory logger)
        {
            _logger = logger.CreateLogger("Plaisted.PowershellHelper");
        }
        /// <summary>
        /// PowershellHelper runs powershell scripts in their own process allowing proper cleanup of spawned processes.
        /// </summary>
        /// <param name="logger">ILogger to be used for PowershellHelper logging.</param>
        public PowershellHelper(ILogger logger)
        {
            _logger = logger;
        }

        public PowershellHelper WithOptions(Action<HelperOptions> options)
        {
            options.DynamicInvoke(this.options);
            return this;
        }
        /// <summary>
        /// Adds commands to the Powershell script.
        /// </summary>
        /// <param name="commands">Commands to be executed sequentially in Powershell.</param>
        /// <returns></returns>
        public PowershellHelper AddCommands(IEnumerable<string> commands)
        {
            this.commands.AddRange(commands);
            return this;
        }
        /// <summary>
        /// Adds a single command to the Powershellscript.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public PowershellHelper AddCommand(string command)
        {
            commands.Add(command);
            return this;
        }

        public PowershellHelper AddEnv(string name, string value)
        {
            procEnvs.Add(new KeyValuePair<string, string>(name, value));
            return this;
        }

        public PowershellHelper AddEnvs(IEnumerable<KeyValuePair<string,string>> envs)
        {
            procEnvs.AddRange(envs);
            return this;
        }

        /// <summary>
        /// Adds objects to the Powershell environment prior to executing added commands.
        /// </summary>
        /// <param name="inputObjects">KVP of variable name and object.</param>
        /// <returns></returns>
        public PowershellHelper AddInputObjects(IEnumerable<KeyValuePair<string, object>> inputObjects)
        {
            this.inputObjects.AddRange(inputObjects);
            return this;
        }
        /// <summary>
        /// Adds object to the Powershell environment prior to executing added commands.
        /// </summary>
        /// <param name="inputObjects">KVP of variable name and object.</param>
        /// <returns></returns>
        public PowershellHelper AddInputObject(KeyValuePair<string, object> inputObject)
        {
            inputObjects.Add(inputObject);
            return this;
        }
        /// <summary>
        /// Adds object to the Powershell environment prior to executing added commands.
        /// </summary>
        /// <param name="name">Name to be used for the variable in powershell.</param>
        /// <param name="inputObject">Object to the set.</param>
        /// <returns></returns>
        public PowershellHelper AddInputObject(string name, object inputObject)
        {
            inputObjects.Add(new KeyValuePair<string,object>(name, inputObject));
            return this;
        }
        /// <summary>
        /// Sets by powershell variable name which objects will be returned to the C# environment after script has finished running.
        /// </summary>
        /// <param name="outputObject"></param>
        /// <returns></returns>
        public PowershellHelper AddOutputObject(string outputObject)
        {
            outputObjects.Add(outputObject);
            return this;
        }
        /// <summary>
        /// Sets by powershell variable name which objects will be returned to the C# environment after script has finished running.
        /// </summary>
        /// <param name="outputObject"></param>
        /// <returns></returns>
        public PowershellHelper AddOutputObjects(IEnumerable<string> outputObjects)
        {
            this.outputObjects.AddRange(outputObjects);
            return this;
        }
        /// <summary>
        /// Runs the powershell script.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Task of the dictionary containing JObjects of output objects.</returns>
        public async Task<PowershellStatus> RunAsync(CancellationToken cancellationToken)
        {
            return await RunAsync(cancellationToken, -1);
        }
        /// <summary>
        /// Runs the powershell script.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="millisecondsTimeout">Timeout length to wait for task to finish.</param>
        /// <returns>Task of completion status.</returns>
        public async Task<PowershellStatus> RunAsync(CancellationToken cancellationToken, int millisecondsTimeout)
        {
            using (var disposables = new DisposableContainer())
            {
                var tempPath = options.SharedTempPath;
                if (string.IsNullOrEmpty(tempPath))
                {
                    tempPath = Path.GetTempPath();
                }
                
                //main script
                var script = new PowershellScript();
                disposables.Add(script);

                //add inputs
                AddInputsToScript(inputObjects, tempPath, script, disposables);

                //add commands
                script.AddCommands(commands);

                //add outputs
                var outputTempFiles = AddOutputsToScript(outputObjects, script, tempPath);

                if (!string.IsNullOrEmpty(options.SharedTempPath))
                {
                    script.SetTempPath(options.SharedTempPath);
                }

                PowershellProcess process = new PowershellProcess(script.CreateTempFile()).WithLogging(_logger);
                process.AddEnvs(procEnvs);
                //set working directory if needed
                if (!string.IsNullOrEmpty(options.WorkingPath))
                {
                    process.WithWorkingDirectory(options.WorkingPath);
                } else
                {
                    process.WithWorkingDirectory(Environment.CurrentDirectory);
                }

                if (options.Credentials != null)
                {
                    process.WithCredentials(options.Credentials);
                }

                _logger.LogInformation("[{EventName}]", "StartMainScript");


                if (options.CleanupMethod == CleanupType.RecursiveAdmin)
                {
                    //use admin based cleanup if requested
                    monitor = new ProcessMonitor.Monitor(_logger).Start();
                    disposables.Add(monitor);
                }

                var runTask = process.RunAsync(cancellationToken, millisecondsTimeout);

                if (options.CleanupMethod == CleanupType.RecursiveAdmin)
                {
                    //use admin based cleanup if requested
                    monitor.WatchProcess(process.ProcessId);
                }

                var exitReason = await runTask;
                ExitCode = process.ExitCode;
                
                _logger.LogInformation("[{EventName}] {ExitCode}", "FinishedMainScript", ExitCode);

                //kill spawned processes
                //works for non-admin but doesn't get recursive processes
                if (options.CleanupMethod == CleanupType.Recursive)
                {
                    using (var cleanupScript = TempScript.GetNonAdminCleaupScript(options.SharedTempPath == null ?
                        Path.GetTempPath() : options.SharedTempPath))
                    {
                        _logger.LogInformation("[{EventName}] {PId}", "StartCleanupScript", process.ProcessId);
                        var pec = await new PowershellProcess(cleanupScript.Path).WithLogging(_logger).RunAsync(new CancellationToken());
                        if (pec != 0)
                        {
                            _logger.LogError("[{EventName}] Process cleanup script ended with {ExitCode}.", "CleanupError", pec);
                        }
                        else
                        {
                            _logger.LogInformation("[{EventName}]", "FinishedCleanupScript");
                        }
                    }

                }
                //read output files
                Output = ReadOutputs(outputTempFiles, _logger);

                return exitReason;
            }
        }
        /// <summary>
        /// Task for when cleanup completion has occurred.
        /// </summary>
        /// <returns></returns>
        public Task CleanupTask()
        {
            if (monitor == null)
            {
                return Task.CompletedTask;
            }
            return monitor.OnFinishTask();
        }
        /// <summary>
        /// Wait sync on cleanup to finish.
        /// </summary>
        public void WaitOnCleanup()
        {
            if (monitor == null)
            {
                return;
            }
            monitor.WaitOnFinish();
        }


        private static void AddInputsToScript(List<KeyValuePair<string, object>> inputObjects, string tempPath, IPowershellScript script, IDisposableContainer disposables)
        {
            foreach (var inputObject in inputObjects)
            {
                var jsonObject = new JsonObjectBridge(inputObject.Key, tempPath);
                disposables.Add(jsonObject);
                jsonObject.Object = inputObject.Value;
                script.SetObject(jsonObject);
            }
        }
        private static List<IJsonObjectBridge> AddOutputsToScript(List<string> outputObjects, IPowershellScript script, string tempPath)
        {
            var outputTempFiles = new List<IJsonObjectBridge>();
            foreach (var output in outputObjects)
            {
                var jsonObject = new JsonObjectBridge(output, tempPath);
                script.SetOutObject(jsonObject);
                outputTempFiles.Add(jsonObject);
            }
            return outputTempFiles;
        }

        private static Dictionary<string, JObject> ReadOutputs(List<IJsonObjectBridge> outputTempFiles, ILogger logger)
        {
            using (var outputDisposables = new DisposableContainer())
            {
                var outputDict = new Dictionary<string, JObject>();
                foreach (var output in outputTempFiles)
                {
                    if (File.Exists(output.TemporaryFile))
                    {
                        //read values
                        try
                        {
                            output.ReadFromTempFile();
                        }
                        catch (JsonException e)
                        {
                            logger.LogError("[{EventName}] {Exception} for object {Name}.", "DeserializationError", e.ToString(), output.Name);
                        }
                    }
                    else
                    {
                        //script will not write output if value null in script
                        logger.LogError("[{EventName}] Object {Name} set to null.", "OutputNotFound", output.Name);
                        output.Object = null;
                    }
                    outputDict.Add(output.Name, output.Object == null ? null : (JObject)output.Object);
                }
                return outputDict;
            }
        }
    }
}
