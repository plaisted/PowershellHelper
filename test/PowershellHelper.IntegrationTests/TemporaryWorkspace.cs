using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Plaisted.PowershellHelper.IntegrationTests
{
    public class TemporaryWorkspace : IDisposable
    {
        private string basePath;
        private string copyBasePath;
        public string TempPath { get; set; }
        public TemporaryWorkspace() : this(Directory.GetCurrentDirectory())
        {
        }
        public TemporaryWorkspace(string basePath)
        {
            this.basePath = basePath;
            TempPath = Path.Combine(basePath, Path.GetRandomFileName());
            Directory.CreateDirectory(TempPath);
        }
        public string AddToTempFolder(string sourcePath, bool sourceRelative, string relativeDestPath = "")
        {
            if (sourceRelative)
                sourcePath = Path.GetFullPath(Path.Combine(copyBasePath, sourcePath));
            if (string.IsNullOrEmpty(relativeDestPath))
                relativeDestPath = Path.GetFileName(sourcePath);
            if (Directory.Exists(sourcePath))
            {
                //directory copy
                return "";
            }
            else if (File.Exists(sourcePath))
            {
                string dest = Path.GetFullPath(Path.Combine(TempPath, relativeDestPath));
                File.Copy(sourcePath, dest);
                return dest;
            }
            else
            {
                throw new FileNotFoundException("Path not found to be copied.");
            }
        }

        public string AddFileWithContents(string fileName, string contents)
        {
            string file = Path.GetFullPath(Path.Combine(TempPath, fileName));
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, contents);
            return file;
        }
        public void Dispose()
        {
            Directory.Delete(TempPath, true);
        }
    }
}
