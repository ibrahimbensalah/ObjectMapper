using System;
using System.ComponentModel;

namespace Xania.ObjectMapper
{
    public class PropertyDependency: IDependency
    {
        public object Value { get; set; }
        public PropertyDescriptor Property { get; set; }

        public string Name => Property.Name;
        public Type TargetType => Property.PropertyType;

        public void SetValue(object instance, object value)
        {
            Property.SetValue(instance, value);
        }

        public object Item1 => Value;
        public Type Item2 => TargetType;
    }
}