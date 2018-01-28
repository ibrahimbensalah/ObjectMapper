using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xania.ObjectMapper
{
    public class Values : IMap<string, object>
    {
        private readonly IDictionary<string, object> _dict;

        public Values(IEnumerable<KeyValuePair<string, object>> mappings)
        {
            _dict = new Dictionary<string, object>();
            foreach (var m in mappings)
            {
                _dict.Add(m.Key, m.Value);
            }
        }

        public Values(IDictionary<string, object> dict)
        {
            _dict = dict;
        }

        public static IMap<string, object> Empty => new Values(Enumerable.Empty<KeyValuePair<string, object>>());

        public bool TryGetValue(string name, out object value)
        {
            return _dict.TryGetValue(name, out value);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}