using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Plaisted.PowershellHelper
{
    internal class TempScript : IDisposable
    {
        public string Path { get; set; }
        public TempScript(string path)
        {
            Path = path;
        }
        public void Dispose()
        {
            if (!string.IsNullOrEmpty(Path)) File.Delete(Path);
        }

        public static TempScript GetTempWatcherScript()
        {
            var assembly = typeof(PowershellScript).GetTypeInfo().Assembly;
            using (var stream = assembly.GetManifestResourceStream("Plaisted.PowershellHelper.Scripts.Recursive-ProcessMonitor.ps1"))
            {
                using (var reader = new StreamReader(stream))
                {
                    var contents = reader.ReadToEnd();
                    var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".ps1");
                    File.WriteAllText(tempFile, contents);
                    return new TempScript(tempFile);
                }
            }
        }

        public static TempScript GetTempWrapperScript()
        {
            var assembly = typeof(PowershellScript).GetTypeInfo().Assembly;
            using (var stream = assembly.GetManifestResourceStream("Plaisted.PowershellHelper.Scripts.Recursive-Wrapper.ps1"))
            {
                using (var reader = new StreamReader(stream))
                {
                    var contents = reader.ReadToEnd();
                    var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".ps1");
                    File.WriteAllText(tempFile, contents);
                    return new TempScript(tempFile);
                }
            }
        }

        public static TempScript GetNonAdminCleaupScript()
        {
            var assembly = typeof(PowershellScript).GetTypeInfo().Assembly;
            using (var stream = assembly.GetManifestResourceStream("Plaisted.PowershellHelper.Scripts.ProcessCleanup-NoAdmin.ps1"))
            {
                using (var reader = new StreamReader(stream))
                {
                    var contents = reader.ReadToEnd();
                    var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".ps1");
                    File.WriteAllText(tempFile, contents);
                    return new TempScript(tempFile);
                }
            }
        }
    }
}
