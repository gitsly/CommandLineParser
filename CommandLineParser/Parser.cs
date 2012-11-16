using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace CommandLineParser
{
    public class Parser
    {

        public class CommandLinePropertySetEventArgs : EventArgs
        {
            public PropertyInfo Property { get; set; }
            public Object Value { get; set; }

            public CommandLinePropertySetEventArgs(PropertyInfo property, Object value)
            {
                Property = property;
                Value = value;
            }
        }

        public EventHandler<CommandLinePropertySetEventArgs> PropertySetByCommandLine;

        private void OnPropertySetByCommandLine(PropertyInfo property, Object value)
        {
            var eventHandler = PropertySetByCommandLine;
            if (eventHandler != null)
            {
                eventHandler(this, new CommandLinePropertySetEventArgs(property, value));
            }
        }

        public Dictionary<string, string> ParseArguments(string[] args, Dictionary<string, PropertyInfo> parameterTypes)
        {
            var identifierRegex = new Regex(@"--([a-z][a-z|0-9]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            var parameters = new Dictionary<string, string>();

            string argumentString = string.Empty;
            foreach (var arg in args)
            {
                argumentString += arg + " ";
            }

            var remainder = argumentString.Trim();


            string param = null;
            while (remainder.Length > 0)
            {
                if (param == null) // Find a --identifier
                {
                    var match = identifierRegex.Match(remainder);
                    if (match.Success)
                    {
                        param = match.Groups[1].Value;
                        remainder = remainder.Substring(match.Length).Trim();
                    }
                }
                else
                {
                    var match = identifierRegex.Match(remainder);
                    if (match.Success)
                    {
                        var value = remainder.Substring(0, match.Index);

                        if (match.Index == 0)
                        {
                            if (parameterTypes[param].PropertyType == typeof(bool))
                            {
                                parameters.Add(param, true.ToString());
                            }
                            else if (parameterTypes[param].PropertyType == typeof(string))
                            {
                                parameters.Add(param, "");
                            }
                        }
                        else
                        {
                            parameters.Add(param, value.Trim());
                            remainder = remainder.Substring(value.Length, remainder.Length - value.Length);
                        }
                        param = null;
                    }
                    else // no more identifiers, remainder is value.
                    {
                        parameters.Add(param, remainder.Trim());
                        remainder = String.Empty;
                        param = null;
                    }
                }
            }

            if (param != null)
            {
                if (parameterTypes[param].PropertyType == typeof(bool))
                {
                    parameters.Add(param, true.ToString());
                }
                else if (parameterTypes[param].PropertyType == typeof(string))
                {
                    parameters.Add(param, "");
                }
            }


            return parameters;
        }


        public void Parse(string[] args)
        {
            if (args == null)
                return;

            var parameters = GetParameterDictionary();
            var parsedVariables = ParseArguments(args, parameters);

            // Set values on properties for found parameters.
            foreach (var entry in parsedVariables)
            {
                var parameterName = entry.Key;
                var valueString = entry.Value;

                if (!parameters.ContainsKey(parameterName)) // parameter mismatch match.
                {
                    throw new InvalidProgramException(String.Format("Unknown parameter specified: {0}", parameterName));
                }

                object value = null;
                try
                {
                    value = ConvertStringToObject(valueString, parameters[parameterName].PropertyType);
                    OnPropertySetByCommandLine(parameters[parameterName], value);
                    parameters[parameterName].SetValue(this, value, null);
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Invalid value: {0}, specified for parameter {1}", valueString, parameterName), ex);
                }
            }
        }

        private object ConvertStringToObject(string text, Type type)
        {
            return TypeDescriptor.GetConverter(type).ConvertFromString(null, CultureInfo.InvariantCulture, text);
        }

        public Dictionary<string, PropertyInfo> GetParameterDictionary()
        {
            var parameters = new Dictionary<string, PropertyInfo>();

            foreach (var prop in GetType().GetProperties())
            {
                var attribute = (ParameterAttribute)Attribute.GetCustomAttribute(prop, typeof(ParameterAttribute));
                if (attribute != null)
                {
                    foreach (var param in attribute.ParamName)
                        parameters.Add(param, prop);
                }
            }

            return parameters;
        }
    }
}
