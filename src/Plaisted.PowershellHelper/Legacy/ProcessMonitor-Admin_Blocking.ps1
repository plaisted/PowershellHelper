### Recursively close child processes from list of process parent-child realations.
function Kill-Children
{
    param([int]$pidToKill, [System.Collections.ArrayList]$processList)
	"Requested recursive kill of $pidToKill." | Write-Host
    $processList | Where {$_.ParentProcessId -eq $pidToKill } | % { Kill-Children -pidToKill $_.ProcessId -processList $processList }
    if ($pidToKill -eq $pid){
		#suicide is bad
		return;
	}
    $procToKill = $Null
    $procToKill = Get-Process -Id $pidToKill -ErrorAction SilentlyContinue
    if ($procToKill -ne $Null)
    {
		Stop-Process -Id $pidToKill -Force -ErrorAction SilentlyContinue
        "Killing $pidToKill." | Write-Host
    } else {
		"Proc $pidToKill does not exist." | Write-Host
	}
}
### Add process creation events to list
$global:psh_Processes = [System.Collections.ArrayList]@()
$psh_EventAction = 
{
	$props = @{ Name=$Event.SourceEventArgs.NewEvent.ProcessName; ProcessId=$Event.SourceEventArgs.NewEvent.ProcessId;ParentProcessId=$Event.SourceEventArgs.NewEvent.ParentProcessId; };
	$tempObj = New-Object PSObject -Property $props;
	$global:psh_Processes.Add($tempObj) | Out-Null;
	Remove-Event $Event.EventIdentifier;
}


"Entering script." | Write-Host
### Setup process created event watcher
$query = "select * from win32_ProcessStartTrace"
Register-CimIndicationEvent -Query $query -SourceIdentifier procMon -Action $psh_EventAction | Out-Null

"Watcher setup." | Write-Host

##Run Main script
try
{
	notepad.exe
	notepad.exe
	notepad.exe
    Start-Sleep -Seconds 3
} finally {

	"Script finished, waiting for events to flush." | Write-Host
	Start-Sleep -m 2000
	Get-EventSubscriber -SourceIdentifier procMon | Unregister-Event
	$global:psh_Processes | Write-Host
	Kill-Children -pidToKill $pid -processList $global:psh_Processes
	"Finished." | Write-Host
}
