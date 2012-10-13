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
        private Dictionary<string, string> Parameters;

        private void ParseParameters(string[] args)
        {
            Parameters = new Dictionary<string, string>();
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
                            if (!Parameters.ContainsKey(Parameter))
                            {
                                Parts[0] = remover.Replace(Parts[0], "$1");
                                Parameters.Add(Parameter, Parts[0]);
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
                            if (!Parameters.ContainsKey(Parameter))
                                Parameters.Add(Parameter, "true");
                        }
                        Parameter = Parts[1];
                        break;

                    // Parameter with enclosed value
                    case 3:
                        // The last parameter is still waiting. 
                        // With no value, set it to true.
                        if (Parameter != null)
                        {
                            if (!Parameters.ContainsKey(Parameter))
                                Parameters.Add(Parameter, "true");
                        }
                        Parameter = Parts[1];

                        // Remove possible enclosing characters (",')
                        if (!Parameters.ContainsKey(Parameter))
                        {
                            Parts[2] = remover.Replace(Parts[2], "$1");
                            Parameters.Add(Parameter, Parts[2]);
                        }
                        Parameter = null;
                        break;
                }
            }
            // In case a parameter is still waiting
            if (Parameter != null)
            {
                if (!Parameters.ContainsKey(Parameter))
                {
                    Parameters.Add(Parameter, "true");
                }
            }
        }


        public void Parse(string arguments)
        {

        }

        public void Parse(string[] args)
        {
            ParseParameters(args);

            // Set values on properties for found parameters.
            foreach (var entry in Parameters)
            {
                var parameterName = (string)entry.Key;

                var prop = GetPropertyByParameterAttributeName(parameterName);
                if (prop == null) // parameter mismatch match.
                {
                    throw new InvalidOperationException(String.Format("Unknown parameter specified: {0}", parameterName));
                }

                prop.SetValue(this, ConvertStringToObject(entry.Value, prop.PropertyType), null);
            }
        }

        private object ConvertStringToObject(string text, Type type)
        {
            return TypeDescriptor.GetConverter(type).ConvertFromString(null, CultureInfo.InvariantCulture, text);
        }

        private PropertyInfo GetPropertyByParameterAttributeName(string paramAttributeName)
        {
            foreach (var prop in GetType().GetProperties())
            {
                var attribute = (ParameterAttribute)Attribute.GetCustomAttribute(prop, typeof(ParameterAttribute));
                if (attribute != null)
                {
                    if (attribute.ParamName == paramAttributeName)
                    {
                        return prop;
                    }
                }
            }
            return null;
        }


    }
}

