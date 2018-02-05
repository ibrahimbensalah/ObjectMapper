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
            new EnumerableMappingResolver(),
            new ObjectMappingResolver()
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

        private IEnumerable<Mapping> GetMappings(Object value, Type targetType)
        {
            var stack = new Stack<Mapping>();
            stack.Push(new Mapping(value, targetType));

            while (stack.Count > 0)
            {
                var curr = stack.Pop();

                var mappings =
                        from r in CustomMappingResolvers.Concat(BuildInMappingResolvers)
                        from mappable in r.Resolve(curr.Value)
                        select mappable.To(curr.TargetType)
                    ;
            }
        }

        public IOption<object> Map(object obj, Type targetType)
        {
            var mappings =
                    from r in CustomMappingResolvers.Concat(BuildInMappingResolvers)
                    from mappable in r.Resolve(obj)
                    select mappable.To(targetType)
                ;

            var results = 
                from option in mappings
                from mapping in option
                let deps =
                    from dep in mapping.DependencyMappings
                    let m = Map(dep.SourceValue, dep.TargetType)
                    select new KeyValuePair<string, IOption<object>>(dep.Name, m)
                select mapping.Create(new Values(deps));

            return results.FirstOrDefault() ?? Option<object>.None();
        }

        class Mapping
        {
            public Mapping(object value, Type targetType)
            {
                Value = value;
                TargetType = targetType;
            }

            public object Value {  get; }
            public Type TargetType { get; }
        }
    }

    public class ObjectMappingResolver : IMappingResolver
    {
        public IOption<IMappable> Resolve(object obj)
        {
            return new MappableObject(obj).Some();
        }
    }

    public class MappableObject : IMappable
    {
        private readonly object _value;

        public MappableObject(object value)
        {
            _value = value;
        }

        public IOption<IMapping> To(Type targetType)
        {
            return CreateObjectMapping(_value, targetType);
        }

        private IOption<IMapping> CreateObjectMapping(object obj, Type targetType)
        {
            if (obj is IEnumerable<KeyValuePair<string, object>> pairs)
                return CreateObjectMapping(pairs, targetType);

            return CreateObjectMapping(
                from PropertyDescriptor sourceProp in TypeDescriptor.GetProperties(obj)
                select new KeyValuePair<string, object>(sourceProp.Name, sourceProp.GetValue(obj)),
                targetType
            );
        }

        private IOption<ObjectMapping> CreateObjectMapping(IEnumerable<KeyValuePair<string, object>> pairs, Type targetType)
        {
            var ctor =
                targetType
                    .GetConstructors()
                    .OrderBy(e => e.GetParameters().Length)
                    .FirstOrDefault();

            if (ctor == null)
                return Option<ObjectMapping>.None();

            return new ObjectMapping(pairs, targetType, ctor).Some();
        }
    }

    public class PrimitiveMappingResolver : IMappingResolver
    {
        public IOption<IMappable> Resolve(object obj)
        {
            if (obj == null)
                return Option<IMappable>.None();

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

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(_value.GetType()))
                return Term(converter.ConvertFrom(_value)).Some();

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return new NullableMapping(_value, Nullable.GetUnderlyingType(targetType)).Some();

            return Option<IMapping>.None();
        }
    }

    public class NullableMapping : IMapping
    {
        public NullableMapping(object value, Type underlyingType)
        {
            DependencyMappings = new[]
                {new GenericDependency {Name = "obj", SourceValue = value, TargetType = underlyingType}};
        }

        public IEnumerable<IDependency> DependencyMappings { get; }

        public IOption<object> Create(IMap<string, object> values)
        {
            var option = values["obj"];
            if (option.IsSome)
                return option;

            return Option<object>.Some(null);
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