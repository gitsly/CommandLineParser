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
        public enum Token
        {
            Identifier,
            Value,
            Separator
        }
        


        public List<Tuple<string, Token>> Tokenize(string commandLine)
        {
            var tokenRegex = new List<Tuple<Token, Regex>>();
            tokenRegex.Add(new Tuple<Token, Regex>(Token.Identifier, new Regex(@"^-{1,2}([a-z][a-z|0-9]*)", RegexOptions.IgnoreCase))); // --identifier
            tokenRegex.Add(new Tuple<Token, Regex>(Token.Identifier, new Regex(@"^/([a-z][a-z|0-9]*)", RegexOptions.IgnoreCase))); // /identifier
            tokenRegex.Add(new Tuple<Token, Regex>(Token.Identifier, new Regex(@"^-{1,2}(\?)", RegexOptions.IgnoreCase))); // -?
            tokenRegex.Add(new Tuple<Token, Regex>(Token.Separator, new Regex(@"^( +|[ *=: *])", RegexOptions.IgnoreCase))); // whitspace, =, / etc.
            tokenRegex.Add(new Tuple<Token, Regex>(Token.Value, new Regex(@"^""([^\""]*)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))); // "" enclosed string
            tokenRegex.Add(new Tuple<Token, Regex>(Token.Value, new Regex(@"^'([^\""']*)'", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))); // '' enclosed string
            tokenRegex.Add(new Tuple<Token, Regex>(Token.Value, new Regex(@"^([0-9]+[,.]?[0-9]*)", RegexOptions.IgnoreCase))); // number
            tokenRegex.Add(new Tuple<Token, Regex>(Token.Value, new Regex(@"^(([a-z]:)?[0-9|.|-|\\|/|a-z]+)", RegexOptions.IgnoreCase))); // path

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

                var tmp = tokens.Select(t => t.Item2);

                if (tokens.Select(t => t.Item2).Take(3).SequenceEqual(new List<Token>() { Token.Identifier, Token.Separator, Token.Value })) // --identifier=123
                {
                    parsedVariables.Add(tokens[0].Item1, tokens[2].Item1);
                    tokens.RemoveRange(0, 3);
                }
                else if (tokens.Select(t => t.Item2).Take(2).SequenceEqual(new List<Token>() { Token.Identifier, Token.Value })) // -identifier 13  (given in separate args)
                {
                    parsedVariables.Add(tokens[0].Item1, tokens[1].Item1);
                    tokens.RemoveRange(0, 2);
                }
                else if (tokens.Select(t => t.Item2).Take(1).SequenceEqual(new List<Token>() { Token.Identifier })) // -booleanStyleArg
                {
                    parsedVariables.Add(tokens[0].Item1, true.ToString());
                    tokens.RemoveRange(0, 1);
                }
                else
                {
                    throw new Exception(String.Format("parse error near: {0}", tokens[0].Item1));
                }

                if (tokens.Count > 0 && tokens[0].Item2 == Token.Separator) // remove unneeded separator after valid parser food.
                    tokens.RemoveRange(0, 1);                   
            }

            return parsedVariables;
        }

        public void Parse(string[] args)
        {
            if (args == null)
                return;

            var tokens = new List<Tuple<string, Token>>();
            foreach (var arg in args)
                tokens.AddRange(Tokenize(arg));

            var parsedVariables = ParseTokens(tokens);
            var parameters = GetParameterDictionary();

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

