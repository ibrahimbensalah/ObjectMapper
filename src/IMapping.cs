using System;
using System.Collections.Generic;

namespace Xania.ObjectMapper
{
    public interface IMap<in TSource, TTarget>: IEnumerable<KeyValuePair<string, object>>
    {
        bool TryGetValue(TSource name, out TTarget value);
    }
}