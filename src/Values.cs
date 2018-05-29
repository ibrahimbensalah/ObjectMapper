using System;
using System.Collections.Generic;
using System.Linq;

namespace Xania.ObjectMapper
{
    public class Mappings : IMap<string, object>
    {
        private readonly IDictionary<string, object> _dict;

        public Mappings(IEnumerable<IPair<string, IOption<object>>> mappings)
        {
            _dict = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var m in mappings)
            {
                if (m.Item2.IsSome)
                    _dict.Add(m.Item1, m.Item2.Value);
            }
        }

        public Mappings(IEnumerable<IPair<string, object>> mappings)
        {
            _dict = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var m in mappings)
            {
                _dict.Add(m.Item1, m.Item2);
            }
        }
        public Mappings(IDictionary<string, object> dict)
        {
            _dict = dict;
        }

        public static IMap<string, object> Empty => new Mappings(Enumerable.Empty<IPair<string, IOption<object>>>());
        public IOption<object> this[string name] => _dict.TryGetValue(name, out var value) ? value.Some() : Option<object>.None();
    }
}