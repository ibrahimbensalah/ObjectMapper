using System;
using System.Collections.Generic;

namespace Xania.ObjectMapper
{
    public static class ObjectMapperExtensions
    {
        public static T MapTo<T>(this object obj)
        {
            return (T) MapTo(obj, typeof(T));
        }

        public static T MapTo<T>(this Mapper mapper, object value)
        {
            return (T)MapTo(mapper, value, typeof(T));
        }

        public static object MapTo(this object obj, Type targetType)
        {
            return MapTo(Mapper.Default, obj, targetType);
        }

        public static object MapTo(this Mapper mapper, object value, Type targetType)
        {
            var option = mapper.Map(value, targetType);
            if (option.IsSome)
                return option.Value;
            return null;
        }

        public static IMap<string, TValue> ToMap<TElement, TValue>(this IEnumerable<TElement> elements, Func<TElement, string> keySelector, Func<TElement, IOption<TValue>> valueSelector)
        {
            var map = new Map<TValue>();
            foreach (var e in elements)
            {
                var v = valueSelector(e);
                var k = keySelector(e);
                map.Add(k, v);
            }
            return map;

        }
    }
}
