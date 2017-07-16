param([int]$processToMonitor,[string]$tempFile,[string]$logFile="")
$global:logFile = $logFile;
### Simple file logger for trace logs.
function Log-To-File
{
	Process {
		try{
			if ($global:logFile -ne ""){
				((Get-Date -format "yyyyMMdd HH:mm:ss.fff") + ": " + $_) | Out-File -Append -FilePath $global:logFile 
		}
		} catch {
		}
	}

}
### Recursively close child processes from list of process parent-child realations.
function Kill-Children
{
    param([int]$pidToKill, [System.Collections.ArrayList]$processList)
	"Requested recursive kill of $pidToKill." | Log-To-File
	if ($pidToKill -eq $pid){
		#suicide is bad
		return;
	}
    $processList | Where {$_.ParentProcessId -eq $pidToKill } | % { Kill-Children -pidToKill $_.ProcessId -processList $processList }
    $procToKill = $Null
    $procToKill = Get-Process -Id $pidToKill -ErrorAction SilentlyContinue
    if ($procToKill -ne $Null)
    {
		Stop-Process -Id $pidToKill -Force -ErrorAction SilentlyContinue
        "Killing $pidToKill." | Log-To-File
    } else {
		"Proc $pidToKill does not exist." | Log-To-File
	}
}
## Capture process creation events to list
$global:psh_Processes = [System.Collections.ArrayList]@()
$psh_EventAction = 
{
	$props = @{ Name=$Event.SourceEventArgs.NewEvent.ProcessName; ProcessId=$Event.SourceEventArgs.NewEvent.ProcessId;ParentProcessId=$Event.SourceEventArgs.NewEvent.ParentProcessId; };
	$tempObj = New-Object PSObject -Property $props;
	$global:psh_Processes.Add($tempObj) | Out-Null;
	Remove-Event $Event.EventIdentifier;
}


"Entering script ($processToMonitor)." | Log-To-File
## Set up watchers
$query = "select * from win32_ProcessStartTrace"
$endQuery = "select * from win32_ProcessStopTrace where ProcessId=$processToMonitor"
$global:psh_Processes = [System.Collections.ArrayList]@()
$global:stillRunning = $true
Register-CimIndicationEvent -Query $query -SourceIdentifier procMon -Action $psh_EventAction | Out-Null
Register-CimIndicationEvent -Query $endQuery -SourceIdentifier endQuery -Action { $global:stillRunning = $false }  | Out-Null
"Watchers setup." | Log-To-File

##send msg back to host to run script
Write-Host "Monitor Started: $processToMonitor"

while ($global:stillRunning)
{
    Wait-Process -Id $processToMonitor -Timeout 1 -ErrorAction SilentlyContinue
	"Waiting for process to exit." | Log-To-File
}

"Process exited, waiting for events to flush." | Log-To-File
Start-Sleep -m 2000
Get-EventSubscriber -SourceIdentifier procMon | Unregister-Event
Get-EventSubscriber -SourceIdentifier endQuery | Unregister-Event

$global:psh_Processes | Log-To-File
Kill-Children -pidToKill $processToMonitor -processList $global:psh_Processes
"Finished." | Log-To-File

trap{
	$_ | Log-To-File
}