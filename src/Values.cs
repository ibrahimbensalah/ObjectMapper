using System;
using System.Collections.Generic;
using System.Linq;

namespace Xania.ObjectMapper
{
    public class Values : IMap<string, object>
    {
        private readonly IDictionary<string, object> _dict;

        public Values(IEnumerable<KeyValuePair<string, IOption<object>>> mappings)
        {
            _dict = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var m in mappings)
            {
                if (m.Value.IsSome)
                    _dict.Add(m.Key, m.Value.Value);
            }
        }

        public Values(IEnumerable<IPair<string, object>> mappings)
        {
            _dict = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var m in mappings)
            {
                _dict.Add(m.Item1, m.Item2);
            }
        }
        public Values(IDictionary<string, object> dict)
        {
            _dict = dict;
        }

        public static IMap<string, object> Empty => new Values(Enumerable.Empty<KeyValuePair<string, IOption<object>>>());
        public IOption<object> this[string name] => _dict.TryGetValue(name, out var value) ? value.Some() : Option<object>.None();
    }
}