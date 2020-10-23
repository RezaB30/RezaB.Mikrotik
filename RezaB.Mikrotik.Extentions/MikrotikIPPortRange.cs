using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Mikrotik.Extentions
{
    /// <summary>
    /// Represents an IP with a port range.
    /// </summary>
    class MikrotikIPPortRange
    {
        /// <summary>
        /// IP.
        /// </summary>
        public uint IP { get; set; }
        /// <summary>
        /// Port Range.
        /// </summary>
        public MikrotikPortRange PortRange { get; set; }
    }
}
