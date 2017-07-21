using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Plaisted.PowershellHelper
{
    internal class JsonObjectBridge : IDisposable, IJsonObjectBridge
    {
        private object _object;
        private Regex standardVarNames = new Regex("^[a-zA-Z0-9_]*$", RegexOptions.Compiled);
        private string json { get; set; }
        public JsonObjectBridge(string name)
        {
            Name = name;
            TemporaryFile = GetTempFileName(Path.GetTempPath());
        }
        public JsonObjectBridge(string name, string tempPath)
        {
            Name = name;
            TemporaryFile = GetTempFileName(tempPath);
        }
        public string TemporaryFile { get; private set; }
        /// <summary>
        /// C# object to be serialized and sent to Powershell scripting environment.
        /// </summary>
        public object Object {
            get {
                return _object;
            }
            set
            {
                _object = value;
                if (value != null)
                {
                    json = JsonConvert.SerializeObject(value);
                }
            }
        }
        /// <summary>
        /// Represents the object name of <see cref="Object"/> for use in the Powershell script.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Escaped name for use in powershell script.
        /// </summary>
        public string EscapedName
        {
            get
            {
                var tempName = Name;
                if (tempName.Contains("`")) { tempName = tempName.Replace("`", "``"); }
                if (tempName.Contains("{")) { tempName = tempName.Replace("{", "`{"); }
                if (tempName.Contains("}")) { tempName = tempName.Replace("}", "`}"); }
                if (!standardVarNames.IsMatch(tempName))
                { tempName = "{" + tempName + "}"; }
                return tempName;
            }
        }
        /// <summary>
        /// Creates a temporary file with <see cref="json"/> as contents and returns path of file.
        /// </summary>
        /// <returns>Path of temporary file created.</returns>
        public string CreateTempFile()
        {
            File.WriteAllText(TemporaryFile, json);
            return TemporaryFile;
        }
        public void ReadFromTempFile()
        {
            json = File.ReadAllText(TemporaryFile);
            _object = JsonConvert.DeserializeObject(json);
        }
        public void Dispose()
        {
            File.Delete(TemporaryFile);
        }
        private string GetTempFileName(string tempPath)
        {
            return Path.Combine(tempPath, Guid.NewGuid().ToString() + ".json");
        }
    }
}
