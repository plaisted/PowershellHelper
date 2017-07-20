param([int]$processToCleanup)

function Kill-Children
{
    param([int]$pidToKill, [bool]$includeParent)
	Get-CimInstance Win32_Process -Filter ParentProcessId=$pidToKill | % { Kill-Children -pidToKill $_.ProcessId -includeParent $true}

	if ($pidToKill -eq $pid){
		#suicide is bad
		return;
	}
	
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

Kill-Children -pidToKill $processToCleanup -includeParent $false