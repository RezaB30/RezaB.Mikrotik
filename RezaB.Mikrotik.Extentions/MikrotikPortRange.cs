using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Mikrotik.Extentions
{
    /// <summary>
    /// Represents a port range.
    /// </summary>
    public class MikrotikPortRange
    {
        /// <summary>
        /// Port range low boundry.
        /// </summary>
        public ushort LowBoundry { get; set; }
        /// <summary>
        /// Port range high boundry.
        /// </summary>
        public ushort HighBoundry { get; set; }
        /// <summary>
        /// Range in Mikrotik string format (#####-#####).
        /// </summary>
        public string RangeString
        {
            get
            {
                if (HighBoundry == 0)
                    return null;
                return LowBoundry + "-" + HighBoundry;
            }
            set
            {
                var parts = value.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    HighBoundry = 0;
                ushort parsed;
                if (!ushort.TryParse(parts[0], out parsed))
                    HighBoundry = 0;
                LowBoundry = parsed;
                if (!ushort.TryParse(parts[1], out parsed))
                    HighBoundry = 0;
                HighBoundry = parsed;
            }
        }
    }
}
