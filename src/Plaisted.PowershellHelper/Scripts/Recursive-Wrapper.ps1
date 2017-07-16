##
param([string]$watcherScriptPath,[string]$userScriptPath,[string]$username="",[string]$password="")

Write-Output "Starting"

$psh_si = new-object System.Diagnostics.Process
$psh_si.StartInfo.Filename = "powershell.exe"
$psh_si.StartInfo.Arguments = "-noprofile -executionpolicy bypass -File $watcherScriptPath -ProcessToMonitor $pid"
$psh_si.StartInfo.UseShellExecute = $false
$psh_si.StartInfo.RedirectStandardOutput = $true
$global:psh_WatcherStarted = $false
$psh_action = { Write-Host $Event.SourceEventArgs.Data; if ($Event.SourceEventArgs.Data.Contains("Monitor Started:")) { $global:psh_WatcherStarted = $true; } }
Register-ObjectEvent -InputObject $psh_si -EventName OutputDataReceived -Action $psh_action -SourceIdentifier "stdOut" | Out-Null
$psh_si.start() | Out-Null
$psh_si.BeginOutputReadLine();
while ($global:psh_WatcherStarted -eq $false)
{
    Wait-Event -SourceIdentifier "stdOut" -Timeout 1 -ErrorAction SilentlyContinue
}
Unregister-Event -SourceIdentifier "stdOut"

function Only-If-Data
{
	Process {
		if ($_ -ne $null -and $_ -ne ""){
			return $_
		}
	}
}

if ($true){
	$psh_main = New-Object System.Diagnostics.Process
	$psh_main.StartInfo.Filename = "powershell.exe"
	$psh_main.StartInfo.Arguments = "-noprofile -executionpolicy bypass -File $userScriptPath"
	$psh_main.StartInfo.UseShellExecute = $false
	$psh_main.StartInfo.RedirectStandardOutput = $true
	$psh_main.StartInfo.RedirectStandardError = $true
	$psh_main.StartInfo.CreateNoWindow = $true

	if ($username -ne ""){
		$psh_main.StartInfo.UserName = $username
		$psh_main.StartInfo.PasswordInClearText = $password
	}
	Register-ObjectEvent -InputObject $psh_main -EventName OutputDataReceived -Action { $Event.SourceEventArgs.Data | Only-If-Data | % {[Console]::WriteLine($_);} } -SourceIdentifier "stdOut" | Out-Null
	Register-ObjectEvent -InputObject $psh_main -EventName ErrorDataReceived -Action { $Event.SourceEventArgs.Data | Only-If-Data | % {[Console]::Error.WriteLine($_); } } -SourceIdentifier "stdErr" | Out-Null
	$psh_main.Start() | Out-Null
	$psh_main.BeginOutputReadLine();
	$psh_main.BeginErrorReadLine();
	## causes some sort of deadlock... ->  $psh_main.WaitForExit();
	Wait-Process -Id $psh_main.Id
	exit $psh_main.ExitCode;
} else {
	&$userScriptPath
	exit 0;
}