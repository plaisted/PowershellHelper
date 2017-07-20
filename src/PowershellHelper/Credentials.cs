using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Plaisted.PowershellHelper
{
    internal class Credentials
    {
        public string UserName { get; set; }
        public string Domain { get; set; }
        public SecureString Password { get; set; }
    }
}
