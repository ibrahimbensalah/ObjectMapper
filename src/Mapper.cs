using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Xania.ObjectMapper
{
    public class Mapper
    {
        public IMappingResolver[] CustomMappingResolvers { get; }

        public static IMappingResolver[] BuildInMappingResolvers = {
            new StringMappingResolver(),
            new PrimitiveMappingResolver(),
            new ArrayMappingResolver(),
            new EnumerableMappingResolver()
        };

        public Mapper(params IMappingResolver[] customMappingResolvers)
        {
            CustomMappingResolvers = customMappingResolvers;
        }

        public static Mapper Default { get; } = new Mapper();

        public T MapStringTo<T>(string str)
        {
            var obj = Convert.ChangeType(str, typeof(T));
            return (T)obj;
        }

        private IOption<TypeMapping> CreateMapping(IEnumerable<KeyValuePair<string, object>> pairs, Type targetType)
        {
            var ctor =
                targetType
                    .GetConstructors()
                    .OrderBy(e => e.GetParameters().Length)
                    .FirstOrDefault();

            if (ctor == null)
                return Option<TypeMapping>.None();

            return new TypeMapping(pairs, targetType, ctor).Some();
        }

        public object Map(object obj, Type targetType)
        {
            if (obj == null)
                return null;

            var customMapping =
                CustomMappingResolvers
                    .Concat(BuildInMappingResolvers)
                    .SelectMany(e => e.Resolve(obj, targetType))
                    .FirstOrDefault();

            if (customMapping != null)
            {
                var mappings =
                    from dep in customMapping.DependencyMappings
                    let value = Map(dep.SourceValue, dep.TargetType)
                    select new KeyValuePair<string, object>(dep.Name, value);

                return customMapping.Create(new Values(mappings));
            }

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(obj.GetType()))
                return converter.ConvertFrom(obj);

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return Map(obj, Nullable.GetUnderlyingType(targetType));

            foreach (var mapping in CreateMapping(obj, targetType))
            {
                var mappings =
                    from dep in mapping.DependencyMappings
                    let value = Map(dep.SourceValue, dep.TargetType)
                    select new KeyValuePair<string, object>(dep.Name, value);

                return mapping.Create(new Values(mappings));
            }

            throw new InvalidOperationException($"Could not resolve mapping to targetType {targetType}");
        }

        private IOption<IMapping> CreateMapping(object obj, Type targetType)
        {
            if (obj is IEnumerable<KeyValuePair<string, object>> pairs)
                return CreateMapping(pairs, targetType);

            return CreateMapping(
                from PropertyDescriptor sourceProp in TypeDescriptor.GetProperties(obj)
                select new KeyValuePair<string, object>(sourceProp.Name, sourceProp.GetValue(obj)),
                targetType
            );
        }
    }

    public class PrimitiveMappingResolver : IMappingResolver
    {
        public IOption<IMapping> Resolve(object obj, Type targetType)
        {
            return ConvertPrimitive(obj, targetType);
        }

        private TerminalMapping Term(object obj)
        {
            return new TerminalMapping(obj);
        }

        private IOption<IMapping> ConvertPrimitive(object obj, Type targetType)
        {
            if (targetType == typeof(float))
                return Term(Convert.ToSingle(obj)).Some();

            if (targetType == typeof(decimal))
                return Term(Convert.ToDecimal(obj)).Some();

            if (targetType == typeof(string))
                return Term(Convert.ToString(obj)).Some();

            if (targetType == typeof(bool))
                return Term(Convert.ToBoolean(obj)).Some();

            if (targetType == typeof(byte))
                return Term(Convert.ToByte(obj)).Some();

            if (targetType == typeof(char))
                return Term(Convert.ToChar(obj)).Some();

            if (targetType == typeof(double))
                return Term(Convert.ToDouble(obj)).Some();

            if (targetType == typeof(Int16))
                return Term(Convert.ToInt16(obj)).Some();

            if (targetType == typeof(Int32))
                return Term(Convert.ToInt32(obj)).Some();

            if (targetType == typeof(Int64))
                return Term(Convert.ToInt64(obj)).Some();

            if (targetType == typeof(SByte))
                return Term(Convert.ToSByte(obj)).Some();

            return Option<IMapping>.None();
        }
    }

    public class StringMappingResolver : IMappingResolver
    {
        public IOption<IMapping> Resolve(object obj, Type targetType)
        {
            if (targetType == typeof(string))
                return new TerminalMapping(Convert.ToString(obj)).Some();

            return Option<IMapping>.None();
        }
    }

    public class EnumerableMappingResolver : IMappingResolver
    {
        public IOption<IMapping> Resolve(object obj, Type targetType)
        {
            var enumerableType = GetInterfaces(targetType).Where(IsEnumerableType).FirstOrDefault();
            if (enumerableType != null)
            {
                var elementType = enumerableType.GenericTypeArguments[0];
                if (obj is IEnumerable enumerable)
                    return new EnumerableMapping(enumerable, targetType, elementType).Some();
                return new EnumerableMapping(new[] { obj }, targetType, elementType).Some();
            }

            return Option<IMapping>.None();
        }

        private IEnumerable<Type> GetInterfaces(Type targetType)
        {
            if (targetType.IsInterface)
                yield return targetType;

            foreach (var type in targetType.GetInterfaces())
                yield return type;
        }

        private bool IsEnumerableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }
    }

    public class ArrayMappingResolver : IMappingResolver
    {
        public IOption<IMapping> Resolve(object obj, Type targetType)
        {
            if (targetType.IsArray)
            {
                var elementType = targetType.GetElementType();
                if (obj is IEnumerable enumerable)
                    return new ArrayMapping(enumerable, elementType).Some();
                return new ArrayMapping(new[] { obj }, elementType).Some();
            }

            return Option<IMapping>.None();
        }
    }

    public class EnumerableMapping : IMapping
    {
        private readonly Type _targetType;
        private readonly Type _elementType;

        public EnumerableMapping(IEnumerable obj, Type targetType, Type elementType)
        {
            _targetType = targetType;
            _elementType = elementType;
            DependencyMappings =
            (from item in obj.OfType<object>()
                select new GenericDependency
                {
                    Name = Guid.NewGuid().ToString(),
                    SourceValue = item,
                    TargetType = elementType
                }
            ).ToArray();
        }

        public IEnumerable<IDependency> DependencyMappings { get; }

        public object Create(IMap<string, object> values)
        {
            var content = DependencyMappings.Select(dep =>
                    values.TryGetValue(dep.Name, out var value) ? value : throw new KeyNotFoundException()
                ).ToArray();

            var arr = Array.CreateInstance(_elementType, content.Length);
            Array.Copy(content, arr, content.Length);
            return arr;
        }
    }

    public class ArrayMapping : IMapping
    {
        private readonly Type _elementType;

        public ArrayMapping(IEnumerable obj, Type elementType)
        {
            _elementType = elementType;
            DependencyMappings =
                (from item in obj.OfType<object>()
                select new GenericDependency
                {
                    Name = Guid.NewGuid().ToString(),
                    SourceValue = item,
                    TargetType = elementType
                }
                    ).ToArray();
        }

        public IEnumerable<IDependency> DependencyMappings { get; }

        public object Create(IMap<string, object> values)
        {
            var content = DependencyMappings.Select(dep =>
                    values.TryGetValue(dep.Name, out var value) ? value : throw new KeyNotFoundException()
                ).ToArray();

            var arr = Array.CreateInstance(_elementType, content.Length);
            Array.Copy(content, arr, content.Length);
            return arr;
        }
    }
}