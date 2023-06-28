using IronAtla.Compiler;
using IronAtla.Compiler.Ast;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tests
{
    [TestClass]
    public class ParserTest
    {
        private Action<AstExpr> ExpectExprApply(Action<AstExpr, AstExpr> f)
        {
            return expr =>
            {
                Assert.IsInstanceOfType(expr, typeof(AstExprApply));
                var app = expr as AstExprApply;
                f(app.Func, app.Arg);
            };
        }

        private Action<AstExpr> ExpectExprPrefix(Action<AstExpr, AstExpr> f)
        {
            return expr =>
            {
                Assert.IsInstanceOfType(expr, typeof(AstExprPrefix));
                var app = expr as AstExprPrefix;
                f(app.Op, app.Rhs);
            };
        }

        private Action<AstExpr> ExpectExprConst<T>(T value)
        {
            return expr =>
            {
                Assert.IsInstanceOfType(expr, typeof(AstExprConst<T>));
                Assert.AreEqual(value, (expr as AstExprConst<T>).Value);
            };
        }

        private Action<AstExpr> ExpectExprIdent(string name)
        {
            return expr =>
            {
                Assert.IsInstanceOfType(expr, typeof(AstExprIdent));
                Assert.AreEqual(name, (expr as AstExprIdent).Value);
            };
        }

        private Action<AstStmt> ExpectStmtVal(string name, Action<AstExpr> f)
        {
            return stmt =>
            {
                Assert.IsInstanceOfType(stmt, typeof(AstStmtVal));
                var valStmt = stmt as AstStmtVal;
                Assert.AreEqual(name, valStmt.IdentToken.Text);
                f(valStmt.Expr);
            };
        }

        [TestMethod]
        public void TestMethod1()
        {
            var parsers = new AtlaParsers();

            var result = parsers.Stmt.Parse(new SourceInput("val _x=f0 123.456f -789"));
            Assert.IsInstanceOfType(result, typeof(ParseSuccess<SourceChar, AstStmt>));
            var success = result.TryGetSuccess();
            var root = success.Result;

            ExpectStmtVal("_x", ExpectExprApply((f, g) =>
            {
                ExpectExprApply((a, b) =>
                {
                    ExpectExprIdent("f0")(a);
                    ExpectExprConst(123.456f)(b);
                })(f);
                ExpectExprPrefix((c, d) =>
                {
                    ExpectExprIdent("-")(c);
                    ExpectExprConst(789)(d);
                })(g);
            }))(root);
        }
    }
}
