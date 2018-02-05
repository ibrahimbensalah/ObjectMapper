using System;
using System.Linq;

namespace Xania.ObjectMapper
{
    public static class ObjectMapperExtensions
    {
        public static T MapTo<T>(this object obj)
        {
            return (T) MapTo(obj, typeof(T));
        }

        public static T MapTo<T>(this Mapper mapper, object obj)
        {
            return (T)MapTo(mapper, obj, typeof(T));
        }

        public static object MapTo(this object obj, Type targetType)
        {
            return MapTo(Mapper.Default, obj, targetType);
        }

        public static object MapTo(this Mapper mapper, object obj, Type targetType)
        {
            var option = mapper.Map(obj, targetType);
            if (option.IsSome)
                return option.Value;
            return null;
        }
    }
}
