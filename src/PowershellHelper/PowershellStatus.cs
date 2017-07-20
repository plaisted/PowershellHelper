using System;
using System.Collections.Generic;
using System.Text;

namespace Plaisted.PowershellHelper
{
    public enum PowershellStatus
    {
        /// <summary>
        /// Process has exited, check exit code for success.
        /// </summary>
        Exited=0,
        /// <summary>
        /// Process was cancelled.
        /// </summary>
        Cancelled=1,
        /// <summary>
        /// Timeout was reached and process was cancelled.
        /// </summary>
        TimedOut=2
    }
}
