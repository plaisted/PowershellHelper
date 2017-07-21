using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plaisted.PowershellHelper
{
    public class HelperOptions
    {
        /// <summary>
        /// Sets the domain user to run the powershell script under.
        /// </summary>
        public RunCredentials Credentials { get; set; }
        /// <summary>
        /// Sets the <see cref="path"/> that the powershell script is executed in.
        /// </summary>
        public string WorkingPath { get; set; }
        /// <summary>
        /// Determines if all processes spawned by the Powershell script should be terminated once the script has exited.
        /// </summary>
        public CleanupType CleanupMethod { get; set; } = CleanupType.RecursiveAdmin;
        /// <summary>
        /// Path for temporary files to be written to. This should be set if scripts are being run under a different
        /// user than the main process as the newly spawned process under new user will not have access to previous
        /// users temporary folder.
        /// </summary>
        public string SharedTempPath { get; set; }
    }
}
