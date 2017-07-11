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

namespace Plaisted.PowershellHelper
{
    public class PowershellHelper
    {
        private ILogger _logger = new OptionalLogger();
        private List<string> commands = new List<string>();
        private List<KeyValuePair<string, object>> inputObjects = new List<KeyValuePair<string, object>>();
        private List<string> outputObjects = new List<string>();
        private bool processCleanup = true;
        private string workingDirectory;
        /// <summary>
        /// Output from the Poweshell script. Objects to be returned set using <see cref="AddOutputObject(string)"/>. If no value set in script the result will be null in dictionary.
        /// </summary>
        public Dictionary<string, JObject> Output { get; private set; }

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
        /// <summary>
        /// Sets the <see cref="path"/> that the powershell script is executed in.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public PowershellHelper WithWorkingDirectory(string path)
        {
            workingDirectory = path;
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
        /// <summary>
        /// Determines if all processes spawned by the Powershell script should be terminated once the script has exited.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public PowershellHelper WithProcessCleanup(bool value)
        {
            processCleanup = value;
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
        public async Task<int> Run(CancellationToken cancellationToken)
        {
            return await Run(cancellationToken, -1);
        }
        /// <summary>
        /// Runs the powershell script.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="millisecondsTimeout">Timeout length to wait for task to finish.</param>
        /// <returns>Task of the dictionary containing JObjects of output objects.</returns>
        public async Task<int> Run(CancellationToken cancellationToken, int millisecondsTimeout)
        {
            using (var disposables = new DisposableContainer())
            {
                //main script
                var script = new PowershellScript();
                disposables.Add(script);

                //add inputs
                AddInputsToScript(inputObjects, script, disposables);

                //add commands
                script.AddCommands(commands);

                //add outputs
                var outputTempFiles = AddOutputsToScript(outputObjects, script);

                //run
                var process = new PowershellProcess(script.CreateTempFile()).WithLogging(_logger);
                if (!string.IsNullOrEmpty(workingDirectory))
                {
                    process.WithWorkingDirectory(workingDirectory);
                }
                int exitCode = await process.RunAsync(cancellationToken, millisecondsTimeout);


                //kill spawned processes
                if (processCleanup)
                {
                    var cleanupScript = new PowershellScript();
                    disposables.Add(cleanupScript);
                    cleanupScript.AddCommand($"Get-CimInstance Win32_Process -Filter ParentProcessId={process.ProcessId.ToString()} " + 
                        "| % { Stop-Process -id $_.ProcessId -Force }");
                    var pec = await new PowershellProcess(cleanupScript.CreateTempFile()).WithLogging(_logger).RunAsync(new CancellationToken());
                    if (pec != 0)
                    {
                        _logger.LogError("[{EventName}] Process cleanup script ended with {ExitCode}.", "CleanupError", pec);
                    };
                }

                //read output files
                Output = ReadOutputs(outputTempFiles, _logger);

                return exitCode;
            }
        }

        private static void AddInputsToScript(List<KeyValuePair<string, object>> inputObjects, IPowershellScript script, IDisposableContainer disposables)
        {
            foreach (var inputObject in inputObjects)
            {
                var jsonObject = new JsonObjectBridge(inputObject.Key);
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

        private static Dictionary<string, JObject> ReadOutputs(List<KeyValuePair<string, string>> outputTempFiles, ILogger logger)
        {
            using (var outputDisposables = new DisposableContainer())
            {
                var outputDict = new Dictionary<string, JObject>();
                foreach (var output in outputTempFiles)
                {
                    var jsonObject = new JsonObjectBridge(output.Key);
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
                    outputDict.Add(output.Key, jsonObject.Object == null ? null : (JObject) jsonObject.Object);
                }
                return outputDict;
            }
        }
    }
}
