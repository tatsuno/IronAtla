namespace IronAtla.Compiler.Ast
{
    public class AstExpr : AstNode
    {
    }

    public class AstExprConst<T> : AstExpr
    {
        public readonly Token Token;
        public readonly T Value;

        public AstExprConst(Token token, T value)
        {
            Token = token;
            Value = value;
        }
    }

    public class AstExprApply : AstExpr
    {
        public readonly AstExpr Func;
        public readonly AstExpr Arg;

        public AstExprApply(AstExpr func, AstExpr arg)
        {
            Func = func;
            Arg = arg;
        }
    }

    public class AstExprIdent : AstExpr
    {
        public readonly Token Token;
        public readonly string Value;

        public AstExprIdent(Token token, string value)
        {
            Token = token;
            Value = value;
        }
    }

    public class AstExprPrefix : AstExpr
    {
        public readonly AstExpr Op;
        public readonly AstExpr Rhs;

        public AstExprPrefix(AstExpr op, AstExpr rhs)
        {
            Op = op;
            Rhs = rhs;
        }
    }

    public class AstExprInfix : AstExpr
    {
        public readonly AstExpr Lhs;
        public readonly AstExpr Op;
        public readonly AstExpr Rhs;

        public AstExprInfix(AstExpr lhs, AstExpr op, AstExpr rhs)
        {
            Lhs = lhs;
            Op = op;
            Rhs = rhs;
        }
    }

    public class AstExprSufix : AstExpr
    {
        public readonly AstExpr Lhs;
        public readonly AstExpr Op;

        public AstExprSufix(AstExpr lhs, AstExpr op)
        {
            Lhs = lhs;
            Op = op;
        }
    }
}
