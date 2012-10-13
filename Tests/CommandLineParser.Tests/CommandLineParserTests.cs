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

            [ParameterAttribute("testInt32")]
            public bool Int32Param { get; set; }

        }



        [Test]
        public void TestOneParameterWithValue()
        {
            var parser = new BasicCommandLineTest();

            parser.Parse(new string[] { "--testParam=123" });
            Assert.AreEqual("123", parser.TestParam);
        }

        [Test]
        public void ShouldThrowInvalidProgramExceptionOnInvalidValueForParam()
        {
            var parser = new BasicCommandLineTest();

            parser.Parse(new string[] { "--testInt32" });
            Assert.IsFalse(parser.BooleanParam);
            //Assert.AreEqual(100, parser.Int32Param);

            Assert.Throws(typeof(InvalidProgramException), delegate
            {
                parser.Parse(new string[] { "--testBool 123" });
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
