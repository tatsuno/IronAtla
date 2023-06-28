using IronAtla.Compiler.Ast;
using System.Collections.Generic;
using System.Linq;

namespace IronAtla.Compiler
{
    public class AtlaParsers : AtlaLexers
    {
        public readonly ParserRef<SourceChar, AstExpr> Expr = new ParserRef<SourceChar, AstExpr>();
        public readonly ParserRef<SourceChar, AstStmt> Stmt = new ParserRef<SourceChar, AstStmt>();

        public AtlaParsers()
        {
            ImplExprParser();
            ImplStmtParser();

            var delimiters = new HashSet<string>
            {
                "=", ":", "::",
                // arrows
                "->", "=>", "~>",
                // braces
                "(", ")", "[", "]", "<", ">", "{", "}",
            };

            Parser<SourceChar, Token> op;
            {
                var infixOp0 = new HashSet<string> { "$" };
                var infixOp1 = new HashSet<string> { ">>" };
                var infixOp2 = new HashSet<string> { "||" };
                var infixOp3 = new HashSet<string> { "&&" };
                var infixOp4 = new HashSet<string> { "==", "!=", "<", "<=", ">", ">=", };
                var infixOp5 = new HashSet<string> { "++" };
                var infixOp6 = new HashSet<string> { "+", "-" };
                var infixOp7 = new HashSet<string> { "*", "/" };
                var infixOp8 = new HashSet<string> { "**" };
                var infixOp9 = new HashSet<string> { "." };

                var infixOps = infixOp0
                    .Union(infixOp1)
                    .Union(infixOp2)
                    .Union(infixOp3)
                    .Union(infixOp4)
                    .Union(infixOp5)
                    .Union(infixOp6)
                    .Union(infixOp7)
                    .Union(infixOp8)
                    .Union(infixOp9);

                var opSigns = new HashSet<char>(infixOps.SelectMany(s => s.ToCharArray()));
            }
        }

        private void ImplExprParser()
        {
            var litrBool = Word("true").Or(Word("false")).Map(s =>
            {
                var token = new Token(TokenKind.Keyword, s);
                return new AstExprConst<bool>(token, token.Text == "true") as AstExpr;
            });

            var litrInt = Digits.Map(s =>
            {
                var token = new Token(TokenKind.Number, s);
                return new AstExprConst<int>(token, int.Parse(s.String)) as AstExpr;
            });

            var litrDouble = Digits.And(Accept('.')).And(Digits).Map(a_b_c =>
            {
                var ((a, b), c) = a_b_c;
                var token = new Token(TokenKind.Number, a + b + c);
                return new AstExprConst<double>(token, double.Parse(token.Text)) as AstExpr;
            });

            var litrFloat = litrDouble.And(Accept('f')).Map(a_b =>
            {
                var (a, b) = a_b;
                var a_ = a as AstExprConst<double>;
                var token = new Token(TokenKind.Number, a_.Token.String + b);
                return new AstExprConst<float>(token, float.Parse(a_.Token.Text)) as AstExpr;
            });

            var ident = Ident.Map(a =>
            {
                var token = new Token(TokenKind.Ident, a);
                return new AstExprIdent(token, token.Text) as AstExpr;
            });

            var factor = litrBool.Or(litrFloat).Or(litrDouble).Or(litrInt).Or(ident);

            var prefix = Op.AndL(Wsp.Many()).And(factor).Map(a_b =>
            {
                var (a, b) = a_b;
                var opToken = new Token(TokenKind.Operator, a);
                return new AstExprPrefix(new AstExprIdent(opToken, a.String), b) as AstExpr;
            });

            var termApply = prefix.Or(factor);

            Parser<SourceChar, AstExpr> ExprApply = termApply.SepBy1(Wsp.Many1()).Map(ts =>
            {
                if (ts.First.Next == null)
                {
                    return ts.First();
                }
                return ts.Skip(1).Aggregate(ts.First.Value, (acc, b) => new AstExprApply(acc, b));
            });

            var term2 = ExprApply;

            // operators
            // TODO

            Expr.Impl = term2;
        }

        private void ImplStmtParser()
        {
            var stmtVal = Word("val").And(
                Wsp.Many1().AndR(Ident.Once())
                .AndL(Wsp.Many()).And(Accept('=').Once())
                .AndL(Wsp.Many()).And(Expr).Once()
                ).Map(a_b_c_d =>
                {
                    var (a, ((b, c), d)) = a_b_c_d;
                    var valToken = new Token(TokenKind.Keyword, a);
                    var idToken = new Token(TokenKind.Ident, b);
                    var eqToken = new Token(TokenKind.Keyword, new SourceString(new LinkedList<SourceChar>(new SourceChar[] { c })));
                    return new AstStmtVal(valToken, idToken, eqToken, d) as AstStmt;
                });

            Stmt.Impl = stmtVal;
        }
    }
}
