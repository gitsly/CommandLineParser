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

    /* C# Regular Expressions Cheat Sheet
 
    \	Marks the next character as either a special character or escapes a literal. For example, "n" matches the character "n". "\n" matches a newline character. The sequence "\\" matches "\" and "\(" matches "(". Note: double quotes may be escaped by doubling them: "<a href=""...>"
    ^	Depending on whether the MultiLine option is set, matches the position before the first character in a line, or the first character in the string.
    $	Depending on whether the MultiLine option is set, matches the position after the last character in a line, or the last character in the string.
    *	Matches the preceding character zero or more times. For example, "zo*" matches either "z" or "zoo".
    +	Matches the preceding character one or more times. For example, "zo+" matches "zoo" but not "z".
    ?	Matches the preceding character zero or one time. For example, "a?ve?" matches the "ve" in "never".
    .	Matches any single character except a newline character.
    (pattern)	Matches pattern and remembers the match. The matched substring can be retrieved from the resulting Matches collection, using Item [0]...[n]. To match parentheses characters ( ), use "\(" or "\)".
    (?<name>pattern)	Matches pattern and gives the match a name.
    (?:pattern)	A non-capturing group
    (?=...)	A positive lookahead
    (?!...)	A negative lookahead
    (?<=...)	A positive lookbehind .
    (?<!...)	A negative lookbehind .
    x|y	Matches either x or y. For example, "z|wood" matches "z" or "wood". "(z|w)oo" matches "zoo" or "wood".
    {n}	n is a non-negative integer. Matches exactly n times. For example, "o{2}" does not match the "o" in "Bob," but matches the first two o's in "foooood".
    {n,}	n is a non-negative integer. Matches at least n times. For example, "o{2,}" does not match the "o" in "Bob" and matches all the o's in "foooood." "o{1,}" is equivalent to "o+". "o{0,}" is equivalent to "o*".
    {n,m}	m and n are non-negative integers. Matches at least n and at most m times. For example, "o{1,3}" matches the first three o's in "fooooood." "o{0,1}" is equivalent to "o?".
    [xyz]	A character set. Matches any one of the enclosed characters. For example, "[abc]" matches the "a" in "plain".
    [^xyz]	A negative character set. Matches any character not enclosed. For example, "[^abc]" matches the "p" in "plain".
    [a-z]	A range of characters. Matches any character in the specified range. For example, "[a-z]" matches any lowercase alphabetic character in the range "a" through "z".
    [^m-z]	A negative range characters. Matches any character not in the specified range. For example, "[m-z]" matches any character not in the range "m" through "z".
    \b	Matches a word boundary, that is, the position between a word and a space. For example, "er\b" matches the "er" in "never" but not the "er" in "verb".
    \B	Matches a non-word boundary. "ea*r\B" matches the "ear" in "never early".
    \d	Matches a digit character. Equivalent to [0-9].
    \D	Matches a non-digit character. Equivalent to [^0-9].
    \f	Matches a form-feed character.
    \k	A back-reference to a named group.
    \n	Matches a newline character.
    \r	Matches a carriage return character.
    \s	Matches any white space including space, tab, form-feed, etc. Equivalent to "[ \f\n\r\t\v]".
    \S	Matches any nonwhite space character. Equivalent to "[^ \f\n\r\t\v]".
    \t	Matches a tab character.
    \v	Matches a vertical tab character.
    \w	Matches any word character including underscore. Equivalent to "[A-Za-z0-9_]".
    \W	Matches any non-word character. Equivalent to "[^A-Za-z0-9_]".
    \num	Matches num, where num is a positive integer. A reference back to remembered matches. For example, "(.)\1" matches two consecutive identical characters.
    \n	Matches n, where n is an octal escape value. Octal escape values must be 1, 2, or 3 digits long. For example, "\11" and "\011" both match a tab character. "\0011" is the equivalent of "\001" & "1". Octal escape values must not exceed 256. If they do, only the first two digits comprise the expression. Allows ASCII codes to be used in regular expressions.
    \xn	Matches n, where n is a hexadecimal escape value. Hexadecimal escape values must be exactly two digits long. For example, "\x41" matches "A". "\x041" is equivalent to "\x04" & "1". Allows ASCII codes to be used in regular expressions.
    \un	Matches a Unicode character expressed in hexadecimal notation with exactly four numeric digits. "\u0200" matches a space character.
    \A	Matches the position before the first character in a string. Not affected by the MultiLine setting
    \Z	Matches the position after the last character of a string. Not affected by the MultiLine setting.
    \G	Specifies that the matches must be consecutive, without any intervening non-matching characters. 
    */

    public class Parser
    {
        private Dictionary<string, string> ParsedArguments;
        private Dictionary<string, PropertyInfo> Parameters;

        private void ParseArguments(string[] args)
        {
            ParsedArguments = new Dictionary<string, string>();
            Regex splitter = new Regex(@"^-{1,2}|^/|=|:| ", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Regex remover = new Regex(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            string Parameter = null;
            string[] Parts;

            // Valid parameters forms:
            // {-,/,--}param{ ,=,:}((",')value(",'))
            // Examples: 
            // -param1 value1 --param2 /param3:"Test-:-work" 
            //   /param4=happy -param5 '--=nice=--'
            foreach (string argument in args)
            {
                var arg = argument.Trim();

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
                                ParsedArguments.Add(Parameter, true.ToString());
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
                                ParsedArguments.Add(Parameter, true.ToString());
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
                    ParsedArguments.Add(Parameter, true.ToString());
                }
            }
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

