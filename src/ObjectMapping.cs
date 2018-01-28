using System.Collections.Generic;
using System.Reflection;

namespace Xania.ObjectMapper
{
    public class ObjectMapping
    {
        public ObjectMapping(ConstructorInfo ctor, IEnumerable<IDependencyMapping> dependencyMappings)
        {
            Ctor = ctor;
            DependencyMappings = dependencyMappings;
        }

        public ConstructorInfo Ctor { get; }
        public IEnumerable<IDependencyMapping> DependencyMappings { get; }

        public object Create(IMap<string, object> values)
        {
            var instance = Ctor.Invoke(new object[0]);
            foreach (var p in DependencyMappings)
            {
                if (values.TryGetValue(p.Name, out var value))
                {
                    p.SetValue(instance, value);
                }
            }
            return instance;
        }
    }
}