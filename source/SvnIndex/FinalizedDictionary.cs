using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SvnQuery
{
    /// <summary>
    /// Threadsafe lookup and updates of highest finalization revision
    /// </summary>
    public class FinalizedDictionary
    {
        readonly Dictionary<string, int> finalized = new Dictionary<string, int>();

        public bool IsFinalized(string path, int revision)
        {
            lock (finalized)
            {
                int rev;
                finalized.TryGetValue(path, out rev);
                return revision <= rev;
            }
        }

        public void Finalize(string path, int revision)
        {
            lock (finalized)
            {
                int rev;
                finalized.TryGetValue(path, out rev);
                if (revision > rev) finalized[path] = rev;
            }
        }

    }
}
