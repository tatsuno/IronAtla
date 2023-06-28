using IronAtla.Compiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class LexerTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var lexers = new AtlaLexers();

            var result = lexers.ExprLexer.Parse(new SourceInput("_x==123.456f + -789"));
            Assert.IsInstanceOfType(result, typeof(ParseSuccess<SourceChar, LinkedList<Token>>));

            var success = result.TryGetSuccess();
            var tokens = success.Result.ToArray();
            var expected = new string[6] {
                "_x", "==", "123.456f", "+", "-", "789"
            };
            Assert.AreEqual(expected.Length, tokens.Length);
            foreach (var (a, b) in expected.Zip(tokens, (a, b) => (a, b)))
            {
                Assert.AreEqual(a, b.Text);
            }
        }
    }
}
