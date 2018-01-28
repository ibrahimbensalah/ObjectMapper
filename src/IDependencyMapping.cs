using System;

namespace Xania.ObjectMapper
{
    public interface IDependencyMapping
    {
        string Name { get; }
        object Value { get; }
        Type Type { get; }
        void SetValue(object instance, object value);
    }
}