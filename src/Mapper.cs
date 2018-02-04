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
            new PrimitiveMappingResolver(),
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

        public IOption<object> Map(object obj, Type targetType)
        {
            if (obj == null)
                return Option<object>.Some(null);

            var customMappings =
                from r in CustomMappingResolvers.Concat(BuildInMappingResolvers)
                from mappable in r.Resolve(obj)
                from mapping in mappable.To(targetType)
                let deps =
                    from dep in mapping.DependencyMappings
                    select Map(dep.SourceValue, dep.TargetType)
                select mapping;

            var customMapping = customMappings.FirstOrDefault();
            if (customMapping != null)
            {
                var mappings =
                        from dep in customMapping.DependencyMappings
                        let m = Map(dep.SourceValue, dep.TargetType)
                        select new KeyValuePair<string, IOption<object>>(dep.Name, m)
                    ;

                return customMapping.Create(new Values(mappings));
            }

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(obj.GetType()))
                return converter.ConvertFrom(obj).Some();

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return Map(obj, Nullable.GetUnderlyingType(targetType));

            foreach (var mapping in CreateMapping(obj, targetType))
            {
                var mappings =
                        from dep in mapping.DependencyMappings
                        let m = Map(dep.SourceValue, dep.TargetType)
                        select new KeyValuePair<string, IOption<object>>(dep.Name, m)
                    ;
                return mapping.Create(new Values(mappings));
            }

            return Option<object>.None();
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
        public IOption<IMappable> Resolve(object obj)
        {
            return new MappablePrimitive(obj).Some();
        }
    }

    public class MappablePrimitive : IMappable
    {
        private readonly object _value;

        public MappablePrimitive(object value)
        {
            _value = value;
        }

        private TerminalMapping Term(object obj)
        {
            return new TerminalMapping(obj);
        }

        public IOption<IMapping> To(Type targetType)
        {
            if (targetType == typeof(float))
                return Term(Convert.ToSingle(_value)).Some();

            if (targetType == typeof(decimal))
                return Term(Convert.ToDecimal(_value)).Some();

            if (targetType == typeof(string))
                return Term(Convert.ToString(_value)).Some();

            if (targetType == typeof(bool))
                return Term(Convert.ToBoolean(_value)).Some();

            if (targetType == typeof(byte))
                return Term(Convert.ToByte(_value)).Some();

            if (targetType == typeof(char))
                return Term(Convert.ToChar(_value)).Some();

            if (targetType == typeof(double))
                return Term(Convert.ToDouble(_value)).Some();

            if (targetType == typeof(Int16))
                return Term(Convert.ToInt16(_value)).Some();

            if (targetType == typeof(Int32))
                return Term(Convert.ToInt32(_value)).Some();

            if (targetType == typeof(Int64))
                return Term(Convert.ToInt64(_value)).Some();

            if (targetType == typeof(SByte))
                return Term(Convert.ToSByte(_value)).Some();

            return Option<IMapping>.None();
        }
    }

    public class EnumerableMappingResolver : IMappingResolver
    {
        public IOption<IMappable> Resolve(object obj)
        {
            return new MappableEnumerable(obj is IEnumerable enumerable ? enumerable : new[] { obj }).Some();
        }
    }

    public class MappableEnumerable : IMappable
    {
        private readonly IEnumerable _enumerable;

        public MappableEnumerable(IEnumerable enumerable)
        {
            _enumerable = enumerable;
        }

        public IOption<IMapping> To(Type targetType)
        {
            if (targetType.IsArray)
            {
                var elementType = targetType.GetElementType();
                return new ArrayMapping(_enumerable, elementType).Some();
            }

            var enumerableType = GetInterfaces(targetType).Where(IsEnumerableType).FirstOrDefault();
            if (enumerableType != null)
            {
                var elementType = enumerableType.GenericTypeArguments[0];
                return new EnumerableMapping(_enumerable, elementType).Some();
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

    public class EnumerableMapping : IMapping
    {
        private readonly Type _elementType;

        public EnumerableMapping(IEnumerable obj, Type elementType)
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

        public IOption<object> Create(IMap<string, object> values)
        {
            var options = DependencyMappings.Select(dep => values[dep.Name]).ToArray();

            if (options.IsSome)
                return Option<object>.None();

            var content = options.Value;

            var arr = Array.CreateInstance(_elementType, content.Length);
            Array.Copy(content, arr, content.Length);
            return arr.Some();
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

        public IOption<object> Create(IMap<string, object> values)
        {
            var options = DependencyMappings.Select(dep => values[dep.Name]).ToArray();

            if (!options.IsSome)
                return Option<object>.None();

            var content = options.Value;
            var arr = Array.CreateInstance(_elementType, content.Length);
            Array.Copy(content, arr, content.Length);
            return arr.Some();
        }
    }
}