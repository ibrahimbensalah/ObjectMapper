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

            return ctor;
        }

        public ObjectMapping(IEnumerable<KeyValuePair<string, object>> pairs, Type targetType, ConstructorInfo ctor)
        {
            TargetType = targetType;
            Ctor = ctor;
            if (ctor == null)
            {
                Dependencies = Enumerable.Empty<IDependency>();
            }
            else
            {
                var keyValuePairs = pairs as KeyValuePair<string, object>[] ?? pairs.ToArray();
                var PropertyMappings =
                    from sourceKvp in keyValuePairs
                    from PropertyDescriptor targetProp in TypeDescriptor.GetProperties(targetType)
                    let excludes = ctor.GetParameters().ToLookup(e => e.Name, StringComparer.InvariantCultureIgnoreCase)
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
        }

        public Type TargetType { get; }
        public ConstructorInfo Ctor { get; }
        public IEnumerable<IDependency> Dependencies { get; }

        public IOption<object> Create(IMap<string, object> values)
        {
            if (Ctor == null)
                return Option<object>.None();

            var option = Ctor.GetParameters().Select(p => values[p.Name].Select(v => (v, p.ParameterType))).AllSome();
            if (!option.IsSome)
                return Option<object>.None();
            var parameters = option.Value;

            var instance = Ctor.Invoke(parameters.Select(p => p.Item1 ?? Default(p.Item2)).ToArray());
            foreach (var p in Dependencies.OfType<PropertyDependency>())
            {
                var propOption = values[p.Name];
                if (propOption.IsSome)
                    p.SetValue(instance, propOption.Value);
            }

            return instance.Some();
        }

        private object Default(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            return null;
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