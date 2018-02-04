using System;
using System.Collections.Generic;
using System.Linq;

namespace Xania.ObjectMapper
{
    public class Values : IMap<string, object>
    {
        private readonly IDictionary<string, IOption<object>> _dict;

        public Values(IEnumerable<KeyValuePair<string, IOption<object>>> mappings)
        {
            _dict = new Dictionary<string, IOption<object>>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var m in mappings)
            {
                _dict.Add(m.Key, m.Value);
            }
        }
        public Values(IDictionary<string, object> dict)
        {
            _dict = dict.ToDictionary(e => e.Key, e => e.Value.Some());
        }
        public static IMap<string, object> Empty => new Values(Enumerable.Empty<KeyValuePair<string, IOption<object>>>());
        public IOption<object> this[string name] => _dict.TryGetValue(name, out var value) ? value : Option<object>.None();
    }
}