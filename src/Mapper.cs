using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Xania.ObjectMapper
{
    public class Mapper
    {
        public static Mapper Default { get; } = new Mapper();

        public T MapStringTo<T>(string str)
        {
            var obj = Convert.ChangeType(str, typeof(T));
            return (T)obj;
        }

        public ObjectMapping MapObject(object obj, Type targetType)
        {
            var pairs =
                from PropertyDescriptor sourceProp in TypeDescriptor.GetProperties(obj)
                select new KeyValuePair<string, object>(sourceProp.Name, sourceProp.GetValue(obj));

            return MapObject(pairs, targetType);
        }


        private ObjectMapping MapObject(IEnumerable<KeyValuePair<string, object>> pairs, Type targetType)
        {
            var ctor = targetType.GetConstructors().OrderByDescending(e => e.GetParameters().Length)
                .FirstOrDefault();

            if (ctor == null)
                return null;

            var propertyMappings =
                from sourceKvp in pairs
                from PropertyDescriptor targetProp in TypeDescriptor.GetProperties(targetType)
                where targetProp.Name.Equals(sourceKvp.Key, StringComparison.InvariantCultureIgnoreCase)
                select new PropertyMapping
                {
                    Value = sourceKvp.Value,
                    Property = targetProp
                };

            return new ObjectMapping(ctor, propertyMappings);
        }

        public object Map(object obj, Type targetType)
        {
            if (obj == null)
                return null;

            if (targetType == obj.GetType())
                return obj;

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(obj.GetType()))
                return converter.ConvertFrom(obj);

            if (TryMapPrimitive(obj, targetType, out var result))
                return result;

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return Map(obj, Nullable.GetUnderlyingType(targetType));

            var mapping = CreateMapping(obj, targetType);

            var mappings =
                from dep in mapping.DependencyMappings
                let value = Map(dep.Value, dep.Type)
                select new KeyValuePair<string, object>(dep.Name, value);

            return mapping.Create(new Values(mappings));
        }

        private ObjectMapping CreateMapping(object obj, Type targetType)
        {
            if (obj is IEnumerable<KeyValuePair<string, object>> pairs)
                return MapObject(pairs, targetType);

            return MapObject(obj, targetType);
        }

        private static bool TryMapPrimitive(object obj, Type targetType, out object value)
        {
            value = null;

            if (targetType == typeof(float))
            {
                value = Convert.ToSingle(obj);
                return true;
            }

            if (targetType == typeof(decimal))
            {
                value = Convert.ToDecimal(obj);
                return true;
            }

            if (targetType == typeof(string))
            {
                value = Convert.ToString(obj);
                return true;
            }

            if (targetType == typeof(bool))
            {
                value = Convert.ToBoolean(obj);
                return true;
            }

            if (targetType == typeof(byte))
            {
                value = Convert.ToByte(obj);
                return true;
            }

            if (targetType == typeof(char))
            {
                value = Convert.ToChar(obj);
                return true;
            }

            if (targetType == typeof(double))
            {
                value = Convert.ToDouble(obj);
                return true;
            }

            if (targetType == typeof(Int16))
            {
                value = Convert.ToInt16(obj);
                return true;
            }

            if (targetType == typeof(Int32))
            {
                value = Convert.ToInt32(obj);
                return true;
            }

            if (targetType == typeof(Int64))
            {
                value = Convert.ToInt64(obj);
                return true;
            }

            if (targetType == typeof(SByte))
            {
                value = Convert.ToSByte(obj);
                return true;
            }

            return false;
        }
    }
}