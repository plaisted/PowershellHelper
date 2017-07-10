using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Plaisted.PowershellHelper
{
    internal class JsonObjectBridge : IDisposable, IJsonObjectBridge
    {
        private object _object;
        private string tempFile;
        private string json { get; set; }
        public JsonObjectBridge(string name)
        {
            Name = name;
        }
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
        /// Creates a temporary file with <see cref="json"/> as contents and returns path of file.
        /// </summary>
        /// <returns>Path of temporary file created.</returns>
        public string CreateTempFile()
        {
            tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, json);
            return tempFile;
        }
        public void FromTempFile(string tempFile)
        {
            this.tempFile = tempFile;
            json = File.ReadAllText(tempFile);
            _object = JsonConvert.DeserializeObject(json);
        }
        public void Dispose()
        {
            File.Delete(tempFile);
        }
    }
}
