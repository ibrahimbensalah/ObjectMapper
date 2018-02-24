using System;

namespace Xania.ObjectMapper
{
    public interface IDependency: IPair<object, Type>
    {
        string Name { get; }
        object Value { get; }
        Type TargetType { get;  }
    }
}