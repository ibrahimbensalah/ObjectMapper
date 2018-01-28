using System;
using System.Collections.Generic;

namespace Xania.ObjectMapper
{
    public interface IMap<TSource, TTarget>: IEnumerable<KeyValuePair<TSource, TTarget>>
    {
        bool TryGetValue(TSource name, out TTarget value);
    }
}