using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xania.ObjectMapper
{
    public static class Option
    {
        public static IOption<T> Some<T>(this T element)
        {
            var elementType = element.GetType();
            if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(Option<>))
                throw new InvalidOperationException();

            return Option<T>.Some(element);
        }

        public static IOption<object> None()
        {
            return Option<object>.None();
        }

        public static IOption<T> SingleOrNone<T>(this IEnumerable<T> enumerable)
        {
            var single = enumerable.SingleOrDefault();
            if (single == null)
                return Option<T>.None();
            return single.Some();
        }

        public static IOption<TR> Select<T, TR>(this IOption<T> option, Func<T, TR> selector)
        {
            foreach (var i in option)
                return selector(i).Some();

            return Option<TR>.None();
        }

        public static IOption<T[]> AllSome<T>(this IEnumerable<IOption<T>> options)
        {
            var list = new List<T>();
            foreach (var o in options)
            {
                if (o.IsSome)
                    list.Add(o.Value);
                else
                    return Option<T[]>.None();
            }
            return list.ToArray().Some();
        }
    }

    public interface IOption<out TValue> : IEnumerable<TValue>
    {
        bool IsSome { get; }
        TValue Value { get; }
    }

    public class Option<TValue> : IOption<TValue>
    {
        public TValue Value { get; }

        private Option(TValue value, bool isSome)
        {
            Value = value;
            IsSome = isSome;
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
            if (IsSome)
                yield return Value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool IsSome { get; }
    }
}