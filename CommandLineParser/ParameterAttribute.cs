using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandLineParser
{
    using System;
    [AttributeUsage(AttributeTargets.Property)]
    public class ParameterAttribute : System.Attribute
    {
        public List<string> ParamName { get; private set; }
        
        public ParameterAttribute(string paramName)  // url is a positional parameter
        {
            ParamName = new List<string>();
            ParamName.Add(paramName);
        }

        public ParameterAttribute(params string[] parameters)
        {
            ParamName = new List<string>();
            ParamName.AddRange(parameters);
        }
    }
}
