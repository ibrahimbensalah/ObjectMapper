using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Xania.ObjectMapper
{
    public class Mapper
    {
        public Map<IPair<object, Type>, object> Cache { get; } = new Map<IPair<object, Type>, object>();

        public IMappingResolver[] CustomMappingResolvers { get; }

        public static IMappingResolver DefaultMappingResolver = new DefaultMappingResolver();

        public Mapper(params IMappingResolver[] customMappingResolvers)
        {
            CustomMappingResolvers = customMappingResolvers;
        }

        public static Mapper Default { get; } = new Mapper();

        abstract class MapResult
        {
            protected MapResult(IPair<object, Type> key)
            {
                Key = key;
            }

            public IPair<object, Type> Key { get; }
        }

        private class RootMapResult : MapResult
        {
            public Object Obj { get; }
            public Type Type { get; }

            public RootMapResult(object obj, Type type)
                : base(Pair.Create(obj, type))
            {
                Obj = obj;
                Type = type;
            }
        }

        private class DependencyMapResult : MapResult
        {
            public IMapping Mapping { get; }

            public DependencyMapResult(IPair<object, Type> key, IMapping mapping) : base(key)
            {
                Mapping = mapping;
            }
        }

        private class ResolvedMapResult : MapResult
        {
            public IMapping Mapping { get; }

            public ResolvedMapResult(IPair<object, Type> key, IMapping mapping) : base(key)
            {
                Mapping = mapping;
            }
        }

        public IOption<object> Map(object obj, Type targetType)
        {
            // var key = Pair.Create(obj, targetType);
            var cache = new Map<IPair<object, Type>, object>();

            var stack = new Stack<MapResult>();
            var rootMap = new RootMapResult(obj, targetType);
            stack.Push(rootMap);

            while (stack.Count > 0)
            {
                var curr = stack.Pop();
                if (cache[curr.Key].IsSome || stack.Any(e => Equals(e.Key, curr.Key)))
                    continue;

                if (curr is RootMapResult root)
                {
                    if (root.Obj != null)
                    {
                        var results =
                                from r in CustomMappingResolvers.Append(DefaultMappingResolver)
                                from mappable in r.Resolve(root.Obj)
                                from mapping in mappable.To(root.Type)
                                select new DependencyMapResult(root.Key, mapping)
                            ;

                        foreach (var result in results)
                        {
                            stack.Push(result);
                        }
                    }
                }
                else if (curr is DependencyMapResult dependencyMap)
                {
                    stack.Push(new ResolvedMapResult(dependencyMap.Key, dependencyMap.Mapping));
                    foreach (var dependency in dependencyMap.Mapping.Dependencies)
                    {
                        var rootMapResult = new RootMapResult(dependency.Value, dependency.TargetType);
                        stack.Push(rootMapResult);
                    }
                }
                else if (curr is ResolvedMapResult resolved)
                {
                    var deps =
                        from dep in resolved.Mapping.Dependencies
                        let key = Pair.Create(dep.Item1, dep.Item2)
                        let value = cache[key]
                        select Pair.Create(dep.Name, value);

                    var instance = resolved.Mapping.Create(new Mappings(deps));
                    if (instance.IsSome)
                        cache.Add(curr.Key, instance.Value);
                }
                else
                    throw new NotImplementedException($"Type pattern: {curr.GetType()}");
            }

            return cache[rootMap.Key];
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

        public static IOption<IMapping> CreateObjectMapping(object obj, Type targetType)
        {
            if (obj is IEnumerable<KeyValuePair<string, object>> pairs)
                return CreateObjectMapping(pairs, targetType);

            return CreateObjectMapping(
                from PropertyDescriptor sourceProp in TypeDescriptor.GetProperties(obj)
                select new KeyValuePair<string, object>(sourceProp.Name, sourceProp.GetValue(obj)),
                targetType
            );
        }

        public static IOption<ObjectMapping> CreateObjectMapping(IEnumerable<KeyValuePair<string, object>> pairs, Type targetType)
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

    public class DefaultMappingResolver : IMappingResolver
    {
        public IOption<IMappable> Resolve(object obj)
        {
            if (obj == null)
                return Option<IMappable>.None();

            if (obj is IMappable mappable)
                return mappable.Some();

            return new DefaultMappable(obj).Some();
        }
    }

    public class DefaultMappable : IMappable
    {
        private readonly object _value;

        public DefaultMappable(object value)
        {
            _value = value;
        }

        private static TerminalMapping Term(object obj)
        {
            return new TerminalMapping(obj);
        }

        public IOption<IMapping> To(Type targetType)
        {
            if (targetType.IsInstanceOfType(_value))
                return Term(_value).Some();

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

            if (targetType == typeof(short))
                return Term(Convert.ToInt16(_value)).Some();

            if (targetType == typeof(int))
                return Term(Convert.ToInt32(_value)).Some();

            if (targetType == typeof(long))
                return Term(Convert.ToInt64(_value)).Some();

            if (targetType == typeof(sbyte))
                return Term(Convert.ToSByte(_value)).Some();

            if (targetType.IsEnum)
                return Term(_value).Some();

            if (targetType.IsArray)
            {
                var elementType = targetType.GetElementType();
                return new ArrayMapping(_value, elementType).Some();
            }

            var enumerableType = GetInterfaces(targetType).Where(IsEnumerableType).FirstOrDefault();
            if (enumerableType != null)
            {
                var elementType = enumerableType.GenericTypeArguments[0];
                return new EnumerableMapping(_value, elementType).Some();
            }

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(_value.GetType()))
                return Term(converter.ConvertFrom(_value)).Some();

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return new NullableMapping(_value, Nullable.GetUnderlyingType(targetType)).Some();

            return MappableObject.CreateObjectMapping(_value, targetType);
        }

        private static IEnumerable<Type> GetInterfaces(Type targetType)
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

    public class NullableMapping : IMapping
    {
        public NullableMapping(object value, Type underlyingType)
        {
            Dependencies = new[]
                {new GenericDependency {Name = "obj", Value = value, TargetType = underlyingType}};
        }

        public IEnumerable<IDependency> Dependencies { get; }

        public IOption<object> Create(IMap<string, object> values)
        {
            var option = values["obj"];
            if (option.IsSome)
                return option;

            return Option<object>.Some(null);
        }
    }

    //public class EnumerableMappingResolver : IMappingResolver
    //{
    //    public IOption<IMappable> Resolve(object obj)
    //    {
    //        return new MappableEnumerable(obj is IEnumerable enumerable ? enumerable : new[] { obj }).Some();
    //    }
    //}

    //public class MappableEnumerable : IMappable
    //{
    //    private readonly IEnumerable _enumerable;

    //    public MappableEnumerable(IEnumerable enumerable)
    //    {
    //        _enumerable = enumerable;
    //    }

    //    public IOption<IMapping> To(Type targetType)
    //    {
    //        if (targetType.IsArray)
    //        {
    //            var elementType = targetType.GetElementType();
    //            return new ArrayMapping(_enumerable, elementType).Some();
    //        }

    //        var enumerableType = GetInterfaces(targetType).Where(IsEnumerableType).FirstOrDefault();
    //        if (enumerableType != null)
    //        {
    //            var elementType = enumerableType.GenericTypeArguments[0];
    //            return new EnumerableMapping(_enumerable, elementType).Some();
    //        }

    //        return Option<IMapping>.None();
    //    }

    //    private IEnumerable<Type> GetInterfaces(Type targetType)
    //    {
    //        if (targetType.IsInterface)
    //            yield return targetType;

    //        foreach (var type in targetType.GetInterfaces())
    //            yield return type;
    //    }

    //    private bool IsEnumerableType(Type type)
    //    {
    //        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
    //    }
    //}

    public class EnumerableMapping : IMapping
    {
        private readonly Type _elementType;

        public EnumerableMapping(object obj, Type elementType)
        {
            var enumerable = obj as IEnumerable ?? new[] { obj };
            _elementType = elementType;
            Dependencies = (
                from item in enumerable.OfType<object>()
                select new GenericDependency
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = item,
                    TargetType = elementType
                }
            ).ToArray();
        }

        public IEnumerable<IDependency> Dependencies { get; }

        public IOption<object> Create(IMap<string, object> values)
        {
            var options = Dependencies.Select(dep => values[dep.Name]).AllSome();

            if (!options.IsSome)
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

        public ArrayMapping(object obj, Type elementType)
        {
            var enumerable = obj as IEnumerable ?? new[] { obj };
            _elementType = elementType;
            Dependencies = (
                from item in enumerable.OfType<object>()
                select new GenericDependency
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = item,
                    TargetType = elementType
                }
            ).ToArray();
        }

        public IEnumerable<IDependency> Dependencies { get; }

        public IOption<object> Create(IMap<string, object> values)
        {
            var options = Dependencies.Select(dep => values[dep.Name]).AllSome();

            if (!options.IsSome)
                return Option<object>.None();

            var content = options.Value;
            var arr = Array.CreateInstance(_elementType, content.Length);
            Array.Copy(content, arr, content.Length);
            return arr.Some();
        }
    }
}