using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Xania.ObjectMapper
{
    public class TypeMapping : IMapping
    {

        public TypeMapping(IEnumerable<KeyValuePair<string, object>> pairs, Type targetType)
            : this(pairs, targetType, GetConstructorInfo(targetType))
        {
        }

        private static ConstructorInfo GetConstructorInfo(Type targetType)
        {
            var ctor = targetType.GetConstructors().OrderBy(e => e.GetParameters().Length)
                .FirstOrDefault();

            if (ctor == null)
                throw new InvalidOperationException($"Could not find a contructor for type {targetType}");

            return ctor;
        }

        public TypeMapping(IEnumerable<KeyValuePair<string, object>> pairs, Type targetType, ConstructorInfo ctor)
        {
            TargetType = targetType;
            Ctor = ctor;

            var keyValuePairs = pairs as KeyValuePair<string, object>[] ?? pairs.ToArray();
            var PropertyMappings =
                from sourceKvp in keyValuePairs
                from PropertyDescriptor targetProp in TypeDescriptor.GetProperties(targetType)
                let excludes = ctor.GetParameters().ToLookup(e=> e.Name, StringComparer.InvariantCultureIgnoreCase)
                where targetProp.Name.Equals(sourceKvp.Key, StringComparison.InvariantCultureIgnoreCase)
                    && !excludes.Contains(targetProp.Name)
                select new PropertyDependency
                {
                    SourceValue = sourceKvp.Value,
                    Property = targetProp
                };

            var ParameterMappings =
                from sourceKvp in keyValuePairs
                from targetPar in ctor.GetParameters()
                where targetPar.Name.Equals(sourceKvp.Key, StringComparison.InvariantCultureIgnoreCase)
                select new GenericDependency
                {
                    Name = targetPar.Name,
                    SourceValue = sourceKvp.Value,
                    TargetType = targetPar.ParameterType
                };

            DependencyMappings = PropertyMappings.OfType<IDependency>().Concat(ParameterMappings);
        }

        public Type TargetType { get; }
        public ConstructorInfo Ctor { get; set; }

        public IEnumerable<IDependency> DependencyMappings { get; }

        public object Create(IMap<string, object> values)
        {
            return Create2(Ctor, values);
        }

        private object Create2(ConstructorInfo ctor, IMap<string, object> values)
        {
            var parameters = ctor.GetParameters().Select(p => values.TryGetValue(p.Name, out var value) ? value : throw new KeyNotFoundException())
                .ToArray();
            var instance = ctor.Invoke(parameters);
            foreach (var p in DependencyMappings.OfType<PropertyDependency>())
            {
                if (values.TryGetValue(p.Name, out var value))
                {
                    p.SetValue(instance, value);
                }
            }

            return instance;
        }
    }

    public class GenericDependency : IDependency
    {
        public string Name { get; set;  }
        public object SourceValue { get; set; }
        public Type TargetType { get; set; }
    }

    /// <summary>
    /// Terminal dependency is kind of constant mapping with no underlying dependencies. 
    /// </summary>
    public class TerminalMapping : IMapping
    {
        public TerminalMapping(Object instance)
        {
            Instance = instance;
        }

        public object Instance { get; }

        public IEnumerable<IDependency> DependencyMappings { get; } = Enumerable.Empty<IDependency>();

        public object Create(IMap<string, object> values)
        {
            return Instance;
        }
    }

    public class GenericMapping : IMapping
    {
        public GenericMapping(Func<IMap<string, object>, object> createFunc, IEnumerable<IDependency> dependencyMappings)
        {
            DependencyMappings = dependencyMappings;
            CreateFunc = createFunc;
        }
        public IEnumerable<IDependency> DependencyMappings { get; }
        public Func<IMap<string, object>, object> CreateFunc { get; }

        public object Create(IMap<string, object> values)
        {
            return CreateFunc(values);
        }
    }

    public interface IMappingResolver
    {
        IOption<IMapping> Resolve(object obj, Type targetType);
    }

    public interface IMapping
    {
        IEnumerable<IDependency> DependencyMappings { get; }
        object Create(IMap<string, object> values);
    }
}