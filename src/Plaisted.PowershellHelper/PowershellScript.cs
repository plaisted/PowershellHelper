using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Plaisted.PowershellHelper
{
    internal class PowershellScript : IDisposable, IPowershellScript
    {
        private List<string> declarations = new List<string>();
        private List<string> commands = new List<string>();
        private List<string> outputs = new List<string>();
        private string tempFile;
        private string triggerPath;


        /// <summary>
        /// Sets if the powershell script should stop on errors or continue
        /// </summary>
        public bool StopOnErrors { get; set; } = true;

        public PowershellScript WaitOnTriggerDeletion(string filePath)
        {
            triggerPath = filePath;
            return this;
        }
        public void AddCommands(IEnumerable<string> commands)
        {
            this.commands.AddRange(commands);
        }
        public void AddCommand(string command)
        {
            this.commands.Add(command);
        }
        public void SetOutObject(string objectName, string tempJsonFile)
        {
            outputs.Add($"if (${objectName} -ne $null) {{ ${objectName} | ConvertTo-Json | Out-File \"{tempJsonFile}\" }}");
        }
        public void SetObject(string objectName, string tempJsonFile)
        {
            declarations.Add($"${objectName} = [IO.File]::ReadAllText(\"{tempJsonFile}\") | ConvertFrom-Json");
        }
        public void SetObject(IJsonObjectBridge jsonObject)
        {
            SetObject(jsonObject.EscapedName, jsonObject.CreateTempFile());
        }
        public void SetOutObject(IJsonObjectBridge jsonObject)
        {
            SetOutObject(jsonObject.EscapedName, jsonObject.TemporaryFile);
        }
        public string CreateTempFile()
        {
            var contents = "";
            //add stop action if needed
            if (StopOnErrors) { contents += "$ErrorActionPreference = \"Stop\"" + Environment.NewLine; }
            //wait on trigger if requested
            if (!string.IsNullOrEmpty(triggerPath))
            {
                //contents += "Start-Sleep -s 15" + Environment.NewLine;
                contents += WaitOnFile(triggerPath);
            }
            //add all object declarations
            contents += string.Join(Environment.NewLine, declarations) + Environment.NewLine;
            //add commands with error handling where needed
            int count = 0;
            foreach (var command in commands)
            {
                contents += "##Command " + count.ToString() + Environment.NewLine;
                contents += command + Environment.NewLine;
                if (StopOnErrors) { contents += $"if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {{ throw \"Command {count.ToString()} did not complete succesfully.\" }}" + Environment.NewLine; }
                contents += Environment.NewLine;
                count++;
            }
            //add output vars
            contents += string.Join(Environment.NewLine, outputs);
            if (StopOnErrors) { contents += LegacyExitCodeHelper(); }
            tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".ps1");
            File.WriteAllText(tempFile, contents);
            return tempFile;
        }
        public void Dispose()
        {
            if (!string.IsNullOrEmpty(tempFile)) File.Delete(tempFile);
        }

        private static string LegacyExitCodeHelper()
        {
            return "trap { Write-Output $_;  exit 1; }";
        }

        public static string CreateTempMonitoringScript(int processId, string tempTriggerFile)
        {
            var tempMon = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".ps1");
            var contents = $@"
$pidToMonitor = {processId}
function Kill-Children
{{
    param([int]$pidToKill, [System.Collections.ArrayList]$processList)
    $processList | Where {{$_.ParentProcessId -eq $pidToKill }} | % {{ Kill-Children -pid $_.ProcessId -processList $processList}}
    $procToKill = $Null
    $procToKill = Get-Process -Id $pidToKill -ErrorAction SilentlyContinue
    if ($procToKill -ne $Null)
    {{
        Write-Host ""Killing $pidToKill $($procToKill.ProcessName)""
        Stop-Process -Id $pidToKill -Force -ErrorAction SilentlyContinue
    }}

    }}

$query = ""select * from win32_ProcessStartTrace""
$endQuery = ""select * from win32_ProcessStopTrace where ProcessId={processId}""
$processes = [System.Collections.ArrayList]@()
$global:stillRunning = $true

Register-CimIndicationEvent -Query $query -SourceIdentifier procMon
Register-CimIndicationEvent -Query $endQuery -SourceIdentifier endQuery -Action {{ $global:stillRunning = $false }} | Out-Null

Write-Host ""Starting monitoring..""

Remove-Item -Path {tempTriggerFile}

while ($true)
{{
    Write-Host ""Monitoring process..""
    Wait-Process -Id $pidToMonitor -Timeout 10 -ErrorAction SilentlyContinue
    Get-Event -SourceIdentifier procMon -ErrorAction SilentlyContinue | % {{$props = @{{Name=$_.SourceEventArgs.NewEvent.ProcessName; ProcessId=$_.SourceEventArgs.NewEvent.ProcessId; ParentProcessId=$_.SourceEventArgs.NewEvent.ParentProcessId;}}; $tempObj = New-Object PSObject -Property $props; $processes.Add($tempObj) | Out-Null; Remove-Event $_.EventIdentifier; }}
    if ((Get-Process -Id $pidToMonitor -ErrorAction SilentlyContinue) -eq $Null)
    {{
        Write-Host ""Process exited.""
        Break
    }}
}}
while ($global:stillRunning) {{}}
Start-Sleep -m 200
Get-Event -SourceIdentifier procMon -ErrorAction SilentlyContinue | % {{ $props = @{{Name=$_.SourceEventArgs.NewEvent.ProcessName; ProcessId=$_.SourceEventArgs.NewEvent.ProcessId; ParentProcessId=$_.SourceEventArgs.NewEvent.ParentProcessId;}}; $tempObj = New-Object PSObject -Property $props; $processes.Add($tempObj) | Out-Null; Remove-Event $_.EventIdentifier; }}
Get-EventSubscriber -SourceIdentifier procMon | Unregister-Event
Get-EventSubscriber -SourceIdentifier endQuery | Unregister-Event
Write-Host $processes

Kill-Children -pidToKill $pidToMonitor -processList $processes";
            contents += Environment.NewLine + LegacyExitCodeHelper();
            File.WriteAllText(tempMon, contents);
            return tempMon;
        }

        private static string WaitOnFile(string filePath)
        {
            var temp = $@"
if ([System.IO.File]::Exists(""{filePath}"")) {{
    Write-Host ""Trigger file exists.""
    $global:wait = $true
    $timer = New-Object timers.timer
    $timer.Interval = 1000
    $cleanupAction = {{
        if (-Not [System.IO.File]::Exists(""{filePath}"")) {{ $global:wait = $false; Write-Host ""FS Timer event."" }}
    }}
    $timerEvent = Register-ObjectEvent -InputObject $timer -EventName elapsed -SourceIdentifier fileTimer -Action $cleanupAction
    $timer.start() 
    $watcher = New-Object System.IO.FileSystemWatcher
    $watcher.Path = ""{Path.GetDirectoryName(filePath)}""
    $watcher.Filter = ""{Path.GetFileName(filePath)}""
    $event = Register-ObjectEvent $watcher Deleted -Action {{ $global:wait = $false; Write-Host ""FS Delete event."" }}
    while ($global:wait) {{}}
    $timer.stop()
    $timer.dispose()
    Unregister-Event fileTimer
}}
            ";
            return temp;

        }
    }
}
