using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CommandLineParser;
using System.Reflection;

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


        }



        [Test]
        public void TestOneParameterWithValue()
        {
            var parser = new BasicCommandLineTest();

            parser.Parse(new string[] { "--testParam=123" });
            Assert.AreEqual("123", parser.TestParam);
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
