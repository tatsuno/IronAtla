using System.Collections.Generic;
using System.Linq;

namespace IronAtla.Compiler
{
    public class SourceChar
    {
        public readonly char Char;
        public readonly Position Position;

        public SourceChar(char c, Position position)
        {
            Char = c;
            Position = position;
        }
    }

    public class SourceString
    {
        public readonly string String;
        public readonly Span Span;

        public SourceString(string str, Span span)
        {
            String = str;
            Span = span;
        }

        public SourceString(LinkedList<SourceChar> chars)
        {
            String = new string(chars.Select(sc => { return sc.Char; }).ToArray());
            if (chars.Count > 0)
            {
                Span = new Span(chars.First.Value.Position, chars.Last.Value.Position.Right());
            }
            else
            {
                Span = Span.Zero();
            }
        }

        public static SourceString operator +(SourceString a, SourceString b)
        {
            return new SourceString(a.String + b.String, a.Span + b.Span);
        }

        public static SourceString operator +(SourceChar a, SourceString b)
        {
            return new SourceString(a.Char + b.String, new Span(Position.Min(b.Span.Start, a.Position), Position.Max(b.Span.End, a.Position)));
        }

        public static SourceString operator +(SourceString a, SourceChar b)
        {
            return new SourceString(a.String + b.Char, new Span(Position.Min(a.Span.Start, b.Position), Position.Max(a.Span.End, b.Position)));
        }

        public static SourceString operator +(Option<SourceChar> a, SourceString b)
        {
            if (a.IsSome()) return a.TryGetValue() + b;
            return b;
        }

    }

    public class SourceInput : Input<SourceChar>
    {
        private readonly Position position;

        public readonly string[] Lines;
        public int Line
        {
            get { return position.Line; }
        }
        public int Column
        {
            get { return position.Column; }
        }
        public int Index
        {
            get { return position.Index; }
        }

        public SourceInput(string text)
        {
            Lines = text.Split('\n');
            position = new Position(0, 0, 0);
        }

        public SourceInput(string[] lines, int line, int column, int index)
        {
            Lines = lines;
            position = new Position(line, column, index);
        }

        override public Option<SourceChar> Get()
        {
            if (Line < 0 || Lines.Length - 1 < Line || Column < 0 || Lines[Line].Length < Column)
            {
                return OptionNone<SourceChar>.Instance;
            }

            if (Column == Lines[Line].Length)
            {
                return new OptionSome<SourceChar>(new SourceChar('\n', new Position(Line, Column, Index)));
            }
            else
            {
                return new OptionSome<SourceChar>(new SourceChar(Lines[Line][Column], new Position(Line, Column, Index)));
            }
        }

        public override object Position()
        {
            return position;
        }

        public override Input<SourceChar> Next()
        {
            if (Column < Lines[Line].Length - 1)
            {
                return new SourceInput(Lines, Line, Column + 1, Index + 1);
            }
            else if (Column == Lines[Line].Length - 1)
            {
                return new SourceInput(Lines, Line, Column + 1, Index);
            }
            else
            {
                return new SourceInput(Lines, Line + 1, 0, Index);
            }
        }
    }
}
