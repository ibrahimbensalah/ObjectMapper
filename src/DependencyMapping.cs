using System;
using System.ComponentModel;

namespace Xania.ObjectMapper
{
    public class PropertyMapping: IDependencyMapping
    {
        public object Value { get; set; }
        public PropertyDescriptor Property { get; set; }

        public string Name => Property.Name;
        public Type Type => Property.PropertyType;

        public void SetValue(object instance, object value)
        {
            Property.SetValue(instance, value);
        }
    }
}