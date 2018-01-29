using System;

namespace Xania.ObjectMapper
{
    public interface IDependency
    {
        string Name { get; }
        object SourceValue { get; }
        Type TargetType { get; }
        // void SetValue(object instance, object value);
    }
}