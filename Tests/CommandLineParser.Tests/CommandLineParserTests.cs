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

        public class BasicCommandLineTest : Parser
        {
            [ParameterAttribute("testParam")]
            public string TestParam { get; set; }

            [ParameterAttribute("testBool")]
            public bool BooleanParam { get; set; }

            [ParameterAttribute("testInt")]
            public Int32 Int32Param { get; set; }

        }


        [Test]
        public void TestRegex1()
        {
            var regularExpression = new Regex(@"""(.*)""|'(.*)'"); // matches quoted (double or single) string.
            var test = @"""heppas""";
            var match = regularExpression.Match(test);

            Assert.IsTrue(match.Success);
        }




        [Test]
        public void TestTokenize()
        {
            var parser = new BasicCommandLineTest();

            var result = parser.Tokenize("--heppas -was a 'ninja'");
            Assert.AreEqual(9, result.Count);
            Assert.AreEqual(5, result.Where(r => r.Item2 == Parser.Token.Separator).Count());
            Assert.AreEqual(3, result.Where(r => r.Item2 == Parser.Token.Identifier).Count());
            Assert.AreEqual(1, result.Where(r => r.Item2 == Parser.Token.String).Count());
        }



        [Test]
        public void ParseOneParameterWithValue()
        {
            var parser = new BasicCommandLineTest();

            parser.Parse(new string[] { "--testParam=123" });
            Assert.AreEqual("123", parser.TestParam);
        }

        [Test]
        public void ParseWithDifferentAssignmentOperators()
        {
            var parser = new BasicCommandLineTest();

            parser.Parse(new string[] { "--testParam='123'" });
            Assert.AreEqual("123", parser.TestParam);

            parser.Parse(new string[] { "--testParam=\"123\"" });
            Assert.AreEqual("123", parser.TestParam);

            parser.Parse(new string[] { "-testParam '123'" });
            Assert.AreEqual("123", parser.TestParam);

            parser.Parse(new string[] { " -testParam '123'" });
            Assert.AreEqual("123", parser.TestParam);

            parser.Parse(new string[] { " -testInt: 123" });
            Assert.AreEqual(123, parser.Int32Param);

            parser.Parse(new string[] { "-testInt:123" });
            Assert.AreEqual(123, parser.Int32Param);

            parser.Parse(new string[] { "-testInt 123" });
            Assert.AreEqual(123, parser.Int32Param);

            parser.Parse(new string[] { "-testInt=123 " });
            Assert.AreEqual(123, parser.Int32Param);
        }


        [Test]
        public void ShouldBeAbleToAcceptDifferentTypes()
        {
            var parser = new BasicCommandLineTest();

            parser.Parse(new string[] { "--testInt 100" });
            
            Assert.IsFalse(parser.BooleanParam);
            Assert.AreEqual(100, parser.Int32Param);
        }

        [Test]
        public void ShouldBeAbleToParseMultipleTimes()
        {
            var parser = new BasicCommandLineTest();

            Assert.IsFalse(parser.BooleanParam);
            
            parser.Parse(new string[] { "--testBool" });
            Assert.IsTrue(parser.BooleanParam);

            parser.Parse(new string[] { "--testBool false" });
            Assert.IsFalse(parser.BooleanParam);

        }

        [Test]
        public void ShouldThrowInvalidProgramExceptionOnInvalidValueForParam()
        {
            var parser = new BasicCommandLineTest();
            Assert.Throws(typeof(InvalidProgramException), delegate
            {
                parser.Parse(new string[] { "--testBool 123" }); // Wrong value for type bool
            });
        }

        [Test]
        public void TestOneParameterWithoutValue()
        {
            var parser = new BasicCommandLineTest();

            parser.Parse(new string[] { "--testBool" });
            Assert.IsTrue(parser.BooleanParam);
        }


    }
}
