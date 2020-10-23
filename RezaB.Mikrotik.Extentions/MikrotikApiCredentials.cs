using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Mikrotik.Extentions
{
    /// <summary>
    /// Credentials for router connection via api port.
    /// </summary>
    public class MikrotikApiCredentials
    {
        /// <summary>
        /// Router IP address.
        /// </summary>
        public string IP { get; private set; }
        /// <summary>
        /// Router api port.
        /// </summary>
        public int Port { get; private set; }
        /// <summary>
        /// Router api username.
        /// </summary>
        public string ApiUsername { get; private set; }
        /// <summary>
        /// Router api password.
        /// </summary>
        public string ApiPassword { get; private set; }
        /// <summary>
        /// Creates a credential for Router connection via api port.
        /// </summary>
        /// <param name="ip">Router IP address</param>
        /// <param name="port">Router api port</param>
        /// <param name="apiUsername">Router api username</param>
        /// <param name="apiPassword">Router api password</param>
        public MikrotikApiCredentials(string ip, int port, string apiUsername, string apiPassword)
        {
            IP = ip;
            Port = port;
            ApiUsername = apiUsername;
            ApiPassword = apiPassword;
        }
    }
}
