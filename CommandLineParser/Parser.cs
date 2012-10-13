using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace CommandLineParser
{
    public class Parser
    {
        private Dictionary<string, string> ParsedArguments;
        private Dictionary<string, PropertyInfo> Parameters;

        private void ParseArguments(string[] args)
        {
            ParsedArguments = new Dictionary<string, string>();
            Regex splitter = new Regex(@"^-{1,2}|^/|=|:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Regex remover = new Regex(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            string Parameter = null;
            string[] Parts;

            // Valid parameters forms:
            // {-,/,--}param{ ,=,:}((",')value(",'))
            // Examples: 
            // -param1 value1 --param2 /param3:"Test-:-work" 
            //   /param4=happy -param5 '--=nice=--'
            foreach (string arg in args)
            {
                // Look for new parameters (-,/ or --) and a
                // possible enclosed value (=,:)
                Parts = splitter.Split(arg, 3);

                switch (Parts.Length)
                {
                    // Found a value (for the last parameter 
                    // found (space separator))
                    case 1:
                        if (Parameter != null)
                        {
                            if (!ParsedArguments.ContainsKey(Parameter))
                            {
                                Parts[0] = remover.Replace(Parts[0], "$1");
                                ParsedArguments.Add(Parameter, Parts[0]);
                            }
                            Parameter = null;
                        }
                        // else Error: no parameter waiting for a value (skipped)
                        break;
                    case 2: // Found just a parameter
                        // The last parameter is still waiting. 
                        // With no value, default to boolean 'true'
                        if (Parameter != null)
                        {
                            if (!ParsedArguments.ContainsKey(Parameter))
                                ParsedArguments.Add(Parameter, "true");
                        }
                        Parameter = Parts[1];
                        break;

                    // Parameter with enclosed value
                    case 3:
                        // The last parameter is still waiting. 
                        // With no value, set it to true.
                        if (Parameter != null)
                        {
                            if (!ParsedArguments.ContainsKey(Parameter))
                                ParsedArguments.Add(Parameter, "true");
                        }
                        Parameter = Parts[1];

                        // Remove possible enclosing characters (",')
                        if (!ParsedArguments.ContainsKey(Parameter))
                        {
                            Parts[2] = remover.Replace(Parts[2], "$1");
                            ParsedArguments.Add(Parameter, Parts[2]);
                        }
                        Parameter = null;
                        break;
                }
            }
            // In case a parameter is still waiting
            if (Parameter != null)
            {
                if (!ParsedArguments.ContainsKey(Parameter))
                {
                    ParsedArguments.Add(Parameter, "true");
                }
            }
        }


        public void Parse(string arguments)
        {

        }

        public void Parse(string[] args)
        {
            ParseArguments(args); // TODO: do this in another way.
            SetupParameterDictionary();

            // Set values on properties for found parameters.
            foreach (var entry in ParsedArguments)
            {
                var parameterName = entry.Key;
                var valueString = entry.Value;

                if (!Parameters.ContainsKey(parameterName)) // parameter mismatch match.
                {
                    throw new InvalidProgramException(String.Format("Unknown parameter specified: {0}", parameterName));
                }

                object value = null;
                try
                {
                    value = ConvertStringToObject(valueString, Parameters[parameterName].PropertyType);
                    Parameters[parameterName].SetValue(this, value, null);
                }
                catch(Exception ex)
                {
                    throw new InvalidProgramException(String.Format("Invalid value: {0}, specified for parameter {1}", valueString, parameterName), ex);
                }
            }
        }

        private object ConvertStringToObject(string text, Type type)
        {
            return TypeDescriptor.GetConverter(type).ConvertFromString(null, CultureInfo.InvariantCulture, text);
        }

        private void SetupParameterDictionary()
        {
            Parameters = new Dictionary<string, PropertyInfo>();

            foreach (var prop in GetType().GetProperties())
            {
                var attribute = (ParameterAttribute)Attribute.GetCustomAttribute(prop, typeof(ParameterAttribute));
                if (attribute != null)
                {
                    Parameters.Add(attribute.ParamName, prop);
                }
            }
        }


    }
}

