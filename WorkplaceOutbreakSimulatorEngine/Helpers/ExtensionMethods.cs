using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Helpers
{
    public static class ExtensionMethods
    {
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> adds)
        {
            if (adds == null)
            {
                return;
            }
            foreach (var item in adds)
            {
                collection.Add(item);
            }
        }
    }
}