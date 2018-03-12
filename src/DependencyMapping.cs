using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;

namespace Xania.ObjectMapper
{
    public class PropertyDependency : IDependency
    {
        public object Value { get; set; }
        public PropertyDescriptor Property { get; set; }

        public string Name => Property.Name;
        public Type TargetType => Property.PropertyType;

        public void SetValue(object instance, object value)
        {
            if (Property.IsReadOnly)
            {
                var collection = Property.GetValue(instance);
                if (collection != null && value is IEnumerable enumerable)
                {
                    foreach (var element in enumerable)
                    {
                        var elementType = element.GetType();
                        var addMethods =
                            from m in TargetType.GetMethods()
                            let parameters = m.GetParameters()
                                .SingleOrDefault(p => p.ParameterType.IsAssignableFrom(elementType))
                                where parameters != null && 
                                      m.Name.Equals("Add", StringComparison.CurrentCultureIgnoreCase)
                                select m
                            ;
                        var addMethod = addMethods.SingleOrDefault();
                        if (addMethod != null)
                        {
                            addMethod.Invoke(collection, new[] {element});
                        }
                    }
                }
                else
                    throw new InvalidOperationException("read only");
            }
            else
            {
                Property.SetValue(instance, value);
            }
        }

        public object Item1 => Value;
        public Type Item2 => TargetType;
    }
}