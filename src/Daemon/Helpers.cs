using System.Collections.Generic;
using System.Dynamic;

namespace Idb.Sec.Convergence.Daemon
{
    public static class Helpers
    {
        public static bool DoesPropertyExist(dynamic settings, string name)
        {
            var o = settings as IDictionary<string, object>;
            if (o != null)
                return settings.ContainsKey(name);

            return settings.GetType().GetProperty(name) != null;
        }
    }
}