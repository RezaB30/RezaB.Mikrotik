using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Mikrotik
{
    public class MikrotikConnector
    {
        protected TcpClient _client;
        private NetworkStream _stream;
        protected int _timeout;

        public bool IsLoggedIn{ get; private set; }
        /// <summary>
        /// Creates the connector for communication with router.
        /// </summary>
        /// <param name="timeout">Waiting timeout.</param>
        public MikrotikConnector(int timeout = 1500)
        {
            _timeout = timeout;
            _client = new TcpClient() { ReceiveTimeout = _timeout, SendTimeout = _timeout, ReceiveBufferSize = int.MaxValue, SendBufferSize = int.MaxValue };
            IsLoggedIn = false;
        }
        /// <summary>
        /// Opens the connection to router.
        /// </summary>
        /// <param name="ipAddress">Router IP address.</param>
        /// <param name="port">Router api port.</param>
        public void Open(string ipAddress, int port)
        {
            if (!_client.Connected)
            {
                try
                {
                    var endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
                    _client.Connect(endpoint);
                    _stream = _client.GetStream();
                }
                catch (Exception ex)
                {
                    HandleException(ex, "Invalid IP address or port number.");
                }
            }
        }
        /// <summary>
        /// Performs login for api user.
        /// </summary>
        /// <param name="username">Api username.</param>
        /// <param name="password">Api password.</param>
        /// <returns>Router response.</returns>
        public MikrotikResponse Login(string username, string password)
        {
            var response = Login_Post_6_45_1(username, password);
            if (IsLoggedIn)
                return response;
            response = Login_Pre_6_45_1(username, password);
            return response;
        }
        // pre 6.45.1 login
        private MikrotikResponse Login_Pre_6_45_1(string username, string password)
        {
            var response = ExecuteCommand("/login");
            if (response.ErrorCode != 0)
            {
                return response;
            }
            var hashKey = response.hashKey;
            response = ExecuteCommand("/login", new MikrotikCommandParameter("name", username), new MikrotikCommandParameter("response", "00" + EncodePassword(password, hashKey)));

            if (response.ErrorCode == 0)
                IsLoggedIn = true;
            return response;
        }
        // post 6.45.1 login
        private MikrotikResponse Login_Post_6_45_1(string username, string password)
        {
            var response = ExecuteCommand("/login", new MikrotikCommandParameter("name", username), new MikrotikCommandParameter("password", password));

            if (response.ErrorCode == 0)
                IsLoggedIn = true;
            return response;
        }
        /// <summary>
        /// Executes a given command and gives the response.
        /// </summary>
        /// <param name="commandText">The command body.</param>
        /// <param name="parameters">The command parameters</param>
        /// <returns>Router response.</returns>
        public MikrotikResponse ExecuteCommand(string commandText, params MikrotikCommandParameter[] parameters)
        {
            try
            {
                // writing the command to network stream
                var commandBytes = Encoding.UTF8.GetBytes(commandText);
                var commandLength = EncodeLength(commandBytes.Length);

                _stream.Write(commandLength, 0, commandLength.Length);
                _stream.Write(commandBytes, 0, commandBytes.Length);

                // writing parameters to network stream
                foreach (var parameter in parameters)
                {
                    string prefix = "=";
                    if (parameter.Type == MikrotikCommandParameter.ParameterType.Query)
                        prefix = "?";
                    var parameterBytes = Encoding.UTF8.GetBytes(prefix + parameter.Name + ((string.IsNullOrEmpty(parameter.Value)) ? null : ("=" + parameter.Value)));
                    var parameterLength = EncodeLength(parameterBytes.Length);
                    _stream.Write(parameterLength, 0, parameterLength.Length);
                    _stream.Write(parameterBytes, 0, parameterBytes.Length);
                }
                // this is the end flag
                _stream.WriteByte(0);

                //waiting for response
                var waitTime = 0;
                while (!_stream.DataAvailable && waitTime < _timeout)
                {
                    Thread.Sleep(100);
                    waitTime += 100;
                }

                //read response
                var results = ReadData();

                return new MikrotikResponse(results);
            }
            catch (Exception ex)
            {
                return CreateExceptionresponse(ex);
            }

        }
        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Close()
        {
            _stream.Close();
            _client.Close();
        }
        /// <summary>
        /// Reads raw data from router.
        /// </summary>
        /// <returns>List of data lines from response.</returns>
        private List<string> ReadData()
        {
            List<string> output = new List<string>();
            string o = "";
            byte[] tmp = new byte[4];
            long count;
            while (true)
            {
                tmp[3] = (byte)_stream.ReadByte();
                //if(tmp[3] == 220) tmp[3] = (byte)connection.ReadByte(); it sometimes happend to me that 
                //mikrotik send 220 as some kind of "bonus" between words, this fixed things, not sure about it though
                if (tmp[3] == 0)
                {
                    output.Add(o);
                    if (o.Substring(0, 5) == "!done")
                    {
                        break;
                    }
                    else
                    {
                        o = "";
                        continue;
                    }
                }
                else
                {
                    if (tmp[3] < 0x80)
                    {
                        count = tmp[3];
                    }
                    else
                    {
                        if (tmp[3] < 0xC0)
                        {
                            int tmpi = BitConverter.ToInt32(new byte[] { (byte)_stream.ReadByte(), tmp[3], 0, 0 }, 0);
                            count = tmpi ^ 0x8000;
                        }
                        else
                        {
                            if (tmp[3] < 0xE0)
                            {
                                tmp[2] = (byte)_stream.ReadByte();
                                int tmpi = BitConverter.ToInt32(new byte[] { (byte)_stream.ReadByte(), tmp[2], tmp[3], 0 }, 0);
                                count = tmpi ^ 0xC00000;
                            }
                            else
                            {
                                if (tmp[3] < 0xF0)
                                {
                                    tmp[2] = (byte)_stream.ReadByte();
                                    tmp[1] = (byte)_stream.ReadByte();
                                    int tmpi = BitConverter.ToInt32(new byte[] { (byte)_stream.ReadByte(), tmp[1], tmp[2], tmp[3] }, 0);
                                    count = tmpi ^ 0xE0000000;
                                }
                                else
                                {
                                    if (tmp[3] == 0xF0)
                                    {
                                        tmp[3] = (byte)_stream.ReadByte();
                                        tmp[2] = (byte)_stream.ReadByte();
                                        tmp[1] = (byte)_stream.ReadByte();
                                        tmp[0] = (byte)_stream.ReadByte();
                                        count = BitConverter.ToInt32(tmp, 0);
                                    }
                                    else
                                    {
                                        //Error in packet reception, unknown length
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    o += (Char)_stream.ReadByte();
                }
            }
            return output;
        }
        /// <summary>
        /// Encodes Password for router.
        /// </summary>
        /// <param name="password">Raw password.</param>
        /// <param name="hashKey">Hash key from initial login command.</param>
        /// <returns>Encoded password.</returns>
        private string EncodePassword(string password, string hashKey)
        {
            // coverting each 2 sequential character to a byte
            var partitioned = Enumerable.Range(0, hashKey.Length / 2).Select(i => hashKey.Substring(i * 2, 2));
            var hashKeyBytes = partitioned.Select(part => byte.Parse(part, System.Globalization.NumberStyles.HexNumber));
            // merge
            var toHashBytes = new List<byte>();
            toHashBytes.Add(0);
            toHashBytes.AddRange(Encoding.UTF8.GetBytes(password));
            toHashBytes.AddRange(hashKeyBytes);
            // hash
            var hashAlgorithm = MD5.Create();
            var hashedBytes = hashAlgorithm.ComputeHash(toHashBytes.ToArray());
            // return
            var result = "";
            foreach (var item in hashedBytes)
            {
                result += item.ToString("x2");
            }
            return result;
        }
        /// <summary>
        /// Gets hex string length from raw int.
        /// </summary>
        /// <param name="length">length of command.</param>
        /// <returns>Encoded length of command.</returns>
        private byte[] EncodeLength(int length)
        {
            if (length < 0x80)
            {
                byte[] tmp = BitConverter.GetBytes(length);
                return new byte[1] { tmp[0] };
            }
            if (length < 0x4000)
            {
                byte[] tmp = BitConverter.GetBytes(length | 0x8000);
                return new byte[2] { tmp[1], tmp[0] };
            }
            if (length < 0x200000)
            {
                byte[] tmp = BitConverter.GetBytes(length | 0xC00000);
                return new byte[3] { tmp[2], tmp[1], tmp[0] };
            }
            if (length < 0x10000000)
            {
                byte[] tmp = BitConverter.GetBytes(length | 0xE0000000);
                return new byte[4] { tmp[3], tmp[2], tmp[1], tmp[0] };
            }
            else
            {
                byte[] tmp = BitConverter.GetBytes(length);
                return new byte[5] { 0xF0, tmp[3], tmp[2], tmp[1], tmp[0] };
            }
        }
        /// <summary>
        /// Handles internal exceptions.
        /// </summary>
        /// <param name="ex">Exception.</param>
        /// <param name="message">Added message.</param>
        protected void HandleException(Exception ex, string message)
        {
            throw new Exception("Error in Mikrotik Connector:" + message, ex);
        }
        /// <summary>
        /// Generates an exception response.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <returns>Generated response.</returns>
        protected MikrotikResponse CreateExceptionresponse(Exception exception)
        {
            var innerException = exception;
            while (innerException.InnerException != null)
            {
                innerException = innerException.InnerException;
            }
            return new MikrotikResponse(innerException.Message, exception);
        }
    }
}
