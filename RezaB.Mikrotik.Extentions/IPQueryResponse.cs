using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Mikrotik.Extentions
{
    public partial class MikrotikRouter
    {
        class IPQueryResponse
        {
            public IEnumerable<uint> UsedLocalIPs { get; set; }

            public IEnumerable<IPPortGroup> UsedRealIPs { get; set; }

            public class IPPortGroup
            {
                public uint IP { get; set; }

                public int Count { get; set; }

                public IEnumerable<MikrotikPortRange> PortRanges { get; set; }
            }
        }
    }
}
