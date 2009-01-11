#region Apache License 2.0

// Copyright 2008-2009 Christian Rodemeyer
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System.Collections.Generic;

namespace SvnQuery
{
    /// <summary>
    /// Threadsafe lookup and updates of highest finalization revision
    /// </summary>
    public class FinalizedMemory
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