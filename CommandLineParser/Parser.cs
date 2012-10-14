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
        private Dictionary<string, PropertyInfo> Parameters;

        public enum Token
        {
            Identifier,
            String,
            Number,
            Separator
        }

        public List<Tuple<string, Token>> Tokenize(string commandLine)
        {
            var tokenRegex = new List<Tuple<Token, Regex>>();
            tokenRegex.Add(new Tuple<Token, Regex>(Token.Identifier, new Regex(@"^([a-z|_][a-z|0-9]*)", RegexOptions.IgnoreCase)));
            tokenRegex.Add(new Tuple<Token, Regex>(Token.String, new Regex(@"^""(.*)""")));
            tokenRegex.Add(new Tuple<Token, Regex>(Token.String, new Regex(@"^'(.*)'")));
            tokenRegex.Add(new Tuple<Token, Regex>(Token.Number, new Regex(@"^([0-9]+[,.]?[0-9]+)", RegexOptions.IgnoreCase)));
            tokenRegex.Add(new Tuple<Token, Regex>(Token.Separator, new Regex(@"^(-{1,2}|^/| +|[ *=: *])", RegexOptions.IgnoreCase)));

            var tokens = new List<Tuple<string, Token>>();

            var reminder = commandLine.Trim();
            while (reminder.Length > 0)
            {
                // Try match valid tokens.
                Match match = null;
                foreach (var token in tokenRegex)
                {
                    match = token.Item2.Match(reminder);
                    if (match.Success)
                    {
                        tokens.Add(new Tuple<string, Token>(match.Groups[1].Value, token.Item1));
                        reminder = reminder.Substring(match.Length);
                        break;
                    }
                }
                if (match == null || !match.Success)
                {
                    throw new Exception(String.Format("Invalid token: '{0}'", reminder));
                }
            }

            return tokens;
        }


        private List<Tuple<string, Token>> RemoveDuplicateSeparatorsInSequence(List<Tuple<string, Token>> tokenList)
        {
            var tokens = new List<Tuple<string, Token>>();
            var prevWasSeparator = false;
            foreach (var token in tokenList)
            {
                if (!prevWasSeparator || (prevWasSeparator && token.Item2 != Token.Separator))
                    tokens.Add(token);
                prevWasSeparator = token.Item2 == Token.Separator;
            }
            return tokens;
        }

        private Dictionary<string, string> ParseTokens(List<Tuple<string, Token>> tokens)
        {
            var parsedVariables = new Dictionary<string, string>(); // TODO: make return value.

            tokens = RemoveDuplicateSeparatorsInSequence(tokens);

            while (tokens.Count > 0)
            {
                if (tokens.Select(t => t.Item2).SequenceEqual(new List<Token>() { Token.Separator, Token.Identifier, Token.Separator, Token.Number })
                    || tokens.Select(t => t.Item2).SequenceEqual(new List<Token>() { Token.Separator, Token.Identifier, Token.Separator, Token.String })) // --identifier=123
                {
                    parsedVariables.Add(tokens[1].Item1, tokens[3].Item1);
                    tokens.RemoveRange(0, 4);
                }
                else if (tokens.Select(t => t.Item2).SequenceEqual(new List<Token>() { Token.Separator, Token.Identifier })) // -booleanStyleArg
                {
                    parsedVariables.Add(tokens[1].Item1, true.ToString());
                    tokens.RemoveRange(0, 2);
                }
                else
                    throw new Exception(String.Format("parse error near: {0}", tokens[0].Item1));
               
            }

            return parsedVariables;
        }

        public void Parse(string[] args)
        {
            var tokens = new List<Tuple<string, Token>>();
            foreach (var arg in args)
                tokens.AddRange(Tokenize(arg));

            var parsedVariables = ParseTokens(tokens);
            SetupParameterDictionary();

            // Set values on properties for found parameters.
            foreach (var entry in parsedVariables)
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

