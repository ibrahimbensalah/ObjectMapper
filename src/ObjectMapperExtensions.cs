using System;

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
            return (T) mapper.Map(obj, typeof(T));
        }

        public static object MapTo(this object obj, Type targetType)
        {
            return Mapper.Default.Map(obj, targetType);
        }
    }
}
