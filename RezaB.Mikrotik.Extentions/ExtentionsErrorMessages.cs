using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Mikrotik.Extentions
{
    public partial class MikrotikRouter
    {
        private static class ExtentionsErrorMessages
        {
            public static string UsedIPListIsNull()
            {
                return "Cannot retrieve used IP list.";
            }

            public static string NoValidLocalIPsFound()
            {
                return "Cannot find any empty local IPs.";
            }

            public static string NoValidRealIPsFound()
            {
                return "Cannot find any empty real IPs.";
            }

            public static string CanNotSetFirewallNAT(string localIP, string realIP, string portRange)
            {
                return string.Format("Cannot set NAT rule for local IP: {0}, real IP: {1}, port range: {2}", localIP, realIP, portRange);
            }

            public static string IPCapacityFor(string IP)
            {
                return "IP capacity reached for: " + IP;
            }

            public static string LocalIPNotFound(string IP)
            {
                return "No such local IP: " + IP;
            }

            public static string RealIPNotValidForStaticIP(string IP)
            {
                return "Not valid for static IP. This IP is already assignd: " + IP;
            }
        }
    }
}
