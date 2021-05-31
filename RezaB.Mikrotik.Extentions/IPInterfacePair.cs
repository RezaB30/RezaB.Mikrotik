using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Mikrotik.Extentions
{
    public class IPInterfacePair
    {
        /// <summary>
        /// DSL line IP.
        /// </summary>
        public string IP { get; set; }
        /// <summary>
        /// DSL line interface name.
        /// </summary>
        public string InterfaceName { get; set; }

        public override string ToString()
        {
            return $"{IP}->{InterfaceName}";
        }
    }
}
