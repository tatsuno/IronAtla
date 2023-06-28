using System;

namespace IronAtla.Compiler
{
    public class Token
    {
        public TokenKind Kind;
        public readonly SourceString String;

        public string Text
        {
            get { return String.String; }
        }
        public Span Span
        {
            get { return String.Span; }
        }

        public Token(TokenKind kind, string text, Span span)
        {
            Kind = kind;
            String = new SourceString(text, span);
        }
        public Token(TokenKind kind, SourceString s)
        {
            Kind = kind;
            String = s;
        }
    }

    // See https://code.visualstudio.com/api/language-extensions/semantic-highlight-guide#standard-token-types-and-modifiers
    public enum TokenKind
    {
        Comment,
        Keyword,
        Operator,
        Delimiter,
        Ident,
        String,
        Number,
    }

    public class Span
    {
        public readonly Position Start;
        public readonly Position End;
        public readonly int Length;

        public Span(Position start, Position end)
        {
            Start = start;
            End = end;
            Length = end.Index - start.Index;
        }

        public static Span Zero()
        {
            return new Span(new Position(0, 0, 0), new Position(0, 0, 0));
        }

        public static Span operator +(Span a, Span b)
        {
            if (a == null || a.Length <= 0) return b;
            if (b == null || b.Length <= 0) return a;
            return new Span(Position.Min(a.Start, b.Start), Position.Max(a.End, b.End));
        }
    }

    public class Position : IComparable<Position>
    {
        public readonly int Line;
        public readonly int Column;
        public readonly int Index;

        public Position(int line, int column, int index)
        {
            Line = line;
            Column = column;
            Index = index;
        }

        public Position Right()
        {
            return new Position(Line, Column + 1, Index + 1);
        }

        public static bool operator <(Position a, Position b)
        {
            return a.Index < b.Index;
        }

        public static bool operator >(Position a, Position b)
        {
            return a.Index > b.Index;
        }

        public static Position Min(Position a, Position b)
        {
            return a < b ? a : b;
        }

        public static Position Max(Position a, Position b)
        {
            return a < b ? b : a;
        }

        public int CompareTo(Position other)
        {
            return this.Index.CompareTo(other.Index);
        }
    }
}
