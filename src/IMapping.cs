using System;
using System.Collections;
using System.Collections.Generic;

namespace Xania.ObjectMapper
{
    public interface IMap<in TSource, out TTarget>
    {
        IOption<TTarget> this[TSource name] { get; }
    }

    public class Map<TValue> : IMap<string, TValue>
    {
        private readonly IDictionary<string, TValue> _values = new Dictionary<string, TValue>(StringComparer.InvariantCultureIgnoreCase);

        public void Add(string key, TValue value)
        {
            _values[key] = value;
        }

        public Map<TValue> Add(string key, IOption<TValue> option)
        {
            if (option.IsSome)
                _values[key] = option.Value;
            return this;
        }

        public IOption<TValue> this[string name] =>
            _values.TryGetValue(name, out var value) ? value.Some() : Option<TValue>.None();
    }

    public class Map<TKey, TValue> : IMap<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _values = new Dictionary<TKey, TValue>();

        public void Add(TKey key, TValue value)
        {
            _values[key] = value;
        }

        public Map<TKey, TValue> Add(TKey key, IOption<TValue> option)
        {
            if (option.IsSome)
                _values[key] = option.Value;
            return this;
        }

        public IOption<TValue> this[TKey name] =>
            _values.TryGetValue(name, out var value) ? value.Some() : Option<TValue>.None();
    }
}