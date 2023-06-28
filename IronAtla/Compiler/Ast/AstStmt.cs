namespace IronAtla.Compiler.Ast
{
    public abstract class AstStmt : AstNode
    {

    }

    public class AstStmtVal : AstStmt
    {
        public readonly Token ValToken;
        public readonly Token IdentToken;
        public readonly Token EqToken;
        public readonly AstExpr Expr;

        public AstStmtVal(Token val, Token ident, Token eq, AstExpr expr)
        {
            ValToken = val;
            IdentToken = ident;
            EqToken = eq;
            Expr = expr;
        }
    }
}
