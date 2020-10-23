using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Mikrotik
{
    public class MikrotikResponse
    {
        private List<string> _source;
        /// <summary>
        /// Raw string coming from router.
        /// </summary>
        public string Raw
        {
            get
            {
                if (_source == null)
                    return string.Empty;
                return string.Join(Environment.NewLine, _source);
            }
        }
        /// <summary>
        /// Error code.
        /// </summary>
        public int ErrorCode { get; private set; }
        /// <summary>
        /// Hash key coming from login request.
        /// </summary>
        public string hashKey
        {
            get
            {
                if (ErrorCode != 0)
                    return "";
                return (_source.FirstOrDefault(line => line.Contains("message=")) ?? _source.FirstOrDefault(line => line.Contains("ret="))).Split(new string[] { "ret=", "message=" }, StringSplitOptions.None).LastOrDefault();
            }
        }
        /// <summary>
        /// Error message from router.
        /// </summary>
        public string ErrorMessage { get; private set; }
        /// <summary>
        /// Contains the exception for internal errors.
        /// </summary>
        public Exception ErrorException { get; private set; }
        /// <summary>
        /// Gets received data from router in rows.
        /// </summary>
        public List<Dictionary<string, string>> DataRows { get; private set; }
        /// <summary>
        /// Creates a response message.
        /// </summary>
        /// <param name="errorMessage">Error message to include in response.</param>
        /// <param name="exception">Exception to include in response.</param>
        public MikrotikResponse(string errorMessage, Exception exception = null)
        {
            ErrorCode = 2;
            ErrorMessage = errorMessage;
            ErrorException = exception;
        }
        /// <summary>
        /// Creates a response message of raw data from router.
        /// </summary>
        /// <param name="message">Raw data from router.</param>
        public MikrotikResponse(List<string> message)
        {
            _source = message;
            CreateDataRows();
            SetErrorCode();
        }
        /// <summary>
        /// Creates processed data from raw data.
        /// </summary>
        private void CreateDataRows()
        {
            DataRows = new List<Dictionary<string, string>>();
            foreach (var row in _source)
            {
                if (row.Contains("="))
                {
                    var dictionary = new Dictionary<string, string>();
                    var partitionedString = row.Split(new string[] { "=" }, StringSplitOptions.None).Where(part => part != "!re").ToArray();
                    for (int i = 0; i < partitionedString.Length - partitionedString.Length % 2; i += 2)
                    {
                        dictionary.Add(partitionedString[i], partitionedString[i + 1]);
                    }
                    DataRows.Add(dictionary);
                }
            }
        }
        /// <summary>
        /// Sets the response error code.
        /// </summary>
        private void SetErrorCode()
        {
            if (_source.Any(line => line.Contains("!fatal") || line.Contains("!trap")))
            {
                ErrorCode = 1;
                if (_source.FirstOrDefault() == "!fataltoo many commands before login")
                {
                    ErrorMessage = "too many commands before login";
                    return;
                }
                var messageLine = _source.FirstOrDefault(line => line.Contains("message="));
                if (string.IsNullOrEmpty(messageLine))
                {
                    ErrorMessage = "";
                    return;
                }
                ErrorMessage = messageLine.Split(new string[] { "message=" }, StringSplitOptions.None).LastOrDefault();
            }
            else
            {
                ErrorCode = 0;
                ErrorMessage = "Success";
            }
        }
    }
}
