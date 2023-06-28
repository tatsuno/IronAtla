using System;
using System.Collections.Generic;

namespace IronAtla.Compiler
{
    public class Parsers<I>
    {
        public static string ExpectedButGot(string expected, I got)
        {
            return $"expected {expected}, but got {got}.";
        }

        public static string EndOfInput(string expected)
        {
            return $"expected {expected}, but end of input.";
        }

        public Parser<I, I> AcceptIf(string expected, Predicate<I> pred)
        {
            return new AnnoParser<I, I>(input =>
            {
                var current = input.Get();
                if (current.IsSome())
                {
                    var got = current.TryGetValue();
                    if (pred(got))
                    {
                        return new ParseSuccess<I, I>(got, input.Next());
                    }
                    else
                    {
                        return new ParseFailure<I, I>(ExpectedButGot(expected, got), input);
                    }
                }
                else
                {
                    return new ParseFailure<I, I>(EndOfInput(expected), input);
                }
            });
        }

        public Parser<I, A> Success<A>(A v)
        {
            return new AnnoParser<I, A>(input => new ParseSuccess<I, A>(v, input));
        }

        public Parser<I, A> Failure<A>(string reason)
        {
            return new AnnoParser<I, A>(input => new ParseFailure<I, A>(reason, input));
        }

        public Parser<I, A> Not<A>(Parser<I, A> p)
        {
            return new AnnoParser<I, A>(input =>
            {
                var result = p.Parse(input);
                if (result.IsSuccess())
                {
                    return new ParseFailure<I, A>("Expected failure", input);
                }
                else
                {
                    return new ParseSuccess<I, A>(default, input);
                }
            });
        }
    }

    public abstract class Parser<I, A>
    {
        private string name = "";
        protected Dictionary<object, ParseResult<I, A>> Cache = new Dictionary<object, ParseResult<I, A>>();

        public Parser<I, A> Named(string name)
        {
            this.name = name;
            return this;
        }

        public abstract ParseResult<I, A> Apply(Input<I> input);

        public ParseResult<I, A> Parse(Input<I> input)
        {
            if (Cache.ContainsKey(input.Position()))
            {
                return Cache[input.Position()];
            }
            else
            {
                return Apply(input);
            }
        }

        public Parser<I, B> Map<B>(Func<A, B> func)
        {
            return new AnnoParser<I, B>(input =>
            {
                return Parse(input).Map(success => new ParseSuccess<I, B>(func(success.Result), success.Next));
            });
        }

        public Parser<I, A> Or(Parser<I, A> other)
        {
            return new AnnoParser<I, A>(input =>
            {
                var result = Parse(input);
                if (result.IsSuccess())
                {
                    return result;
                }
                else
                {
                    return other.Parse(input);
                }
            });
        }

        public Parser<I, (A, B)> And<B>(Parser<I, B> other)
        {
            return new AnnoParser<I, (A, B)>(input =>
            {
                return Parse(input).Bind(lhs =>
                {
                    return other.Parse(lhs.Next).Map(rhs => new ParseSuccess<I, (A, B)>((lhs.Result, rhs.Result), rhs.Next));
                });
            });
        }

        public Parser<I, A> AndL<B>(Parser<I, B> other)
        {
            return And(other).Map<A>(ab => ab.Item1);
        }

        public Parser<I, B> AndR<B>(Parser<I, B> other)
        {
            return And(other).Map<B>(ab => ab.Item2);
        }

        // make failure to error
        public Parser<I, A> Once()
        {
            return new AnnoParser<I, A>(input =>
            {
                var result = Apply(input);
                if (result.IsFailure())
                {
                    var failure = result.TryGetFailure();
                    return new ParseError<I, A>(failure.Reason, failure.FailAt);
                }
                else
                {
                    return result;
                }
            });
        }

        // This parser does not match EOI. (for preffer performance)
        public Parser<I, LinkedList<A>> Many()
        {
            return new AnnoParser<I, LinkedList<A>>(input =>
            {
                var current = input;
                var ret = new LinkedList<A>();
                while (true)
                {
                    var got = current.Get();
                    if (!got.IsSome()) { break; }

                    var result = Parse(current);
                    if (!result.IsSuccess()) { break; }

                    var success = result.TryGetSuccess();
                    ret.AddLast(success.Result);
                    current = success.Next;
                }

                return new ParseSuccess<I, LinkedList<A>>(ret, current);
            });
        }

        public Parser<I, LinkedList<A>> Many1()
        {
            return And(Many()).Map(x_xs =>
            {
                var ret = new LinkedList<A>(x_xs.Item2);
                ret.AddFirst(x_xs.Item1);
                return ret;
            });
        }

        public Parser<I, Option<A>> Optional()
        {
            return new AnnoParser<I, Option<A>>(input =>
            {
                var result = Parse(input);
                if (result.IsSuccess())
                {
                    var success = result.TryGetSuccess();
                    return new ParseSuccess<I, Option<A>>(new OptionSome<A>(success.Result), success.Next);
                }
                else
                {
                    var failure = result.TryGetFailure();
                    return new ParseSuccess<I, Option<A>>(OptionNone<A>.Instance, failure.FailAt);
                }
            });
        }

        public Parser<I, LinkedList<A>> SepBy<B>(Parser<I, B> sep)
        {
            return new AnnoParser<I, LinkedList<A>>(input =>
            {
                var current = input;
                var ret = new LinkedList<A>();
                while (true)
                {
                    var got = current.Get();
                    if (!got.IsSome()) break; // break immediately when reach the end of input.

                    var withSep = AndL(sep).Parse(current);
                    if (withSep.IsSuccess())
                    {
                        var success = withSep.TryGetSuccess();
                        ret.AddLast(success.Result);
                        current = success.Next;
                        continue;
                    }

                    var lastOne = Parse(current);
                    if (lastOne.IsSuccess())
                    {
                        var success = lastOne.TryGetSuccess();
                        ret.AddLast(success.Result);
                        current = success.Next;
                    }
                    break;
                }

                return new ParseSuccess<I, LinkedList<A>>(ret, current);
            });
        }

        public Parser<I, LinkedList<A>> SepBy1<B>(Parser<I, B> sep)
        {
            return And(sep.AndR(SepBy(sep)).Optional()).Map(a_bs =>
            {
                var (a, bs) = a_bs;
                if (bs.IsSome())
                {
                    var ret = new LinkedList<A>(bs.TryGetValue());
                    ret.AddFirst(a);
                    return ret;
                }
                else
                {
                    return new LinkedList<A>(new A[] { a });
                }
            });
        }
    }

    // instead of annonymous class
    public class AnnoParser<I, A> : Parser<I, A>
    {
        private readonly Func<Input<I>, ParseResult<I, A>> apply;

        public AnnoParser(Func<Input<I>, ParseResult<I, A>> apply)
        {
            this.apply = apply;
        }

        public override ParseResult<I, A> Apply(Input<I> input)
        {
            return apply(input);
        }
    }

    public class ParserRef<I, A> : Parser<I, A>
    {
        public Parser<I, A> Impl { private get; set; }

        public override ParseResult<I, A> Apply(Input<I> input)
        {
            return Impl.Apply(input);
        }
    }

    public abstract class ParseResult<I, A>
    {
        public readonly ProblemSink ProblemSink;

        protected ParseResult()
        {
            ProblemSink = new ProblemSink();
        }

        public bool IsSuccess()
        {
            return this is ParseSuccess<I, A>;
        }

        public bool IsFailure()
        {
            return this is ParseFailure<I, A>;
        }

        public bool IsError()
        {
            return this is ParseError<I, A>;
        }

        public ParseSuccess<I, A> TryGetSuccess()
        {
            return this as ParseSuccess<I, A>;
        }

        public ParseFailure<I, A> TryGetFailure()
        {
            return this as ParseFailure<I, A>;
        }

        public ParseError<I, A> TryGetError()
        {
            return this as ParseError<I, A>;
        }

        public ParseResult<I, B> Map<B>(Func<ParseSuccess<I, A>, ParseSuccess<I, B>> func)
        {
            if (IsSuccess())
            {
                var success = TryGetSuccess();
                return func(success);
            }
            else
            {
                var failure = TryGetFailure();
                return new ParseFailure<I, B>(failure.Reason, failure.FailAt);
            }
        }

        public ParseResult<I, B> Bind<B>(Func<ParseSuccess<I, A>, ParseResult<I, B>> func)
        {
            if (IsSuccess())
            {
                var success = TryGetSuccess();
                return func(success);
            }
            else
            {
                var failure = TryGetFailure();
                return new ParseFailure<I, B>(failure.Reason, failure.FailAt);
            }
        }
    }

    public class ParseSuccess<I, A> : ParseResult<I, A>
    {
        public readonly A Result;
        public readonly Input<I> Next;

        public ParseSuccess(A result, Input<I> next)
        {
            this.Result = result;
            this.Next = next;
        }
    }

    public abstract class ParseNoSuccess<I, A> : ParseResult<I, A>
    {
        public readonly string Reason;
        public readonly Input<I> FailAt;

        public ParseNoSuccess(string reason, Input<I> failAt)
        {
            this.Reason = reason;
            this.FailAt = failAt;
        }
    }

    public class ParseFailure<I, A> : ParseNoSuccess<I, A>
    {
        public ParseFailure(string reason, Input<I> failAt) : base(reason, failAt) { }
    }

    public class ParseError<I, A> : ParseNoSuccess<I, A>
    {
        public ParseError(string reason, Input<I> failAt) : base(reason, failAt) { }
    }

    public abstract class Input<I>
    {
        public abstract object Position();
        public abstract Option<I> Get();
        public abstract Input<I> Next();
    }

    public class ProblemSink
    {
        private List<Problem> problems;

        public ProblemSink()
        {
            this.problems = new List<Problem>();
        }
    }

    public abstract class Option<T>
    {
        public bool IsSome()
        {
            return this is OptionSome<T>;
        }

        public T TryGetValue()
        {
            var some = this as OptionSome<T>;
            if (some != null)
            {
                return some.Value;
            }
            else
            {
                return default(T);
            }
        }
    }

    public class OptionSome<T> : Option<T>
    {
        public readonly T Value;

        public OptionSome(T value)
        {
            Value = value;
        }
    }

    public class OptionNone<T> : Option<T>
    {
        private OptionNone() { }

        public readonly static OptionNone<T> Instance = new OptionNone<T>();
    }

}
