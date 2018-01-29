using System;
using System.ComponentModel;

namespace Xania.ObjectMapper
{
    public class PropertyDependency: IDependency
    {
        public object SourceValue { get; set; }
        public PropertyDescriptor Property { get; set; }

        public string Name => Property.Name;
        public Type TargetType => Property.PropertyType;

        public void SetValue(object instance, object value)
        {
            Property.SetValue(instance, value);
        }
    }
}