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

using System.Diagnostics;
using Lucene.Net.Search;

namespace App_Code
{
    /// <summary>
    /// Summary description for QueryResult
    /// </summary>
    public class QueryResult
    {
        readonly Hits _luceneHits;
        readonly int _searchTime;
        readonly int _indexRevision;
        readonly int _searchCcount;

        public QueryResult(Stopwatch sw, int indexRevision, int searchCount, Hits hits)
        {
            _searchTime = (int) sw.ElapsedMilliseconds + 1;
            _indexRevision = indexRevision;
            _searchCcount = searchCount;
            _luceneHits = hits;
        }

        public int SearchCount
        {
            get { return _searchCcount; }
        }

        public int HitCount
        {
            get { return _luceneHits.Length(); }
        }

        public Hit this[int i]
        {
            get { return new Hit(_luceneHits.Doc(i)); }
        }

        public int SearchTime
        {
            get { return _searchTime; }
        }

        public int IndexRevision
        {
            get { return _indexRevision; }
        }
    }
}