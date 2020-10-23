using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Mikrotik
{
    /// <summary>
    /// Represents a command parameter for mikrotik commands.
    /// </summary>
    public class MikrotikCommandParameter
    {
        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Value of the parameter.
        /// </summary>
        public string Value { get; private set; }
        /// <summary>
        /// Type of the parameter.
        /// </summary>
        public ParameterType Type { get; private set; }
        /// <summary>
        /// Create a command parameter for a mikrotik command.
        /// </summary>
        /// <param name="name">Name of the parameter.</param>
        /// <param name="value">Value of the parameter.</param>
        /// <param name="type">Type of the parameter.</param>
        public MikrotikCommandParameter(string name, string value, ParameterType type = ParameterType.Standard)
        {
            Name = name;
            Value = value;
            Type = type;
        }

        public enum ParameterType
        {
            Standard,
            Query
        }
    }
}
