using System.Collections.Generic;
using System.Linq;

namespace IronAtla.Compiler
{
    // parsing with whitespaces
    public class AtlaLexers : Parsers<SourceChar>
    {
        public Parser<SourceChar, SourceChar> Wsp; // white spaces

        private Parser<SourceChar, SourceChar> Alpha;
        private Parser<SourceChar, SourceChar> Alpha_;
        private Parser<SourceChar, SourceChar> Digit;
        private Parser<SourceChar, SourceChar> ZeroDigit;
        private Parser<SourceChar, SourceChar> NonZeroDigit;
        private Parser<SourceChar, SourceChar> AlphaDigit;
        private Parser<SourceChar, SourceChar> AlphaDigit_;
        public Parser<SourceChar, SourceString> Digits;
        public Parser<SourceChar, SourceString> Op;
        public Parser<SourceChar, SourceString> Ident;

        private Parser<SourceChar, SourceString> UIntNotZero;
        private Parser<SourceChar, SourceString> UInt;

        public Parser<SourceChar, LinkedList<Token>> ExprLexer;

        public Parser<SourceChar, SourceChar> Accept(char c)
        {
            return AcceptIf(c.ToString(), sc => sc.Char == c);
        }

        private Parser<SourceChar, SourceString> Accept(string s)
        {
            return s.Select(c => Accept(c)).Aggregate(Success<LinkedList<SourceChar>>(default), (a, b) => a.And(b).Map((cs_c) =>
            {
                var list = new LinkedList<SourceChar>(cs_c.Item1 ?? new LinkedList<SourceChar>());
                list.AddLast(cs_c.Item2);
                return list;
            })).Map(cs => new SourceString(cs));
        }

        public Parser<SourceChar, SourceString> Word(string s)
        {
            return Accept(s).AndL(Not(AlphaDigit_));
        }

        public AtlaLexers()
        {
            Wsp = Accept(' ').Or(Accept('\n'));

            Alpha = AcceptIf("letter", c => { return 'a' <= c.Char && c.Char <= 'z' || 'A' <= c.Char && c.Char <= 'Z'; });
            Alpha_ = Alpha.Or(Accept('_'));
            Digit = AcceptIf("digit", c => { return '0' <= c.Char && c.Char <= '9'; });
            ZeroDigit = Accept('0');
            NonZeroDigit = AcceptIf("non zero digit", c => { return '1' <= c.Char && c.Char <= '9'; });
            AlphaDigit = Alpha.Or(Digit);
            AlphaDigit_ = AlphaDigit.Or(Accept('_'));

            Digits = Digit.Many1().Map(cs => new SourceString(cs));

            Op = AcceptIf("operator sign", c => new HashSet<char>()
            {
                '+', '-', '*', '/', '%',
                '=', '|', '&', '<', '>',
                '!', '~', '?', '^', '$', '.'
            }.Contains(c.Char)).Many1().Map(cs => new SourceString(cs));

            Ident = Alpha_.And(AlphaDigit_.Many()).Map(c_cs =>
            {
                var chars = c_cs.Item2;
                chars.AddFirst(c_cs.Item1);

                return new SourceString(chars);
            });

            UIntNotZero = NonZeroDigit.And(Digit.Many()).Map(a_b =>
            {
                var chars = new LinkedList<SourceChar>(a_b.Item2);
                chars.AddFirst(a_b.Item1);
                return new SourceString(chars);
            });
            UInt = ZeroDigit.Map(c => new SourceString(new LinkedList<SourceChar>(new SourceChar[] { c }))).Or(UIntNotZero);

            ExprLexer = ImplExprLexer();
        }

        private Parser<SourceChar, LinkedList<Token>> ImplExprLexer()
        {
            var keywords = new HashSet<string> {
                // booleans
                "true", "false",
                // declarations
                "val", "var", "fn", "type", "import",
                // flow controls
                "for", "in", "if", "else", "while", "return", "break", "continue",
            };

            var keyword = keywords
                .Select(s => Accept(s).AndL(Not(AlphaDigit_)))
                .Aggregate(Failure<Token>("no matching keyword"), (acc, x) => acc.Or(x.Map(s => new Token(TokenKind.Keyword, s))));

            var delimChars = new HashSet<char>()
            {
                '(', ')', '[', ']', '{', '}',
                ':', ';', ','
            };

            var delim = AcceptIf("delimiter", c => delimChars.Contains(c.Char)).Map(c => new Token(TokenKind.Delimiter, new SourceString(new LinkedList<SourceChar>(new SourceChar[] { c }))));

            return keyword.Or(delim).SepBy(Wsp.Many());
        }
    }
}
