using RezaB.Mikrotik;
using RezaB.Networking.IP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Mikrotik.Extentions
{
    public partial class MikrotikRouter : MikrotikConnector, IDisposable
    {
        private const string defaultLogPrefix = "radiusr_mikrotik_api";
        private const string defaultNetmapIPPoolName = "radiusr_CGNAT_pool";
        private const string defaultVerticalNATIPPoolName = "radiusr_VerticalNAT_pool";
        private const int defaultIPPoolRangeSplit = 25; // between 1-100 (Mikrotik does not accept more than 100)
        private const ushort PortRangeCapacity = 500;
        private StringBuilder _exceptionLog = new StringBuilder();
        /// <summary>
        /// Router api credentials.
        /// </summary>
        public MikrotikApiCredentials Credentials { get; private set; }
        /// <summary>
        /// Gets the error log.
        /// </summary>
        public string ExceptionLog
        {
            get
            {
                return _exceptionLog.ToString();
            }
        }
        /// <summary>
        /// Creates a router api based on given credentials.
        /// </summary>
        /// <param name="credentials">Router credentials.</param>
        /// <param name="timeout">Timeout for the connection.</param>   
        public MikrotikRouter(MikrotikApiCredentials credentials, int timeout = 1500)
        {
            Credentials = credentials;
            _timeout = timeout;
        }

        #region private methods
        /// <summary>
        /// Initializes the connection including login.
        /// </summary>
        private bool InitializeConnection()
        {
            if (!IsLoggedIn)
            {
                // login
                try
                {
                    Open(Credentials.IP, Credentials.Port);
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    return false;
                }
                var loginResponse = Login(Credentials.ApiUsername, Credentials.ApiPassword);
                if (loginResponse.ErrorCode != 0)
                {
                    LogError(loginResponse.ErrorException, loginResponse.ErrorMessage);
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// Gets the existing NAT IPs in the router.
        /// </summary>
        /// <returns>Set of local IPs and real IPs in the NAT table</returns>
        private IPQueryResponse GetUsedIPs()
        {
            // create query parameters
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("chain", "srcnat", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("action", "src-nat", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "tcp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "udp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("disabled", "true", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#!", null, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", "to-ports,src-address,to-addresses"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }

            return new IPQueryResponse()
            {
                UsedLocalIPs = response.DataRows.Select(row => row["src-address"]).Distinct().Where(row => !string.IsNullOrEmpty(row))
                .Select(ip => IPTools.GetUIntValue(ip)),
                UsedRealIPs = response.DataRows.Select(row => new { IP = row["to-addresses"], PortRange = row.ContainsKey("to-ports") ? new MikrotikPortRange() { RangeString = row["to-ports"] } : null })
                .GroupBy(row => row.IP).Select(row => new IPQueryResponse.IPPortGroup() { IP = IPTools.GetUIntValue(row.Key), Count = row.Count(), PortRanges = row.Select(r => r.PortRange).OrderByDescending(r => r.HighBoundry) })
            };
        }
        /// <summary>
        /// Gets the next available local IP based on used IPs.
        /// </summary>
        /// <param name="localIPPools">IP pools to select from.</param>
        /// <param name="usedLocalIPs">Local IPs that are currently in use.</param>
        /// <returns>Selected local IP.</returns>
        private uint? GetNextAvailableLocalIP(IEnumerable<IPPool> localIPPools, IEnumerable<uint> usedLocalIPs)
        {
            // sort local IP pools
            localIPPools = localIPPools.OrderBy(pool => pool.LowBoundry);
            // find the next empty local IP slot
            uint? emptyLocalIP = null;
            foreach (var pool in localIPPools)
            {
                var currentIpIteration = pool.LowBoundry;
                while (pool.HighBoundry >= currentIpIteration)
                {
                    if (!usedLocalIPs.Contains(currentIpIteration))
                    {
                        emptyLocalIP = currentIpIteration;
                        break;
                    }
                    currentIpIteration++;
                }
                if (emptyLocalIP.HasValue)
                {
                    break;
                }
            }

            return emptyLocalIP;
        }
        /// <summary>
        /// Gets the next real IP port range available.
        /// </summary>
        /// <param name="realIPPools">Real IP pools.</param>
        /// <param name="usedRealIPs">Used real IPs.</param>
        /// <param name="IPCapacity">IP capacity for each real IP.</param>
        /// <returns>Next available real IP port range.</returns>
        private MikrotikIPPortRange GetNextAvailableRealIPPortRange(IEnumerable<IPPool> realIPPools, IEnumerable<IPQueryResponse.IPPortGroup> usedRealIPs, int IPCapacity)
        {
            // sort real IP pools
            realIPPools = realIPPools.OrderBy(pool => pool.LowBoundry);
            // find the next valid real IP and port range
            uint? validRealIP = null;
            MikrotikPortRange validPortRange = null;
            foreach (var pool in realIPPools)
            {
                var currentIPIteration = pool.LowBoundry;
                while (pool.HighBoundry >= currentIPIteration)
                {
                    var currentUsedIP = usedRealIPs.FirstOrDefault(ip => ip.IP == currentIPIteration);
                    if (currentUsedIP == null || (currentUsedIP.Count / 2) < IPCapacity)
                    {
                        validRealIP = currentIPIteration;
                        // finding a valid port range
                        var currentPortHighBoundry = ushort.MaxValue;
                        if (currentUsedIP != null)
                        {
                            for (int i = 0; i < IPCapacity; i++)
                            {
                                if (!currentUsedIP.PortRanges.Any(range => range.HighBoundry >= currentPortHighBoundry && range.LowBoundry <= currentPortHighBoundry))
                                    break;
                                currentPortHighBoundry -= PortRangeCapacity;
                            }
                        }
                        validPortRange = new MikrotikPortRange()
                        {
                            HighBoundry = currentPortHighBoundry,
                            LowBoundry = (ushort)(currentPortHighBoundry - PortRangeCapacity + 1)
                        };
                        break;
                    }
                    currentIPIteration++;
                }
                if (validRealIP.HasValue)
                {
                    break;
                }
            }
            if (!validRealIP.HasValue)
            {
                return null;
            }

            return new MikrotikIPPortRange()
            {
                IP = validRealIP.Value,
                PortRange = validPortRange
            };
        }
        /// <summary>
        /// Gets NAT rule ids from the router.
        /// </summary>
        /// <param name="localIP">Local IP associated with the rules.</param>
        /// <param name="protocols">Which protocols to include.</param>
        /// <returns></returns>
        private IEnumerable<string> FindNATRulesIdsByLocalIP(string localIP, params string[] protocols)
        {
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("chain", "srcnat", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("action", "src-nat", MikrotikCommandParameter.ParameterType.Query));
            for (int i = 0; i < protocols.Length; i++)
            {
                parameterList.Add(new MikrotikCommandParameter("protocol", protocols[i], MikrotikCommandParameter.ParameterType.Query));
                if (i >= 1)
                {
                    parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
                }
            }
            parameterList.Add(new MikrotikCommandParameter("src-address", localIP, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }
            // create id list
            var toEditIdList = response.DataRows.Select(row => row[".id"]).ToArray();
            return toEditIdList;
        }

        private void LogError(Exception ex, string message = null)
        {
            if (!string.IsNullOrWhiteSpace(message))
                _exceptionLog.AppendLine(message);
            while (ex != null)
            {
                _exceptionLog.AppendLine(ex.Message);
                ex = ex.InnerException;
            }
        }

        private void LogError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                _exceptionLog.AppendLine(message);
        }

        private void InsertSingleNetmap(ref bool errorOccured, string sourceAddress, string destinationAddress, string toPorts)
        {
            var router = new MikrotikRouter(Credentials, _timeout);
            if (!router.InitializeConnection())
                return;

            MikrotikResponse response;
            // create command to add the NAT rule
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("chain", "srcnat"));
            parameterList.Add(new MikrotikCommandParameter("src-address", sourceAddress));
            parameterList.Add(new MikrotikCommandParameter("protocol", "tcp"));
            parameterList.Add(new MikrotikCommandParameter("action", "netmap"));
            parameterList.Add(new MikrotikCommandParameter("to-addresses", destinationAddress));
            parameterList.Add(new MikrotikCommandParameter("to-ports", toPorts));
            parameterList.Add(new MikrotikCommandParameter("log-prefix", defaultLogPrefix));
            // add disabled
            parameterList.Add(new MikrotikCommandParameter("disabled", "true"));
            // check for errors
            if (errorOccured)
                return;
            // execute for tcp
            try
            {
                response = router.ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    errorOccured = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                errorOccured = true;
                return;
            }
            // change to udp
            parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "protocol"));
            parameterList.Add(new MikrotikCommandParameter("protocol", "udp"));
            // check for errors
            if (errorOccured)
                return;
            // execute for udp
            try
            {
                response = router.ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    errorOccured = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                errorOccured = true;
                return;
            }
            //// change to icmp
            //parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "protocol"));
            //parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "dst-port"));
            //parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "to-ports"));
            //parameterList.Add(new MikrotikCommandParameter("protocol", "icmp"));
            //// check for errors
            //if (errorOccured)
            //    return;
            //// execute for icmp
            //try
            //{
            //    response = router.ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
            //    if (response.ErrorCode != 0)
            //    {
            //        LogError(response.ErrorException, response.ErrorMessage);
            //        errorOccured = true;
            //        return;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    LogError(ex);
            //    errorOccured = true;
            //    return;
            //}
            //// add source NAT rules (src-nat)------------------
            //// change to src-tcp
            //parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "chain"));
            //parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "protocol"));
            //parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "src-address"));
            //parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "to-addresses"));
            //parameterList.Add(new MikrotikCommandParameter("chain", "dstnat"));
            //parameterList.Add(new MikrotikCommandParameter("protocol", "tcp"));
            //parameterList.Add(new MikrotikCommandParameter("src-address", destinationAddress));
            //parameterList.Add(new MikrotikCommandParameter("to-addresses", sourceAddress));
            //parameterList.Add(new MikrotikCommandParameter("to-ports", toPorts));
            //parameterList.Add(new MikrotikCommandParameter("dst-port", toPorts));
            //// check for errors
            //if (errorOccured)
            //    return;
            //// execute for src-tcp
            //try
            //{
            //    response = router.ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
            //    if (response.ErrorCode != 0)
            //    {
            //        LogError(response.ErrorException, response.ErrorMessage);
            //        errorOccured = true;
            //        return;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    LogError(ex);
            //    errorOccured = true;
            //    return;
            //}
            //// change to src-udp
            //parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "protocol"));
            //parameterList.Add(new MikrotikCommandParameter("protocol", "udp"));
            //// check for errors
            //if (errorOccured)
            //    return;
            //// execute for src-udp
            //try
            //{
            //    response = router.ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
            //    if (response.ErrorCode != 0)
            //    {
            //        LogError(response.ErrorException, response.ErrorMessage);
            //        errorOccured = true;
            //        return;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    LogError(ex);
            //    errorOccured = true;
            //    return;
            //}
        }

        private bool CleanNetmaps(bool isLastChangeReverse = false)
        {
            // initialize connection
            if (!InitializeConnection())
                return false;

            // create query parameters
            var disabled = isLastChangeReverse ? "true" : "false";
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("action", "netmap", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("disabled", disabled, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("log-prefix", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "tcp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "udp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "icmp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
            // create id list to remove
            var toRemoveIdList = response.DataRows.Select(row => row[".id"]).ToArray();
            // create command parameters to remove NAT rule
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toRemoveIdList)));
            // send the command
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/remove", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            if (isLastChangeReverse)
            {
                return ReverseNetmapIPPoolChanges();
            }
            else
            {
                return ClearNetmapIPPools();
            }
        }

        private bool ConfirmNetmapIPpools(bool clearOldPools = true)
        {
            // initialize connection
            if (!InitializeConnection())
                return false;

            List<MikrotikCommandParameter> parameterList;
            MikrotikResponse response;
            if (clearOldPools)
            {
                // remove old ip pool
                // get current pool id
                parameterList = new List<MikrotikCommandParameter>();
                parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
                // create filters
                parameterList.Add(new MikrotikCommandParameter(".proplist", ".id,name"));
                // send the command
                try
                {
                    response = ExecuteCommand("/ip/pool/getall", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    return false;
                }
                // create id list to remove
                var toRemoveIdList = response.DataRows.Where(row => row["name"].StartsWith(defaultNetmapIPPoolName) && !row["name"].StartsWith(defaultNetmapIPPoolName + "_new_")).Select(row => row[".id"]).ToArray();
                // create command parameters to remove NAT rule
                parameterList = new List<MikrotikCommandParameter>();
                parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toRemoveIdList)));
                // send the command
                try
                {
                    response = ExecuteCommand("/ip/pool/remove", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    return false;
                }
            }

            // update new IP pool name
            // get current pool id
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id,name"));
            // send the command
            try
            {
                response = ExecuteCommand("/ip/pool/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
            // create id list to rename
            var toEditIdList = response.DataRows.Select(row => new { ID = row[".id"], Name = row["name"].Replace("_new_", "_") }).OrderBy(row => row.Name).ToArray();
            var index = 0;
            foreach (var item in toEditIdList)
            {
                // create command parameters to remove NAT rule
                parameterList = new List<MikrotikCommandParameter>();
                parameterList.Add(new MikrotikCommandParameter(".id", item.ID));
                parameterList.Add(new MikrotikCommandParameter("name", clearOldPools ? defaultNetmapIPPoolName + "_" + index.ToString("000") : item.Name));
                // send the command
                try
                {
                    response = ExecuteCommand("/ip/pool/set", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    return false;
                }
                index++;
            }

            return true;
        }

        private bool ReverseNetmapIPPoolChanges()
        {
            // initialize connection
            if (!InitializeConnection())
                return false;

            // remove old ip pool
            // get current pool id
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id,name"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/pool/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
            // create id list to remove
            var toRemoveIdList = response.DataRows.Where(row => row["name"].StartsWith(defaultNetmapIPPoolName + "_new_")).Select(row => row[".id"]).ToArray();
            // create command parameters to remove NAT rule
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toRemoveIdList)));
            // send the command
            try
            {
                response = ExecuteCommand("/ip/pool/remove", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            return true;
        }

        private bool ClearNetmapIPPools()
        {
            // initialize connection
            if (!InitializeConnection())
                return false;

            // remove old ip pool
            // get current pool id
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id,name"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/pool/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
            // create id list to remove
            var toRemoveIdList = response.DataRows.Where(row => !row["name"].StartsWith(defaultNetmapIPPoolName + "_new_")).Select(row => row[".id"]).ToArray();
            // create command parameters to remove NAT rule
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toRemoveIdList)));
            // send the command
            try
            {
                response = ExecuteCommand("/ip/pool/remove", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            return true;
        }

        private bool CreateNetmapIPPools(IEnumerable<SimpleNetmapRule> netmapRules)
        {
            // create ranges string for IP pools
            List<string> ranges = new List<string>();
            try
            {
                List<string> rangesList = new List<string>();
                foreach (var rule in netmapRules)
                {
                    //var localStart = IPTools.ParseIPSubnet(rule.SourceAddress);
                    rangesList.Add(IPTools.GetStringValue(rule.SourceAddress.MinBound + 2) + "-" + IPTools.GetStringValue(rule.SourceAddress.MinBound + rule.SourceAddress.Count - 2));
                }
                var currentBatch = new List<string>();
                var batchCount = (rangesList.Count / defaultIPPoolRangeSplit) + 1;
                for (int i = 0; i < batchCount - 1; i++)
                {
                    ranges.Add(string.Join(",", rangesList.ToArray(), i * defaultIPPoolRangeSplit, defaultIPPoolRangeSplit));
                }
                ranges.Add(string.Join(",", rangesList.ToArray(), (batchCount - 1) * defaultIPPoolRangeSplit, rangesList.Count - (batchCount - 1) * defaultIPPoolRangeSplit));
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                return false;
            }

            // initialize connection
            if (!InitializeConnection())
                return false;

            // check for next available pool no
            // get current pool names
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id,name"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/pool/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
            var queueNoAddition = 0;
            if (response.DataRows.Count > 0)
            {
                if (int.TryParse(response.DataRows.Select(row => row["name"]).Max().Split('_').LastOrDefault(), out queueNoAddition))
                    queueNoAddition++;
            }

            for (int i = ranges.Count - 1; i >= 0; i--)
            {
                // create query parameters
                parameterList = new List<MikrotikCommandParameter>();
                parameterList.Add(new MikrotikCommandParameter("name", defaultNetmapIPPoolName + "_new" + "_" + (i + queueNoAddition).ToString("000")));
                parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix));
                parameterList.Add(new MikrotikCommandParameter("ranges", ranges[i]));
                if (i != ranges.Count - 1)
                    parameterList.Add(new MikrotikCommandParameter("next-pool", defaultNetmapIPPoolName + "_new" + "_" + (i + queueNoAddition + 1).ToString("000")));
                // execute command
                try
                {
                    response = ExecuteCommand("/ip/pool/add", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    return false;
                }
            }

            return true;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Sets firewall NAT rule for TCP, UDP and ICMP protocols.
        /// </summary>
        /// <param name="localIP">Local IP to set.</param>
        /// <param name="realIP">Real IP to set.</param>
        /// <param name="portRange">Port range for real IP to set.</param>
        /// <param name="logPrefix">Log prefix to set.</param>
        /// <param name="includeICMP">If include ICMP in the set of rules.</param>
        /// <returns>If the operation was successful.</returns>
        public bool SetFirewallNATFor_TCP_UDP(string localIP, string realIP, string portRange, string logPrefix, bool includeICMP = false, bool isStaticIP = false)
        {
            // create command to add the NAT rule
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("chain", "srcnat"));
            parameterList.Add(new MikrotikCommandParameter("src-address", localIP));
            parameterList.Add(new MikrotikCommandParameter("protocol", "tcp"));
            parameterList.Add(new MikrotikCommandParameter("dst-port", "0-65535"));
            parameterList.Add(new MikrotikCommandParameter("action", "src-nat"));
            parameterList.Add(new MikrotikCommandParameter("log-prefix", logPrefix));
            parameterList.Add(new MikrotikCommandParameter("to-addresses", realIP));
            if (portRange != null)
            {
                parameterList.Add(new MikrotikCommandParameter("to-ports", portRange));
            }
            parameterList.Add(new MikrotikCommandParameter("place-before", "0"));
            // add disabled
            //parameterList.Add(new MikrotikCommandParameter("disabled", "true"));
            // execute for tcp
            try
            {
                var response = ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    if (response.ErrorMessage == "no such item")
                    {
                        parameterList.RemoveAll(parameter => parameter.Name == "place-before");
                        response = ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
                        if (response.ErrorCode != 0)
                        {
                            LogError(response.ErrorException, response.ErrorMessage);
                            return false;
                        }
                    }
                    else
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
            // change to udp
            parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "protocol"));
            parameterList.Add(new MikrotikCommandParameter("protocol", "udp"));
            // execute for udp
            try
            {
                var response = ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
            if (includeICMP)
            {
                // change to icmp
                parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "protocol"));
                parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "dst-port"));
                parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "to-ports"));
                parameterList.Add(new MikrotikCommandParameter("protocol", "icmp"));
                // execute for icmp
                try
                {
                    var response = ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    return false;
                }
            }

            if (isStaticIP)
            {
                // add incoming tcp
                parameterList.Clear();
                parameterList.Add(new MikrotikCommandParameter("chain", "dstnat"));
                parameterList.Add(new MikrotikCommandParameter("dst-address", realIP));
                parameterList.Add(new MikrotikCommandParameter("protocol", "tcp"));
                parameterList.Add(new MikrotikCommandParameter("dst-port", "0-65535"));
                parameterList.Add(new MikrotikCommandParameter("to-ports", "0-65535"));
                parameterList.Add(new MikrotikCommandParameter("action", "dst-nat"));
                parameterList.Add(new MikrotikCommandParameter("log-prefix", logPrefix));
                parameterList.Add(new MikrotikCommandParameter("to-addresses", localIP));
                parameterList.Add(new MikrotikCommandParameter("place-before", "0"));
                // execute for tcp
                try
                {
                    var response = ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    return false;
                }
                // change to udp
                parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "protocol"));
                parameterList.Add(new MikrotikCommandParameter("protocol", "udp"));
                // execute for udp
                try
                {
                    var response = ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    return false;
                }
            }

            return true;
        }
        /// <summary>
        /// Get list of online users by username from the router.
        /// </summary>
        /// <returns>List of usernames</returns>
        public List<string> GetOnlineUsers()
        {
            // initialize connection
            if (!InitializeConnection())
                return null;
            // get online usernames list
            var response = ExecuteCommand("/ppp/active/getall", new MikrotikCommandParameter(".proplist", "name"));
            //Close();
            if (response.ErrorCode != 0)
            {
                LogError(response.ErrorException, response.ErrorMessage);
                return null;
            }
            return response.DataRows.Select(row => row["name"]).ToList();
        }
        /// <summary>
        /// Disconnects a user from the router.
        /// </summary>
        /// <param name="username">Disconnecting users' usernames</param>
        /// <returns>If it was a success</returns>
        public bool DisconnectUser(params string[] usernames)
        {
            // initialize connection
            if (!InitializeConnection())
                return false;
            // get relevant ids for removal
            string relevantIds;
            try
            {
                // create query parameters
                var parameterList = new List<MikrotikCommandParameter>();
                parameterList.Add(new MikrotikCommandParameter(".proplist", ".id"));
                // create filter parameters
                for (int i = 0; i < usernames.Length; i++)
                {
                    parameterList.Add(new MikrotikCommandParameter("name", usernames[i], MikrotikCommandParameter.ParameterType.Query));
                    if (i > 0)
                        parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));

                }
                // execute command
                var response = ExecuteCommand("/ppp/active/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
                relevantIds = string.Join(",", response.DataRows.Select(row => row[".id"]));
            }
            catch
            {
                return false;
            }
            try
            {
                var response = ExecuteCommand("/ppp/active/remove", new MikrotikCommandParameter(".id", relevantIds));
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Gets current users rate limits.
        /// </summary>
        /// <returns>Rate limit dictionary</returns>
        public Dictionary<string, string> GetCurrentRateLimits()
        {
            // initialize connection
            if (!InitializeConnection())
                return null;
            // get rate limit
            try
            {
                var response = ExecuteCommand("/queue/simple/getall", new MikrotikCommandParameter(".proplist", "target,max-limit"));
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return null;
                }

                var usernameRegex = /*new Regex(@"((\d|\w{1,})@(\d|\w{1,}))");*/ new Regex(@"^(?:<.*-(?<username>.*)-.*>)|(?:<.*-(?<username>.*)>)$");
                var rateRegexMega = new Regex("^(?<value>.*)(000000)$");
                var rateRegexKilo = new Regex("^(?<value>.*)(000)$");

                var results = response.DataRows.Where(row => usernameRegex.IsMatch(row["target"])).ToList();
                results.ForEach(row => row["target"] = /*usernameRegex.Match(row["target"]).Value*/ usernameRegex.Match(row["target"]).Groups["username"].Value);

                for (int i = 0; i < results.Count(); i++)
                {
                    var parts = results[i]["max-limit"].Split('/');

                    for (int j = 0; j < parts.Count(); j++)
                    {
                        var mega = rateRegexMega.Match(parts[j]).Groups["value"].Value;
                        if (!string.IsNullOrEmpty(mega))
                        {
                            parts[j] = mega + "M";
                        }
                        else
                        {
                            var kilo = rateRegexKilo.Match(parts[j]).Groups["value"].Value;
                            if (!string.IsNullOrEmpty(kilo))
                                parts[j] = kilo + "k";
                        }
                    }
                    results[i]["max-limit"] = string.Join("/", parts);
                }

                return results.GroupBy(row => row["target"]).Select(g => new { key = g.Key, val = string.Join(",", g.Select(inter => inter["max-limit"]).Distinct()) }).ToDictionary(item => item.key, item => item.val);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }
        }
        /// <summary>
        /// Sets a NAT rule for an static IP.
        /// </summary>
        /// <param name="localIPPools">NAS Local IP pool.</param>
        /// <param name="realIP">The real IP to set.</param>
        /// <param name="logPrefix">NAT rule log prefix.</param>
        /// <param name="IPCapacity">NAS IP Capacity.</param>
        /// <param name="includeICMP">If should add ICMP protocol rule</param>
        /// <returns>Set NAT rule info.</returns>
        public IPPortInfo SetStaticNATFor_TCP_UDP(IEnumerable<IPPool> localIPPools, string realIP, string logPrefix, int IPCapacity, bool includeICMP = false)
        {
            // initialize connection
            if (!InitializeConnection())
                return null;
            // get current rules
            var usedIPs = GetUsedIPs();
            if (usedIPs == null)
            {
                LogError(null, ExtentionsErrorMessages.UsedIPListIsNull());
                return null;
            }

            // check if this real IP is already assigned
            if (usedIPs.UsedRealIPs.Select(IPPort => IPPort.IP).Contains(IPTools.GetUIntValue(realIP)))
            {
                LogError(null, ExtentionsErrorMessages.RealIPNotValidForStaticIP(realIP));
                return null;
            }

            // find a valid local IP
            var nextValidLocalIP = GetNextAvailableLocalIP(localIPPools, usedIPs.UsedLocalIPs);
            if (nextValidLocalIP == null)
            {
                LogError(null, ExtentionsErrorMessages.NoValidLocalIPsFound());
                return null;
            }

            // setup results
            var result = new IPPortInfo()
            {
                LocalIP = IPTools.GetStringValue(nextValidLocalIP.Value),
                RealIP = realIP,
                PortRange = null
            };

            // set the NAT rule
            if (!SetFirewallNATFor_TCP_UDP(result.LocalIP, result.RealIP, result.PortRange, logPrefix, includeICMP, true))
            {
                LogError(null, ExtentionsErrorMessages.CanNotSetFirewallNAT(result.LocalIP, result.RealIP, result.PortRange));
                return null;
            }

            return result;
        }
        /// <summary>
        /// Adds a username to the NAT list for TCP,UDP and ICMP protocols.
        /// </summary>
        /// <param name="realIP">User real IP</param>
        /// <param name="logPrefix">User's username</param>
        /// <returns>Assigned rule properties</returns>
        [Obsolete("This is depricated, use SetAutoNATFor_TCP_UDP instead.")]
        public IPPortInfo SetFirewallNATFor_TCP_UDP(string realIP, string logPrefix)
        {
            // initialize connection
            if (!InitializeConnection())
                return null;
            // create query parameters
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("chain", "srcnat", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("action", "src-nat", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "tcp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "udp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "icmp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("disabled", "true", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#!", null, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", "to-ports,src-address,to-addresses"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }
            // create IP uints
            var IPBytes = response.DataRows.Select(row => row["src-address"]).Distinct().Where(row => !string.IsNullOrEmpty(row))
                .Select(ip => ip.Split('.').Select(part => byte.Parse(part)).Reverse().ToArray())
                .Select(bytes => BitConverter.ToUInt32(bytes, 0)).ToArray();
            // find next local IP in line
            string nextIP = "10.0.0.0";
            if (IPBytes.Length > 0)
            {
                nextIP = string.Join(".", BitConverter.GetBytes(IPBytes.Max() + 1).Reverse());
            }
            // find next port range
            var currentIPPorts = response.DataRows.Where(row => row.ContainsKey("to-ports")).GroupBy(row => row["to-addresses"]).FirstOrDefault(g => g.Key == realIP);
            var nextPortRange = (65535 - PortRangeCapacity) + "-65535";
            if (currentIPPorts != null)
            {
                var portRangeFloor = currentIPPorts.Select(row => row["to-ports"])
                    .Select(portRange => ushort.Parse(portRange.Split('-')[0]))
                    .Min();
                nextPortRange = (portRangeFloor - PortRangeCapacity) + "-" + (portRangeFloor - 1);
            }
            // create command to add the NAT rule
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("chain", "srcnat"));
            parameterList.Add(new MikrotikCommandParameter("src-address", nextIP));
            parameterList.Add(new MikrotikCommandParameter("protocol", "tcp"));
            parameterList.Add(new MikrotikCommandParameter("dst-port", "0-65535"));
            parameterList.Add(new MikrotikCommandParameter("action", "src-nat"));
            parameterList.Add(new MikrotikCommandParameter("log-prefix", logPrefix));
            parameterList.Add(new MikrotikCommandParameter("to-addresses", realIP));
            parameterList.Add(new MikrotikCommandParameter("to-ports", nextPortRange));
            parameterList.Add(new MikrotikCommandParameter("place-before", "0"));
            // add disabled
            //parameterList.Add(new MikrotikCommandParameter("disabled", "true"));
            // execute for tcp
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }
            // change to udp
            parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "protocol"));
            parameterList.Add(new MikrotikCommandParameter("protocol", "udp"));
            // execute for udp
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }
            // change to icmp
            parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "protocol"));
            parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "dst-port"));
            parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "to-ports"));
            parameterList.Add(new MikrotikCommandParameter("protocol", "icmp"));
            // execute for icmp
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }

            return new IPPortInfo()
            {
                LocalIP = nextIP,
                PortRange = nextPortRange
            };
        }
        /// <summary>
        /// Sets a NAT rule far a given real IP.
        /// </summary>
        /// <param name="localIPPools">NAS Local IP pool.</param>
        /// <param name="realIP">The real IP to set.</param>
        /// <param name="logPrefix">NAT rule log prefix.</param>
        /// <param name="IPCapacity">NAS IP Capacity.</param>
        /// <param name="includeICMP">If should add ICMP protocol rule</param>
        /// <returns>Set NAT rule info.</returns>
        public IPPortInfo SetFirewallNATFor_TCP_UDP(IEnumerable<IPPool> localIPPools, string realIP, string logPrefix, int IPCapacity, bool includeICMP = false)
        {
            // initialize connection
            if (!InitializeConnection())
                return null;
            // get current rules
            var usedIPs = GetUsedIPs();
            if (usedIPs == null)
            {
                LogError(null, ExtentionsErrorMessages.UsedIPListIsNull());
                return null;
            }

            // find next valid local IP
            var nextValidLocalIP = GetNextAvailableLocalIP(localIPPools, usedIPs.UsedLocalIPs);
            if (nextValidLocalIP == null)
            {
                LogError(null, ExtentionsErrorMessages.NoValidLocalIPsFound());
                return null;
            }

            // find valid port range for the real IP.
            var nextValidRealIPPortRange = GetNextAvailableRealIPPortRange(new IPPool[] { new IPPool() { LowBoundryIP = realIP, HighBoundryIP = realIP } }, usedIPs.UsedRealIPs, IPCapacity);
            if (nextValidRealIPPortRange == null)
            {
                LogError(null, ExtentionsErrorMessages.NoValidRealIPsFound());
                return null;
            }

            // setup results
            var result = new IPPortInfo()
            {
                LocalIP = IPTools.GetStringValue(nextValidLocalIP.Value),
                RealIP = IPTools.GetStringValue(nextValidRealIPPortRange.IP),
                PortRange = nextValidRealIPPortRange.PortRange.RangeString
            };

            // set the new rule
            if (!SetFirewallNATFor_TCP_UDP(result.LocalIP, result.RealIP, result.PortRange, logPrefix, includeICMP))
            {
                LogError(null, ExtentionsErrorMessages.CanNotSetFirewallNAT(result.LocalIP, result.RealIP, result.PortRange));
                return null;
            }

            return result;
        }
        /// <summary>
        /// Sets the NAT rules for a new client automatically.
        /// </summary>
        /// <param name="localIPPools">NAS local IP pools</param>
        /// <param name="realIPPools">NAS real IP pools</param>
        /// <param name="logPrefix">NAT rule log prefix</param>
        /// <param name="IPCapacity">NAS IP capacity</param>
        /// <param name="includeICMP">If should add ICMP protocol rule</param>
        /// <returns>Set NAT rule info.</returns>
        public IPPortInfo SetAutoNATFor_TCP_UDP(IEnumerable<IPPool> localIPPools, IEnumerable<IPPool> realIPPools, string logPrefix, int IPCapacity, bool includeICMP = false)
        {
            // initialize connection
            if (!InitializeConnection())
                return null;
            // getting list of set NAT rules
            var UsedIPsQuery = GetUsedIPs();
            if (UsedIPsQuery == null)
            {
                LogError(null, ExtentionsErrorMessages.UsedIPListIsNull());
                return null;
            }

            //-------------------------------Find valid slots--------------------------------------
            // find the next empty local IP slot
            var emptyLocalIP = GetNextAvailableLocalIP(localIPPools, UsedIPsQuery.UsedLocalIPs);
            if (emptyLocalIP == null)
            {
                LogError(null, ExtentionsErrorMessages.NoValidLocalIPsFound());
                return null;
            }

            // find the next empty real Ip and port range
            var realIPPortRange = GetNextAvailableRealIPPortRange(realIPPools, UsedIPsQuery.UsedRealIPs, IPCapacity);
            if (realIPPortRange == null)
            {
                LogError(null, ExtentionsErrorMessages.NoValidRealIPsFound());
                return null;
            }

            // Finalize valid values
            var result = new IPPortInfo()
            {
                LocalIP = IPTools.GetStringValue(emptyLocalIP.Value),
                RealIP = IPTools.GetStringValue(realIPPortRange.IP),
                PortRange = realIPPortRange.PortRange.RangeString
            };
            //-------------------------------------------------------------------------------------

            // set the new rule
            if (!SetFirewallNATFor_TCP_UDP(result.LocalIP, result.RealIP, result.PortRange, logPrefix, includeICMP))
            {
                LogError(null, ExtentionsErrorMessages.CanNotSetFirewallNAT(result.LocalIP, result.RealIP, result.PortRange));
                return null;
            }

            return result;
        }
        /// <summary>
        /// Removes a NAT rule from router.
        /// </summary>
        /// <param name="localIP">To be removed local IP</param>
        /// <returns>If it was a success</returns>
        [Obsolete("Use the other implementation with real IP indication for more stability")]
        public bool RemoveFirewallNATFor_TCP_UDP(string localIP)
        {
            // initialize connection
            if (!InitializeConnection())
                return false;
            // create query parameters
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("chain", "srcnat", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("action", "src-nat", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "tcp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "udp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "icmp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("src-address", localIP, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
            // create id list to remove
            var toRemoveIdList = response.DataRows.Select(row => row[".id"]).ToArray();
            // create command parameters to remove NAT rule
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toRemoveIdList)));
            // send the command
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/remove", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            return true;
        }
        /// <summary>
        /// Removes a NAT rule from router.
        /// </summary>
        /// <param name="localIP">NAT rule local IP</param>
        /// <param name="realIP">NAT rule real IP</param>
        /// <returns>If it was a success</returns>
        public bool RemoveFirewallNATFor_TCP_UDP(string localIP, string realIP)
        {
            // initialize connection
            if (!InitializeConnection())
                return false;
            // create query parameters
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("chain", "srcnat", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("chain", "dstnat", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("action", "src-nat", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("action", "dst-nat", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "tcp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "udp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "icmp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("src-address", localIP, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("dst-address", realIP, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("to-addresses", realIP, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("to-addresses", localIP, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
            // create id list to remove
            var toRemoveIdList = response.DataRows.Select(row => row[".id"]).ToArray();
            // create command parameters to remove NAT rule
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toRemoveIdList)));
            // send the command
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/remove", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            return true;
        }
        /// <summary>
        /// Updates a firewall NAT rule for a user.
        /// </summary>
        /// <param name="localIP">User's local IP</param>
        /// <param name="newRealIP">New user's real IP</param>
        /// <returns>Updated rule info</returns>
        public IPPortInfo UpdateFirewallNATFor_TCP_UDP(string localIP, string newRealIP, int IPCapacity)
        {
            // initialize connection
            if (!InitializeConnection())
                return null;

            // find next port range
            var usedIPPorts = GetUsedIPs();
            if (usedIPPorts == null)
            {
                LogError(null, ExtentionsErrorMessages.UsedIPListIsNull());
                return null;
            }
            var usedRealIPs = usedIPPorts != null ? usedIPPorts.UsedRealIPs : Enumerable.Empty<IPQueryResponse.IPPortGroup>();
            var currentIPPorts = GetNextAvailableRealIPPortRange(new IPPool[] { new IPPool() { LowBoundryIP = newRealIP, HighBoundryIP = newRealIP } }, usedRealIPs, IPCapacity);
            if (currentIPPorts == null)
            {
                LogError(null, ExtentionsErrorMessages.IPCapacityFor(newRealIP));
                return null;
            }

            // find and edit tcp & udp rules
            {
                // create id list to edit
                var toEditIdList = FindNATRulesIdsByLocalIP(localIP, "tcp", "udp");
                if (!toEditIdList.Any())
                {
                    LogError(null, ExtentionsErrorMessages.LocalIPNotFound(localIP));
                    return null;
                }
                // create command parameters to update NAT rule
                var parameterList = new List<MikrotikCommandParameter>();
                parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toEditIdList)));
                parameterList.Add(new MikrotikCommandParameter("to-addresses", newRealIP));
                parameterList.Add(new MikrotikCommandParameter("to-ports", currentIPPorts.PortRange.RangeString));
                // send the command
                try
                {
                    var response = ExecuteCommand("/ip/firewall/nat/set", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    return null;
                }
            }

            // find and edit icmp rules
            {
                // create id list to edit
                var toEditIdList = FindNATRulesIdsByLocalIP(localIP, "icmp");
                if (toEditIdList.Any())
                {
                    // create command parameters to remove NAT rule
                    var parameterList = new List<MikrotikCommandParameter>();
                    parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toEditIdList)));
                    parameterList.Add(new MikrotikCommandParameter("to-addresses", newRealIP));
                    // send the command
                    try
                    {
                        var response = ExecuteCommand("/ip/firewall/nat/set", parameterList.ToArray());
                        if (response.ErrorCode != 0)
                        {
                            LogError(response.ErrorException, response.ErrorMessage);
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(ex);
                        return null;
                    }
                }
            }

            return new IPPortInfo()
            {
                LocalIP = localIP,
                PortRange = currentIPPorts.PortRange.RangeString
            };
        }
        /// <summary>
        /// Clears all NAT rules from router.
        /// </summary>
        /// <returns>If it was a success</returns>
        public bool ClearFirewallNATFor_TCP_UDP()
        {
            // initialize connection
            if (!InitializeConnection())
                return false;

            // create query parameters
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("chain", "srcnat", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("action", "src-nat", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "tcp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "udp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "icmp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
            // create id list to remove
            var toRemoveIdList = response.DataRows.Select(row => row[".id"]).ToArray();
            // create command parameters to remove NAT rule
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toRemoveIdList)));
            // send the command
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/remove", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            return true;
        }
        /// <summary>
        /// Fetches the netmap table from router.
        /// </summary>
        /// <returns></returns>
        public List<NetMapRoutingTable> GetNetmapRoutes()
        {
            // initialize connection
            if (!InitializeConnection())
                return null;

            // create query parameters
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("action", "netmap", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "tcp", MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", "src-address,to-addresses,to-ports"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }
            // create results
            var results = new List<NetMapRoutingTable>();
            try
            {
                foreach (var item in response.DataRows)
                {
                    var currentLocalIPSubnet = IPTools.ParseIPSubnet(item["src-address"]);
                    var currentRealIPSubnet = IPTools.ParseIPSubnet(item["to-addresses"]);
                    var currentPortRange = item["to-ports"].Split('-');
                    results.Add(new NetMapRoutingTable()
                    {
                        LocalIPLowerBound = currentLocalIPSubnet.MinBound,
                        RealIPLowerBound = currentRealIPSubnet.MinBound,
                        Count = currentLocalIPSubnet.Count,
                        PortLowerBound = ushort.Parse(currentPortRange[0]),
                        PortUpperBound = ushort.Parse(currentPortRange[1])
                    });
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }

            return results.OrderBy(r => r.LocalIPLowerBound).ToList();
        }
        /// <summary>
        /// Creates NAT rules based on netmap translation.
        /// </summary>
        /// <param name="localIPSubnetStart">Local IP/ICDR to start from</param>
        /// <param name="RealIPSubnet">Real IP/ICDR to map to</param>
        /// <param name="externalPortCount">Number of ports in each map</param>
        /// <returns></returns>
        public bool InsertNetmapCluster(string localIPSubnetStart, string RealIPSubnet, int externalPortCount, bool preserveLastByte = false)
        {
            bool errorOccured = false;
            var NATRuleList = IPTools.CreateNetmapRulesFromCluster(localIPSubnetStart, RealIPSubnet, Convert.ToUInt16(externalPortCount), preserveLastByte);
            Parallel.ForEach(NATRuleList, (NATRule) =>
            {
                InsertSingleNetmap(ref errorOccured, IPTools.GetIPSubnetString(NATRule.SourceAddress), IPTools.GetIPSubnetString(NATRule.ToAddresses), NATRule.ToPorts);
            });

            if (!errorOccured)
            {
                errorOccured = !CreateNetmapIPPools(NATRuleList);
            }
            if (errorOccured)
            {
                CleanNetmaps(true);
            }

            return !errorOccured;
        }

        public bool ConfirmNetmapChanges(bool clearIPPools = false)
        {
            // initialize connection
            if (!InitializeConnection())
                return false;

            // create query parameters
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("action", "netmap", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("disabled", "true", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("log-prefix", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "tcp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "udp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "icmp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            // create id list to update
            var toEditIdList = response.DataRows.Select(row => row[".id"]).ToArray();
            // create command parameters to update NAT rule
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toEditIdList)));
            parameterList.Add(new MikrotikCommandParameter("disabled", "false"));
            // send the command
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/set", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            return ConfirmNetmapIPpools(clearIPPools);
        }
        /// <summary>
        /// Removes latest added netmap rules.
        /// </summary>
        /// <returns></returns>
        public bool ReverseChanges()
        {
            return CleanNetmaps(true);
        }
        /// <summary>
        /// Removes all active netmap rules.
        /// </summary>
        /// <returns></returns>
        public bool ClearActiveNetmaps()
        {
            return CleanNetmaps(false);
        }
        /// <summary>
        /// Inserts a single vertical NAT rule for a local IP, real IP and port range.
        /// </summary>
        /// <param name="errorOccured">Error state parameter</param>
        /// <param name="localIP">Local IP</param>
        /// <param name="realIP">Real IP</param>
        /// <param name="portRange">Port range</param>
        private void InsertVerticalNATRule(ref bool errorOccured, string localIP, string realIP, string portRange)
        {
            var router = new MikrotikRouter(Credentials, _timeout);
            if (!router.InitializeConnection())
                return;

            try
            {
                // create command to add the NAT rule
                var parameterList = new List<MikrotikCommandParameter>();
                parameterList.Add(new MikrotikCommandParameter("chain", "srcnat"));
                parameterList.Add(new MikrotikCommandParameter("src-address", localIP));
                parameterList.Add(new MikrotikCommandParameter("protocol", "tcp"));
                parameterList.Add(new MikrotikCommandParameter("dst-port", "0-65535"));
                parameterList.Add(new MikrotikCommandParameter("action", "src-nat"));
                parameterList.Add(new MikrotikCommandParameter("log-prefix", defaultLogPrefix));
                parameterList.Add(new MikrotikCommandParameter("to-addresses", realIP));
                parameterList.Add(new MikrotikCommandParameter("to-ports", portRange));
                // add disabled
                parameterList.Add(new MikrotikCommandParameter("disabled", "true"));
                // execute for tcp
                try
                {
                    var response = router.ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {

                        LogError(response.ErrorException, response.ErrorMessage);
                        errorOccured = true;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    errorOccured = true;
                    return;
                }
                // change to udp
                parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "protocol"));
                parameterList.Add(new MikrotikCommandParameter("protocol", "udp"));
                // execute for udp
                try
                {
                    var response = router.ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        errorOccured = true;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    errorOccured = true;
                    return;
                }
                // change to icmp
                parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "protocol"));
                parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "dst-port"));
                parameterList.Remove(parameterList.FirstOrDefault(par => par.Name == "to-ports"));
                parameterList.Add(new MikrotikCommandParameter("protocol", "icmp"));
                // execute for icmp
                try
                {
                    var response = router.ExecuteCommand("/ip/firewall/nat/add", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        errorOccured = true;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    errorOccured = true;
                    return;
                }
            }
            finally
            {
                router.Close();
            }
        }
        /// <summary>
        /// Inserts a set of vertical NAT rules based on an IP map.
        /// </summary>
        /// <param name="IPMap">The IP mapping.</param>
        /// <returns></returns>
        private AssignedIPs InsertSingleVerticalNATRuleSet(VerticalIPMapRule IPMap)
        {
            var errorOccured = false;
            var rules = IPTools.CreateVerticalNATRulesFromPools(IPMap);
            Parallel.ForEach(rules, NATRule =>
            {
                // insert vertical NAT rule
                InsertVerticalNATRule(ref errorOccured, NATRule.LocalIP, NATRule.RealIP, NATRule.PortRange);
            });

            if (errorOccured)
                return null;

            var localIPs = rules.Select(r => IPTools.GetUIntValue(r.LocalIP));
            var realIPs = rules.Select(r => r.RealIP).Distinct().Select(ip => IPTools.GetUIntValue(ip));
            var results = new AssignedIPs()
            {
                LocalIPs = new IPPool()
                {
                    LowBoundry = localIPs.Min(),
                    HighBoundry = localIPs.Max()
                },
                RealIPs = new IPPool()
                {
                    LowBoundry = realIPs.Min(),
                    HighBoundry = realIPs.Max()
                }
            };
            return results;
        }
        /// <summary>
        /// Inserts a cluster set of NAT rules.
        /// </summary>
        /// <param name="IPmaps">The IP map sets.</param>
        /// <returns></returns>
        public bool InsertVerticalNATRuleCluster(IEnumerable<VerticalIPMapRule> IPmaps)
        {
            var errorOccured = false;
            var assignedIPs = new ConcurrentBag<AssignedIPs>();
            Parallel.ForEach(IPmaps, map =>
            {
                var currentAssignedIPs = InsertSingleVerticalNATRuleSet(map);
                if (currentAssignedIPs == null)
                {
                    errorOccured = true;
                }
                else
                {
                    assignedIPs.Add(currentAssignedIPs);
                }
            });
            if (errorOccured)
            {
                CleanVerticalNATs(true);
                return false;
            }

            if (!InitializeConnection())
            {
                return false;
            }

            var ranges = new List<string>();
            foreach (var assignedIP in assignedIPs)
            {
                // exclude first two and last IP
                ranges.Add(IPTools.GetStringValue(assignedIP.LocalIPs.LowBoundry + 2) + "-" + IPTools.GetStringValue(assignedIP.LocalIPs.HighBoundry - 1));
            }

            // check for next available pool no
            // get current pool names
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id,name"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/pool/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    CleanVerticalNATs(true);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                CleanVerticalNATs(true);
                return false;
            }
            var queueNoAddition = 0;
            if (response.DataRows.Count > 0)
            {
                if (int.TryParse(response.DataRows.Select(row => row["name"]).Max().Split('_').LastOrDefault(), out queueNoAddition))
                    queueNoAddition++;
            }

            for (int i = ranges.Count - 1; i >= 0; i--)
            {
                // create query parameters
                parameterList = new List<MikrotikCommandParameter>();
                parameterList.Add(new MikrotikCommandParameter("name", defaultVerticalNATIPPoolName + "_new" + "_" + (i + queueNoAddition).ToString("000")));
                parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix));
                parameterList.Add(new MikrotikCommandParameter("ranges", ranges[i]));
                if (i != ranges.Count - 1)
                    parameterList.Add(new MikrotikCommandParameter("next-pool", defaultVerticalNATIPPoolName + "_new" + "_" + (i + queueNoAddition + 1).ToString("000")));
                // execute command
                try
                {
                    response = ExecuteCommand("/ip/pool/add", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        CleanVerticalNATs(true);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    CleanVerticalNATs(true);
                    return false;
                }
            }

            return true;
        }
        /// <summary>
        /// Inserts a collection of NAT rules and address lists for vertical DSL line interfaces.
        /// </summary>
        /// <param name="dslRoutingCollection">The parameter collection.</param>
        /// <returns></returns>
        public bool InsertVerticalDSLNATRuleset(VerticalDSLParameterCollection dslRoutingCollection)
        {
            var dslRoutingTable = new VerticalDSLRoutingTable()
            {
                LocalIPSubnets = dslRoutingCollection.LocalIPs,
                DSLIPs = dslRoutingCollection.DSLLines.Select(l => IPTools.GetUIntValue(l.IP)).ToArray()
            };

            var ruleset = IPTools.CreateVerticalNATRulesFromDSLRoutingTable(dslRoutingTable);
            var errorOccured = false;
            var LineInterfaceDictionary = dslRoutingCollection.DSLLines.ToDictionary(item => item.IP, item => item.InterfaceName);
            var addressList = new ConcurrentBag<IPInterfacePair>();
            Parallel.ForEach(ruleset, rule =>
            {
                InsertVerticalNATRule(ref errorOccured, rule.LocalIP, rule.RealIP, rule.PortRange);
                addressList.Add(new IPInterfacePair()
                {
                    IP = rule.LocalIP,
                    InterfaceName = LineInterfaceDictionary[rule.RealIP]
                });
            });
            if (errorOccured)
            {
                CleanVerticalNATs(true);
                return false;
            }

            if (!InitializeConnection())
            {
                return false;
            }

            var ranges = new List<string>();
            foreach (var assignedIP in dslRoutingTable.LocalIPSubnets)
            {
                // exclude first two and last IP
                ranges.Add(IPTools.GetStringValue(assignedIP.MinBound + 2) + "-" + IPTools.GetStringValue(assignedIP.MinBound + assignedIP.Count - 2));
            }

            // check for next available pool no
            // get current pool names
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id,name"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/pool/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    CleanVerticalNATs(true);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                CleanVerticalNATs(true);
                return false;
            }
            var queueNoAddition = 0;
            if (response.DataRows.Count > 0)
            {
                if (int.TryParse(response.DataRows.Select(row => row["name"]).Max().Split('_').LastOrDefault(), out queueNoAddition))
                    queueNoAddition++;
            }

            for (int i = ranges.Count - 1; i >= 0; i--)
            {
                // create query parameters
                parameterList = new List<MikrotikCommandParameter>();
                parameterList.Add(new MikrotikCommandParameter("name", defaultVerticalNATIPPoolName + "_new" + "_" + (i + queueNoAddition).ToString("000")));
                parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix));
                parameterList.Add(new MikrotikCommandParameter("ranges", ranges[i]));
                if (i != ranges.Count - 1)
                    parameterList.Add(new MikrotikCommandParameter("next-pool", defaultVerticalNATIPPoolName + "_new" + "_" + (i + queueNoAddition + 1).ToString("000")));
                // execute command
                try
                {
                    response = ExecuteCommand("/ip/pool/add", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        CleanVerticalNATs(true);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    CleanVerticalNATs(true);
                    return false;
                }
            }

            // set address lists
            if (addressList.Any())
            {
                Parallel.ForEach(addressList, currentAddress =>
                {
                    InsertAddressList(ref errorOccured, currentAddress);
                });
            }

            if (errorOccured)
            {
                CleanAddressList(true);
                return false;
            }

            return true;
        }

        private void InsertAddressList(ref bool errorOccured, IPInterfacePair pair)
        {
            var router = new MikrotikRouter(Credentials, _timeout);
            if (!router.InitializeConnection())
                return;

            // create query parameters
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix));
            parameterList.Add(new MikrotikCommandParameter("address", pair.IP));
            parameterList.Add(new MikrotikCommandParameter("list", pair.InterfaceName));
            // add disabled
            parameterList.Add(new MikrotikCommandParameter("disabled", "true"));
            // execute command
            try
            {
                var response = router.ExecuteCommand("/ip/firewall/address-list/add", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    errorOccured = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                errorOccured = true;
                return;
            }
        }
        /// <summary>
        /// Enables newly added vertical NAT rules and its IP pool.
        /// </summary>
        /// <returns></returns>
        public bool ConfirmVerticalNAT(bool clearIPPools = false)
        {
            // initialize connection
            if (!InitializeConnection())
                return false;

            // create query parameters
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("action", "src-nat", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("disabled", "true", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("log-prefix", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "tcp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "udp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "icmp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            // create id list to update
            var toEditIdList = response.DataRows.Select(row => row[".id"]).ToArray();
            // create command parameters to update NAT rule
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toEditIdList)));
            parameterList.Add(new MikrotikCommandParameter("disabled", "false"));
            // send the command
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/set", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            return ConfirmVerticalNATIPPools(clearIPPools);
        }
        /// <summary>
        /// Confirms added IP pool and removes the old one.
        /// </summary>
        /// <returns></returns>
        private bool ConfirmVerticalNATIPPools(bool clearOldPools = false)
        {
            // initialize connection
            if (!InitializeConnection())
                return false;

            List<MikrotikCommandParameter> parameterList;
            MikrotikResponse response;

            if (clearOldPools)
            {
                // remove old ip pool
                // get current pool id
                parameterList = new List<MikrotikCommandParameter>();
                parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
                // create filters
                parameterList.Add(new MikrotikCommandParameter(".proplist", ".id,name"));
                // send the command
                try
                {
                    response = ExecuteCommand("/ip/pool/getall", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    return false;
                }
                // create id list to remove
                var toRemoveIdList = response.DataRows.Where(row => row["name"].StartsWith(defaultNetmapIPPoolName) && !row["name"].StartsWith(defaultNetmapIPPoolName + "_new_")).Select(row => row[".id"]).ToArray();
                // create command parameters to remove NAT rule
                parameterList = new List<MikrotikCommandParameter>();
                parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toRemoveIdList)));
                // send the command
                try
                {
                    response = ExecuteCommand("/ip/pool/remove", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    return false;
                }
            }

            // update new IP pool name
            // get current pool id
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id,name"));
            // send the command
            try
            {
                response = ExecuteCommand("/ip/pool/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
            // create id list to rename
            var toEditIdList = response.DataRows.Select(row => new { ID = row[".id"], Name = row["name"].Replace("_new_", "_") }).OrderBy(row => row.Name).ToArray();
            var index = 0;
            foreach (var item in toEditIdList)
            {
                // create command parameters to remove NAT rule
                parameterList = new List<MikrotikCommandParameter>();
                parameterList.Add(new MikrotikCommandParameter(".id", item.ID));
                parameterList.Add(new MikrotikCommandParameter("name", clearOldPools ? defaultVerticalNATIPPoolName + "_" + index.ToString("000") : item.Name));
                // send the command
                try
                {
                    response = ExecuteCommand("/ip/pool/set", parameterList.ToArray());
                    if (response.ErrorCode != 0)
                    {
                        LogError(response.ErrorException, response.ErrorMessage);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    return false;
                }
                index++;
            }

            return true;
        }
        /// <summary>
        /// Confirms all set values for vertical DSL type.
        /// </summary>
        /// <param name="clearOldPools"></param>
        /// <returns></returns>
        public bool ConfirmAddressLists(bool clearOldPools = false)
        {
            // initialize connection
            if (!InitializeConnection())
                return false;

            // create query parameters
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("disabled", "true", MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/firewall/address-list/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            // create id list to update
            var toEditIdList = response.DataRows.Select(row => row[".id"]).ToArray();
            // create command parameters to update NAT rule
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toEditIdList)));
            parameterList.Add(new MikrotikCommandParameter("disabled", "false"));
            // send the command
            try
            {
                response = ExecuteCommand("/ip/firewall/address-list/set", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            return ConfirmVerticalNAT(clearOldPools);
        }

        private bool CleanVerticalNATs(bool isLastChangeReverse = false)
        {
            // initialize connection
            if (!InitializeConnection())
                return false;

            // create query parameters
            var disabled = isLastChangeReverse ? "true" : "false";
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("action", "src-nat", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("disabled", disabled, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("log-prefix", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "tcp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "udp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("protocol", "icmp", MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
            // create id list to remove
            var toRemoveIdList = response.DataRows.Select(row => row[".id"]).ToArray();
            // create command parameters to remove NAT rule
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toRemoveIdList)));
            // send the command
            try
            {
                response = ExecuteCommand("/ip/firewall/nat/remove", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            if (isLastChangeReverse)
            {
                return ReverseVerticalNATIPPoolChanges();
            }
            else
            {
                return ClearVerticalNATIPPools();
            }
        }

        private bool ClearVerticalNATIPPools()
        {
            // initialize connection
            if (!InitializeConnection())
                return false;

            // remove old ip pool
            // get current pool id
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id,name"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/pool/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
            // create id list to remove
            var toRemoveIdList = response.DataRows.Where(row => !row["name"].StartsWith(defaultVerticalNATIPPoolName + "_new_")).Select(row => row[".id"]).ToArray();
            // create command parameters to remove NAT rule
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toRemoveIdList)));
            // send the command
            try
            {
                response = ExecuteCommand("/ip/pool/remove", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            return true;
        }

        private bool ReverseVerticalNATIPPoolChanges()
        {
            // initialize connection
            if (!InitializeConnection())
                return false;

            // remove old ip pool
            // get current pool id
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id,name"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/pool/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
            // create id list to remove
            var toRemoveIdList = response.DataRows.Where(row => row["name"].StartsWith(defaultVerticalNATIPPoolName + "_new_")).Select(row => row[".id"]).ToArray();
            // create command parameters to remove NAT rule
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toRemoveIdList)));
            // send the command
            try
            {
                response = ExecuteCommand("/ip/pool/remove", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            return true;
        }
        /// <summary>
        /// Clears all vertical NAT rules.
        /// </summary>
        /// <returns></returns>
        public bool ClearActiveVerticalNATs()
        {
            return CleanVerticalNATs(false);
        }
        /// <summary>
        /// Removes last added vertical NATs.
        /// </summary>
        /// <returns></returns>
        public bool ReverseVerticalNATChanges()
        {
            return CleanVerticalNATs(true);
        }

        private bool CleanAddressList(bool isLastChangeReverse = false)
        {
            // initialize connection
            if (!InitializeConnection())
                return false;

            // create query parameters
            var disabled = isLastChangeReverse ? "true" : "false";
            var parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter("comment", defaultLogPrefix, MikrotikCommandParameter.ParameterType.Query));
            parameterList.Add(new MikrotikCommandParameter("disabled", disabled, MikrotikCommandParameter.ParameterType.Query));
            // create filters
            parameterList.Add(new MikrotikCommandParameter(".proplist", ".id"));
            // send the command
            MikrotikResponse response;
            try
            {
                response = ExecuteCommand("/ip/firewall/address-list/getall", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            // create id list to remove
            var toRemoveIdList = response.DataRows.Select(row => row[".id"]).ToArray();
            // create command parameters to remove NAT rule
            parameterList = new List<MikrotikCommandParameter>();
            parameterList.Add(new MikrotikCommandParameter(".id", string.Join(",", toRemoveIdList)));
            // send the command
            try
            {
                response = ExecuteCommand("/ip/firewall/address-list/remove", parameterList.ToArray());
                if (response.ErrorCode != 0)
                {
                    LogError(response.ErrorException, response.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

            return CleanVerticalNATs(isLastChangeReverse);
        }
        /// <summary>
        /// Clears all of previously added rules (addresses, NAT, pools)
        /// </summary>
        /// <returns></returns>
        public bool ReverseAddressLists()
        {
            return CleanAddressList(true);
        }
        /// <summary>
        /// Clears all of added rules (addresses, NAT, pools)
        /// </summary>
        /// <returns></returns>
        public bool CleanAddressLists()
        {
            return CleanAddressList(false);
        }

        public void Dispose()
        {
            Close();
        }
        #endregion
    }
}
