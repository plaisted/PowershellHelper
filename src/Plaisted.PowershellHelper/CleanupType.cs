using System;
using System.Collections.Generic;
using System.Text;

namespace Plaisted.PowershellHelper
{
    public enum CleanupType
    {
        /// <summary>
        /// Does not perform any cleanup of processing after running the script. Probably shouldn't be using this library
        /// if all you use is this.
        /// </summary>
        None=0,
        /// <summary>
        /// Spawns a secondary powershell process after completion of first to kill all processes with the main task as
        /// their parent.
        /// </summary>
        Children=1,
        /// <summary>
        /// Spawns a secondary powershell process in order to capture process created events to recursively kill processes
        /// after main script has completed.
        /// </summary>
        Recursive=2
    }
}
