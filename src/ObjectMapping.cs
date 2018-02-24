using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Xania.ObjectMapper
{
    public class ObjectMapping : IMapping
    {

        public ObjectMapping(IEnumerable<KeyValuePair<string, object>> pairs, Type targetType)
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

        public ObjectMapping(IEnumerable<KeyValuePair<string, object>> pairs, Type targetType, ConstructorInfo ctor)
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
                    Value = sourceKvp.Value,
                    Property = targetProp
                };

            var ParameterMappings =
                from sourceKvp in keyValuePairs
                from targetPar in ctor.GetParameters()
                where targetPar.Name.Equals(sourceKvp.Key, StringComparison.InvariantCultureIgnoreCase)
                select new GenericDependency
                {
                    Name = targetPar.Name,
                    Value = sourceKvp.Value,
                    TargetType = targetPar.ParameterType
                };

            Dependencies = PropertyMappings.OfType<IDependency>().Concat(ParameterMappings);
        }

        public Type TargetType { get; }
        public ConstructorInfo Ctor { get; }
        public IEnumerable<IDependency> Dependencies { get; }

        public IOption<object> Create(IMap<string, object> values)
        {
            var parameters = Ctor.GetParameters().Select(p => values[p.Name]).ToArray();
            if (!parameters.IsSome)
                return Option<object>.None();

            var instance = Ctor.Invoke(parameters.Value);
            foreach (var p in Dependencies.OfType<PropertyDependency>())
            {
                var propOption = values[p.Name];
                if (propOption.IsSome)
                    p.SetValue(instance, propOption.Value);
            }

            return instance.Some();
        }
    }

    public class MappableDictionary: IMappable
    {
        private readonly IEnumerable<KeyValuePair<string, object>> _values;

        public MappableDictionary(IEnumerable<KeyValuePair<string, object>> values)
        {
            _values = values;
        }

        public MappableDictionary(IDictionary<string, object> values)
        {
            _values = values;
        }

        public IOption<IMapping> To(Type targetType)
        {
            return new ObjectMapping(_values, targetType).Some();
        }
    }

    public class GenericDependency : IDependency
    {
        public string Name { get; set;  }
        public object Value { get; set; }
        public Type TargetType { get; set; }
        public object Item1 => Value;
        public Type Item2 => TargetType;
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

        public IEnumerable<IDependency> Dependencies { get; } = Enumerable.Empty<IDependency>();

        public IOption<object> Create(IMap<string, object> values)
        {
            return Instance.Some();
        }
    }

    public class GenericMapping : IMapping
    {
        public GenericMapping(Func<IMap<string, object>, IOption<object>> createFunc, IEnumerable<IDependency> dependencyMappings)
        {
            Dependencies = dependencyMappings;
            CreateFunc = createFunc;
        }
        public IEnumerable<IDependency> Dependencies { get; }
        public Func<IMap<string, object>, IOption<object>> CreateFunc { get; }

        public IOption<object> Create(IMap<string, object> values)
        {
            return CreateFunc(values);
        }
    }

    public interface IMappingResolver
    {
        IOption<IMappable> Resolve(object obj);
    }

    public interface IMapping
    {
        IEnumerable<IDependency> Dependencies { get; }
        IOption<object> Create(IMap<string, object> values);
    }

    public interface IMappable
    {
        IOption<IMapping> To(Type targetType);
    }
}