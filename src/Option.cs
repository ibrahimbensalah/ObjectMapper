using System;
using System.Collections;
using System.Collections.Generic;

namespace Xania.ObjectMapper
{
    public static class Option
    {
        public static IOption<T> Some<T>(this T element)
        {
            return Option<T>.Some(element);
        }

        public static IOption<R> Select<T, R>(this IOption<T> option, Func<T, R> selector)
        {
            foreach (var i in option)
                return selector(i).Some();

            return Option<R>.None();
        }
    }

    public interface IOption<out TValue> : IEnumerable<TValue>
    {
    }

    public class Option<TValue> : IOption<TValue>
    {
        private readonly TValue _value;
        private readonly bool _isSome;

        private Option(TValue value, bool isSome)
        {
            _value = value;
            _isSome = isSome;
        }

        public static Option<TValue> Some(TValue element)
        {
            return new Option<TValue>(element, true);
        }

        public static Option<TValue> None()
        {
            return new Option<TValue>(default(TValue), false);
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            if (_isSome)
                yield return _value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}