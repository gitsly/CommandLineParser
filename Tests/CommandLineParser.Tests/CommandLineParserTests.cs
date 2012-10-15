using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CommandLineParser;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CommandLineParser.Tests
{
    [TestFixture]
    public class CommandLineParserTests
    {
        private class BasicCommandLineTest : Parser
        {
            [ParameterAttribute("testParam")]
            public string TestParam { get; set; }

            [ParameterAttribute("testBool")]
            public bool BooleanParam { get; set; }

            [ParameterAttribute("testInt")]
            public Int32 Int32Param { get; set; }

            [ParameterAttribute("testInt64")]
            public Int64 Int64Param { get; set; }

            [ParameterAttribute("testFloat")]
            public float FloatParam { get; set; }

            [ParameterAttribute("path", "filename")]
            public string PathParam { get; set; }
        }

        private BasicCommandLineTest parser;

        [SetUp]
        public void SetupEachTest()
        {
            parser = new BasicCommandLineTest();
        }

        [Test, Explicit]
        public void RegexTest() // Use for testing out regular expressions.
        {
            //var test = new Regex(@"((?<="")[^\""]*(?=""))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            var test = new Regex(@"^""([^\""]*)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            var secureStoragePath = @"""hep"" ""pas""";

            var match = test.Match(secureStoragePath);

            if (match.Success)
            {
                Console.WriteLine("matched: '{0}', from pos: {1}, entire: '{2}'", match.Groups[1].Value, match.Index, match.Value);
            }
        }


        [Test]
        public void TestTokenize()
        {
            var result = parser.Tokenize("--heppas -was a 'ninja'=123");
            Assert.AreEqual(9, result.Count);
            Assert.AreEqual(4, result.Where(r => r.Item2 == Parser.Token.Separator).Count());
            Assert.AreEqual(2, result.Where(r => r.Item2 == Parser.Token.Identifier).Count());
            Assert.AreEqual(3, result.Where(r => r.Item2 == Parser.Token.Value).Count());
        }



        [Test]
        public void ParseOneParameterWithValue()
        {
            parser.Parse(new string[] { "--testParam=123" });
            Assert.AreEqual("123", parser.TestParam);
        }

        [Test]
        public void ParseParameterValueFromString()
        {
            parser.Parse(new string[] { @"--testParam=""teststring""" });
            Assert.AreEqual("teststring", parser.TestParam);
        }

        [Test]
        public void ModifyPropertyWithTwoDifferentParams()
        {
            parser.Parse(new string[] { @"-path test1" });
            Assert.AreEqual("test1", parser.PathParam);

            parser.Parse(new string[] { @"-filename test2" });
            Assert.AreEqual("test2", parser.PathParam);

        }


        [Test]
        public void ShouldBeAbleToParseMultipleStringArguments()
        {
            var args = new string[] { @"-testParam ""test"" -path ""again""" };
            parser.Parse(args);
            Assert.AreEqual("test", parser.TestParam);
            Assert.AreEqual("again", parser.PathParam);
        }

        [Test]
        public void ParseInt64Param()
        {
            parser.Parse(new string[] { "--testInt64 =          1100200300400" });
            Assert.AreEqual(1100200300400, parser.Int64Param);
        }

        [Test]
        public void ParsePathParam()
        {
            parser.Parse(new string[] { "-path", "./test.bin" });
            Assert.AreEqual("./test.bin", parser.PathParam);
        }


        [Test]
        public void ParseWithDifferentAssignmentOperators()
        {
            parser.Parse(new string[] { "--testParam='123'" });
            Assert.AreEqual("123", parser.TestParam);

            parser.Parse(new string[] { "--testParam=\"456\"" });
            Assert.AreEqual("456", parser.TestParam);

            parser.Parse(new string[] { "-testParam '789'" });
            Assert.AreEqual("789", parser.TestParam);

            parser.Parse(new string[] { " -testParam '910'" });
            Assert.AreEqual("910", parser.TestParam);

            parser.Parse(new string[] { " -testInt= 716" });
            Assert.AreEqual(716, parser.Int32Param);

            parser.Parse(new string[] { "-testInt:1" });
            Assert.AreEqual(1, parser.Int32Param);

            parser.Parse(new string[] { "/testInt 123" });
            Assert.AreEqual(123, parser.Int32Param);

            parser.Parse(new string[] { "-testInt=67 " });
            Assert.AreEqual(67, parser.Int32Param);

            parser.Parse(new string[] { "-testFloat = 2.25 " });
            Assert.AreEqual(2.25, parser.FloatParam);

            parser.Parse(new string[] { "-testFloat = 026.7500 " });
            Assert.AreEqual(026.7500, parser.FloatParam);
        }

        [Test]
        public void ShouldBeAbleToUseMultipleArguments()
        {
            var args = new string[] { "-testFloat", "4.5", "--testInt", "32"};
            parser.Parse(args);
            Assert.AreEqual(4.5, parser.FloatParam);
            Assert.AreEqual(32, parser.Int32Param);
        }


        [Test]
        public void ShouldBeAbleToAcceptDifferentTypes()
        {
            parser.Parse(new string[] { "--testInt 100" });
            
            Assert.IsFalse(parser.BooleanParam);
            Assert.AreEqual(100, parser.Int32Param);
        }

        [Test]
        public void ShouldBeAbleToParseMultipleTimes()
        {
            Assert.IsFalse(parser.BooleanParam);
            
            parser.Parse(new string[] { "--testBool" });
            Assert.IsTrue(parser.BooleanParam);

            parser.Parse(new string[] { "--testInt 100" });
            Assert.AreEqual(100, parser.Int32Param);

        }

        [Test]
        public void ShouldThrowExceptionOnInvalidValueForParam()
        {
            Assert.Throws(typeof(Exception), delegate
            {
                parser.Parse(new string[] { "--testBool 123" }); // Wrong value for type bool
            });
        }

        [Test]
        public void TestOneParameterWithoutValue()
        {
            parser.Parse(new string[] { "--testBool" });
            Assert.IsTrue(parser.BooleanParam);
        }


    }
}
