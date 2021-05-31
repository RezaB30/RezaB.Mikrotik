using RezaB.Networking.IP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Mikrotik.Extentions
{
    public class VerticalDSLParameterCollection
    {
        /// <summary>
        /// A list of local IPs.
        /// </summary>
        public IEnumerable<IPSubnet> LocalIPs { get; set; }
        /// <summary>
        /// A list of DSL lines.
        /// </summary>
        public IEnumerable<IPInterfacePair> DSLLines { get; set; }
    }
}
