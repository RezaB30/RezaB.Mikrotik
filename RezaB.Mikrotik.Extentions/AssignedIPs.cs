using RezaB.Networking.IP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Mikrotik.Extentions
{
    class AssignedIPs
    {
        public IPPool LocalIPs { get; set; }

        public IPPool RealIPs { get; set; }
    }
}
