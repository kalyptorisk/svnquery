#region Apache License 2.0

// Copyright 2008-2010 Christian Rodemeyer
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Search;

namespace SvnQuery
{
    public class HitList : IEnumerable<Hit>
    {
        readonly Hits _hits;

        internal HitList(Hits hits)
        {
            _hits = hits;
        }

        public IEnumerator<Hit> GetEnumerator()
        {
           for (int i = 0; i < Count; ++i)
           {
                  yield return this[i];
           }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get { return _hits.Length(); }
        }

        public Hit this[int index]
        {
            get { return new Hit(_hits.Doc(index)); }
        }
    }

}