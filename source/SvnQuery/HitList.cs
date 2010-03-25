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
        internal HitList(Hits hits)
        {}

        public IEnumerator<Hit> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public Hit this[int index]
        {
            get { throw new NotImplementedException(); }
        }
    }

    //public HitViewModel this[int i]
    // {
    //     get { return new HitViewModel(_luceneHits.Doc(i)); }
    // }

    // public IEnumerator<HitViewModel> GetEnumerator()
    // {
    //     for (int i = 0; i < HitCount; ++i)
    //     {
    //         yield return this[i];
    //     }
    // }
}