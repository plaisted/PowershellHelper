# PowershellHelper
PowershellHelper is a library for running PowerShell scripts from C# in their own process and cleaning abandoned processes after they have exited.

Example script run from c# using the libarry:
```csharp
var helper = new PowershellHelper(loggerFactory).WithProcessCleanup(CleanupType.Recursive);
helper.AddCommand("cmd.exe \"/c start notepad.exe\"");
helper.AddCommand("notepad.exe");
var exitCode = await helper.Run(cancellationToken);
```
The script will start cmd.exe, and 2x notepad.exe and then exit. Running this script in a typical manner (powershell.exe, powershell runspace) would leave these processes opened indefinitely.

Examining the trace logs we can see all spawned processes were terminated after the main powershell process exited:
```powershell
20170716 11:26:03.598: Watcher script started for process 30128.
20170716 11:26:04.486: Process exited, waiting for events to flush.
20170716 11:26:06.499: Process creations recorded:
20170716 11:26:06.538: @{ProcessId=31764; Name=powershell.exe; ParentProcessId=30128} # <- main PS instance to run script
20170716 11:26:06.552: @{ProcessId=31216; Name=conhost.exe; ParentProcessId=31764} # <- spawned process (cmd.exe helper)
20170716 11:26:06.565: @{ProcessId=30656; Name=cmd.exe; ParentProcessId=31764} # <- spawned process
20170716 11:26:06.576: @{ProcessId=29228; Name=notepad.exe; ParentProcessId=31764} # <- spawned process
20170716 11:26:06.590: @{ProcessId=19216; Name=notepad.exe; ParentProcessId=30656} # <- spawned process by child process
20170716 11:26:06.611: Requested recursive kill of 30128.
20170716 11:26:06.632: Requested recursive kill of 31764.
20170716 11:26:06.651: Requested recursive kill of 31216.
20170716 11:26:06.670: Proc 31216 already exited.
20170716 11:26:06.681: Requested recursive kill of 30656.
20170716 11:26:06.692: Requested recursive kill of 19216.
20170716 11:26:06.728: Killing 19216.
20170716 11:26:06.749: Proc 30656 already exited.
20170716 11:26:06.762: Requested recursive kill of 29228.
20170716 11:26:06.797: Killing 29228.
20170716 11:26:06.819: Proc 31764 already exited.
20170716 11:26:06.838: Proc 30128 already exited.
20170716 11:26:06.851: Watcher script finished for process 30128.
```

# CleanupType
```
CeanupType.None
```
Does not perform any cleanup of processing after running the script. Probably shouldn't be using this library if all you use is this.

```
CeanupType.Recursive
```
Spawns a secondary powershell process after completion of first to recursively kill all processes spawned by the main task. This will be unable to kill 'grandchild' processes if the 'child' process no longer exists (broken family tree).
```
CeanupType.RecursiveAdmin
```
Spawns a secondary powershell process in order to capture process created events to recursively kill processes after main script has completed. This will catch abandoned processes with broken family tree. REQUIRES ADMIN.

