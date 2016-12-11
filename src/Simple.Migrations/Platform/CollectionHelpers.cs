using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SimpleMigrations.Platform
{
    internal static class CollectionHelpers
    {
#if NETSTANDARD12
        public static ReadOnlyCollection<T> AsReadOnly<T>(this IList<T> list)
        {
            return new ReadOnlyCollection<T>(list);
        }
#endif
    }
}
